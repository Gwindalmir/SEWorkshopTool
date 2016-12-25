using Sandbox;
using Sandbox.Engine.Utils;
using SpaceEngineers.Game;
using System;
using System.Reflection;
using VRage.FileSystem;
using VRage.Utils;
using Sandbox.Engine.Platform;
using Sandbox.Engine.Networking;
using SteamSDK;
using System.IO;
using ParallelTasks;
using Sandbox.Game;
using System.Windows.Forms;
using System.Threading;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Sandbox.Game.World;
using VRage.Game;
using VRage.Scripting;
using System.Collections.Generic;
using System.Linq;

namespace SEWorkshopTool
{
    class Program
    {
        private static MySandboxGame m_spacegame = null;
        private static MyCommonProgramStartup m_startup;
        private static Sandbox.MySteamService m_steamService;

        const uint AppId_SE = 244850;      // MUST MATCH SE
        const uint AppId_ME = 333950;      // TODO

        static int Main(string[] args)
        {
            var options = new Options();
            var parser = new CommandLine.Parser(with => with.HelpWriter = Console.Error);

            if (parser.ParseArgumentsStrict(args, options, () => Environment.Exit(1)))
            {
                // Steam API doesn't initialize correctly if it can't find steam_appid.txt
                if (!File.Exists("steam_appid.txt"))
                    Directory.SetCurrentDirectory(Path.GetDirectoryName(typeof(VRage.FastResourceLock).Assembly.Location) + "\\..");

                if (options.ModPaths == null &&
                    options.Blueprints == null &&
                    options.IngameScripts == null &&
                    options.Scenarios == null &&
                    options.Worlds == null)
                {
                    System.Console.WriteLine(CommandLine.Text.HelpText.AutoBuild(options).ToString());
                    return 1;
                }

                try
                {
                    // Initialize game code
                    InitSandbox(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpaceEngineers"));
                }
                catch(Exception ex)
                {
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("An exception occurred intializing game libraries: {0}", ex.Message));
                    MySandboxGame.Log.WriteLineAndConsole(ex.StackTrace);
                    return 2;
                }

                if (MySteam.API == null)
                {
                    MySandboxGame.Log.WriteLineAndConsole("* Steam not detected. Is Steam UAC elevated? *");
                    MySandboxGame.Log.WriteLineAndConsole("* Only compile testing is available. *");
                    MySandboxGame.Log.WriteLineAndConsole("");

                    if (options.Download)
                        return 3;

                    options.Upload = false;
                }

                MySandboxGame.Log.WriteLineAndConsole(string.Format("SEWT {0}", Assembly.GetExecutingAssembly().GetName().Version));

                ParameterInfo[] parameters;
                if (options.Compile)
                {
                    // Init ModAPI
                    var initmethod = typeof(MySandboxGame).GetMethod("InitModAPI", BindingFlags.Instance | BindingFlags.NonPublic);
                    MyDebug.AssertDebug(initmethod != null);

                    if (initmethod != null)
                    {
                        parameters = initmethod.GetParameters();
                        MyDebug.AssertDebug(parameters.Count() == 0);

                        if(!(parameters.Count() == 0))
                            initmethod = null;
                    }

                    if (initmethod != null)
                        initmethod.Invoke(m_spacegame, null);
                    else
                        MySandboxGame.Log.WriteLineAndConsole(string.Format(Constants.ERROR_Reflection, "InitModAPI"));
                }

                // Keen's code for WriteAndShareFileBlocking has a UI dependency
                // This method need to be replaced with a custom one, which removes the unnecessary UI code.
                var methodtoreplace = typeof(MySteamWorkshop).GetMethod("WriteAndShareFileBlocking", BindingFlags.Static | BindingFlags.NonPublic);
                var methodtoinject = typeof(InjectedMethod).GetMethod("WriteAndShareFileBlocking", BindingFlags.Static | BindingFlags.NonPublic);

                MyDebug.AssertDebug(methodtoreplace != null);
                if (methodtoreplace != null)
                {
                    parameters = methodtoreplace.GetParameters();
                    MyDebug.AssertDebug(parameters.Count() == 1);
                    MyDebug.AssertDebug(parameters[0].ParameterType == typeof(string));

                    if (!(parameters.Count() == 1 && parameters[0].ParameterType == typeof(string)))
                        methodtoreplace = null;
                }

                if (methodtoreplace != null && methodtoinject != null)
                    MethodUtil.ReplaceMethod(methodtoreplace, methodtoinject);
                else
                    MySandboxGame.Log.WriteLineAndConsole(string.Format(Constants.ERROR_Reflection, "WriteAndShareFileBlocking"));

                System.Threading.Tasks.Task Task;

                if (options.Download)
                    Task = DownloadMods(options);
                else
                    Task = UploadMods(options);

                try
                {
                    // Wait for file transfers to finish (separate thread)
                    while (!Task.Wait(500))
                    {
                        if (MySteam.API != null)
                            MySteam.API.RunCallbacks();
                    }
                }
                catch(AggregateException ex)
                {
                    MyDebug.AssertDebug(Task.IsFaulted);
                    MyDebug.AssertDebug(ex.InnerException != null);
                    var exception = ex.InnerException;
                    MySandboxGame.Log.WriteLineAndConsole("An exception occurred: " + exception.Message);
                    MySandboxGame.Log.WriteLineAndConsole(exception.StackTrace);
                }

                // Cleanup
                CleanupSandbox();
            }
            return 0;
        }

