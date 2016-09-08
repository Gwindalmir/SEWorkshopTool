using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEWorkshopTool
{
    public sealed class Options
    {
        [Option("visibility", DefaultValue = SteamSDK.PublishedFileVisibility.Public, HelpText = "Sets mod visibility (for new only). Accepted values: Public, FriendsOnly, Private")]
        public SteamSDK.PublishedFileVisibility Visibility { get; set; }

        [Option("dev", DefaultValue = false, HelpText = "Set to true if the mod will have the 'development' tag when uploaded")]
        public bool Development { get; set; }

        [Option('c', "compile", DefaultValue = false, HelpText = "Compile the mod before uploading. Will not upload if compilation fails.")]
        public bool Compile { get; set; }

        [Option('d', "dry-run", DefaultValue = false, HelpText = "Only run a test, do not actually upload. Useful with --compile")]
        public bool DryRun { get; set; }

        [Option("download", DefaultValue = false, HelpText = "Download mods")]
        public bool Download { get; set; }

        [Option('e', "extract", DefaultValue = false, HelpText = "Extract downloaded mods (valid for download only)")]
        public bool Extract { get; set; }

        [Option('u', "update-only", DefaultValue = false, HelpText = "Only update existing mods (don't upload new)")]
        public bool UpdateOnly { get; set; }

        // Disable for now
        //[Option('f', "force", DefaultValue = false, HelpText = "Force operation. USE WITH CAUTION! (not valid everywhere)")]
        public bool Force { get; set; }

        [OptionArray('m', "mods", HelpText = "List of directories of mods to upload; or Workshop ID of mods to download (when in download mode)", Required = true)]
        public string[] ModPaths { get; set; }

        [OptionArray('t', "tags", HelpText = "List of workshop mod categories/tags to use (removes previous, default is keep existing)")]
        public string[] Tags { get; set; }
    }
}
