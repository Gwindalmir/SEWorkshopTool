using Sandbox;
using Sandbox.Engine.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using VRage;
using VRage.Game;
using VRage.GameServices;
using VRage.Utils;
using System.Threading;
using VRage.Scripting;
using Phoenix.WorkshopTool.Extensions;
using System.Diagnostics;
#if SE
using MyDebug = Phoenix.WorkshopTool.Extensions.MyDebug;
#else
using WorkshopId = System.UInt64;
using TErrorSeverity = VRage.Scripting.ErrorSeverity;
using VRage.Session;
#endif

namespace Phoenix.WorkshopTool
{
    // The enum values must match the workshop tags used
    // Case is important here
    enum WorkshopType
    {
        Invalid,
        Mod,
        IngameScript,
        Blueprint,
        World,
        Scenario,
    }

    class Uploader : IMod
    {
        readonly HashSet<string> m_ignoredExtensions = new HashSet<string>();
        readonly HashSet<string> m_ignoredPaths = new HashSet<string>();
        uint[] m_dlcs;
        ulong[] m_deps;
        ulong[] m_depsToAdd;
        ulong[] m_depsToRemove;
        string m_modPath;
        bool m_compile;
        bool m_dryrun;
        string m_title;
        string m_description;
        string m_changelog;
        PublishedFileVisibility? m_visibility;
        WorkshopType m_type;
        string[] m_tags = new string[0];
        string[] m_tagsToAdd = new string[0];
        string[] m_tagsToRemove = new string[0];
        bool m_force;
        string m_previewFilename;

        private Dictionary<WorkshopId, MyWorkshopItem> m_workshopItems = new Dictionary<WorkshopId, MyWorkshopItem>();
        WorkshopId[] m_modId;
        public WorkshopId[] ModId { get { return m_modId; } }
        ulong IMod.ModId { get { return m_modId.GetIds().FirstOrDefault(); } }

        public string Title { get { return m_title; } }
        public string ModPath { get { return m_modPath; } }

        public Uploader(WorkshopType type, string path, Options.UploadVerb options, string description = null, string changelog = null)
        {
            m_modPath = path;

            if (ulong.TryParse(m_modPath, out ulong id))
                m_modId = WorkshopIdExtensions.ToWorkshopIds(new[] { id });
            else
                m_modId = WorkshopHelper.GetWorkshopIdFromMod(m_modPath);

            // Fill defaults before assigning user-defined ones
            FillPropertiesFromPublished();

            m_compile = options.Compile;
            m_dryrun = options.DryRun;

            if (options.Visibility != null)
                m_visibility = options.Visibility;

            if (string.IsNullOrEmpty(m_title))
                m_title = Path.GetFileName(path);

            m_description = description;
            m_changelog = changelog;

            m_type = type;
            m_force = options.Force;

            if(options.Thumbnail != null)
                m_previewFilename = options.Thumbnail;
            var mappedlc = MapDLCStringsToInts(options.DLCs);
            ProcessDLCs(mappedlc, MapDLCStringsToInts(options.DLCToAdd), MapDLCStringsToInts(options.DLCToRemove));

            if (options.Tags != null)
                m_tags = options.Tags.ToArray();
            if (options.TagsToAdd != null)
                m_tagsToAdd = options.TagsToAdd.ToArray();
            if (options.TagsToRemove != null)
                m_tagsToRemove = options.TagsToRemove.ToArray();

            ProcessTags();
            ProcessDependencies(options.Dependencies, options.DependenciesToAdd, options.DependenciesToRemove);

            // This file list should match the PublishXXXAsync methods in MyWorkshop
            switch (m_type)
            {
                case WorkshopType.Mod:
                    m_ignoredPaths.Add("modinfo.sbmi");
                    break;
                case WorkshopType.IngameScript:
                    break;
                case WorkshopType.World:
                    m_ignoredPaths.Add("Backup");
                    break;
                case WorkshopType.Blueprint:
                    break;
                case WorkshopType.Scenario:
                    break;
            }

            options.ExcludeExtensions?.Select(s => "." + s.TrimStart(new[] { '.', '*' })).ForEach(s => m_ignoredExtensions.Add(s));
            options.IgnorePaths?.ForEach(s => m_ignoredPaths.Add(s));

            // Start with the parent file, if it exists. This is at %AppData%\SpaceEngineers\Mods.
            if (IgnoreFile.TryLoadIgnoreFile(Path.Combine(m_modPath, "..", ".wtignore"), Path.GetFileName(m_modPath), out var extensionsToIgnore, out var pathsToIgnore))
            {
                extensionsToIgnore.ForEach(s => m_ignoredExtensions.Add(s));
                pathsToIgnore.ForEach(s => m_ignoredPaths.Add(s));
            }

            if (IgnoreFile.TryLoadIgnoreFile(Path.Combine(m_modPath, ".wtignore"), out extensionsToIgnore, out pathsToIgnore))
            {
                extensionsToIgnore.ForEach(s => m_ignoredExtensions.Add(s));
                pathsToIgnore.ForEach(s => m_ignoredPaths.Add(s));
            }
        }

