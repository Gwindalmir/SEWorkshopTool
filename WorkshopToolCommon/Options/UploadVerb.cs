using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.WorkshopTool.Options
{
    // TODO: Verb aliases, coming in CommandLineParser 2.9.0
    [Verb("publish", Hidden = true, HelpText = "Upload/publish workshop items")]
    public class PublishVerb : UploadVerb
    { }

    [Verb("upload", HelpText = "Upload/publish workshop items")]
    public class UploadVerb : UGCOptionBase
    {
        [Option('u', "update-only", Default = false, HelpText = "Only update existing mods (don't upload new)")]
        public bool UpdateOnly { get; set; }

        [Option('d', "dry-run", Default = false, HelpText = "Only run a test, do not actually upload")]
        public bool DryRun { get; set; }

        private bool _compile;
        [Option('c', "compile", Required = false, Default = true, SetName = "compile", HelpText = "Compile the mod before uploading; Skip upload if compilation fails")]
        public bool Compile { get => _compile; set => _compile = value; }

        [Option('C', "no-compile", Required = false, SetName = "nocompile", HelpText = "Don't compile the mod before uploading")]
        public bool NoCompile { get => !_compile; set => _compile = !value; }

        [Option("exclude", HelpText = "List of extensions to exclude from upload (use .wtignore instead)")]
        public IEnumerable<string> ExcludeExtensions { get; set; }

        [Option("ignore", HelpText = "List of paths to exclude from upload (use .wtignore instead)")]
        public IEnumerable<string> IgnorePaths { get; set; }

        [Option("description", HelpText = "File containing the description to set for workshop item", MetaValue = "<filename>")]
        public string DescriptionFile { get; set; }

        [Option("message", HelpText = "Changelog message (requires actual content update)", MetaValue = "<text or filename>")]
        public string Changelog { get; set; }

        [Option("visibility", Default = null, HelpText = "Sets mod visibility (defaults: current for existing, Private for new)")]
        public PublishedFileVisibility? Visibility { get; set; }

        [Option("thumb", HelpText = "Thumbnail to upload (doesn't re-upload mod)", MetaValue = "<filename>")]
        public string Thumbnail { get; set; }

        [Option("dependencies", HelpText = "Specify dependencies to other mods (modids only). Use 0 to remove all.", MetaValue = "<id>[,<id>]")]
        public IEnumerable<ulong> Dependencies { get; set; }



        [Option("tags", HelpText = "List of workshop mod categories/tags to use (overwrites previous, default: keep)")]
        public IEnumerable<string> Tags { get; set; }

#if SE
        [Option("dlc", HelpText = "Add DLC dependency to mod, accepts numeric ID or name. Use 0 or None to remove all DLC.")]
#endif
        public IEnumerable<string> DLCs { get; set; }

        [Option("blueprints", Min = 1, Group = "workshop", HelpText = "List of folder names of blueprints")]
        public IEnumerable<string> Blueprints { get; set; }

        [Option("scenarios", Min = 1, Group = "workshop", HelpText = "List of folder names of scenarios")]
        public IEnumerable<string> Scenarios { get; set; }

        [Option("worlds", Min = 1, Group = "workshop", HelpText = "List of folder names of worlds")]
        public IEnumerable<string> Worlds { get; set; }
    }
}
