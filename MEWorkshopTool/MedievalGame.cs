using Medieval;
using Phoenix.WorkshopTool;
using Sandbox;
using Sandbox.Game;
using VRage;
using VRage.GameServices;

namespace Phoenix.MEWorkshopTool
{
    class MedievalGame : GameBase
    {
        protected override bool SetupBasicGameInfo()
        {
            MyMedievalGame.SetupBasicGameInfo();
            m_startup = new MyCommonProgramStartup(new string[] { });

            var appDataPath = m_startup.GetAppDataPath();
            MyInitializer.InvokeBeforeRun(AppId, MyPerGameSettings.BasicGameInfo.ApplicationName + "ModTool", appDataPath);

            if (!m_startup.Check64Bit()) return false;

            m_steamService = new WorkshopTool.MySteamService(MySandboxGame.IsDedicated, AppId);
            MyServiceManager.Instance.AddService<IMyGameService>(m_steamService);
            MyMedievalGame.SetupPerGameSettings();

            return true;
        }

        protected override MySandboxGame InitGame()
        {
            return new MyMedievalGame(null);
        }
    }
}