        // This won't do anything for ME, but removed compilation conditionals to clean up code
        private uint[] MapDLCStringsToInts(IEnumerable<string> stringdlcs)
        {
            var dlcs = new HashSet<uint>();

            if (stringdlcs == null)
                return dlcs.ToArray();

            foreach (var dlc in stringdlcs)
            {
                uint value;
                if (uint.TryParse(dlc, out value))
                {
                    dlcs.Add(value);
                }
                else
                {
                    var dlcid = dlc.TryGetDLC();
                    if (dlcid > 0)
                        dlcs.Add(dlcid);
                    else
                    {
                        if (stringdlcs.Count() == 1 && dlc.Equals("none", StringComparison.InvariantCultureIgnoreCase))
                            dlcs.Add(0);
                        else
                            MySandboxGame.Log.WriteLineAndConsole($"Invalid DLC specified: {dlc}");
                    }
                }
            }
            return dlcs.ToArray();
        }

        /// <summary>
        /// Compiles the mod
        /// </summary>
        /// <returns></returns>
        public bool Compile()
        {
            // Compile
            if (m_compile)
            {
                if (m_type == WorkshopType.Mod)
                {
                    MySandboxGame.Log.WriteLineAndConsole("Compiling...");
                    var mod = WorkshopHelper.GetContext(m_modPath, m_workshopItems[m_modId[0]], m_modId, m_title);
                    if (WorkshopHelper.LoadScripts(m_modPath, mod))
                    {
                        // Process any errors
                        var errors = WorkshopHelper.GetErrors();

                        if (errors.Count > 0)
                        {
                            int errorCount = 0;
                            int warningCount = 0;

                            // This is not efficient, but I'm lazy
                            foreach (var error in errors)
                            {
                                if (error.Severity >= TErrorSeverity.Error)
                                    errorCount++;
                                if (error.Severity == TErrorSeverity.Warning)
                                    warningCount++;
                            }

                            if (errorCount > 0)
                                MySandboxGame.Log.WriteLineError(string.Format("There are {0} compile errors:", errorCount));
                            if (warningCount > 0)
                                MySandboxGame.Log.WriteLineWarning(string.Format("There are {0} compile warnings:", warningCount));

                            // Output raw message, which is usually in msbuild friendly format, for automated tools
                            foreach (var error in errors)
                            {
                                var color = error.Severity == TErrorSeverity.Warning ? ConsoleColor.Yellow : ConsoleColor.Red;
                                ProgramBase.ConsoleWriteColored(color, () =>
                                    System.Console.Error.WriteLine(error.GetErrorText()));
                            }

                            WorkshopHelper.ClearErrors();

                            if (errorCount > 0)
                            {
                                MySandboxGame.Log.WriteLineError("Compilation FAILED!");
                                return false;
                            }
                        }
                        MySandboxGame.Log.WriteLineAndConsole("Compilation successful!");
                    }
                }
#if SE
                else if(m_type == WorkshopType.IngameScript)
                {
                    // Load the ingame script from the disk
                    // I don't like this, but meh
                    var input = new StreamReader(Path.Combine(m_modPath, "Script.cs"));
                    var program = input.ReadToEnd();
                    input.Close();
                    var scripts = new List<Script>();
                    scripts.Add(MyScriptCompiler.Static.GetIngameScript(program, "Program", typeof(Sandbox.ModAPI.Ingame.MyGridProgram).Name, "sealed partial"));

                    var messages = new List<Message>();
                    var assembly = MyVRage.Platform.Scripting.CompileIngameScriptAsync(Path.Combine(VRage.FileSystem.MyFileSystem.UserDataPath, "SEWT-Script" + Path.GetFileName(m_modPath)), program, out messages, "SEWT Compiled PB Script", "Program", typeof(Sandbox.ModAPI.Ingame.MyGridProgram).Name).Result;

                    if (messages.Count > 0)
                    {
                        MySandboxGame.Log.WriteLineAndConsole(string.Format("There are {0} compile messages:", messages.Count));
                        int errors = 0;
                        foreach (var msg in messages)
                        {
                            var color = msg.IsError ? ConsoleColor.Red : ConsoleColor.Gray;
                            ProgramBase.ConsoleWriteColored(color, () => 
                                MySandboxGame.Log.WriteLineAndConsole(msg.Text));

                            if (msg.IsError)
                                errors++;
                        }
                        if (errors > 0)
                        {
                            return false;
                        }
                    }

                    if (assembly == null)
                        return false;
                }
#endif
                return true;
            }
            return true;
        }

