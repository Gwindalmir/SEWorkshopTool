using CommandLine;
using System.Collections.Generic;
using VRage.GameServices;

namespace Phoenix.WorkshopTool
{
    public sealed class LegacyOptions : Options.OptionBase
    {
        private const string OptionSet = "MainFunctions";

        [Option("visibility", Default = null, HelpText = "Sets mod visibility (for new only). Accepted values: Public, FriendsOnly, Private, Unlisted")]
        public PublishedFileVisibility? Visibility { get; set; }

        [Option("dev", Default = false, Hidden = true, HelpText = "This is obsolete and no longer functional.")]
        public bool Development { get; set; }

        [Option('c', "compile", Default = false, HelpText = "Compile the mod before uploading. Will not upload if compilation fails.")]
        public bool Compile { get; set; }

        [Option('d', "dry-run", Default = false, HelpText = "Only run a test, do not actually upload. Useful with --compile")]
        public bool DryRun { get; set; }

        [Option("download", Group = "main", HelpText = "Download mods")]
        public bool Download { get; set; }

        [Option("upload", Group = "main", HelpText = "Upload and publish mods")]
        public bool Upload { get; set; }

        [Option('e', "extract", Default = false, HelpText = "Extract downloaded mods (valid for download only)")]
        public bool Extract { get; set; }

        [Option('u', "update-only", Default = false, HelpText = "Only update existing mods (don't upload new)")]
        public bool UpdateOnly { get; set; }

        [Option('x', "exclude", HelpText = "List of extensions to exclude from upload")]
        public IEnumerable<string> ExcludeExtensions { get; set; }

        [Option("ignore", HelpText = "List of paths to exclude from upload")]
        public IEnumerable<string> IgnorePaths { get; set; }

        [Option('m', "mods", HelpText = "List of directories of mods to upload; or Workshop ID of mods to download (when in download mode), use quotes if spaces")]
        public IEnumerable<string> ModPaths { get; set; }

        [Option('b', "blueprints", HelpText = "List of directories of blueprints to upload; or Workshop ID of blueprints to download (when in download mode)")]
        public IEnumerable<string> Blueprints { get; set; }

        [Option('s', "scenarios", HelpText = "List of directories of scenarios to upload; or Workshop ID of scenarios to download (when in download mode)")]
        public IEnumerable<string> Scenarios { get; set; }

        [Option('w', "worlds", HelpText = "List of directories of worlds to upload; or Workshop ID of worlds to download (when in download mode)")]
        public IEnumerable<string> Worlds { get; set; }
#if SE
        [Option('i', "scripts", HelpText = "List of directories of scripts to upload; or Workshop ID of scripts to download (when in download mode)")]
#endif
        public IEnumerable<string> IngameScripts { get; set; }

        [Option('t', "tags", HelpText = "List of workshop mod categories/tags to use (removes previous, default is keep existing)")]
        public IEnumerable<string> Tags { get; set; }

        [Option("collections", HelpText = "List of Workshop IDs of collections to download")]
        public IEnumerable<ulong> Collections { get; set; }

        [Option("thumb", HelpText = "Thumbnail to upload (doesn't re-upload mod)")]
        public string Thumbnail { get; set; }

        [Option("clearsteamcloud", Group = "main", HelpText = "Clear Steam Cloud (WARNING!). THIS WILL DELETE YOUR STEAM CLOUD FOR SE! Use with --force to actually delete.")]
        public bool ClearSteamCloud { get; set; }

        [Option("deletecloudfile", HelpText = "Delete individual file or files from the Steam Cloud")]
        public IEnumerable<string> DeleteSteamCloudFiles { get; set; }
#if SE
        [Option("listdlc", Group = "main", HelpText = "List available DLCs")]
#endif
        public bool ListDLCs { get; set; }
#if SE
        [Option("dlc", Group = "main", HelpText = "Add DLC dependency to mod, accepts numeric ID or name. Use 0 or None to remove all DLC.")]
#endif
        public IEnumerable<string> DLCs { get; set; }

        [Option("dependencies", HelpText = "Specify dependencies to other mods. Use 0 to remove all.")]
        public IEnumerable<string> Dependencies { get; set; }

        [Option("description", HelpText = "File containing the description to set for workshop item")]
        public string DescriptionFile { get; set; }

        [Option("message", HelpText = "Changelog message (requires actual content update)")]
        public string Changelog { get; set; }
    }
}
