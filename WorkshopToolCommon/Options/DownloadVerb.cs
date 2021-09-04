using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.WorkshopTool.Options
{
    [Verb("get", HelpText = "Download workshop items")]
    public class DownloadVerb : OptionBase
    {
        [Option("ids", Required = true, Min = 1, HelpText = "Workshop IDs of items or collections to download")]
        public IEnumerable<ulong> Ids { get; set; }

        [Option('E', "no-extract", Default = false, HelpText = "Don't automatically extract downloaded workshop items to AppData.")]
        public bool NoExtract { get; set; }
    }
}
