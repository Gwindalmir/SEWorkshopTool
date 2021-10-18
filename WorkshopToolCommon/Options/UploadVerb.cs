using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phoenix.WorkshopTool.Options
{
    // TODO: Verb aliases, coming in CommandLineParser 2.9.0
    [Verb("upload", Hidden = true, HelpText = "Upload/publish workshop items")]
    public class PublishVerb : UploadVerb
    { }

    // Seemingly duplicate properties are option aliases
    [Verb("push", HelpText = "Upload/publish workshop items")]
    public class UploadVerb : PublishVerbBase
    {
        [Option('u', "update-only", Default = false, HelpText = "Only update existing mods (don't upload new)")]
        public bool UpdateOnly { get; set; }

        private bool _compile;
        [Option('c', "compile", Required = false, Default = true, SetName = "compile", HelpText = "Compile the mod before uploading; Skip upload if compilation fails")]
        public bool Compile { get => _compile; set => _compile = value; }

        [Option('C', "no-compile", Required = false, SetName = "nocompile", HelpText = "Don't compile the mod before uploading")]
        public bool NoCompile { get => !_compile; set => _compile = !value; }

        [Option("exclude-ext", HelpText = "List of extensions to exclude from upload (use .wtignore instead)")]
        public IEnumerable<string> ExcludeExtensions { get; set; }

        [Option("exclude-path", HelpText = "List of paths to exclude from upload (use .wtignore instead)")]
        public IEnumerable<string> IgnorePaths { get; set; }

        [Option("message", HelpText = "Changelog message (requires actual content update)", MetaValue = "<text or filename>")]
        public string Changelog { get; set; }

        [Option("discord-webhooks", HelpText = "A link to a webhook(s) that will publish update notes", MetaValue = "<url>")]
        public IEnumerable<string> DiscordWebhookUrls { get; set; }
    }
}
