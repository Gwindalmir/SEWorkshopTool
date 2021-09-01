using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.WorkshopTool.Options
{
    [Verb("change", HelpText = "Push metadata changes to workshop items (tags, thumbnail, etc.)")]
    public class ChangeVerb : PublishVerbBase
    { }
}
