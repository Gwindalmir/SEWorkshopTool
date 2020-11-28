using Gwindalmir.Updater;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Phoenix.WorkshopTool
{
    // DO NOT REFERENCE GAME LIBRARIES HERE!
    public abstract class ProgramBase
    {
        internal static void CheckForUpdate(Action<string> logMethod = null)
        {
            // Direct log to console, in an attempt to check for an update if there's
            // an initialization problem (ie. game updated, and many runtime errors).
            if (logMethod == null)
                logMethod = Console.WriteLine;

            try
            {
                var updateChecker = new UpdateChecker(prereleases: true);
                if (updateChecker.IsNewerThan(Assembly.GetEntryAssembly().GetName().Version))
                {
                    var release = updateChecker.GetLatestRelease();
                    var asset = release.GetMatchingAsset(Assembly.GetEntryAssembly().GetName().Name);

                    if (!string.IsNullOrEmpty(asset?.Url))
                    {
                        logMethod($"Update Check: UPDATE AVAILABLE: {Assembly.GetEntryAssembly().GetName().Name} {release.TagName}");
                        logMethod($"Download at: {asset.Url}");
                        return;
                    }
                }
                logMethod($"Update Check: No update available");
            }
            catch (Exception ex)
            {
                // Don't cause problems if update checker failed. Just report it.
                logMethod($"Error checking for update: {ex.Message}");
            }
        }
    }
}
