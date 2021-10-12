using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.WorkshopTool.Options
{
    public abstract class OptionBase
    {
#if SE
        [Option("modio", HelpText = "Use mod.io by default")]
#endif
        public bool ModIO { get; set; } = false;

#if SE
        [Option("appdata", Default = "%AppData%\\SpaceEngineers", HelpText = "Specify custom AppData location")]
#else
        [Option("appdata", Default = "%AppData%\\MedievalEngineers", HelpText = "Specify custom AppData location")]
#endif
        public string AppData { get; set; }

        [Option('f', "force", Default = false, HelpText = "Force operation. USE WITH CAUTION! (not valid everywhere)")]
        public bool Force { get; set; }
    }
}
