using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;


namespace SEBatchModTool
{
    /// <summary>
    /// Keen's steam service calls RestartIfNecessary, which triggers steam to think the game was launched
    /// outside of Steam, which causes this process to exit, and the game to launch instead with an arguments warning.
    /// We have to override the default behavior, then forcibly set the correct options.
    /// </summary>
    public class MySteamService : Sandbox.MySteamService, IDisposable
    {
        public MySteamService(bool isDedicated, uint appId)
            :base(true, appId)
        {
            // TODO: Add protection for this mess... somewhere
            var steam = typeof(Sandbox.MySteamService);
            steam.GetProperty("AppId").GetSetMethod(true).Invoke(this, new object[] { appId });
            if (isDedicated)
            {
                steam.GetField("SteamServerAPI").SetValue(this, SteamSDK.SteamServerAPI.Instance);
            }
            else
            {
                steam.GetField("SteamAPI").SetValue(this, SteamSDK.SteamAPI.Instance);
                steam.GetProperty("IsActive").GetSetMethod(true).Invoke(this, new object[] { SteamAPI != null });

                if (IsActive)
                {
                    UserId = SteamAPI.GetSteamUserId();
                    steam.GetProperty("UserName").GetSetMethod(true).Invoke(this, new object[] { SteamAPI.GetSteamName() });
                    steam.GetProperty("OwnsGame").GetSetMethod(true).Invoke(this, new object[] { SteamAPI.HasGame() });
                    steam.GetProperty("UserUniverse").GetSetMethod(true).Invoke(this, new object[] { SteamAPI.GetSteamUserUniverse() });
                    steam.GetProperty("BranchName").GetSetMethod(true).Invoke(this, new object[] { SteamAPI.GetBranchName() });
                    SteamAPI.LoadStats();
                }
            }
        }
    }
}
