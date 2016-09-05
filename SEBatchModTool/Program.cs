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

namespace SEBatchModTool
{
    class Program
    {
        private static MySandboxGame m_spacegame = null;
        private static MyCommonProgramStartup m_startup;
        const uint AppId_SE = 244850;      // MUST MATCH SE
        const uint AppId_ME = 333950;      // TODO

        static void Main(string[] args)
        {
            var options = new Options();
            var parser = new CommandLine.Parser(with => with.HelpWriter = Console.Error);

            if (parser.ParseArgumentsStrict(args, options, () => Environment.Exit(-2)))
            {
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

                UploadMods(options);

                // Cleanup
                CleanupSandbox();
            }
        }

        #region Sandbox stuff
        private static void CleanupSandbox()
        {
            VRage.Plugins.MyPlugins.Unload();   // Prevents assert in debug
        }

        // This is mostly copied from MyProgram.Main(), with UI stripped out.
        private static void InitSandbox(string instancepath)
        {
            if (m_spacegame != null)
                m_spacegame.Exit();

            SpaceEngineersGame.SetupBasicGameInfo();
            m_startup = new MyCommonProgramStartup(new string[] { });

            var appDataPath = m_startup.GetAppDataPath();
            MyInitializer.InvokeBeforeRun(AppId_SE, MyPerGameSettings.BasicGameInfo.ApplicationName, appDataPath);
            MyInitializer.InitCheckSum();

            if (!m_startup.Check64Bit()) return;

            using (MySteamService steamService = new MySteamService(MySandboxGame.IsDedicated, AppId_SE))
            {
                SpaceEngineersGame.SetupPerGameSettings();

                if (!m_startup.CheckSteamRunning(steamService)) return;

                VRageGameServices services = new VRageGameServices(steamService);

                if (!MySandboxGame.IsDedicated)
                    MyFileSystem.InitUserSpecific(steamService.UserId.ToString());

                try
                {
                    // NOTE: an assert may be thrown in debug, about missing Tutorials.sbx. Ignore it.
                    m_spacegame = new SpaceEngineersGame(services, null);
                }
                catch(Exception ex)
                {
                    // This shouldn't fail, but don't stop even if it does
                    System.Console.WriteLine("An exception occured, ignoring: " + ex.Message);
                }
            }
        }
        #endregion

        static void UploadMods(Options options)
        {
            // Get PublishItemBlocking internal method via reflection
            System.Console.WriteLine(System.Environment.NewLine + "Beginning batch mod upload...");
            System.Threading.Tasks.Task[] Tasks = new System.Threading.Tasks.Task[options.ModPaths.Length];

            var Task = System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                for (int idx = 0; idx < options.ModPaths.Length; idx++)
                {
                    var mod = new Uploader(Path.GetFullPath(options.ModPaths[idx]), options.Compile, options.DryRun, options.Development, options.Visibility);
                    System.Console.WriteLine("Processing mod: {0}", mod.Title);

                    if (mod.Compile())
                    {
                        if( mod.Publish() )
                            System.Console.WriteLine("Complete: {0}", mod.Title);
                    }
                    else
                    {
                        System.Console.WriteLine("Skipping mod: {0}", mod.Title);
                    }
                    System.Console.WriteLine();
                }
            });

            Thread.Sleep(2000);
            // Wait for uploads to finish (separate thread)
            while (!Task.Wait(500))
            {
                if (MySteam.API != null)
                    MySteam.API.RunCallbacks();
            }

            System.Console.WriteLine("Batch mod upload complete!");
        }
    }
}