        #region Sandbox stuff
        private static void CleanupSandbox()
        {
            m_steamService.Dispose();
            m_spacegame.Dispose();
            m_steamService = null;
            m_spacegame = null;
        }

        // This is mostly copied from MyProgram.Main(), with UI stripped out.
        private static void InitSandbox(string instancepath)
        {
            if (m_spacegame != null)
                m_spacegame.Exit();

            SpaceEngineersGame.SetupBasicGameInfo();
            m_startup = new MyCommonProgramStartup(new string[] { });

            var appDataPath = m_startup.GetAppDataPath();
            MyInitializer.InvokeBeforeRun(AppId_SE, MyPerGameSettings.BasicGameInfo.ApplicationName + "ModTool", appDataPath);
            MyInitializer.InitCheckSum();

            if (!m_startup.Check64Bit()) return;

            m_steamService = new MySteamService(MySandboxGame.IsDedicated, AppId_SE);
            SpaceEngineersGame.SetupPerGameSettings();


            if (System.Diagnostics.Debugger.IsAttached)
                m_startup.CheckSteamRunning(m_steamService);        // Just give the warning message box when debugging, ignore for release

            VRageGameServices services = new VRageGameServices(m_steamService);

            if (!MySandboxGame.IsDedicated)
                MyFileSystem.InitUserSpecific(m_steamService.UserId.ToString());

            try
            {
                // NOTE: an assert may be thrown in debug, about missing Tutorials.sbx. Ignore it.
                m_spacegame = new SpaceEngineersGame(services, null);

                // Initializing the workshop means the categories are available
                var initWorkshopMethod = typeof(SpaceEngineersGame).GetMethod("InitSteamWorkshop", BindingFlags.NonPublic | BindingFlags.Instance);
                MyDebug.AssertDebug(initWorkshopMethod != null);

                if (initWorkshopMethod != null)
                {
                    var parameters = initWorkshopMethod.GetParameters();
                    MyDebug.AssertDebug(parameters.Count() == 0);
                }

                if (initWorkshopMethod != null)
                    initWorkshopMethod.Invoke(m_spacegame, null);
                else
                    MySandboxGame.Log.WriteLineAndConsole(string.Format(Constants.ERROR_Reflection, "InitSteamWorkshop"));
            }
            catch(Exception ex)
            {
                // This shouldn't fail, but don't stop even if it does
                MySandboxGame.Log.WriteLineAndConsole("An exception occured, ignoring: " + ex.Message);
            }
        }
        #endregion