        /// <summary>
        /// Publishes the mod to the workshop
        /// </summary>
        /// <returns></returns>
        public bool Publish()
        {
            bool newMod = false;

            if(!Directory.Exists(m_modPath))
            {
                MySandboxGame.Log.WriteLineWarning(string.Format("Directory does not exist {0}. Wrong option?", m_modPath ?? string.Empty));
                return false;
            }

            // Upload/Publish
            if(((IMod)this).ModId == 0)
            {
                MySandboxGame.Log.WriteLineAndConsole(string.Format("Uploading new {0}: {1}", m_type.ToString(), m_title));
                newMod = true;

                if (m_modId.Length == 0)
                    m_modId = WorkshopIdExtensions.ToWorkshopIds(new ulong[] { 0 });
            }
            else
            {
                MySandboxGame.Log.WriteLineAndConsole(string.Format("Updating {0}: {1}; {2}", m_type.ToString(), m_modId.AsString(), m_title));
            }

            // Add the global game filter for file extensions
            WorkshopHelper.IgnoredExtensions?.ForEach(s => m_ignoredExtensions.Add(s));

            PrintItemDetails();

            MyWorkshopItem[] items = null;

            if (m_dryrun)
            {
                MySandboxGame.Log.WriteLineAndConsole("DRY-RUN; Publish skipped");
                return true;
            }
            else
            {
                InjectedMethod.ChangeLog = m_changelog;
                if (WorkshopHelper.PublishItemBlocking(m_modPath, m_title, m_description, m_modId, (MyPublishedFileVisibility)(m_visibility ?? PublishedFileVisibility.Private), m_tags, m_ignoredExtensions, m_ignoredPaths, m_dlcs, out items))
                {
                    m_modId = items.ToWorkshopIds();
                }
                else
                {
                    MySandboxGame.Log.WriteLineError(string.Format(Constants.ERROR_Reflection, "PublishItemBlocking"));
                }
                
                // SE libraries don't support updating dependencies, so we have to do that separately
                WorkshopHelper.PublishDependencies(m_modId, m_depsToAdd, m_depsToRemove);
            }
            if (((IMod)this).ModId == 0 || !WorkshopHelper.PublishSuccess)
            {
                MySandboxGame.Log.WriteLineError("Upload/Publish FAILED!");
                return false;
            }
            else
            {
                MySandboxGame.Log.WriteLineAndConsole(string.Format("Upload/Publish success: {0}", m_modId.AsString()));
                if (newMod)
                {
                    if(WorkshopHelper.GenerateModInfo(m_modPath, items, m_modId, MyGameService.UserId))
                    {
                        MySandboxGame.Log.WriteLineAndConsole(string.Format("Create modinfo.sbmi success: {0}", m_modId.AsString()));
                    }
                    else
                    {
                        MySandboxGame.Log.WriteLineAndConsole(string.Format("Create modinfo.sbmi FAILED: {0}", m_modId.AsString()));
                        return false;
                    }
                }
            }
            return true;
        }

