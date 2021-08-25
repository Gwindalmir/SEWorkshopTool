using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.WorkshopTool.Options
{
    public abstract class UGCOptionBase : OptionBase
    {
        [Option("mods", Group = "workshop", HelpText = "List of folder names of mods, use quotes if spaces")]
        public IEnumerable<string> ModPaths { get; set; }
#if SE
        [Option("scripts", Group = "workshop", HelpText = "List of folder names of scripts")]
#endif
        public IEnumerable<string> IngameScripts { get; set; }
    }
}
