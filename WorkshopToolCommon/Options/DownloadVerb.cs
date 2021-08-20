using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.WorkshopTool.Options
{
    public class DownloadVerb : UGCOptionBase
    {
        [Option("ids", Min = 1, SetName = "mod", HelpText = "Workshop IDs of items or collections to download")]
        public IEnumerable<ulong> Ids { get; set; }

        [Option('E', "no-extract", Default = false, HelpText = "Don't automatically extract downloaded workshop items to AppData.")]
        public bool Extract { get; set; }

    }
}