        bool FillPropertiesFromPublished()
        {
            var results = WorkshopHelper.GetItemsBlocking(m_modId);
            if(results?.Count > 0)
            {
                System.Threading.Thread.Sleep(1000); // Fix for DLC not being filled in
                if (results.Count > 0)
                {
                    m_workshopItems[m_modId[0]] = results[0];
                    
                    if(m_modId.Length > 1 && results.Count > 1)
                        m_workshopItems[m_modId[1]] = results[1];

                    m_title = results[0].Title;

                    // Check if the mod owner in the sbmi matches steam owner
                    var owner = results[0].OwnerId;

                    if (m_visibility == null)
                        m_visibility = (PublishedFileVisibility)(int)results[0].Visibility;

#if SE
                    m_dlcs = results[0].DLCs.ToArray();
#endif
                    m_deps = results[0].Dependencies.ToArray();

                    MyDebug.AssertDebug(owner == MyGameService.UserId);
                    if (owner != MyGameService.UserId)
                    {
                        MySandboxGame.Log.WriteLineError(string.Format("Owner mismatch! Mod owner: {0}; Current user: {1}", owner, MyGameService.UserId));
                        MySandboxGame.Log.WriteLineError("Upload/Publish FAILED!");
                        return false;
                    }
                    return true;
                }
                return false;
            }
            return true;
        }

        ulong ParseOrGetWorkshopID(string idOrName)
        {
            if (ulong.TryParse(idOrName, out var id))
            {
                return id;
            }
            else
            {
                // Dependencies can only be mods (not blueprints, scripts, etc)
                id = WorkshopHelper.GetWorkshopIdFromMod(Path.Combine(WorkshopHelper.GetWorkshopItemPath(m_type), idOrName)).FirstOrDefault().GetId();

                // TODO: Better handle if an unpublished mod is passed in, but bailing on exception is fine for now
                if (id == 0)
                    throw new ArgumentException($"Cannot determine Workshop ID of dependency '{idOrName}'. Is it a published mod?");
                return id;
            }
        }

