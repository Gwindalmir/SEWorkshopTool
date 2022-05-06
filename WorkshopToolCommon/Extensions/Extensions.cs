using Gwindalmir.Updater;
using Sandbox;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.GameServices;
using Sandbox.Engine.Networking;
using VRage.Utils;
#if SE
using Error = VRage.Game.MyDefinitionErrors.Error;
using MyDebug = Phoenix.WorkshopTool.Extensions.MyDebug;
#else
using Error = VRage.Scripting.MyScriptCompiler.Message;
using VRage.Logging;
#endif

namespace Phoenix.WorkshopTool.Extensions
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

    public static class LoggingExtensions
    {
        /// <summary>
        /// Logs an exception.
        /// </summary>
        /// <param name="ex">Exception to log.</param>
        /// <param name="customMessage">Message text to log, exception message will be appended.</param>
        public static void Log(this Exception ex, string customMessage = "ERROR: An exception occurred: ")
        {
            ProgramBase.ConsoleWriteColored(ConsoleColor.Red, () =>
            {
                MySandboxGame.Log.WriteLineAndConsole(customMessage + ex.Message);
                MySandboxGame.Log.WriteLineToConsole("Check the log file for details.");
                MySandboxGame.Log.WriteLine(ex.StackTrace);
            });
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

        public static void WriteLineError(this MyLog log, string msg)
        {
            ProgramBase.ConsoleWriteColored(ConsoleColor.Red, () =>
                log.WriteLineAndConsole(msg));
        }

        public static void WriteLineWarning(this MyLog log, string msg)
        {
            ProgramBase.ConsoleWriteColored(ConsoleColor.Yellow, () =>
                log.WriteLineAndConsole(msg));
        }
    }

    public static class WorkshopIdExtensions
    {
#if SE
        public static ulong GetId(this VRage.Game.WorkshopId id)
        {
            return id.Id;
        }

        public static ulong[] GetIds(this VRage.Game.WorkshopId[] ids)
        {
            return ids.Select(i => i.Id).ToArray();
        }

        public static string AsString(this VRage.Game.WorkshopId[] ids)
        {
            var result = new StringBuilder();

            if (ids is null)
                return result.ToString();

            foreach (var id in ids)
                result.Append($"{id.ServiceName ?? string.Empty}/{id.Id};");

            result.Remove(result.Length - 1, 1);

            return result.ToString();
        }

        public static string AsStringURL(this VRage.Game.WorkshopId[] ids)
        {
            var result = new StringBuilder();

            if (ids is null)
                return result.ToString();

            foreach (var id in ids)
            {
                if(id.ServiceName == "modio")
                    result.Append($"https://mod.io/search/id/mods/{id.Id};");
                else
                    result.Append($"https://steamcommunity.com/sharedfiles/filedetails/?id={id.Id};");
            }
            result.Remove(result.Length - 1, 1);

            return result.ToString();
        }

        public static WorkshopId[] ToWorkshopIds(this IEnumerable<ulong> ids)
        {
            return ids.Select(id => new VRage.Game.WorkshopId(id, MyGameService.GetDefaultUGC().ServiceName)).ToArray();
        }

        public static WorkshopId ToWorkshopId(this ulong id)
        {
            return new VRage.Game.WorkshopId(id, MyGameService.GetDefaultUGC().ServiceName);
        }
#else
        // This is just a wrapper so the same method call can be used for either game.
        public static ulong GetId(this ulong id)
        {
            return id;
        }

        public static ulong[] GetIds(this ulong[] ids)
        {
            return ids;
        }

        public static string AsString(this ulong[] id)
        {
            return id[0].ToString();
        }

        public static string AsStringURL(this ulong[] id)
        {
            return $"https://steamcommunity.com/sharedfiles/filedetails/?id={id[0]}";
        }

        public static ulong[] ToWorkshopIds(this MyWorkshopItem[] items)
        {
            return items?.Select(i => i.Id)?.ToArray();
        }

        public static ulong[] ToWorkshopIds(this IEnumerable<ulong> ids)
        {
            return ids.ToArray();
        }

        public static ulong ToWorkshopId(this ulong id)
        {
            return id;
        }
#endif
    }

    public static class ConsoleExtensions
    {
        public static bool IsInteractive(this TextWriter stream)
        {
            if (!Environment.UserInteractive)
                return false;

            if (Console.Out == stream)
                return !Console.IsOutputRedirected;

            if (Console.Error == stream)
                return !Console.IsErrorRedirected;

            return false;
        }
    }

    public static class UpdaterExtensions
    {
        public static string GetChangelog(this Release release)
        {
            var end = release.Body.IndexOf("To Install");

            if (end <= 0)
                end = release.Body.Length;

            return release.Body.Substring(0, end).Trim();
        }
    }

    public static class MyDefinitionErrorExtensions
    {
        public static string GetErrorText(this Error error)
        {
#if SE
            return error.Message;
#else
            return error.Text;
#endif
        }
    }

    public static class MyWorkshopExtensions
    {
        internal static void AddHiddenTags(this List<MyWorkshop.Category> categories, WorkshopType? type = null)
        {
            switch (type)
            {
                case WorkshopType.Mod:
#if SE
                    categories.Add(new MyWorkshop.Category() { Id = "campaign" });
                    categories.Add(new MyWorkshop.Category() { Id = "font" });
                    categories.Add(new MyWorkshop.Category() { Id = "noscripts" });
#endif
                    break;
                case WorkshopType.Blueprint:
                    MyWorkshop.BlueprintCategories.ForEach(c => categories.Add(c));
#if SE
                    categories.Add(new MyWorkshop.Category() { Id = "large_grid" });
                    categories.Add(new MyWorkshop.Category() { Id = "small_grid" });
                    categories.Add(new MyWorkshop.Category() { Id = "safe" });   // Mod.io only?
#endif
                    break;
                case WorkshopType.Scenario:
                    break;
                case WorkshopType.World:
                    break;
                case WorkshopType.IngameScript:
                    break;
                case null:
#if SE
                    // 'obsolete' tag is always available, as is 'No Mods' and 'experimental'
                    categories.Add(new MyWorkshop.Category() { Id = "obsolete" });
                    categories.Add(new MyWorkshop.Category() { Id = "no mods" });
                    categories.Add(new MyWorkshop.Category() { Id = "experimental" });
#else
                    // ME also has tags for supported game version
                    categories.Add(new MyWorkshop.Category() { Id = "0.6" });
                    categories.Add(new MyWorkshop.Category() { Id = "0.7" });
#endif
                    break;
                default:
                    MyDebug.FailRelease("Invalid category.");
                    break;
            }
        }
    }

    public static class MyDLCExtensions
    {
        public static uint TryGetDLC(this string dlc)
        {
#if SE
            Sandbox.Game.MyDLCs.MyDLC dlcvalue;
            if (Sandbox.Game.MyDLCs.TryGetDLC(dlc, out dlcvalue))
                return dlcvalue.AppId;
            else
                return 0;
#else
            return 0;
#endif
        }
    }
}
