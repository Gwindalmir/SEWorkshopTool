using Sandbox;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Steamworks;
using VRageRender;
using VRage.FileSystem;
using VRage.Utils;
using VRage.GameServices;
using System.Diagnostics;
using VRage;
using CommandLine;
using Phoenix.WorkshopTool.Options;
using CommandLine.Text;
using Phoenix.WorkshopTool.Extensions;
using System.Text;
#if SE
using ParallelTasks;
using MyDebug = Phoenix.WorkshopTool.Extensions.MyDebug;
#else
using VRage.Library.Threading;
#endif

namespace Phoenix.WorkshopTool
{
    public abstract class GameBase
    {
        protected static readonly string LaunchDirectory = Environment.CurrentDirectory;
        protected MySandboxGame m_game = null;
        protected MyCommonProgramStartup m_startup;
        protected IMyGameService m_steamService;
        protected static readonly uint AppId = 244850;
        protected static readonly string AppName = "SEWT";
        protected static readonly bool IsME = false;
        protected string[] m_args;
        protected bool m_useModIO = false;

        static GameBase()
        {
            // Make sure the current directory is where the game files are
            Directory.SetCurrentDirectory(Path.GetDirectoryName(typeof(FastResourceLock).Assembly.Location));
            var root = Path.Combine(Path.GetDirectoryName(typeof(FastResourceLock).Assembly.Location), "..");
#if SE
            // Steam API doesn't initialize correctly if it can't find steam_appid.txt
            if (!File.Exists("steam_appid.txt"))
                Directory.SetCurrentDirectory(root);
#else
            AppName = "MEWT";
            IsME = true;
#endif
            // If the file can't be found, assume it's SE
            if (File.Exists("steam_appid.txt"))
            {
                var appid = File.ReadAllText(Path.Combine(root, "steam_appid.txt"));
                AppId = uint.Parse(appid);
            }

            // Override the ExePath, so the game classes can initialize when the exe is outside the game directory
            MyFileSystem.ExePath = new FileInfo(Assembly.GetAssembly(typeof(FastResourceLock)).Location).DirectoryName;
        }

        public GameBase()
        {
            ReflectionHelper.ReplaceMethod(ReflectionHelper.ReflectSteamRestartApp(), typeof(InjectedMethod), nameof(InjectedMethod.RestartAppIfNecessary), BindingFlags.Static | BindingFlags.Public);

            Type[] copyAllBaseTypes = { typeof(string), typeof(string) };
            ReflectionHelper.ReplaceMethod(ReflectionHelper.ReflectFileCopy(copyAllBaseTypes), typeof(GameBase), nameof(GameBase.CopyAll), BindingFlags.Static | BindingFlags.Public, types: copyAllBaseTypes);

            Type[] copyAllConditionalTypes = { typeof(string), typeof(string), typeof(Predicate<string>) };
            ReflectionHelper.ReplaceMethod(ReflectionHelper.ReflectFileCopy(copyAllConditionalTypes), typeof(GameBase), nameof(GameBase.CopyAllConditional), BindingFlags.Static | BindingFlags.Public, types: copyAllConditionalTypes);
        }

        // Event handler for loading assemblies not in the same directory as the exe.
        // This assumes the current directory contains the assemblies.
        public static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return Assembly.LoadFrom(AssemblyResolver(sender, args, ".dll"));
        }

