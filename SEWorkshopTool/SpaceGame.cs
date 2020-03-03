using Phoenix.WorkshopTool;
using Sandbox;
using Sandbox.Game;
using System.Collections.Generic;
using System.Reflection;
using VRage;
using VRage.GameServices;
using VRage.Utils;
using VRageRender;

namespace Phoenix.SEWorkshopTool
{
    class SpaceGame : GameBase
    {
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
            SpaceEngineersGame.SetupPerGameSettings();
            ManuallyAddDLCs();
            return true;
        }

        protected override MySandboxGame InitGame()
        {
            return new SpaceEngineersGame(m_args);
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
