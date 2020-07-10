using Sandbox;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage;
using VRage.GameServices;
#if SE
using VRage.Utils;
#else
using VRage.Logging;
#endif

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

    public static class LoggingHelper
    {
        /// <summary>
        /// Logs an exception.
        /// </summary>
        /// <param name="ex">Exception to log.</param>
        /// <param name="customMessage">Message text to log, exception message will be appended.</param>
        public static void Log(this Exception ex, string customMessage = "ERROR: An exception occurred: ")
        {
            MySandboxGame.Log.WriteLineAndConsole(customMessage + ex.Message);
            MySandboxGame.Log.WriteLineToConsole("Check the log file for details.");
            MySandboxGame.Log.WriteLine(ex.StackTrace);
        }

        /// <summary>
        /// This is a log wrapper for ME.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="msg"></param>
        public static void WriteLineToConsole(this MyLog log, string msg)
        {
            log.WriteLineAndConsole(msg);
        }
    }
}
