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
            MyInitializer.InvokeBeforeRun(AppId, MyPerGameSettings.BasicGameInfo.ApplicationName + "ModTool");

            if (!m_startup.Check64Bit()) return false;

            MyMedievalGame.SetupPerGameSettings();

            return true;
        }

        public override int InitGame(string[] args)
        {
            MyMedievalGame.SetupBasicGameInfo();
            m_startup = new MyCommonProgramStartup(args);
            VRage.FileSystem.MyFileSystem.Init(MyPerGameSettings.BasicGameInfo.ApplicationName);

            var appInformation = new VRage.Engine.AppInformation("Medieval Engineers", Medieval.MyMedievalGame.ME_VERSION, "", "", "", Medieval.MyMedievalGame.VersionString);
            var vrageCore = new VRage.Engine.VRageCore(appInformation, true);
            var configuration = VRage.Engine.Util.CoreProgram.LoadParameters("MEWT.config");
            vrageCore.GetType().GetMethod("LoadSystems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(vrageCore, new[] { configuration });
            vrageCore.GetType().GetField("m_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(vrageCore, 1);
            vrageCore.GetType().GetMethod("InitSystems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(vrageCore, new object[] { configuration.SystemConfiguration, true });
            vrageCore.GetType().GetMethod("LoadMetadata", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(vrageCore, new[] { configuration });
            vrageCore.GetType().GetMethod("InitSystems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(vrageCore, new object[] { configuration.SystemConfiguration, false });

            m_steamService = new WorkshopTool.MySteamService();
            m_steamService.Init(new VRage.Steam.MySteamService.Parameters() { Server = MySandboxGame.IsDedicated, AppId = AppId });

            VRage.Logging.MyLog.Default = MySandboxGame.Log = new VRage.Logging.MyLog();
            MySandboxGame.Log.Init(MyPerGameSettings.BasicGameInfo.ApplicationName + "ModTool.log", null);
            VRage.Plugins.MyPlugins.Load();
            return base.InitGame(args);
        }
        protected override MySandboxGame InitGame()
        {
            return new MyMedievalGame();
        }
    }
}
