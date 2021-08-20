using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.WorkshopTool.Options
{
    [Verb("cloud", isDefault: false, HelpText = "Interact with saved files on the Steam Cloud.")]
    public class CloudVerb : OptionBase
    {
        [Option('l', "list", Default = true, SetName = "list", HelpText = "List files stored in the cloud.")]
        public bool List { get; set; }

        [Option("clear", Default = false, SetName = "clear", HelpText = "Clear Steam Cloud (WARNING!). THIS WILL DELETE YOUR STEAM CLOUD FOR SE! Use with --force to actually delete.")]
        public bool Clear { get; set; }

        [Option("delete", SetName = "delete", HelpText = "Delete individual file or files from the Steam Cloud")]
        public IEnumerable<string> Files { get; set; }

    }
}
