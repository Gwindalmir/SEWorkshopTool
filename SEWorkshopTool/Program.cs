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

        static void Main(string[] args)
        {
            var options = new Options();
            var parser = new CommandLine.Parser(with => with.HelpWriter = Console.Error);

            if (parser.ParseArgumentsStrict(args, options, () => Environment.Exit(-2)))
            {
                // Steam API doesn't initialize correctly if it can't find steam_appid.txt
                if (!File.Exists("steam_appid.txt"))
                    Directory.SetCurrentDirectory(Path.GetDirectoryName(typeof(VRage.FastResourceLock).Assembly.Location) + "\\..");

                // Initialize game code
                InitSandbox(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpaceEngineers"));

                if (options.Compile)
                {
                    // Init ModAPI
                    var initmethod = typeof(MySandboxGame).GetMethod("InitModAPI", BindingFlags.Instance | BindingFlags.NonPublic);

                    if (initmethod != null)
                        initmethod.Invoke(m_spacegame, null);
                }

                // Keen's code for WriteAndShareFileBlocking has a UI dependency
                // This method need to be replaced with a custom one, which removes the unnecessary UI code.
                var methodtoreplace = typeof(MySteamWorkshop).GetMethod("WriteAndShareFileBlocking", BindingFlags.Static | BindingFlags.NonPublic);
                var methodtoinject = typeof(InjectedMethod).GetMethod("WriteAndShareFileBlocking", BindingFlags.Static | BindingFlags.NonPublic);

                if (methodtoreplace != null && methodtoinject != null)
                    MethodUtil.ReplaceMethod(methodtoreplace, methodtoinject);

                System.Threading.Tasks.Task Task;

                if (options.Download)
                    Task = DownloadMods(options);
                else
                    Task = UploadMods(options);

                // Wait for file transfers to finish (separate thread)
                while (!Task.Wait(500))
                {
                    if (MySteam.API != null)
                        MySteam.API.RunCallbacks();
                }

                // Cleanup
                CleanupSandbox();
            }
        }

        #region Sandbox stuff
        private static void CleanupSandbox()
        {
            VRage.Plugins.MyPlugins.Unload();   // Prevents assert in debug
            m_steamService.Dispose();
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

            if (!m_startup.CheckSteamRunning(m_steamService)) return;

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
                if( initWorkshopMethod != null)
                    initWorkshopMethod.Invoke(m_spacegame, null);
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
                itemPaths = GetGlobbedPaths(options.ModPaths);
                ProcessItemsUpload(WorkshopType.mod, itemPaths, options);

                // Process blueprints
                itemPaths = GetGlobbedPaths(options.Blueprints);
                ProcessItemsUpload(WorkshopType.blueprint, itemPaths, options);

                // Process ingame scripts
                itemPaths = GetGlobbedPaths(options.IngameScripts);
                ProcessItemsUpload(WorkshopType.ingameScript, itemPaths, options);

                // Process worlds
                itemPaths = GetGlobbedPaths(options.Worlds);
                ProcessItemsUpload(WorkshopType.world, itemPaths, options);

                // Process scenarios
                itemPaths = GetGlobbedPaths(options.Scenarios);
                ProcessItemsUpload(WorkshopType.scenario, itemPaths, options);

                MySandboxGame.Log.WriteLineAndConsole("Batch workshop upload complete!");
            });

            return Task;
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
                var mod = new Uploader(type, Path.GetFullPath(paths[idx]), options.Tags, options.Compile, options.DryRun, options.Development, options.Visibility, options.Force);
                if (options.UpdateOnly && mod.ModId == 0)
                {
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("--update-only passed, skipping: {0}", mod.Title));
                    continue;
                }
                MySandboxGame.Log.WriteLineAndConsole(string.Format("Processing {0}: {1}", type.ToString(), mod.Title));

                if (mod.Compile())
                {
                    if (mod.Publish())
                        MySandboxGame.Log.WriteLineAndConsole(string.Format("Complete: {0}", mod.Title));
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
            MySandboxGame.Log.WriteLineAndConsole("Beginning batch mod download...");
            MySandboxGame.Log.WriteLineAndConsole(string.Empty);

            var Task = System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                var items = new List<MySteamWorkshop.SubscribedItem>();
                var modids = options.ModPaths.Select(ulong.Parse);

                if (MySteamWorkshop.GetItemsBlocking(items, modids))
                {
                    var result = MySteamWorkshop.DownloadModsBlocking(items);
                    if( result.Success )
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
                        MySandboxGame.Log.WriteLineAndConsole(string.Format("Mod '{0}' tags: ", item.PublishedFileId, item.Tags));
                        if (options.Extract)
                        {
                            var mod = new Downloader(MyFileSystem.ModsPath, item.PublishedFileId, item.Title, item.Tags);
                            mod.Extract();
                        }
                        MySandboxGame.Log.WriteLineAndConsole(string.Empty);
                    }
                }
                MySandboxGame.Log.WriteLineAndConsole("Batch mod download complete!");
            });

            return Task;
        }
    }
}
