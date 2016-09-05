using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEBatchModTool
{
    public sealed class Options
    {
        private SteamSDK.PublishedFileVisibility m_visibility = SteamSDK.PublishedFileVisibility.Public;
        public SteamSDK.PublishedFileVisibility Visibility { get { return m_visibility; } }

        [Option("visibility", DefaultValue = "Public", HelpText = "Sets mod visibility (for new only). Accepted values: Public, FriendsOnly, Private")]
        public string VisibilityString
        {
            get { return Visibility.ToString(); }
            set
            {
                if(!Enum.TryParse<SteamSDK.PublishedFileVisibility>(value, out m_visibility))
                {
                    throw new ArgumentOutOfRangeException("Visibility must be one of: Public, FriendsOnly, Private");
                }
            }
        }

        [Option("dev", DefaultValue = false, HelpText = "Set to true if the mod will have the 'development' tag when uploaded")]
        public bool Development { get; set; }

        [Option('c', "compile", DefaultValue = false, HelpText = "Compile the mod before uploading. Will not upload if compilation fails.")]
        public bool Compile { get; set; }

        [Option('d', "dry-run", DefaultValue = false, HelpText = "Only run a test, do not actually upload. Useful with --compile")]
        public bool DryRun { get; set; }

        [OptionArray('m', "mods", HelpText = "List of directories of mods to upload", Required = true)]
        public string[] ModPaths { get; set; } 
    }
}