        static System.Threading.Tasks.Task UploadMods(Options options)
        {
            MySandboxGame.Log.WriteLineAndConsole(string.Empty);

            var Task = System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                MySandboxGame.Log.WriteLineAndConsole("Beginning batch workshop upload...");
                MySandboxGame.Log.WriteLineAndConsole(string.Empty);
                List<string> itemPaths;

                // Process mods
                itemPaths = GetGlobbedPaths(TestPathAndMakeAbsolute(WorkshopType.Mod, options.ModPaths));
                ProcessItemsUpload(WorkshopType.Mod, itemPaths, options);

                // Process blueprints
                itemPaths = GetGlobbedPaths(TestPathAndMakeAbsolute(WorkshopType.Blueprint, options.Blueprints));
                ProcessItemsUpload(WorkshopType.Blueprint, itemPaths, options);

                // Process ingame scripts
                itemPaths = GetGlobbedPaths(TestPathAndMakeAbsolute(WorkshopType.IngameScript, options.IngameScripts));
                ProcessItemsUpload(WorkshopType.IngameScript, itemPaths, options);

                // Process worlds
                itemPaths = GetGlobbedPaths(TestPathAndMakeAbsolute(WorkshopType.World, options.Worlds));
                ProcessItemsUpload(WorkshopType.World, itemPaths, options);

                // Process scenarios
                itemPaths = GetGlobbedPaths(TestPathAndMakeAbsolute(WorkshopType.Scenario, options.Scenarios));
                ProcessItemsUpload(WorkshopType.Scenario, itemPaths, options);

                MySandboxGame.Log.WriteLineAndConsole("Batch workshop upload complete!");
            });

