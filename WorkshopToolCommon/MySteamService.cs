using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;

#if SE
using MySteamServiceBase = VRage.Steam.MySteamService;
#else
using MySteamServiceBase = Sandbox.MySteamService;
#endif

namespace Phoenix.WorkshopTool
{
    /// <summary>
    /// Keen's steam service calls RestartIfNecessary, which triggers steam to think the game was launched
    /// outside of Steam, which causes this process to exit, and the game to launch instead with an arguments warning.
    /// We have to override the default behavior, then forcibly set the correct options.
    /// </summary>
    public class MySteamService : MySteamServiceBase
    {
        public MySteamService(bool isDedicated, uint appId)
            : base(true, appId)
        {
            // TODO: Add protection for this mess... somewhere
            SteamSDK.SteamServerAPI.Instance.Dispose();
            var steam = typeof(MySteamServiceBase);
#if SE
            steam.GetProperty("SteamServerAPI").GetSetMethod(true).Invoke(this, new object[] { null });
            steam.GetField("m_gameServer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(this, null);
#else
            steam.GetField("SteamServerAPI").SetValue(this, null);
#endif

            steam.GetProperty("AppId").GetSetMethod(true).Invoke(this, new object[] { appId });
            if (isDedicated)
            {
#if SE
                steam.GetProperty("SteamServerAPI").GetSetMethod(true).Invoke(this, new object[] { null });
                steam.GetField("m_gameServer").SetValue(this, new VRage.Steam.MySteamGameServer());
#else
                steam.GetField("SteamServerAPI").SetValue(this, SteamSDK.SteamServerAPI.Instance);
#endif
            }
            else
            {
                var SteamAPI = SteamSDK.SteamAPI.Instance;
#if SE
                steam.GetProperty("API").GetSetMethod(true).Invoke(this, new object[] { SteamSDK.SteamAPI.Instance });
#else
                steam.GetField("m_steamAPI", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    .SetValue(this, SteamSDK.SteamAPI.Instance);
#endif
                steam.GetProperty("IsActive").GetSetMethod(true).Invoke(this, new object[] { 
#if SE
                    SteamSDK.SteamAPI.Instance.Init()
#else
                    SteamSDK.SteamAPI.Instance != null
#endif
                 });

#if SE
                if (IsActive)
#else
                if (SteamAPI != null)
#endif
                {
                    steam.GetProperty("UserId").GetSetMethod(true).Invoke(this, new object[] { SteamAPI.GetSteamUserId() });
                    steam.GetProperty("UserName").GetSetMethod(true).Invoke(this, new object[] { SteamAPI.GetSteamName() });
                    steam.GetProperty("OwnsGame").GetSetMethod(true).Invoke(this, new object[] { SteamAPI.HasGame() });
                    steam.GetProperty("UserUniverse").GetSetMethod(true).Invoke(this, new object[] { SteamAPI.GetSteamUserUniverse() });
                    steam.GetProperty("BranchName").GetSetMethod(true).Invoke(this, new object[] { SteamAPI.GetBranchName() });
                    SteamAPI.LoadStats();

#if SE
                    steam.GetProperty("InventoryAPI").GetSetMethod(true).Invoke(this, new object[] { new VRage.Steam.MySteamInventory() });

                    steam.GetMethod("RegisterCallbacks",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .Invoke(this, null);
#endif
                }
            }

#if SE
            steam.GetProperty("Peer2Peer").GetSetMethod(true).Invoke(this, new object[] { new VRage.Steam.MySteamPeer2Peer() });
#endif
        }
    }
}
