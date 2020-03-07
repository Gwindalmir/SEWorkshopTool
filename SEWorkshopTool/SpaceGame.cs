using Phoenix.WorkshopTool;
using Sandbox;
using Sandbox.Game;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using VRage;
using VRage.GameServices;
using VRage.Utils;
using VRageRender;

namespace Phoenix.SEWorkshopTool
{
    class SpaceGame : GameBase
    {
        private string ModIO_GameName;
        private string ModIO_GameID;
        private string ModIO_Key;
        private string ModIO_TestGameID;
        private string ModIO_TestKey;

        public SpaceGame() : base()
        {
            InitModIO();
        }

        protected override bool SetupBasicGameInfo()
        {
            SpaceEngineersGame.SetupBasicGameInfo();
            m_startup = new MyCommonProgramStartup(m_args);

            var appDataPath = m_startup.GetAppDataPath();
            VRage.Platform.Windows.MyVRageWindows.Init(MyPerGameSettings.BasicGameInfo.ApplicationName, MySandboxGame.Log, appDataPath, false);
            MyInitializer.InvokeBeforeRun(AppId, MyPerGameSettings.BasicGameInfo.ApplicationName + "ModTool", MyVRage.Platform.System.GetAppDataPath());
            MyRenderProxy.Initialize((IMyRender)new MyNullRender());
            MyInitializer.InitCheckSum();

            if (m_startup.PerformColdStart()) return false;
            if (!m_startup.Check64Bit()) return false;

            m_steamService = VRage.Steam.MySteamGameService.Create(MySandboxGame.IsDedicated, AppId);
            MyServiceManager.Instance.AddService(m_steamService);
            MyServiceManager.Instance.AddService(VRage.Steam.MySteamUgcService.Create(AppId, m_steamService));

            if (m_useModIO)
            {
                MyLog.Default.WriteLineAndConsole("Using mod.io service, instead of Steam.");
                MyServiceManager.Instance.AddService(VRage.Mod.Io.MyModIoService.Create(MySandboxGame.IsDedicated, MyServiceManager.Instance.GetService<IMyGameService>(), ModIO_GameName, ModIO_GameID, ModIO_Key, ModIO_TestGameID, ModIO_TestKey, false));
            }

            SpaceEngineersGame.SetupPerGameSettings();
            ManuallyAddDLCs();
            return true;
        }

        protected override MySandboxGame InitGame()
        {
            return new SpaceEngineersGame(m_args);
        }