        // This can process passed in dependencies, either as the workshop id (ulong), or a path.
        // If a path is passed in, then an attempt will be made to grab the workshop id from the local mod.
        // Mod must be published for that to work.
        void ProcessDependencies(IEnumerable<string> deps, IEnumerable<string> add, IEnumerable<string> remove)
        {
            var existingDeps = m_workshopItems[m_modId[0]].Dependencies.ToList();

            // Check if the deps contains exactly one element, and that element is a 0 or "none",
            // if so, set the result to a list of a single ulong value of 0
            // Otherwise, just fill it with the contents transformed to ids.
            var explicitDeps = (deps?.Count() == 1 && (deps.First() == "0" || deps.First().Equals("none", StringComparison.InvariantCultureIgnoreCase)))
                                    ? new List<ulong>(){ 0 } : new List<ulong>(deps?.Select(id => ParseOrGetWorkshopID(id)) ?? new List<ulong>());
            var depsToAdd = new List<ulong>(add.Select(id => ParseOrGetWorkshopID(id)));
            var depsToRemove = new List<ulong>(remove.Select(id => ParseOrGetWorkshopID(id)));

            // Steam actually requests the list of deps to add and remove explicitly, so we have to figure it out
            if (explicitDeps?.Count > 0)
            {
                if (explicitDeps.Count == 1 && explicitDeps[0] == 0)
                {
                    // Remove ALL dependencies
                    depsToRemove.AddRange(existingDeps);
                }
                else if (existingDeps?.Count > 0)
                {
                    // Any dependencies that existed, but weren't specified, will be removed
                    depsToRemove.AddRange(existingDeps.Except(explicitDeps));
                    depsToAdd.AddRange(explicitDeps.Except(existingDeps));
                }
            }

            // Remove from add/remove list any dependencies that don't exist, or aren't configured to set
            depsToAdd.RemoveAll(d => existingDeps.Contains(d) || explicitDeps?.Contains(d) == true);
            depsToRemove.RemoveAll(d => !existingDeps.Contains(d) && !(explicitDeps?.Contains(d) == true));

            // Filter out items that aren't actually mods, these can crash the game if set
            // Don't check depsToRemove though, so users can remove invalid ones that already exist
            WorkshopHelper.GetItemsBlocking(explicitDeps).ForEach(i => { if (!CheckDependency(i)) explicitDeps.Remove(i.Id); });
            WorkshopHelper.GetItemsBlocking(depsToAdd).ForEach(i => { if (!CheckDependency(i)) depsToAdd.Remove(i.Id); });

            m_deps = existingDeps.Union(explicitDeps ?? new List<ulong>()).Union(depsToAdd).Except(depsToRemove).Where(i => i != 0).Distinct().ToArray();
            m_depsToAdd = depsToAdd.Distinct().ToArray();
            m_depsToRemove = depsToRemove.Distinct().ToArray();
        }

        bool CheckDependency(MyWorkshopItem item)
        {
            if (item.ItemType == MyWorkshopItemType.Item && item.Tags.Contains("Mod", StringComparer.InvariantCultureIgnoreCase))
                return true;

            if (item.ItemType != MyWorkshopItemType.Item)
                MySandboxGame.Log.WriteLineWarning($"Dependency '{item.Id}' is not a valid workshop type, skipping.");
            else
                MySandboxGame.Log.WriteLineWarning($"Dependency '{item.Id}' has the category '{item.Tags.FirstOrDefault() ?? ""}', not 'mod', skipping.");
            return false;
        }

        void ProcessDLCs(IEnumerable<uint> dlcs, IEnumerable<uint> add, IEnumerable<uint> remove)
        {
#if SE
            var existingDeps = m_workshopItems[m_modId[0]].DLCs.ToList();
            var explicitDeps = dlcs?.ToList();
            var depsToAdd = add?.ToList();
            var depsToRemove = remove?.ToList();

            // Steam actually requests the list of deps to add and remove explicitly, so we have to figure it out
            if (explicitDeps?.Count > 0)
            {
                // If a "0" or "none" was specified for DLC, that means remove them all.
                if (explicitDeps.Count == 1 && explicitDeps[0] == 0)
                {
                    // Remove ALL DLCS
                    depsToRemove.AddRange(existingDeps);
                }
                else if (existingDeps?.Count > 0)
                {
                    // Any dependencies that existed, but weren't specified, will be removed
                    depsToRemove.AddRange(existingDeps.Except(explicitDeps));
                    depsToAdd.AddRange(explicitDeps.Except(existingDeps));
                }
            }

            // Remove from add/remove list any dependencies that don't exist, or aren't configured to set
            depsToAdd.RemoveAll(d => existingDeps.Contains(d) || explicitDeps?.Contains(d) == true);
            depsToRemove.RemoveAll(d => !existingDeps.Contains(d) && !(explicitDeps?.Contains(d) == true));

            m_dlcs = existingDeps.Union(explicitDeps ?? new List<uint>()).Union(depsToAdd).Except(depsToRemove).Where(i => i != 0).Distinct().ToArray();
#endif
        }

