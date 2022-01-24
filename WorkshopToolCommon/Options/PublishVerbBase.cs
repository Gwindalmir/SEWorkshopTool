using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phoenix.WorkshopTool.Options
{
    public abstract class PublishVerbBase : UGCOptionBase
    {
        [Option('d', "dry-run", Default = false, HelpText = "Only run a test, do not actually upload")]
        public bool DryRun { get; set; }
        [Option('d', "dryrun", Default = false, HelpText = "Only run a test, do not actually upload", Hidden = true)]
        public bool Dry_Run { get => DryRun; set => DryRun = DryRun || value; }

        [Option("description", HelpText = "File containing the description to set for workshop item", MetaValue = "<filename>")]
        public string DescriptionFile { get; set; }

        [Option("visibility", Default = null, HelpText = "Sets mod visibility (defaults: current for existing, Private for new)")]
        public PublishedFileVisibility? Visibility { get; set; }

        [Option("thumb", HelpText = "Thumbnail to upload (doesn't re-upload mod)", MetaValue = "<filename>")]
        public string Thumbnail { get; set; }

        [Option("dependencies", HelpText = "Specify dependencies to other mods. Use 0 to remove all.", MetaValue = "<id>")]
        public IEnumerable<string> Dependencies { get; set; }

        // Provide aliases for add-dependency and remove-dependency, as add-dependencies and remove-dependencies, respectively
        protected IList<string> _dependenciesToAdd = new List<string>();
        [Option("add-dependency", HelpText = "List of dependencies to add", Hidden = true)]
        public IEnumerable<string> DependencyToAdd { get => _dependenciesToAdd; set => value?.ForEach(t => _dependenciesToAdd.Add(t)); }
        [Option("add-dependencies", HelpText = "List of dependencies to add")]
        public IEnumerable<string> DependenciesToAdd { get => _dependenciesToAdd; set => value?.ForEach(t => _dependenciesToAdd.Add(t)); }

        protected IList<string> _dependenciesToRemove = new List<string>();
        [Option("remove-dependency", HelpText = "List of dependencies to remove", Hidden = true)]
        public IEnumerable<string> DependencyToRemove { get => _dependenciesToRemove; set => value?.ForEach(t => _dependenciesToRemove.Add(t)); }
        [Option("remove-dependencies", HelpText = "List of dependencies to remove")]
        public IEnumerable<string> DependenciesToRemove { get => _dependenciesToRemove; set => value?.ForEach(t => _dependenciesToRemove.Add(t)); }

        internal bool TagsSpecified => Tags != null || TagsToAdd != null || TagsToRemove != null;

        [Option("tags", HelpText = "List of workshop mod categories/tags to use (overwrites previous, default: keep)")]
        public IEnumerable<string> Tags { get; set; }

        // Provide aliases for add-tag and remove-tag, as add-tags and remove-tags, respectively
        protected IList<string> _tagsToAdd = new List<string>();
        [Option("add-tag", HelpText = "List of workshop categories/tags to add", Hidden = true)]
        public IEnumerable<string> TagToAdd { get => _tagsToAdd; set => value?.ForEach(t => _tagsToAdd.Add(t)); }
        [Option("add-tags", HelpText = "List of workshop categories/tags to add")]
        public IEnumerable<string> TagsToAdd { get => _tagsToAdd; set => value?.ForEach(t => _tagsToAdd.Add(t)); }

        protected IList<string> _tagsToRemove = new List<string>();
        [Option("remove-tag", HelpText = "List of workshop categories/tags to remove", Hidden = true)]
        public IEnumerable<string> TagToRemove { get => _tagsToRemove; set => value?.ForEach(t => _tagsToRemove.Add(t)); }
        [Option("remove-tags", HelpText = "List of workshop categories/tags to remove")]
        public IEnumerable<string> TagsToRemove { get => _tagsToRemove; set => value?.ForEach(t => _tagsToRemove.Add(t)); }

#if SE
        [Option("dlc", HelpText = "Add DLC dependency to mod, accepts numeric ID or name. Use 0 or None to remove all.")]
#endif
        public IEnumerable<string> DLCs { get; set; }

#if SE
        [Option("add-dlc", HelpText = "Add DLC dependency to mod.")]
#endif
        public IEnumerable<string> DLCToAdd { get; set; }

#if SE
        [Option("remove-dlc", HelpText = "Remove DLC dependency from mod.")]
#endif
        public IEnumerable<string> DLCToRemove { get; set; }

        [Option("blueprints", Min = 1, Group = "workshop", HelpText = "List of folder names of blueprints")]
        public IEnumerable<string> Blueprints { get; set; }

        [Option("scenarios", Min = 1, Group = "workshop", HelpText = "List of folder names of scenarios")]
        public IEnumerable<string> Scenarios { get; set; }

        [Option("worlds", Min = 1, Group = "workshop", HelpText = "List of folder names of worlds")]
        public IEnumerable<string> Worlds { get; set; }
    }
}