        protected void InitModIO()
        {
            var assemblyname = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(MySandboxGame).Assembly.Location), "SpaceEngineers.exe");
            if (Assembly.ReflectionOnlyLoadFrom(assemblyname) is Assembly asm)
            {
                if(asm.GetType("SpaceEngineers.MyProgram", false) is System.Type program)
                {
                    ModIO_GameName = program.GetField("MODIO_GAME_NAME", BindingFlags.NonPublic | BindingFlags.Static)?.GetRawConstantValue() as string;
                    ModIO_TestGameID = program.GetField("MODIO_TEST_GAMEID", BindingFlags.NonPublic | BindingFlags.Static)?.GetRawConstantValue() as string;
                    ModIO_TestKey = program.GetField("MODIO_TEST_APIKEY", BindingFlags.NonPublic | BindingFlags.Static)?.GetRawConstantValue() as string;
                    ModIO_GameID = program.GetField("MODIO_LIVE_GAMEID", BindingFlags.NonPublic | BindingFlags.Static)?.GetRawConstantValue() as string;
                    ModIO_Key = program.GetField("MODIO_LIVE_APIKEY", BindingFlags.NonPublic | BindingFlags.Static)?.GetRawConstantValue() as string;
                }
                else
                {
                    MySandboxGame.Log.WriteLineAndConsole(string.Format(Constants.ERROR_Reflection, "SpaceEngineers.MyProgram"));
                }
            }
            else
            {
                MySandboxGame.Log.WriteLineAndConsole(string.Format(Constants.ERROR_Reflection, "SpaceEngineers.exe"));
            }
        }

        protected override void AuthenticateWorkshop()
        {
            if (m_useModIO)
            {
                base.AuthenticateWorkshop();

                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var email = config.AppSettings.Settings["auth-login"]?.Value;
                var token = config.AppSettings.Settings["auth-token"]?.Value;
                var expires = int.Parse(config.AppSettings.Settings["auth-expires"]?.Value ?? "0");

                if (string.IsNullOrEmpty(email))
                {
                    System.Console.WriteLine(
                        "Authentication to mod.io required." + Environment.NewLine +
                        "This will create an OAuth2 token for your account." + Environment.NewLine +
                        "This is a one-time process, and does NOT require your password." + Environment.NewLine +
                        $"Your email and token will be saved in {config.FilePath}." + Environment.NewLine +
                        "Protect this file." + Environment.NewLine);
                    System.Console.Write("Enter the email associated with your mod.io account: ");
                    email = System.Console.ReadLine();
                }

                if (string.IsNullOrEmpty(token))
                {
                    Sandbox.Engine.Networking.MyGameService.WorkshopService.RequestSecurityCode(email, (r) =>
                    {
                        if (r == MyGameServiceCallResult.OK)
                        {
                            System.Console.WriteLine("Mod.io has sent security code to your email. It expires after 15 minutes.");
                            System.Console.Write("Enter Security code: ");
                            var code = System.Console.ReadLine();

                            Sandbox.Engine.Networking.MyGameService.WorkshopService.AuthenticateWithSecurityCode(code, (a) =>
                            {
                                if (a == MyGameServiceCallResult.OK)
                                {
                                    var clsMyModIo = typeof(VRage.Mod.Io.MyModIoService).Assembly.GetType("VRage.Mod.Io.MyModIo");
                                    System.Console.WriteLine("Authentication successful!");

                                    var accessToken = clsMyModIo.GetField("m_authenticatedToken", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
                                    token = (string)accessToken.GetType().GetField("access_token", BindingFlags.Public | BindingFlags.Instance)?.GetValue(accessToken);
                                    expires = (int)accessToken.GetType().GetField("date_expires", BindingFlags.Public | BindingFlags.Instance)?.GetValue(accessToken);

                                    config.AppSettings.Settings.Add("auth-login", email);
                                    config.AppSettings.Settings.Add("auth-token", token);
                                    config.AppSettings.Settings.Add("auth-expires", expires.ToString());
                                    config.Save();

                                    MyLog.Default.WriteLineAndConsole($"Your authentication token has been saved in {config.FilePath}. Do not delete or replace this file, or you will need to authenticate again.");
                                    PostAuthentication(token, expires);
                                }
                            });
                        }
                    });
                }
                else
                {
                    PostAuthentication(token, expires);
                }
            }
        }

        private void PostAuthentication(string token, int expires)
        {
            var clsMyModIo = typeof(VRage.Mod.Io.MyModIoService).Assembly.GetType("VRage.Mod.Io.MyModIo");

            // Create auth token class
            var access_token_t = typeof(VRage.Mod.Io.MyModIoService).Assembly.GetType("VRage.Mod.Io.Data.AccessToken");
            var token_instance = Activator.CreateInstance(access_token_t);
            access_token_t.GetField("access_token", BindingFlags.Public | BindingFlags.Instance)?.SetValue(token_instance, token);
            access_token_t.GetField("date_expires", BindingFlags.Public | BindingFlags.Instance)?.SetValue(token_instance, expires);

            // Tell the game we're authenticated
            clsMyModIo.GetField("m_authenticatedToken", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, token_instance);
            clsMyModIo.GetField("m_authenticated", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, 3);
            clsMyModIo.GetField("m_authenticatedUserId", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, ulong.MaxValue);
        }

        // This is to manually add any DLC not added to MyDLCs.DLCs, so the lookup later can happen
        private void ManuallyAddDLCs()
        {
            var obj = typeof(Sandbox.Game.MyDLCs.MyDLC).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
                new System.Type[] { typeof(uint), typeof(string), typeof(MyStringId), typeof(MyStringId), typeof(string), typeof(string), typeof(string), typeof(string) }, null);

            if (obj == null)
            {
                MySandboxGame.Log.WriteLineAndConsole(string.Format(Constants.ERROR_Reflection, "MyDLC.ctor"));
                return;
            }

            // The 2013 First release is listed on steam as a valid DLC mods can have.
            // But the game doesn't acknowledge it, so add it manually.
            var SpaceEngineers2013DLC = obj.Invoke(new object[]
            {
                573900U,
                "FirstRelease",
                MyStringId.GetOrCompute("Space Engineers 2013"),
                MyStringId.GetOrCompute("Space Engineers First Release"),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            }) as Sandbox.Game.MyDLCs.MyDLC;


            var dlcs = (Dictionary<uint, Sandbox.Game.MyDLCs.MyDLC>)(typeof(Sandbox.Game.MyDLCs).GetField("m_dlcs", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
            var dlcsByName = (Dictionary<string, Sandbox.Game.MyDLCs.MyDLC>)(typeof(Sandbox.Game.MyDLCs).GetField("m_dlcsByName", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));

            if (dlcs != null && !dlcs.ContainsKey(SpaceEngineers2013DLC.AppId))
            {
                dlcs[SpaceEngineers2013DLC.AppId] = SpaceEngineers2013DLC;
                dlcsByName[SpaceEngineers2013DLC.Name] = SpaceEngineers2013DLC;
            }
        }

    }
}