        void ProcessTags()
        {
            // TODO: This code could be better.

            // 0a) Get the list of existing tags, if there are any
            var existingTags = GetTags();
            var userTags = new List<string>(m_tags);

            // 0b) Add user-specified tags *to add*
            m_tagsToAdd.ForEach(t => userTags.Add(t));

            // Order or tag processing matters
            // 1) Copy mod type into tags
            var modtype = m_type.ToString();

            // 2) Verify the modtype matches what was listed in the workshop
            // TODO If type doesn't match, process as workshop type
            if (existingTags != null && existingTags.Length > 0)
            {
                var msg = string.Format("Workshop category '{0}' does not match expected '{1}'. Is something wrong?", existingTags[0], modtype);
                MySandboxGame.Log.WriteLineWarning(msg);
                MyDebug.AssertDebug(existingTags.Contains(modtype, StringComparer.InvariantCultureIgnoreCase), msg);
            }

#if SE
            // 3a) check if user passed in the 'development' tag
            // If so, remove it, and mark the mod as 'dev' so it doesn't get flagged later
            userTags.RemoveAll(t => t.Equals(MyWorkshop.WORKSHOP_DEVELOPMENT_TAG, StringComparison.InvariantCultureIgnoreCase));
#endif
            // 4) If no user-specified tags were set, grab them from the workshop
            // NOTE: Specifically check m_tags here
            if (m_tags?.Length == 0 && existingTags?.Length > 0)
                existingTags.ForEach(t => userTags.Add(t));

            // 5) If tags contain mod type, remove it
            userTags.RemoveAll(t => t.Equals(modtype, StringComparison.InvariantCultureIgnoreCase));

            // 6) Check user-specified tags to add and remove
            m_tagsToRemove.ForEach(t => userTags.RemoveAll(n => n.Equals(t, StringComparison.InvariantCultureIgnoreCase)));

            // 7) Strip empty values
            userTags.RemoveAll(x => string.IsNullOrEmpty(x?.Trim()));

            if (userTags.Count > 0)
            {
                // Verify passed in tags are valid for this mod type
                var validTags = new List<MyWorkshop.Category>();
                validTags.AddHiddenTags();
                switch (m_type)
                {
                    case WorkshopType.Mod:
                        MyWorkshop.ModCategories.ForEach(c => validTags.Add(c));
                        break;
                    case WorkshopType.Blueprint:
                        MyWorkshop.BlueprintCategories.ForEach(c => validTags.Add(c));
                        break;
                    case WorkshopType.Scenario:
                        MyWorkshop.ScenarioCategories.ForEach(c => validTags.Add(c));
                        break;
                    case WorkshopType.World:
                        MyWorkshop.WorldCategories.ForEach(c => validTags.Add(c));
                        break;
                    case WorkshopType.IngameScript:
                        //tags = new MyWorkshop.Category[0];     // There are none currently
                        break;
                    default:
                        MyDebug.FailRelease("Invalid category.");
                        break;
                }

                // Mods and blueprints have extra tags not in the above lists
                validTags.AddHiddenTags(m_type);

                // This query gets all the items in 'm_tags' that do *not* exist in 'validTags'
                // This is for detecting invalid tags passed in
                var invalidItems = (from utag in userTags
                                   where !(
                                        from tag in validTags
                                        select tag.Id
                                   ).Contains(utag, StringComparer.InvariantCultureIgnoreCase)
                                   select utag);

                if (invalidItems.Count() > 0)
                {
                    MySandboxGame.Log.WriteLineWarning(string.Format("{0} invalid tags: {1}", (m_force ? "Forced" : "Removing"), string.Join(", ", invalidItems)));

                    if (!m_force)
                        invalidItems.ToList().ForEach(t => userTags.RemoveAll(n => n.Equals(t, StringComparison.InvariantCultureIgnoreCase)));
                }

                // Now prepend the 'Type' tag
                var newTags = new List<string>();
                newTags.AddOrInsert(m_type.ToString(), 0);

                var tags = from tag in validTags select tag.Id;

                // Convert all tags to proper-case
                for (var x = 0; x < userTags.Count; x++)
                {
                    var tag = userTags[x];
                    var newtag = (from vtag in tags where (string.Compare(vtag, tag, true) == 0) select vtag).FirstOrDefault();

                    if (!string.IsNullOrEmpty(newtag))
                        newTags.AddOrInsert(newtag, x + 1);
                    else
                        newTags.AddOrInsert(userTags[x], x + 1);
                }

                userTags = newTags;
            }
#if SE
            // 8) Always remove DEV tag, if present
            if (userTags.Contains(MyWorkshop.WORKSHOP_DEVELOPMENT_TAG))
                userTags.RemoveAll(t => t.Equals(MyWorkshop.WORKSHOP_DEVELOPMENT_TAG, StringComparison.InvariantCultureIgnoreCase));
#endif
            // Sanity check, tags, if set, should always have the type
            if (userTags?.Count == 0)
                userTags.Add(m_type.ToString());

            // Done
            m_tags = userTags.Distinct().ToArray();
        }

