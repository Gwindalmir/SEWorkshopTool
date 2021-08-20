using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.WorkshopTool.Options
{
    // TODO: Verb aliases, coming in CommandLineParser 2.9.0
    [Verb("publish", true, HelpText = "Upload and publish mods")]
    public class PublishVerb : UploadVerb
    { }

    [Verb("upload", true, HelpText = "Upload and publish mods")]
    public class UploadVerb : UGCOptionBase
    {
        [Option('u', "update-only", Default = false, HelpText = "Only update existing mods (don't upload new)")]
        public bool UpdateOnly { get; set; }

        [Option('d', "dry-run", Default = false, HelpText = "Only run a test, do not actually upload. Useful with --compile")]
        public bool DryRun { get; set; }

        private bool _compile;
        [Option('c', "compile", Required = false, Default = true, SetName = "compile", HelpText = "Compile the mod before uploading. Will not upload if compilation fails.")]
        public bool Compile { get => _compile; set => _compile = value; }

        [Option('C', "no-compile", Required = false, SetName = "compile", HelpText = "Don't compile the mod before uploading. Will upload even if compilation would have failed.")]
        public bool NoCompile { get => !_compile; set => _compile = !value; }

        [Option("mods", SetName = "mod", HelpText = "List of directories of mods to upload; or Workshop ID of mods to download (when in download mode), use quotes if spaces")]
        public IEnumerable<string> ModPaths { get; set; }

        [Option("blueprints", SetName = "blueprint", HelpText = "List of directories of blueprints to upload; or Workshop ID of blueprints to download (when in download mode)")]
        public IEnumerable<string> Blueprints { get; set; }

        [Option("scenarios", SetName = "scenario", HelpText = "List of directories of scenarios to upload; or Workshop ID of scenarios to download (when in download mode)")]
        public IEnumerable<string> Scenarios { get; set; }

        [Option("worlds", SetName = "world", HelpText = "List of directories of worlds to upload; or Workshop ID of worlds to download (when in download mode)")]
        public IEnumerable<string> Worlds { get; set; }
#if SE
        [Option("scripts", SetName = "script", HelpText = "List of directories of scripts to upload; or Workshop ID of scripts to download (when in download mode)")]
#endif
        public IEnumerable<string> IngameScripts { get; set; }

        [Option('x', "exclude", HelpText = "List of extensions to exclude from upload")]
        public IEnumerable<string> ExcludeExtensions { get; set; }

        [Option("ignore", HelpText = "List of paths to exclude from upload")]
        public IEnumerable<string> IgnorePaths { get; set; }

        [Option('t', "tags", HelpText = "List of workshop mod categories/tags to use (removes previous, default is keep existing)")]
        public IEnumerable<string> Tags { get; set; }

        [Option("thumb", HelpText = "Thumbnail to upload (doesn't re-upload mod)")]
        public string Thumbnail { get; set; }

        [Option("dependencies", HelpText = "Specify dependencies to other mods (modids only). Use 0 to remove all.")]
        public IEnumerable<ulong> Dependencies { get; set; }

        [Option("description", HelpText = "File containing the description to set for workshop item")]
        public string DescriptionFile { get; set; }

        [Option("message", HelpText = "Changelog message (requires actual content update)")]
        public string Changelog { get; set; }

        [Option("visibility", Default = null, HelpText = "Sets mod visibility (for new only). Accepted values: Public, FriendsOnly, Private, Unlisted")]
        public PublishedFileVisibility? Visibility { get; set; }

#if SE
        [Option("dlc", HelpText = "Add DLC dependency to mod, accepts numeric ID or name. Use 0 or None to remove all DLC.")]
#endif
        public IEnumerable<string> DLCs { get; set; }
    }
}
