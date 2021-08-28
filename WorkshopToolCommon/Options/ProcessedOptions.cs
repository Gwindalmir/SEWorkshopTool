using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phoenix.WorkshopTool.Options
{
    public class ProcessedOptions
    {
        public Type Type { get; }

        // Common properties
        public bool ModIO { get; set; }
        public string AppData { get; set; }
        public bool Force { get; set; }

        // Download properties
        public bool Download => Type == typeof(DownloadVerb);
        public bool Extract { get; set; }
        public IList<ulong> Ids { get; set; }
        public IList<ulong> Collections { get; set; }

        // Upload properties
        public bool Upload => Type == typeof(UploadVerb);
        public bool UpdateOnly { get; set; }
        public bool DryRun { get; set; }
        public bool Compile { get; set; }
        public IList<string> Mods { get; set; }
        public IList<string> Blueprints { get; set; }
        public IList<string> Scenarios { get; set; }
        public IList<string> Worlds { get; set; }
        public IList<string> IngameScripts { get; set; }
        public IList<string> ExcludeExtensions { get; set; }
        public IList<string> IgnorePaths { get; set; }

        IList<string> _tags;
        public IList<string> Tags
        {
            get => _tags;
            set
            {
                // If user comma-separated the tags, split them
                if (value != null)
                {
                    var tags = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
                    value.ForEach(s => s.Split(',', ';').ForEach(t => tags.Add(t)));
                    _tags = tags.ToList();
                }
            }
        }

        IList<string> _tagsToAdd;
        public IList<string> TagsToAdd
        {
            get => _tagsToAdd;
            set
            {
                if (value != null)
                {
                    var tags = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
                    value.ForEach(s => s.Split(',', ';').ForEach(t => tags.Add(t)));
                    _tagsToAdd = tags.ToList();
                }
            }
        }

        IList<string> _tagsToRemove;
        public IList<string> TagsToRemove
        {
            get => _tagsToAdd;
            set
            {
                if (value != null)
                {
                    var tags = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
                    value.ForEach(s => s.Split(',', ';').ForEach(t => tags.Add(t)));
                    _tagsToAdd = tags.ToList();
                }
            }
        }

        public IList<string> DLCs { get; set; }
        public IList<ulong> Dependencies { get; set; }
        public string Thumbnail { get; set; }
        public string DescriptionFile { get; set; }
        public string Changelog { get; set; }
        public PublishedFileVisibility? Visibility { get; set; }

        // Cloud
        public bool ListCloud { get; set; }
        public bool Clear { get; set; }
        public IEnumerable<string> Files { get; set; }

        // Currently relies on LegacyOptions
        public bool ListDLCs { get; set; }

        public ProcessedOptions(DownloadVerb options) 
            : this((OptionBase) options)
        {
            Ids = options.Ids.ToList();
            Extract = !options.NoExtract;
        }

        public ProcessedOptions(UploadVerb options)
            : this((OptionBase)options)
        {
            UpdateOnly = options.UpdateOnly;
            DryRun = options.DryRun;
            Compile = options.Compile;
            Thumbnail = options.Thumbnail;
            DescriptionFile = options.DescriptionFile;
            Changelog = options.Changelog;
            Visibility = options.Visibility;

            Mods = options.Mods?.ToList();
            Blueprints = options.Blueprints?.ToList();
            Scenarios = options.Scenarios?.ToList();
            IngameScripts = options.IngameScripts?.ToList();

            ExcludeExtensions = options.ExcludeExtensions?.ToList();
            IgnorePaths = options.IgnorePaths?.ToList();
            Tags = options.Tags?.ToList();
            TagsToAdd = options.TagsToAdd?.ToList();
            TagsToRemove = options.TagsToRemove?.ToList();
            DLCs = options.DLCs?.ToList();
            Dependencies = options.Dependencies?.ToList();
        }

        public ProcessedOptions(CompileVerb options)
            : this((OptionBase)options)
        {
            Compile = true;
            Mods = options.Mods?.ToList();
            IngameScripts = options.IngameScripts?.ToList();
        }

        public ProcessedOptions(CloudVerb options)
            : this((OptionBase)options)
        {
            ListCloud = options.List;
            Files = options.Files;
            Clear = options.Clear;
        }

        public ProcessedOptions(OptionBase options)
        {
            Type = options.GetType();
            ModIO = options.ModIO;
            AppData = options.AppData;
            Force = options.Force;
        }

        public ProcessedOptions(LegacyOptions options)
        {
            if (options.Download)
                Type = typeof(DownloadVerb);
            else if (options.Upload)
                Type = typeof(UploadVerb);
            else if (options.ClearSteamCloud || options.DeleteSteamCloudFiles?.Count() > 0)
                Type = typeof(CloudVerb);

            ModIO = options.ModIO;
            AppData = options.AppData;
            Force = options.Force;

            UpdateOnly = options.UpdateOnly;
            DryRun = options.DryRun;
            Compile = options.Compile;
            Thumbnail = options.Thumbnail;
            DescriptionFile = options.DescriptionFile;
            Changelog = options.Changelog;
            Visibility = options.Visibility;

            Mods = options.ModPaths?.ToList();
            Blueprints = options.Blueprints?.ToList();
            Scenarios = options.Scenarios?.ToList();
            IngameScripts = options.IngameScripts?.ToList();

            var allids = new HashSet<ulong>();
            Mods?.ForEach(s =>
            {
                ulong val;
                if (ulong.TryParse(s, out val))
                    allids.Add(val);
            });

            Blueprints?.ForEach(s => allids.Add(ulong.Parse(s)));
            Scenarios?.ForEach(s => allids.Add(ulong.Parse(s)));
            IngameScripts?.ForEach(s => allids.Add(ulong.Parse(s)));
            Ids = allids.ToList();

            Extract = options.Extract;
            ExcludeExtensions = options.ExcludeExtensions?.ToList();
            IgnorePaths = options.IgnorePaths?.ToList();
            Tags = options.Tags?.ToList();
            DLCs = options.DLCs?.ToList();
            Dependencies = options.Dependencies?.ToList();
            Collections = options.Collections?.ToList();
            ListDLCs = options.ListDLCs;
            if (options.ClearSteamCloud)
            {
                if (Force || options.DeleteSteamCloudFiles?.Count() > 0)
                    Clear = options.ClearSteamCloud;
                else
                    ListCloud = true;
            }
            Files = options.DeleteSteamCloudFiles?.ToList();
        }

        public static implicit operator ProcessedOptions(DownloadVerb options) => new ProcessedOptions(options);
        public static implicit operator ProcessedOptions(UploadVerb options) => new ProcessedOptions(options);
        public static implicit operator ProcessedOptions(CompileVerb options) => new ProcessedOptions(options);
        public static implicit operator ProcessedOptions(CloudVerb options) => new ProcessedOptions(options);
        public static implicit operator ProcessedOptions(LegacyOptions options) => new ProcessedOptions(options);

        public static explicit operator DownloadVerb(ProcessedOptions options)
        {
            var result = new DownloadVerb();
            result.AppData = options.AppData;
            result.Force = options.Force;
            result.ModIO = options.ModIO;
            result.NoExtract = !options.Extract;
            result.Ids = options.Ids;
            return result;
        }

        public static explicit operator CloudVerb(ProcessedOptions options)
        {
            var result = new CloudVerb();
            result.AppData = options.AppData;
            result.Force = options.Force;
            result.ModIO = options.ModIO;
            result.Clear = options.Clear;
            result.List = options.ListCloud;
            result.Files = options.Files;
            return result;
        }

        public static explicit operator UploadVerb(ProcessedOptions options)
        {
            var result = new UploadVerb();
            result.AppData = options.AppData;
            result.Force = options.Force;
            result.ModIO = options.ModIO;
            result.Changelog = options.Changelog;
            result.DescriptionFile = options.Changelog;
            result.Compile = options.Compile;
            result.DryRun = options.DryRun;
            result.Thumbnail = options.Thumbnail;
            result.UpdateOnly = options.UpdateOnly;
            result.Visibility = options.Visibility;
            result.Blueprints = options.Blueprints;
            result.Dependencies = options.Dependencies;
            result.DLCs = options.DLCs;
            result.ExcludeExtensions = options.ExcludeExtensions;
            result.IgnorePaths = options.IgnorePaths;
            result.IngameScripts = options.IngameScripts;
            result.Mods = options.Mods;
            result.Scenarios = options.Scenarios;
            result.Tags = options.Tags;
            result.TagsToAdd = options.TagsToAdd;
            result.TagsToRemove = options.TagsToRemove;

            result.Worlds = options.Worlds;
            return result;
        }

        public static explicit operator CompileVerb(ProcessedOptions options)
        {
            var result = new CompileVerb();
            result.AppData = options.AppData;
            result.Force = options.Force;
            result.ModIO = options.ModIO;
            result.IngameScripts = options.IngameScripts;
            result.Mods = options.Mods;
            return result;
        }
    }
}
