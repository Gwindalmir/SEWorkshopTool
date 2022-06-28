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
            ReflectionHelper.ReflectVRageCoreMethod("LoadSystems")?.Invoke(vrageCore, new[] { configuration });
            ReflectionHelper.ReflectVRageCoreField("m_state")?.SetValue(vrageCore, 1);
            ReflectionHelper.ReflectVRageCoreMethod("InitSystems")?.Invoke(vrageCore, new object[] { configuration.SystemConfiguration, true });
            ReflectionHelper.ReflectVRageCoreMethod("LoadMetadata")?.Invoke(vrageCore, new[] { configuration });
            ReflectionHelper.ReflectVRageCoreMethod("InitSystems")?.Invoke(vrageCore, new object[] { configuration.SystemConfiguration, false });

            m_steamService = new MySteamService();
            ((MySteamService)(m_steamService)).Init(new VRage.Steam.MySteamService.Parameters() { Server = m_ds, AppId = AppId });

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
