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
            MyInitializer.InvokeBeforeRun(AppId, MyPerGameSettings.BasicGameInfo.ApplicationName + "ModTool", MyVRage.Platform.GetAppDataPath());
            MyRenderProxy.Initialize((IMyRender)new MyNullRender());
            MyInitializer.InitCheckSum();

            if (!m_startup.Check64Bit()) return false;

            m_steamService = new WorkshopTool.MySteamService(MySandboxGame.IsDedicated, AppId);
            MyServiceManager.Instance.AddService<IMyGameService>(m_steamService);
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
            var dlcs = (Dictionary<uint, Sandbox.Game.MyDLCs.MyDLC>)(typeof(Sandbox.Game.MyDLCs).GetField("m_dlcs", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
            var dlcsByName = (Dictionary<string, Sandbox.Game.MyDLCs.MyDLC>)(typeof(Sandbox.Game.MyDLCs).GetField("m_dlcsByName", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));

            if (dlcs != null && !dlcs.ContainsKey(Sandbox.Game.MyDLCs.MyDLC.StylePack.AppId))
            {
                dlcs[Sandbox.Game.MyDLCs.MyDLC.StylePack.AppId] = Sandbox.Game.MyDLCs.MyDLC.StylePack;
                dlcsByName[Sandbox.Game.MyDLCs.MyDLC.StylePack.Name] = Sandbox.Game.MyDLCs.MyDLC.StylePack;
            }
        }

    }
}