        public static Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            return Assembly.ReflectionOnlyLoadFrom(AssemblyResolver(sender, args, ".exe"));
        }

        private static string AssemblyResolver(object sender, ResolveEventArgs args, string ext)
        {
            var assemblyname = new AssemblyName(args.Name).Name;
            var assemblyPath = ResolveFromRoot(assemblyname, ext, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            if (!File.Exists(assemblyPath))
                assemblyPath = ResolveFromRoot(assemblyname, ext, Environment.CurrentDirectory);

            return assemblyPath;
        }

        private static string ResolveFromRoot(string assemblyname, string ext, string root)
        {
            var assemblyPath = Path.Combine(root, assemblyname + ext);

            if (!File.Exists(assemblyPath))
                assemblyPath = Path.Combine(root, "Bin64", assemblyname + ext);

            if (!File.Exists(assemblyPath))
                assemblyPath = Path.Combine(root, "Bin64", "x64", assemblyname + ext);

            var sublength = assemblyname.LastIndexOf('.');

            if (sublength == -1)
                sublength = assemblyname.Length;

            if (!File.Exists(assemblyPath))
                assemblyPath = Path.Combine(root, assemblyname.Substring(0, sublength) + ext);

            if (!File.Exists(assemblyPath))
                assemblyPath = Path.Combine(root, "Bin64", assemblyname.Substring(0, sublength) + ext);

            return assemblyPath;
        }

        public virtual int InitGame(string[] args)
        {
            ProcessedOptions options = default(ProcessedOptions);
            var parser = new CommandLine.Parser(with => with.HelpWriter = null);

            var result = parser.ParseArguments<DownloadVerb, UploadVerb, PublishVerb, ChangeVerb, CompileVerb, CloudVerb>(args)
                .WithParsed(o => options = (ProcessedOptions)(dynamic)o)
                .WithNotParsed(l =>
                {
                    parser.ParseArguments<LegacyOptions>(args)
                        .WithParsed(o =>
                        {
                            options = o;

                            if (options.ListDLCs)
                                return;

                            ProgramBase.ConsoleWriteColored(ConsoleColor.Yellow, () => Console.Error.WriteLine("You are using the legacy command-line arguments. These will be removed after v0.8!"));

                            string newargs = null;
                            if (options.Upload)
                                newargs = parser.FormatCommandLine((UploadVerb)options, s=> s.SkipDefault = true);
                            else if (options.Download)
                                newargs = parser.FormatCommandLine((DownloadVerb)options, s => s.SkipDefault = true);
                            else if (options.Type == typeof(ChangeVerb))
                                newargs = parser.FormatCommandLine((ChangeVerb)options, s => s.SkipDefault = true);
                            else if (options.Type == typeof(CloudVerb))
                                newargs = parser.FormatCommandLine((CloudVerb)options, s => s.SkipDefault = true);

                            if (newargs != null)
                                ProgramBase.ConsoleWriteColored(ConsoleColor.Yellow, () => Console.Error.WriteLine($"Use this instead: {Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)} {newargs}"));
                        })
                    ;
                });

            if (options == default(ProcessedOptions))
            {
                var helptext = HelpText.AutoBuild(result, h =>
                {
                    //h.OptionComparison = HelpText.RequiredThenAlphaComparison;
                    h.AddEnumValuesToHelpText = true;

                    if (Console.Out.IsInteractive() || Console.Error.IsInteractive())
                        h.MaximumDisplayWidth = Console.WindowWidth;

                    return h;
                });
                
                // Print the help text in yellow if it's not a user requested help prompt
                result.WithNotParsed(e =>
                {
                    if(e.IsHelp() || e.IsVersion())
                        Console.WriteLine(helptext.ToString());
                    else
                        ProgramBase.ConsoleWriteColored(ConsoleColor.Yellow, () =>
                            Console.Error.WriteLine(helptext.ToString()));
                });
                return Cleanup(1);
            }
            else
            {
                if (options.Ids == null && 
                    options.Mods == null &&
                    options.Blueprints == null &&
#if SE
                    options.IngameScripts == null &&
#endif
                    options.Scenarios == null &&
                    options.Worlds == null &&
                    options.Collections == null)
                {
                    if (!options.Clear && !options.ListCloud && !options.ListDLCs)
                    {
                        ProgramBase.ConsoleWriteColored(ConsoleColor.Yellow, () =>
                                Console.WriteLine(HelpText.AutoBuild(result, null, null).ToString()));
                        return Cleanup(1);
                    }
                }
                
                // SE requires -appdata, but the commandline dll requires --appdata, so fix it
                for (var idx = 0; idx < args.Length; idx++)
                    if (string.Compare(args[idx], "--appdata", StringComparison.InvariantCultureIgnoreCase) == 0)
                        args[idx] = "-appdata";

                m_useModIO = options.ModIO;
                try
                {
                    // Initialize game code
                    InitSandbox(args);
                }
                catch (Exception ex)
                {
                    ProgramBase.CheckForUpdate();
                    ex.Log("ERROR: An exception occurred intializing game libraries: ");
                    return Cleanup(2);
                }

                if (!SteamAPI.IsSteamRunning())
                {
                    MySandboxGame.Log.WriteLineWarning("ERROR: * Steam not detected. Is Steam running and not as Admin? *");
                    MySandboxGame.Log.WriteLineWarning("* Only compile testing is available. *");
                    MySandboxGame.Log.WriteLineAndConsole("");

                    if (options.Download || (options.Upload && !options.Compile))
                        return Cleanup(3);
                }

                MySandboxGame.Log.WriteLineAndConsole($"{AppName} {Assembly.GetExecutingAssembly().GetName().Version}");

                ProgramBase.CheckForUpdate(MySandboxGame.Log.WriteLineAndConsole);

                MySandboxGame.Log.WriteLineToConsole(string.Empty);
                ProgramBase.ConsoleWriteColored(ConsoleColor.White, () =>
                    MySandboxGame.Log.WriteLineAndConsole($"Log file: {MySandboxGame.Log.GetFilePath()}"));
                MySandboxGame.Log.WriteLineToConsole(string.Empty);

                // Make sure file paths are properly rooted based on the user's current directory at launch
                MySandboxGame.Log.WriteLineAndConsole($"Relative root: {LaunchDirectory}");

#if SE
                if (options.Compile)
                {
                    var initmethod = ReflectionHelper.ReflectInitModAPI();

                    if (initmethod != null)
                        initmethod.Invoke(m_game, null);
                    else
                        MySandboxGame.Log.WriteLineError(string.Format(Constants.ERROR_Reflection, "InitModAPI"));
                }
#endif
                ReplaceMethods();

                System.Threading.Tasks.Task<bool> Task;

                if (options.Download)
                    Task = DownloadMods(options);
                else if (options.Clear || options.ListCloud)
                    Task = ClearSteamCloud(options.Files.ToArray(), options.Force);
                else if (options.ListDLCs)
                    Task = System.Threading.Tasks.Task<bool>.Factory.StartNew(()=> { ListDLCs(); return true; });
                else
                    Task = UploadMods(options);

                try
                {
                    // Wait for file transfers to finish (separate thread)
                    while (!Task.Wait(100))
                    {
                        MyGameService.Update();
                    }
                }
                catch(AggregateException ex)
                {
                    MyDebug.AssertRelease(Task.IsFaulted);
                    MyDebug.AssertRelease(ex.InnerException != null);
                    ex.InnerException.Log();
                    return Cleanup(4);
                }
                catch(Exception ex)
                {
                    ex.Log();
                    return Cleanup(5);
                }

                // If the task reported any error, return exit code
                if (!Task.Result)
                    return Cleanup(-1);
            }

            return Cleanup();
        }

        void ReplaceMethods()
        {
#if SE
            ReflectionHelper.ReplaceMethod(WorkshopHelper.ReflectSteamWorkshopItemPublisherMethod("UpdatePublishedItem"), typeof(InjectedMethod), "UpdatePublishedItem", BindingFlags.Instance | BindingFlags.NonPublic);
            ReflectionHelper.ReplaceMethod(WorkshopHelper.ReflectToService(), typeof(MySteamHelper), nameof(MySteamHelper.ToService), BindingFlags.Static | BindingFlags.Public);
            ReflectionHelper.ReplaceMethod(WorkshopHelper.ReflectToSteam(), typeof(MySteamHelper), nameof(MySteamHelper.ToSteam), BindingFlags.Static | BindingFlags.Public);
            ReflectionHelper.ReplaceMethod(WorkshopHelper.ReflectCreateRequest(), typeof(InjectedMethod), "CreateRequest", BindingFlags.Static | BindingFlags.NonPublic);

#else
            ReflectionHelper.ReplaceMethod(WorkshopHelper.ReflectSteamWorkshopItemPublisherMethod("UpdatePublishedItem", BindingFlags.Instance | BindingFlags.Public), typeof(InjectedMethod), nameof(InjectedMethod.UpdatePublishedItem), BindingFlags.Public | BindingFlags.Instance);
#endif
        }

        // Returns argument for chaining
        private int Cleanup(int errorCode = 0)
        {
            if (errorCode != 0)
                MySandboxGame.Log.WriteLineError("Check the log file above for error details.");

            CleanupSandbox();
            return errorCode;
        }

        #region Sandbox stuff
        private void CleanupSandbox()
        {
            try
            {
                m_steamService?.ShutDown();
                m_game?.Dispose();
                m_steamService = null;
                m_game = null;
            }
            catch(Exception ex)
            {
                // Don't spam console with annoying cleanup error
                MySandboxGame.Log.WriteLine(ex.Message);
                MySandboxGame.Log.WriteLine(ex.StackTrace);
            }
#if !SE
            VRage.Logging.MyLog.Default.Dispose();
#endif
        }

        protected abstract bool SetupBasicGameInfo();
        protected abstract MySandboxGame InitGame();

        // This is mostly copied from MyProgram.Main(), with UI stripped out.
        protected virtual void InitSandbox(string[] args)
        {
            m_args = args;
            // Infinario was removed from SE in update 1.184.6, but is still in ME
            var infinario = typeof(MyFakes).GetField("ENABLE_INFINARIO");

            if (infinario != null)
                infinario.SetValue(null, false);
            
            if (m_game != null)
                m_game.Exit();

            if (!SetupBasicGameInfo())
                return;

            // Init null render so profiler-enabled builds don't crash
            var render = new MyNullRender();
            MyRenderProxy.Initialize(render);
#if SE
            EmptyKeys.UserInterface.Engine engine = (EmptyKeys.UserInterface.Engine)new VRage.UserInterface.MyEngine();

            if (System.Diagnostics.Debugger.IsAttached)
                m_startup.CheckSteamRunning();        // Just give the warning message box when debugging, ignore for release

            if (!Sandbox.Engine.Platform.Game.IsDedicated)
                MyFileSystem.InitUserSpecific(m_steamService.UserId.ToString());
#endif

            try
            {
#if !SE
                MyRenderProxy.GetRenderProfiler().SetAutocommit(false);
                MyRenderProxy.GetRenderProfiler().InitMemoryHack("MainEntryPoint");
#endif
                // NOTE: an assert may be thrown in debug, about missing Tutorials.sbx. Ignore it.
                m_game = InitGame();

                // Initializing the workshop means the categories are available
                var initWorkshopMethod = WorkshopHelper.ReflectInitSteamWorkshop();
                MyDebug.AssertRelease(initWorkshopMethod != null);

                if (initWorkshopMethod != null)
                {
                    var parameters = initWorkshopMethod.GetParameters();
                    MyDebug.AssertRelease(parameters.Count() == 0);
                }

                if (initWorkshopMethod != null)
                    initWorkshopMethod.Invoke(m_game, null);
                else
                    MySandboxGame.Log.WriteLineError(string.Format(Constants.ERROR_Reflection, "InitSteamWorkshop"));
            }
            catch (Exception ex)
            {
                // This shouldn't fail, but don't stop even if it does
                ex.Log("WARNING: An exception occured, ignoring: ");
            }

            AuthenticateWorkshop();
        }
        #endregion Sandbox stuff

        #region Steam
        private System.Threading.Tasks.Task<bool> ClearSteamCloud(string [] filesToDelete, bool force = false)
        {
            var Task = System.Threading.Tasks.Task<bool>.Factory.StartNew(() =>
            {
                void WriteLine(string line)
                {
                    const int TIMESTAMP_LENGTH = 26;

                    // Check if the table is too large to fit with the logging timestamp
                    if (Console.Out.IsInteractive() && Console.WindowWidth < line.Length + TIMESTAMP_LENGTH)
                        Console.Out.WriteLine(line);
                    else
                        MySandboxGame.Log.WriteLineAndConsole(line);
                }

                ulong totalBytes = 0;
                ulong availableBytes = 0;
                
                MySteamService.Service.GetRemoteStorageQuota(out totalBytes, out availableBytes);
                MySandboxGame.Log.WriteLineAndConsole(string.Format("Quota: total = {0:N0} kiB, available = {1:N0} kiB", totalBytes / 1024, availableBytes / 1024));

                int totalCloudFiles = MySteamService.Service.GetRemoteStorageFileCount();
                var wantsToDelete = force || filesToDelete?.Length > 0;

                MySandboxGame.Log.WriteLineAndConsole(string.Format("Listing {0} cloud files", totalCloudFiles));
                MySandboxGame.Log.IncreaseIndent();

                var rows = new List<Tuple<string, int, bool, bool>>();
                for (int i = 0; i < totalCloudFiles; ++i)
                {
                    int fileSize = 0;
                    string fileName = MySteamService.Service.GetRemoteStorageFileNameAndSize(i, out fileSize);
                    bool persisted = MySteamService.Service.IsRemoteStorageFilePersisted(fileName);
                    bool forgot = false;

                    // Here's how the if works: 
                    // Delete if --force AND no files were manually specified
                    // OR if the file specified matches the file on the cloud
                    if ((force && filesToDelete == null) || (persisted && fileName.StartsWith("tmp") && fileName.EndsWith(".tmp")) ||
                        (filesToDelete?.Length > 0 && filesToDelete.Contains(fileName, StringComparer.CurrentCultureIgnoreCase))) // dont sync useless temp files
                    {
                        forgot = MySteamService.Service.RemoteStorageFileForget(fileName);

                        // force actually deletes the file on local disk, don't do that unless --force specified
                        if (force)
                        {
                            forgot = MySteamService.Service.DeleteFromCloud(fileName);
                            // Delete is immediate, and alters the count, so adjust for that
                            totalCloudFiles--;
                            i--;
                        }
                    }
                    rows.Add(new Tuple<string, int, bool, bool>(fileName, fileSize, persisted, forgot));
                }

                var forgotHeader = force ? "Deleted" : "Forgotten";
                var colWidths = new List<int>();
                colWidths.Add(Math.Max(rows.Select(r => r.Item1.Length).DefaultIfEmpty(0).Max() + 1, 10));
                colWidths.Add(10);
                colWidths.Add(8);
                colWidths.Add(forgotHeader.Length);

                var rowFormat = $"{{0,-{colWidths[0]}}}|{{1,{colWidths[1]}}}|{{2,{colWidths[2]}}}|{{3,{colWidths[3]}}}";
                WriteLine(string.Format(rowFormat, "Filename".PadRight(colWidths[0]), "Size (kiB)".PadRight(colWidths[1]), "In Cloud", forgotHeader));
                WriteLine(string.Format(rowFormat, new string('-', colWidths[0]), new string('-', colWidths[1]), new string('-', colWidths[2]), new string('-', colWidths[3])));

                foreach (var row in rows)
                    WriteLine(string.Format(rowFormat, row.Item1, (row.Item2 / 1024).ToString("##,#' '"), row.Item3.ToString().PadRight(6), (wantsToDelete ? row.Item4.ToString() : "N/A").PadRight(colWidths[3] - 2)));

                MySandboxGame.Log.DecreaseIndent();

                if (wantsToDelete)
                {
                    MySteamService.Service.GetRemoteStorageQuota(out totalBytes, out availableBytes);
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("Quota: total = {0:N0} kiB, available = {1:N0} kiB", totalBytes / 1024, availableBytes / 1024));
                }
                return true;
            });
            return Task;
        }
        #endregion Steam

        #region Upload
        static System.Threading.Tasks.Task<bool> UploadMods(ProcessedOptions options)
        {
            MySandboxGame.Log.WriteLineAndConsole(string.Empty);

            var Task = System.Threading.Tasks.Task<bool>.Factory.StartNew(() =>
            {
                bool success = true;
                MySandboxGame.Log.WriteLineAndConsole("Beginning batch workshop upload...");
                MySandboxGame.Log.WriteLineAndConsole(string.Empty);
                List<string> itemPaths;

                // Process mods
                itemPaths = GetGlobbedPaths(TestPathAndMakeAbsolute(WorkshopType.Mod, options.Mods));
                if (!ProcessItemsUpload(WorkshopType.Mod, itemPaths, options))
                    success = false;

                // Process blueprints
                itemPaths = GetGlobbedPaths(TestPathAndMakeAbsolute(WorkshopType.Blueprint, options.Blueprints));
                if (!ProcessItemsUpload(WorkshopType.Blueprint, itemPaths, options))
                    success = false;
#if SE
                // Process ingame scripts
                itemPaths = GetGlobbedPaths(TestPathAndMakeAbsolute(WorkshopType.IngameScript, options.IngameScripts));
                if (!ProcessItemsUpload(WorkshopType.IngameScript, itemPaths, options))
                    success = false;
#endif
                // Process worlds
                itemPaths = GetGlobbedPaths(TestPathAndMakeAbsolute(WorkshopType.World, options.Worlds));
                if (!ProcessItemsUpload(WorkshopType.World, itemPaths, options))
                    success = false;

                // Process scenarios
                itemPaths = GetGlobbedPaths(TestPathAndMakeAbsolute(WorkshopType.Scenario, options.Scenarios));
                if (!ProcessItemsUpload(WorkshopType.Scenario, itemPaths, options))
                    success = false;

                MySandboxGame.Log.WriteLineAndConsole("Batch workshop upload complete!");
                return success;
            });

            return Task;
        }

        static bool ProcessItemsUpload(WorkshopType type, List<string> paths, ProcessedOptions options)
        {
            const int MAX_DESCRIPTION_LENGTH = 8000;

            bool success = true;
            for (int idx = 0; idx < paths.Count; idx++)
            {
                var pathname = Path.GetFullPath(paths[idx]);

                // Check if path is really a modid (this is kind of hacky right now)
                if (!Directory.Exists(pathname) && ulong.TryParse(paths[idx], out var id))
                {
                    if (options.Compile)
                    {
                        MySandboxGame.Log.WriteLineAndConsole(string.Format("'--compile' option not valid with a ModID: {0}", id));
                        continue;
                    }
                    pathname = paths[idx];
                }

                var tags = options.Tags?.ToArray();

                if (!string.IsNullOrEmpty(options.Thumbnail) &&
                    !Path.IsPathRooted(options.Thumbnail))
                    options.Thumbnail = Path.GetFullPath(Path.Combine(LaunchDirectory, options.Thumbnail));

                // Read the description filename, if set
                string description = null;
                if (!string.IsNullOrEmpty(options.DescriptionFile))
                {
                    if (!Path.IsPathRooted(options.DescriptionFile))
                        options.DescriptionFile = Path.GetFullPath(Path.Combine(LaunchDirectory, options.DescriptionFile));

                    var fi = new FileInfo(options.DescriptionFile);
                    if (fi.Exists)
                    {
                        description = File.ReadAllText(options.DescriptionFile);
                        // If the file size itself is <= 8000, no problem
                        if (fi.Length > MAX_DESCRIPTION_LENGTH)
                        {
                            // Steamworks sends the description to Steam as UTF-8, so verify UTF-8 size.
                            // If the file was saved as UTF-16 encoding, but only used 7-bit characters, then the UTF-8 size will be lower.
                            var utf8len = Encoding.UTF8.GetByteCount(description);
                            if (utf8len > MAX_DESCRIPTION_LENGTH)
                                MySandboxGame.Log.WriteLineWarning(string.Format("Description is too long, current UTF-8 size: {0} bytes; maximum: {1}", utf8len, MAX_DESCRIPTION_LENGTH));
                        }
                    }
                    else
                        MySandboxGame.Log.WriteLineWarning(string.Format("Unable to set description, file does not exist: {0}", options.DescriptionFile));
                }

                // Read the changelog from a file, if detected
                var changelog = options.Changelog;
                if (!string.IsNullOrEmpty(options.Changelog))
                {
                    try
                    {
                        if (!Path.IsPathRooted(options.Changelog))
                        {
                            var rootedPath = Path.GetFullPath(Path.Combine(LaunchDirectory, options.Changelog));

                            if (File.Exists(rootedPath))
                                options.Changelog = rootedPath;
                        }

                        if (File.Exists(options.Changelog))
                        {
                            MySandboxGame.Log.WriteLineAndConsole(string.Format("Reading changelog from file: {0}", options.Changelog));
                            changelog = File.ReadAllText(options.Changelog);
                        }
                    }
                    catch(Exception ex)
                        when (ex is NotSupportedException || ex is IOException || ex is ArgumentException)
                    {
                        // Assume the string provided isn't a filename
                        // Could contain invalid characters that GetFullPath can't handle.
                    }
                }

                var mod = new Uploader(type, pathname, (UploadVerb)options, description, changelog);
                if (options.UpdateOnly && ((IMod)mod).ModId == 0)
                {
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("--update-only passed, skipping: {0}", mod.Title));
                    continue;
                }
                MySandboxGame.Log.WriteLineAndConsole(string.Format("Processing {0}: {1}", type.ToString(), mod.Title));

                if (mod.Compile())
                {
                    if (!SteamAPI.IsSteamRunning())
                    {
                        MySandboxGame.Log.WriteLineError("Cannot publish, Steam not detected!");
                        return false;
                    }

                    if (options.Upload)
                    {
                        if (mod.Publish()) 
                        {
                            if (!options.DryRun && !string.IsNullOrEmpty(options.DiscordWebhookUrl))
                            {
                                MySandboxGame.Log.WriteLineAndConsole(string.Format("Discord-Webhook-Url: {0}", options.DiscordWebhookUrl));
                                DiscordWebhook hook = new DiscordWebhook(type, mod.Title, changelog);
                                if (hook.Call(options.DiscordWebhookUrl, out string error))
                                    MySandboxGame.Log.WriteLineAndConsole("Sent payload to discord webhook");
                                else
                                    MySandboxGame.Log.WriteLineWarning(string.Format("Discord webhook error: {0}", error));
                            }

                            MySandboxGame.Log.WriteLineAndConsole(string.Format("Complete: {0}", mod.Title));
                        }
                        else
                        {
                            success = false;
                            MySandboxGame.Log.WriteLineError(string.Format("Error occurred: {0}", mod.Title));
                        }
                    }
                    else
                    {
                        if (((IMod)mod).ModId == 0)
                        {
                            MySandboxGame.Log.WriteLineWarning(string.Format("Mod not published, skipping: {0}", mod.Title));
                            success = false;
                        }
                        else
                        {
                            MySandboxGame.Log.WriteLineAndConsole(string.Format("Not uploading: {0}", mod.Title));
                            // Don't send metadata updates unless it's a publishing verb
                            if (options.Type.IsSubclassOf(typeof(PublishVerbBase)))
                            {
                                foreach (var item in mod.ModId)
                                    mod.UpdatePreviewFileOrTags(item);
                            }
                            MySandboxGame.Log.WriteLineAndConsole(string.Format("Complete: {0}", mod.Title));
                        }
                    }
                }
                else
                {
                    MySandboxGame.Log.WriteLineWarning(string.Format("Skipping {0}: {1}", type.ToString(), mod.Title));
                    success = false;
                }

                MySandboxGame.Log.WriteLineAndConsole(string.Empty);
            }
            return success;
        }
        #endregion  Upload

        #region Download
        static System.Threading.Tasks.Task<bool> DownloadMods(ProcessedOptions options)
        {
            // Get PublishItemBlocking internal method via reflection
            MySandboxGame.Log.WriteLineAndConsole(string.Empty);

            var Task = System.Threading.Tasks.Task<bool>.Factory.StartNew(() =>
            {
                bool success = true;

                MySandboxGame.Log.WriteLineAndConsole("Beginning batch workshop download...");
                MySandboxGame.Log.WriteLineAndConsole(string.Empty);

                if (options.Collections?.Count() > 0 || options.Ids?.Count() > 0)
                {
                    var items = new List<MyWorkshopItem>();

                    // get collection information
                    options.Collections?.ForEach(i => items.AddRange(WorkshopHelper.GetCollectionDetails(i)));
                    WorkshopHelper.GetItemsBlocking(options?.Ids)?.ForEach(item =>
                    {
                        // Ids can contain any workshop id, including collections, so check each one
                        if (item.ItemType == MyWorkshopItemType.Collection)
                            items.AddRange(WorkshopHelper.GetCollectionDetails(item.Id));
                        else
                            items.Add(item);
                    });

                    options.Mods = CombineCollectionWithList(WorkshopType.Mod, items, options.Mods);
                    options.Blueprints = CombineCollectionWithList(WorkshopType.Blueprint, items, options.Blueprints);
#if SE
                    options.IngameScripts = CombineCollectionWithList(WorkshopType.IngameScript, items, options.IngameScripts);
#endif
                    options.Worlds = CombineCollectionWithList(WorkshopType.World, items, options.Worlds);
                    options.Scenarios = CombineCollectionWithList(WorkshopType.Scenario, items, options.Scenarios);
                }

                if (!ProcessItemsDownload(WorkshopType.Mod, options.Mods, options))
                    success = false;
                if (!ProcessItemsDownload(WorkshopType.Blueprint, options.Blueprints, options))
                    success = false;
#if SE
                if (!ProcessItemsDownload(WorkshopType.IngameScript, options.IngameScripts, options))
                    success = false;
#endif
                if (!ProcessItemsDownload(WorkshopType.World, options.Worlds, options))
                    success = false;
                if (!ProcessItemsDownload(WorkshopType.Scenario, options.Scenarios, options))
                    success = false;

                MySandboxGame.Log.WriteLineAndConsole("Batch workshop download complete!");
                return success;
            });

            return Task;
        }

        static bool ProcessItemsDownload(WorkshopType type, IEnumerable<string> paths, ProcessedOptions options)
        {
            if (paths == null || paths?.Count() == 0 )
                return true;

            var width = Console.Out.IsInteractive() ? Console.WindowWidth : 256;

            MySandboxGame.Log.WriteLineAndConsole(string.Format("Processing {0}s...", type.ToString()));

            var modids = paths.Select(ulong.Parse);
            var workshopIds = modids.ToWorkshopIds();
            var downloadPath = WorkshopHelper.GetWorkshopItemPath(type);

            var items = WorkshopHelper.GetItemsBlocking(workshopIds);

            if (items?.Count > 0)
            {
                System.Threading.Thread.Sleep(1000); // Fix for DLC not being filled in
                
                bool success = false;
                if (type == WorkshopType.Mod)
                {
                    var result = WorkshopHelper.DownloadModsBlocking(items);
                    success = result.Success;
                }
                else
                {
                    if (type == WorkshopType.Blueprint)
                    {
                        var loopsuccess = false;
                        foreach (var item in items)
                        {
                            loopsuccess = WorkshopHelper.DownloadBlueprintBlocking(item);
                            if (!loopsuccess)
                                MySandboxGame.Log.WriteLineError(string.Format("Download of {0} FAILED!", item.Id));
                            else
                                success = true;
                        }
                    }
#if SE
                    else if (type == WorkshopType.IngameScript)
                    {
                        var loopsuccess = false;
                        foreach (var item in items)
                        {
                            loopsuccess = MyWorkshop.DownloadScriptBlocking(item);
                            if (!loopsuccess)
                                MySandboxGame.Log.WriteLineError(string.Format("Download of {0} FAILED!", item.Id));
                            else
                                success = true;
                        }
                    }
#endif
                    else if (type == WorkshopType.World || type == WorkshopType.Scenario)
                    {
                        var loopsuccess = false;

                        foreach (var item in items)
                        {
                            string path;
                            // This downloads and extracts automatically, no control over it
                            loopsuccess = WorkshopHelper.TryCreateWorldInstanceBlocking(type, item, out path, options.Force);
                            if (!loopsuccess)
                            {
                                MySandboxGame.Log.WriteLineError(string.Format("Download of {0} FAILED!", item.Id));
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
                    MySandboxGame.Log.WriteLineError("Download FAILED!");
                    return false;
                }

                foreach (var item in items)
                {
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("Downloading mod: {0}; {1}", item.Id, item.Title));
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("Visibility: {0}", item.Visibility));
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("Tags: {0}", string.Join(", ", string.Join(", ", item.Tags))));

#if SE
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("DLC requirements: {0}",
                        (item.DLCs.Count > 0 ? string.Join(", ", item.DLCs.Select(i =>
                        {
                            try { return Sandbox.Game.MyDLCs.DLCs[i].Name; }
                            catch { return $"Unknown({i})"; }
                        })) : "None")));
#endif
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("Dependencies: {0}", (item.Dependencies.Count > 0 ? string.Empty : "None")));

                    if (item.Dependencies.Count > 0)
                    {
                        var depIds = item.Dependencies.ToWorkshopIds();

                        var depItems = WorkshopHelper.GetItemsBlocking(depIds);
                        if (depItems?.Count > 0)
                            depItems.ForEach(i => MySandboxGame.Log.WriteLineAndConsole(string.Format("{0,15} -> {1}",
                                i.Id, i.Title.Substring(0, Math.Min(i.Title.Length, width - 45)))));
                        else
                            MySandboxGame.Log.WriteLineAndConsole(string.Format("     {0}", string.Join(", ", item.Dependencies)));
                    }

                    MySandboxGame.Log.WriteLineAndConsole(string.Format("Location: {0}", item.Folder));

                    if (options.Extract)
                    {
                        var mod = new Downloader(downloadPath, item);
                        mod.Extract();
                    }
                    MySandboxGame.Log.WriteLineAndConsole(string.Empty);
                }
            }
            return true;
        }
        #endregion Download

        #region Pathing
        static string[] TestPathAndMakeAbsolute(WorkshopType type, IEnumerable<string> pathsin)
        {
            var paths = pathsin?.ToArray();
            for (int idx = 0; paths != null && idx < paths.Length; idx++)
            {
                // If the passed in path doesn't exist, and is relative, try to match it with the expected data directory
                if (!Directory.Exists(paths[idx]) && !Path.IsPathRooted(paths[idx]))
                {
                    // Check if value is actually a mod id, and work remotely, if so.
                    var newpath = Path.Combine(WorkshopHelper.GetWorkshopItemPath(type), paths[idx]);

                    if (Directory.Exists(newpath) || !ulong.TryParse(paths[idx], out var id))
                        paths[idx] = newpath;
                }
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
                if (!Directory.Exists(Path.GetDirectoryName(path)) && ulong.TryParse(path, out ulong id))
                {
                    // Kind of hacky right now
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("Detected ModID, and directory not found: {0}", path));
                    itemPaths.Add(path);
                    continue;
                }

                var dirs = Directory.EnumerateDirectories(Path.GetDirectoryName(path), Path.GetFileName(path));

                if (dirs.Count() == 0)
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("Directory not found, skipping: {0}", path));

                itemPaths.AddRange(dirs
                    .Where(i => !(Path.GetFileName(i).StartsWith(".") ||                // Ignore directories starting with "." (eg. ".vs")
                                Path.GetFileName(i).StartsWith(Constants.SEWT_Prefix))) // also ignore directories starting with "[_SEWT_]" (downloaded by this mod)
                            .Select(i => i).ToList());
            }
            return itemPaths;
        }
        #endregion Pathing

        protected virtual void AuthenticateWorkshop()
        {
        }

        private void ListDLCs()
        {
#if SE
            MySandboxGame.Log.WriteLineAndConsole("Valid DLC:");
            foreach (var dlc in Sandbox.Game.MyDLCs.DLCs.Values)
            {
                MySandboxGame.Log.WriteLineAndConsole($"Name: {dlc.Name}, ID: {dlc.AppId}");
            }
#endif
        }

        static string[] CombineCollectionWithList(WorkshopType type, List<MyWorkshopItem> items, IEnumerable<string> existingitems)
        {
            var tempList = new List<string>();

            // Check mods
            items.Where(i => i.Tags.Contains(type.ToString(), StringComparer.InvariantCultureIgnoreCase))
                                .ForEach(i => tempList.Add(
                                    i.Id.ToString()
                                    ));

            if (tempList.Count > 0)
            {
                if(existingitems != null)
                    tempList = tempList.Union(existingitems).ToList();

                return tempList.ToArray();
            }
            return existingitems?.ToArray();
        }

        public static void CopyAll(string source, string target)
        {
            CopyAllConditional(source, target, (string s) => true);
        }

        public static void CopyAllConditional(string source, string target, Predicate<string> condition)
        {
            if (!Directory.Exists(target))
                Directory.CreateDirectory(target);

            foreach (string file in Directory.EnumerateFiles(source, "*.*", SearchOption.AllDirectories))
            {
                if (condition(file))
                {
                    string fileName = Path.GetFileName(file);
                    string fileDirectory = Path.GetDirectoryName(file);
                    string relativePath = GetRelativePath(source, fileDirectory);
                    if (!string.IsNullOrWhiteSpace(relativePath))
                    {
                        Directory.CreateDirectory(Path.Combine(target, relativePath));
                        File.Copy(file, Path.Combine(target, relativePath, fileName), true);
                    }
                    else
                    {
                        File.Copy(file, Path.Combine(target, fileName), true);
                    }
                }
            }
        }

        private static string GetRelativePath(string relative_to, string path)
        {
            if (relative_to.EndsWith("\\"))
                relative_to = relative_to.Remove(relative_to.Length - 1);

            if (relative_to.Equals(path, StringComparison.InvariantCultureIgnoreCase))
                return string.Empty;

            string relativePath = path.Remove(0, relative_to.Length);
            if (relativePath.StartsWith("\\"))
                relativePath = relativePath.Remove(0, 1);

            return relativePath;
        }
    }
}