        string[] GetTags()
        {
            var results = m_workshopItems?.Values ?? (ICollection<MyWorkshopItem>)WorkshopHelper.GetItemsBlocking(m_modId);
            if(results?.Count > 0)
            {
                if (results.Count > 0)
                    return results.First().Tags.ToArray();
                else
                    return null;
            }

            return null;
        }

        uint[] GetDLC()
        {
#if SE
            var results = m_workshopItems?.Values ?? (ICollection<MyWorkshopItem>)WorkshopHelper.GetItemsBlocking(m_modId);

            if (results?.Count > 0)
            {
                if (results.Count > 0)
                    return results.First().DLCs.ToArray();
                else
                    return null;
            }
#endif
            return null;
        }

        PublishedFileVisibility GetVisibility()
        {
            var results = m_workshopItems?.Values ?? (ICollection<MyWorkshopItem>)WorkshopHelper.GetItemsBlocking(m_modId);
            if (results?.Count > 0)
            {
                if (results.Count > 0)
                    return (PublishedFileVisibility)(int)results.First().Visibility;
                else
                    return PublishedFileVisibility.Private;
            }

            return PublishedFileVisibility.Private;
        }

        public bool UpdatePreviewFileOrTags(WorkshopId modId)
        {
            MyWorkshopItemPublisher publisher = WorkshopHelper.GetPublisher(modId);
            var modid = modId.GetId();

            publisher.Id = modid;
            publisher.Title = Title;
            publisher.Visibility = (MyPublishedFileVisibility)(int)(m_visibility ?? GetVisibility());
            publisher.Thumbnail = m_previewFilename;
            publisher.Tags = new List<string>(m_tags);
            publisher.Folder = m_modPath;
#if SE
            if (m_dlcs != null)
                publisher.DLCs = new HashSet<uint>(m_dlcs);
#endif
            if (m_deps != null)
                publisher.Dependencies = new List<ulong>(m_deps);

            AutoResetEvent resetEvent = new AutoResetEvent(false);
            try
            {
                publisher.ItemPublished += ((result, id) =>
                {
                    if (result == MyGameServiceCallResult.OK)
                    {
                        MySandboxGame.Log.WriteLineAndConsole("Published file update successful");

                        if (!string.IsNullOrEmpty(m_previewFilename))
                            MySandboxGame.Log.WriteLineAndConsole(string.Format("Updated thumbnail: {0}", Title));
                    }
                    else
                        MySandboxGame.Log.WriteLineError(string.Format("Error during publishing: {0}", (object)result));
                    resetEvent.Set();
                });

                PrintItemDetails();

                if(m_dryrun)
                {
                    MySandboxGame.Log.WriteLineAndConsole("DRY-RUN; Publish skipped");
                    return true;
                }
                publisher.Publish();
                WorkshopHelper.PublishDependencies(m_modId, m_depsToAdd, m_depsToRemove);

                if (!resetEvent.WaitOne())
                    return false;
            }
            finally
            {
                if (resetEvent != null)
                    resetEvent.Dispose();
            }
            return true;
        }

