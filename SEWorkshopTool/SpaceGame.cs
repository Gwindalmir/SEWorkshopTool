using Phoenix.WorkshopTool;
using Sandbox;
using Sandbox.Game;
using SpaceEngineers.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            SpaceEngineersGame.SetupPerGameSettings();

            return true;
        }

        protected override MySandboxGame InitGame(VRageGameServices services)
        {
            return new SpaceEngineersGame(services, null);
        }
    }
}
