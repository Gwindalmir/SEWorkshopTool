using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage;
using VRage.GameServices;

namespace Phoenix.WorkshopTool
{
#if SE
    static class MyDebug
    {
        [DebuggerStepThrough]
        public static void AssertDebug(bool condition, string message = null)
        {
            VRage.MyDebug.Assert(condition, message);
        }

        [DebuggerStepThrough]
        public static void AssertRelease(bool condition, string message = null)
        {
            VRage.MyDebug.AssertRelease(condition, message);
        }

        [DebuggerStepThrough]
        public static void FailRelease(string message = null)
        {
            VRage.MyDebug.FailRelease(message);
        }
    }
#endif

    public static class MySteamHelper
    {
        public static ERemoteStoragePublishedFileVisibility ToSteam(
          this MyPublishedFileVisibility visibility)
        {
            switch (visibility)
            {
                case MyPublishedFileVisibility.Public:
                    return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic;
                case MyPublishedFileVisibility.FriendsOnly:
                    return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityFriendsOnly;
                case MyPublishedFileVisibility.Private:
                    return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate;
                default:
                    return (ERemoteStoragePublishedFileVisibility)(int)visibility;
            }
        }

        public static MyPublishedFileVisibility ToService(
          this ERemoteStoragePublishedFileVisibility visibility)
        {
            switch (visibility)
            {
                case ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic:
                    return MyPublishedFileVisibility.Public;
                case ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityFriendsOnly:
                    return MyPublishedFileVisibility.FriendsOnly;
                case ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate:
                    return MyPublishedFileVisibility.Private;
                default:
                    return (MyPublishedFileVisibility)(int)visibility;
            }
        }
    }
}