        bool ValidateThumbnail()
        {
            const int MAX_SIZE = 1048576;

            // Check preview thumbnail size, must be < 1MB
            var previewFilename = m_previewFilename;

            if (!File.Exists(m_previewFilename))
            {
                foreach(var filename in WorkshopHelper.PreviewFileNames)
                {
                    var pathname = Path.Combine(m_modPath, filename);

                    if (File.Exists(pathname))
                        previewFilename = pathname;
                }
            }

            if (!string.IsNullOrEmpty(previewFilename))
            {
                var fileinfo = new FileInfo(previewFilename);
                if (fileinfo.Length >= MAX_SIZE)
                {
                    MySandboxGame.Log.WriteLineWarning($"Thumbnail too large: Must be less than {MAX_SIZE} bytes; Size: {fileinfo.Length} bytes");
                    return false;
                }
            }
            return true;
        }

        void PrintItemDetails()
        {
            const int MAX_LENGTH = 40;

            MySandboxGame.Log.WriteLineAndConsole(string.Format("Visibility: {0}", m_visibility));
            MySandboxGame.Log.WriteLineAndConsole(string.Format("Tags: {0}", string.Join(", ", m_tags)));

            if (!string.IsNullOrEmpty(m_description))
                MySandboxGame.Log.WriteLineAndConsole($"Description: {m_description.Substring(0, Math.Min(m_description.Length, MAX_LENGTH))}{(m_description.Length > MAX_LENGTH ? "..." : "")}");

            if (!string.IsNullOrEmpty(m_changelog))
                MySandboxGame.Log.WriteLineAndConsole($"Changelog: {m_changelog.Substring(0, Math.Min(m_changelog.Length, MAX_LENGTH))}{(m_changelog.Length > MAX_LENGTH ? "..." : "")}");
#if SE
            MySandboxGame.Log.WriteLineAndConsole(string.Format("DLC requirements: {0}",
                (m_dlcs?.Length > 0 ? string.Join(", ", m_dlcs.Select(i =>
                {
                    try { return Sandbox.Game.MyDLCs.DLCs[i].Name; }
                    catch { return $"Unknown({i})"; }
                })) : "None")));
#endif
            MySandboxGame.Log.WriteLineAndConsole(string.Format("Dependencies: {0}", (m_deps?.Length > 0 ? string.Empty : "None")));

            if (m_deps?.Length > 0)
            {
                var width = Console.Out.IsInteractive() ? Console.WindowWidth : 256;

                var depIds = m_deps.ToWorkshopIds();
                var depItems = WorkshopHelper.GetItemsBlocking(depIds.ToArray());

                if (depItems?.Count > 0)
                    depItems.ForEach(i => MySandboxGame.Log.WriteLineAndConsole(string.Format("{0,15} -> {1}",
                        i.Id, i.Title.Substring(0, Math.Min(i.Title.Length, width - 45)))));
                else
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("     {0}", string.Join(", ", m_deps)));
            }
            MySandboxGame.Log.WriteLineAndConsole(string.Format("Thumbnail: {0}", m_previewFilename ?? "No change"));
            ValidateThumbnail();
        }
    }
}
