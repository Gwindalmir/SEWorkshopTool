using Phoenix.WorkshopTool;
using Sandbox;
using Sandbox.Game;
using SpaceEngineers.Game;
using VRage;
using VRage.GameServices;

namespace Phoenix.SEWorkshopTool
{
    class SpaceGame : GameBase
    {
        protected override bool SetupBasicGameInfo()
        {
            SpaceEngineersGame.SetupBasicGameInfo();
            m_startup = new MyCommonProgramStartup(new string[] { });

            var appDataPath = m_startup.GetAppDataPath();
            MyInitializer.InvokeBeforeRun(AppId, MyPerGameSettings.BasicGameInfo.ApplicationName + "ModTool", appDataPath);
            MyInitializer.InitCheckSum();

            if (!m_startup.Check64Bit()) return false;

            m_steamService = new WorkshopTool.MySteamService(MySandboxGame.IsDedicated, AppId);
            MyServiceManager.Instance.AddService<IMyGameService>(m_steamService);
            SpaceEngineersGame.SetupPerGameSettings();

            return true;
        }

        protected override MySandboxGame InitGame()
        {
            return new SpaceEngineersGame(new string[] { });
        }
    }
}
