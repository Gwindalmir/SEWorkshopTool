﻿using System;
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
            get => _tagsToRemove;
            set
            {
                if (value != null)
                {
                    var tags = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
                    value.ForEach(s => s.Split(',', ';').ForEach(t => tags.Add(t)));
                    _tagsToRemove = tags.ToList();
                }
            }
        }

        IList<string> _dependencies;
        public IList<string> Dependencies
        {
            get => _dependencies;
            set
            {
                // If user comma-separated the dependencies, split them
                if (value != null)
                {
                    var dependencies = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
                    value.ForEach(s => s.Split(',', ';').ForEach(t => dependencies.Add(t)));
                    _dependencies = dependencies.ToList();
                }
            }
        }

        IList<string> _dependenciesToAdd;
        public IList<string> DependenciesToAdd
        {
            get => _dependenciesToAdd;
            set
            {
                if (value != null)
                {
                    var dependencies = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
                    value.ForEach(s => s.Split(',', ';').ForEach(t => dependencies.Add(t)));
                    _dependenciesToAdd = dependencies.ToList();
                }
            }
        }

        IList<string> _dependenciesToRemove;
        public IList<string> DependenciesToRemove
        {
            get => _dependenciesToRemove;
            set
            {
                if (value != null)
                {
                    var dependencies = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
                    value.ForEach(s => s.Split(',', ';').ForEach(t => dependencies.Add(t)));
                    _dependenciesToRemove = dependencies.ToList();
                }
            }
        }

        public IList<string> DiscordWebhookUrls { get; set; }
        public IList<string> DLCs { get; set; }
        public IList<string> DLCToAdd { get; set; }
        public IList<string> DLCToRemove { get; set; }

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
            : this((PublishVerbBase)options)
        {
            UpdateOnly = options.UpdateOnly;
            Compile = options.Compile;
            Changelog = options.Changelog;

            ExcludeExtensions = options.ExcludeExtensions?.ToList();
            IgnorePaths = options.IgnorePaths?.ToList();
            DiscordWebhookUrls = options.DiscordWebhookUrls.ToList();
        }

        public ProcessedOptions(ChangeVerb options)
            : this((PublishVerbBase)options)
        {
        }

        public ProcessedOptions(CompileVerb options)
            : this((UGCOptionBase)options)
        {
            Compile = true;
        }

        public ProcessedOptions(CloudVerb options)
            : this((OptionBase)options)
        {
            ListCloud = options.List;
            Files = options.Files;
            Clear = options.Clear;
        }

        public ProcessedOptions(PublishVerbBase options)
            : this((UGCOptionBase)options)
        {
            DryRun = options.DryRun;
            Thumbnail = options.Thumbnail;
            DescriptionFile = options.DescriptionFile;
            Visibility = options.Visibility;

            Blueprints = options.Blueprints?.ToList();
            Scenarios = options.Scenarios?.ToList();

            Tags = options.Tags?.ToList();
            TagsToAdd = options.TagsToAdd?.ToList();
            TagsToRemove = options.TagsToRemove?.ToList();
            DLCs = options.DLCs?.ToList();
            DLCToAdd = options.DLCToAdd?.ToList();
            DLCToRemove = options.DLCToRemove?.ToList();
            Dependencies = options.Dependencies?.ToList();
            DependenciesToAdd = options.DependenciesToAdd?.ToList();
            DependenciesToRemove = options.DependenciesToRemove?.ToList();
        }

        public ProcessedOptions(UGCOptionBase options)
            : this((OptionBase)options)
        {
            Mods = options.Mods?.ToList();
            IngameScripts = options.IngameScripts?.ToList();
        }

        public ProcessedOptions(OptionBase options)
        {
            Type = options.GetType();
            ModIO = options.ModIO;
            AppData = Environment.ExpandEnvironmentVariables(options.AppData);
            Force = options.Force;
        }

        public ProcessedOptions(LegacyOptions options)
        {
            if (options.Download)
                Type = typeof(DownloadVerb);
            else if (options.UpdateOnly)    // ORDER MATTERS! UpdateOnly must come before Upload
                Type = typeof(ChangeVerb);
            else if (options.Upload)
                Type = typeof(UploadVerb);
            else if (options.ClearSteamCloud || options.DeleteSteamCloudFiles?.Count() > 0)
                Type = typeof(CloudVerb);

            ModIO = options.ModIO;
            AppData = Environment.ExpandEnvironmentVariables(options.AppData);
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

            Blueprints?.ForEach(s =>
            {
                ulong val;
                if (ulong.TryParse(s, out val))
                    allids.Add(val);
            });
            Scenarios?.ForEach(s =>
            {
                ulong val;
                if (ulong.TryParse(s, out val))
                    allids.Add(val);
            });
            IngameScripts?.ForEach(s =>
            {
                ulong val;
                if (ulong.TryParse(s, out val))
                    allids.Add(val);
            });
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
        public static implicit operator ProcessedOptions(ChangeVerb options) => new ProcessedOptions(options);
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
            result.DiscordWebhookUrls = options.DiscordWebhookUrls;
            result.Thumbnail = options.Thumbnail;
            result.UpdateOnly = options.UpdateOnly;
            result.Visibility = options.Visibility;
            result.Blueprints = options.Blueprints;
            result.Dependencies = options.Dependencies;
            result.DependenciesToAdd = options.DependenciesToAdd;
            result.DependenciesToRemove = options.DependenciesToRemove;
            result.DLCs = options.DLCs;
            result.DLCToAdd = options.DLCToAdd;
            result.DLCToRemove = options.DLCToRemove;
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

        public static explicit operator ChangeVerb(ProcessedOptions options)
        {
            var result = new ChangeVerb();
            result.AppData = options.AppData;
            result.Force = options.Force;
            result.ModIO = options.ModIO;
            result.DescriptionFile = options.Changelog;
            result.DryRun = options.DryRun;
            result.Thumbnail = options.Thumbnail;
            result.Visibility = options.Visibility;
            result.Blueprints = options.Blueprints;
            result.Dependencies = options.Dependencies;
            result.DependenciesToAdd = options.DependenciesToAdd;
            result.DependenciesToRemove = options.DependenciesToRemove;
            result.DLCs = options.DLCs;
            result.DLCToAdd = options.DLCToAdd;
            result.DLCToRemove = options.DLCToRemove;
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
