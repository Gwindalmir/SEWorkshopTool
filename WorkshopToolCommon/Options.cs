using CommandLine;
using VRage.GameServices;

namespace Phoenix.WorkshopTool
{
    public sealed class Options
    {
        private const string OptionSet = "MainFunctions";

        [Option("visibility", DefaultValue = null, HelpText = "Sets mod visibility (for new only). Accepted values: Public, FriendsOnly, Private")]
        public MyPublishedFileVisibility? Visibility { get; set; }

        [Option("dev", DefaultValue = false, HelpText = "Set to true if the mod will have the 'development' tag when uploaded")]
        public bool Development { get; set; }

        [Option('c', "compile", DefaultValue = false, HelpText = "Compile the mod before uploading. Will not upload if compilation fails.")]
        public bool Compile { get; set; }

        [Option('d', "dry-run", DefaultValue = false, HelpText = "Only run a test, do not actually upload. Useful with --compile")]
        public bool DryRun { get; set; }

        [Option("download", DefaultValue = false, HelpText = "Download mods", MutuallyExclusiveSet = OptionSet)]
        public bool Download { get; set; }

        [Option("upload", DefaultValue = false, HelpText = "Upload and publish mods", MutuallyExclusiveSet = OptionSet)]
        public bool Upload { get; set; }

        [Option('e', "extract", DefaultValue = false, HelpText = "Extract downloaded mods (valid for download only)")]
        public bool Extract { get; set; }

        [Option('u', "update-only", DefaultValue = false, HelpText = "Only update existing mods (don't upload new)")]
        public bool UpdateOnly { get; set; }

        [OptionArray('x', "exclude", HelpText = "List of extensions to exclude from upload")]
        public string[] ExcludeExtensions { get; set; }

        [OptionArray("ignore", HelpText = "List of paths to exclude from upload")]
        public string[] IgnorePaths { get; set; }

        [Option('f', "force", DefaultValue = false, HelpText = "Force operation. USE WITH CAUTION! (not valid everywhere)")]
        public bool Force { get; set; }

        [OptionArray('m', "mods", HelpText = "List of directories of mods to upload; or Workshop ID of mods to download (when in download mode), use quotes if spaces")]
        public string[] ModPaths { get; set; }

        [OptionArray('b', "blueprints", HelpText = "List of directories of blueprints to upload; or Workshop ID of blueprints to download (when in download mode)")]
        public string[] Blueprints { get; set; }

        [OptionArray('s', "scenarios", HelpText = "List of directories of scenarios to upload; or Workshop ID of scenarios to download (when in download mode)")]
        public string[] Scenarios { get; set; }

        [OptionArray('w', "worlds", HelpText = "List of directories of worlds to upload; or Workshop ID of worlds to download (when in download mode)")]
        public string[] Worlds { get; set; }

#if SE
        [OptionArray('i', "scripts", HelpText = "List of directories of scripts to upload; or Workshop ID of scripts to download (when in download mode)")]
        public string[] IngameScripts { get; set; }
#endif
        [OptionArray('t', "tags", HelpText = "List of workshop mod categories/tags to use (removes previous, default is keep existing)")]
        public string[] Tags { get; set; }

        [OptionArray("collections", HelpText = "List of Workshop IDs of collections to download")]
        public string[] Collections { get; set; }

        [Option("thumb", HelpText = "Thumbnail to upload (doesn't re-upload mod)")]
        public string Thumbnail { get; set; }

        [Option("clearsteamcloud", DefaultValue = false, HelpText = "Clear Steam Cloud (WARNING!). THIS WILL DELETE YOUR STEAM CLOUD FOR SE! Use with --force to actually delete.", MutuallyExclusiveSet = OptionSet)]
        public bool ClearSteamCloud { get; set; }

        [OptionArray("deletecloudfile", HelpText = "Delete individual file or files from the Steam Cloud")]
        public string[] DeleteSteamCloudFiles { get; set; }

#if SE
        [Option("listdlc", HelpText = "List available DLCs", MutuallyExclusiveSet = OptionSet)]
#endif
        public bool ListDLCs { get; set; }

#if SE
        [OptionArray("dlc", HelpText = "Add DLC dependency to mod, accepts numeric ID or name. Use 0 or None to remove all DLC.")]
#endif
        public string[] DLCs { get; set; }

#if false
        [Option("modio", HelpText = "Use mod.io instead of Steam.")]
#endif
        public bool ModIO { get; set; } = false;

        [OptionArray("dependencies", HelpText = "Specify dependencies to other mods (modids only). Use 0 to remove all.")]
        public ulong[] Dependencies { get; set; }

        [Option("appdata", HelpText = "Specify custom AppData location (default is %AppData%\\SpaceEngineers)")]
        public string AppData { get; set; }
    }
}