            return Task;
        }

        static string[] TestPathAndMakeAbsolute(WorkshopType type, string[] paths)
        {
            for (int idx = 0; paths != null && idx < paths.Length; idx++)
            {
                // If the passed in path doesn't exist, and is relative, try to match it with the expected data directory
                if (!Directory.Exists(paths[idx]) && !Path.IsPathRooted(paths[idx]))
                    paths[idx] = Path.Combine(WorkshopHelper.GetWorkshopItemPath(type), paths[idx]);
            }
            return paths;
        }

        /// <summary>
        /// Processes list of files, and returns a glob expanded list.
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        static List<string> GetGlobbedPaths(string[] paths)
        {
            List<string> itemPaths = new List<string>();

            if (paths == null)
                return itemPaths;

            foreach (var path in paths)
            {
                var dirs = Directory.EnumerateDirectories(Path.GetDirectoryName(path), Path.GetFileName(path));
                
                itemPaths.AddList(dirs
                    .Where(i => !(Path.GetFileName(i).StartsWith(".") ||                // Ignore directories starting with "." (eg. ".vs")
                                Path.GetFileName(i).StartsWith(Constants.SEWT_Prefix))) // also ignore directories starting with "[_SEWT_]" (downloaded by this mod)
                            .Select(i => i).ToList());
            }
            return itemPaths;
        }

        static void ProcessItemsUpload(WorkshopType type, List<string> paths, Options options)
        {
            for (int idx = 0; idx < paths.Count; idx++)
            {
                var mod = new Uploader(type, Path.GetFullPath(paths[idx]), options.Tags, options.ExcludeExtensions, options.Compile, options.DryRun, options.Development, options.Visibility, options.Force);
                if (options.UpdateOnly && mod.ModId == 0)
                {
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("--update-only passed, skipping: {0}", mod.Title));
                    continue;
                }
                MySandboxGame.Log.WriteLineAndConsole(string.Format("Processing {0}: {1}", type.ToString(), mod.Title));

                if (mod.Compile())
                {
                    if (options.Upload)
                    {
                        if (mod.Publish())
                            MySandboxGame.Log.WriteLineAndConsole(string.Format("Complete: {0}", mod.Title));
                    }
                    else
                    {
                        MySandboxGame.Log.WriteLineAndConsole(string.Format("Not uploading: {0}", mod.Title));
                        MySandboxGame.Log.WriteLineAndConsole(string.Format("Complete: {0}", mod.Title));
                    }
                }
                else
                {
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("Skipping {0}: {1}", type.ToString(), mod.Title));
                }
                MySandboxGame.Log.WriteLineAndConsole(string.Empty);
            }
        }

        static System.Threading.Tasks.Task DownloadMods(Options options)
        {
            // Get PublishItemBlocking internal method via reflection
            MySandboxGame.Log.WriteLineAndConsole(string.Empty);

            var Task = System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                MySandboxGame.Log.WriteLineAndConsole("Beginning batch workshop download...");
                MySandboxGame.Log.WriteLineAndConsole(string.Empty);

                ProcessItemsDownload(WorkshopType.Mod, options.ModPaths, options);
                ProcessItemsDownload(WorkshopType.Blueprint, options.Blueprints, options);
                ProcessItemsDownload(WorkshopType.IngameScript, options.IngameScripts, options);
                ProcessItemsDownload(WorkshopType.World, options.Worlds, options);
                ProcessItemsDownload(WorkshopType.Scenario, options.Scenarios, options);

                MySandboxGame.Log.WriteLineAndConsole("Batch workshop download complete!");
            });

            return Task;
        }

        static void ProcessItemsDownload(WorkshopType type, string[] paths, Options options)
        {
            if (paths == null)
                return;

            var items = new List<MySteamWorkshop.SubscribedItem>();
            var modids = paths.Select(ulong.Parse);

            MySandboxGame.Log.WriteLineAndConsole(string.Format("Processing {0}s...", type.ToString()));

            var downloadPath = WorkshopHelper.GetWorkshopItemPath(type);

            if (MySteamWorkshop.GetItemsBlocking(items, modids))
            {
                bool success = false;
                if (type == WorkshopType.Mod)
                {
                    var result = MySteamWorkshop.DownloadModsBlocking(items);
                    success = result.Success;
                }
                else
                {
                    if (type == WorkshopType.Blueprint)
                    {
                        success = MySteamWorkshop.DownloadBlueprintsBlocking(items);
                    }
                    else if (type == WorkshopType.IngameScript)
                    {
                        var loopsuccess = false;
                        foreach (var item in items)
                        {
                            loopsuccess = MySteamWorkshop.DownloadScriptBlocking(item);
                            if (!loopsuccess)
                                MySandboxGame.Log.WriteLineAndConsole(string.Format("Download of {0} FAILED!", item.PublishedFileId));
                            else
                                success = true;
                        }
                    }
                    else if(type == WorkshopType.World || type == WorkshopType.Scenario)
                    {
                        var loopsuccess = false;
                        string path;
                        MySteamWorkshop.MyWorkshopPathInfo pathinfo = type == WorkshopType.World ? 
                                                                MySteamWorkshop.MyWorkshopPathInfo.CreateWorldInfo() : 
                                                                MySteamWorkshop.MyWorkshopPathInfo.CreateScenarioInfo();

                        foreach (var item in items)
                        {
                            // This downloads and extracts automatically, no control over it
                            loopsuccess = MySteamWorkshop.TryCreateWorldInstanceBlocking(item, pathinfo, out path, false);
                            if (!loopsuccess)
                            {
                                MySandboxGame.Log.WriteLineAndConsole(string.Format("Download of {0} FAILED!", item.PublishedFileId));
                            }
                            else
                            {
                                MySandboxGame.Log.WriteLineAndConsole(string.Format("Downloaded '{0}' to {1}", item.Title, path));
                                success = true;
                            }
                        }
                    }
                    else
                    {
                        throw new NotSupportedException(string.Format("Downloading of {0} not yet supported.", type.ToString()));
                    }
                }

                if (success)
                {
                    MySandboxGame.Log.WriteLineAndConsole("Download success!");
                }
                else
                {
                    MySandboxGame.Log.WriteLineAndConsole("Download FAILED!");
                    return;
                }

                foreach (var item in items)
                {
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("{0} '{1}' tags: {2}", item.PublishedFileId, item.Title, string.Join(", ", item.Tags)));
                    if (options.Extract)
                    {
                        var mod = new Downloader(downloadPath, item.PublishedFileId, item.Title, item.Tags);
                        mod.Extract();
                    }
                    MySandboxGame.Log.WriteLineAndConsole(string.Empty);
                }
            }
        }
    }
}
