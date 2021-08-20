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

        IList<ulong> Ids { get; set; }
        public bool Extract { get; set; }

        // Upload properties
        public bool Upload => Type == typeof(UploadVerb);
        public bool UpdateOnly { get; set; }
        public bool DryRun { get; set; }
        public bool Compile { get; set; }
        public IList<string> Collections { get; set; }
        public IList<string> ModPaths { get; set; }
        public IList<string> Blueprints { get; set; }
        public IList<string> Scenarios { get; set; }
        public IList<string> Worlds { get; set; }
        public IList<string> IngameScripts { get; set; }
        public IList<string> ExcludeExtensions { get; set; }
        public IList<string> IgnorePaths { get; set; }
        public IList<string> Tags { get; set; }
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

        // TODO
        public bool ListDLCs { get; set; }


        public ProcessedOptions(DownloadVerb options) 
            : this((OptionBase) options)
        {
            Ids = options.Ids.ToList();
            Extract = options.Extract;
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

            ModPaths = options.ModPaths?.ToList();
            Blueprints = options.Blueprints?.ToList();
            Scenarios = options.Scenarios?.ToList();
            IngameScripts = options.IngameScripts?.ToList();

            ExcludeExtensions = options.ExcludeExtensions?.ToList();
            IgnorePaths = options.IgnorePaths?.ToList();
            Tags = options.Tags?.ToList();
            DLCs = options.DLCs?.ToList();
            Dependencies = options.Dependencies?.ToList();
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

            ModPaths = options.ModPaths?.ToList();
            Blueprints = options.Blueprints?.ToList();
            Scenarios = options.Scenarios?.ToList();
            IngameScripts = options.IngameScripts?.ToList();

            ExcludeExtensions = options.ExcludeExtensions?.ToList();
            IgnorePaths = options.IgnorePaths?.ToList();
            Tags = options.Tags?.ToList();
            DLCs = options.DLCs?.ToList();
            Dependencies = options.Dependencies?.ToList();
            Collections = options.Collections?.ToList();
            ListDLCs = options.ListDLCs;
            if (options.ClearSteamCloud && Force)
                Clear = options.ClearSteamCloud;
            else
                ListCloud = true;
            Files = options.DeleteSteamCloudFiles?.ToList();
        }

        public static explicit operator ProcessedOptions(DownloadVerb options) => new ProcessedOptions(options);
        public static explicit operator ProcessedOptions(UploadVerb options) => new ProcessedOptions(options);
        public static explicit operator ProcessedOptions(CloudVerb options) => new ProcessedOptions(options);
        public static explicit operator ProcessedOptions(LegacyOptions options) => new ProcessedOptions(options);

        public static explicit operator DownloadVerb(ProcessedOptions options)
        {
            var result = new DownloadVerb();
            result.AppData = options.AppData;
            result.Force = options.Force;
            result.ModIO = options.ModIO;
            result.Extract = options.Extract;
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
            result.ModPaths = options.ModPaths;
            result.Scenarios = options.Scenarios;
            result.Tags = options.Tags;
            result.Worlds = options.Worlds;
            return result;
        }
    }
}
