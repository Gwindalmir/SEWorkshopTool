using Phoenix.WorkshopTool;
using Phoenix.WorkshopTool.Extensions;
using Sandbox;
using Sandbox.Engine.Networking;
using Sandbox.Game;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using VRage;
using VRage.GameServices;
using VRage.Mod.Io;
using VRage.Scripting;
using VRage.Steam;
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
            
            m_steamService = MySteamGameService.Create(MySandboxGame.IsDedicated, AppId);
            MyServiceManager.Instance.AddService(m_steamService);
            MyServerDiscoveryAggregator serverDiscoveryAggregator = new MyServerDiscoveryAggregator();
            MySteamGameService.InitNetworking(false, m_steamService, MyPerGameSettings.BasicGameInfo.GameName, serverDiscoveryAggregator, true, true);

            // If user specified --modio, set that as the "default" (added first)
            var modioService = MyModIoService.Create(MyServiceManager.Instance.GetService<IMyGameService>(), ModIO_GameName, ModIO_GameID, ModIO_Key, ModIO_TestGameID, ModIO_TestKey, MyPlatformGameSettings.UGC_TEST_ENVIRONMENT, m_useModIO ? true : false);
            
            if (m_useModIO)
                MyGameService.WorkshopService.AddAggregate(modioService);
            MyGameService.WorkshopService.AddAggregate(MySteamUgcService.Create(AppId, m_steamService));
            
            if (!m_useModIO)
                MyGameService.WorkshopService.AddAggregate(modioService);

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
            ModIO_GameName = MyPlatformGameSettings.MODIO_GAME_NAME;
            ModIO_TestGameID = MyPlatformGameSettings.MODIO_TEST_GAMEID;
            ModIO_TestKey = MyPlatformGameSettings.MODIO_TEST_APIKEY;
            ModIO_GameID = MyPlatformGameSettings.MODIO_LIVE_GAMEID;
            ModIO_Key = MyPlatformGameSettings.MODIO_LIVE_APIKEY;
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
                        $"Your email and token will be saved in {config.FilePath}." + Environment.NewLine);
                    ProgramBase.ConsoleWriteColored(ConsoleColor.White,
                        "Protect this file." + Environment.NewLine);
                    System.Console.Write("Enter the email associated with your mod.io account: ");
                    email = System.Console.ReadLine();
                }

                if (string.IsNullOrEmpty(token))
                {
                    var clsMyModIo = typeof(VRage.Mod.Io.MyModIoService).Assembly.GetType("VRage.Mod.Io.MyModIo");
                    clsMyModIo.InvokeMember("EmailRequest", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod, null, null, new object[]{email, (Action<MyGameServiceCallResult>)((r) =>
                    {
                        if (r == MyGameServiceCallResult.OK)
                        {
                            System.Console.WriteLine("Mod.io has sent security code to your email. It expires after 15 minutes.");
                            System.Console.Write("Enter Security code: ");
                            var code = System.Console.ReadLine();

                            clsMyModIo.InvokeMember("EmailExchange", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod, null, null, new object[] { code, (Action<MyGameServiceCallResult>)((a) =>
                            {
                                if (a == MyGameServiceCallResult.OK)
                                {
                                    System.Console.WriteLine("Authentication successful!");

                                    var accessToken = clsMyModIo.GetField("m_authenticatedToken", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
                                    token = (string)accessToken.GetType().GetField("access_token", BindingFlags.Public | BindingFlags.Instance)?.GetValue(accessToken);
                                    expires = (int)accessToken.GetType().GetField("date_expires", BindingFlags.Public | BindingFlags.Instance)?.GetValue(accessToken);

                                    config.AppSettings.Settings.Add("auth-login", email);
                                    config.AppSettings.Settings.Add("auth-token", token);
                                    config.AppSettings.Settings.Add("auth-expires", expires.ToString());
                                    config.Save();

                                    MySandboxGame.Log.WriteLineAndConsole($"Your authentication token has been saved in {config.FilePath}. Do not delete or replace this file, or you will need to authenticate again.");
                                    PostAuthentication(token, expires);
                                }
                            })});
                        }
                    })});
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
            var obj = typeof(MyDLCs.MyDLC).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
                new Type[] { typeof(uint), typeof(string), typeof(MyStringId), typeof(MyStringId), typeof(string), typeof(string), typeof(string), typeof(string) }, null);

            if (obj == null)
            {
                MySandboxGame.Log.WriteLineError(string.Format(Constants.ERROR_Reflection, "MyDLC.ctor"));
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
            }) as MyDLCs.MyDLC;


            var dlcs = (Dictionary<uint, MyDLCs.MyDLC>)(typeof(MyDLCs).GetField("m_dlcs", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
            var dlcsByName = (Dictionary<string, MyDLCs.MyDLC>)(typeof(MyDLCs).GetField("m_dlcsByName", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));

            if (dlcs != null && !dlcs.ContainsKey(SpaceEngineers2013DLC.AppId))
            {
                dlcs[SpaceEngineers2013DLC.AppId] = SpaceEngineers2013DLC;
                dlcsByName[SpaceEngineers2013DLC.Name] = SpaceEngineers2013DLC;
            }
        }

    }
}
