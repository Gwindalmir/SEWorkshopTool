using Medieval;
using Phoenix.WorkshopTool;
using Sandbox;
using Sandbox.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.FileSystem;
using VRage.Utils;
using VRageRender;

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
            MyInitializer.InitCheckSum();

            if (!m_startup.Check64Bit()) return false;

            m_steamService = new WorkshopTool.MySteamService(MySandboxGame.IsDedicated, AppId);
            MyMedievalGame.SetupPerGameSettings();

            return true;
        }

        protected override MySandboxGame InitGame(VRageGameServices services)
        {
            return new MyMedievalGame(services, null);
        }
    }
}
