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
#if SE
using Sandbox.Game.World;
#else
using TErrorSeverity = VRage.Scripting.ErrorSeverity;
using MySteam = VRage.GameServices.MyGameService;
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
        ulong[] m_depsToRemove;
        string m_modPath;
        bool m_compile;
        bool m_dryrun;
        string m_title;
        string m_description;
        string m_changelog;
        PublishedFileVisibility? m_visibility;
        WorkshopType m_type;
        string[] m_tags = new string[1];
        bool m_isDev = false;
        bool m_force;
        string m_previewFilename;
#if SE
        WorkshopId[] m_modId;
        public WorkshopId[] ModId { get { return m_modId; } }
        ulong IMod.ModId { get { return m_modId[0].Id; } }
#else
        ulong m_modId = 0;
        public ulong ModId { get { return m_modId; } }
#endif

        private static object _scriptManager;
        private static PublishItemBlocking _publishMethod;
        private static LoadScripts _compileMethod;
        private static HashSet<string> _globalIgnoredExtensions;
        private static string[] _previewFileNames;

#if SE
        private delegate ValueTuple<MyGameServiceCallResult, string> PublishItemBlocking(string localFolder, string publishedTitle, string publishedDescription, WorkshopId[] workshopId, MyPublishedFileVisibility visibility, string[] tags, HashSet<string> ignoredExtensions, HashSet<string> ignoredPaths, uint[] requiredDLCs, out MyWorkshopItem[] outIds);
        private delegate void LoadScripts(string path, MyModContext mod = null);
        private static bool PublishSuccess { get; set; }
#else
        private delegate ulong PublishItemBlocking(string localFolder, string publishedTitle, string publishedDescription, ulong? workshopId, MyPublishedFileVisibility visibility, string[] tags, HashSet<string> ignoredExtensions = null, HashSet<string> ignoredPaths = null);
        private delegate void LoadScripts(MyModContext mod = null);

        // Static delegate instance of ref-getter method, statically initialized.
        // Requires an 'OfInterestClass' instance argument to be provided by caller.
        static MethodUtil.RefGetter<MyWorkshop, bool> __refget_m_publishSuccess;
        // Default returns true, as a reflection error doesn't necessarily mean the publish failed.
        // Check log file for error.
        // This is a dynamic getter for the MyWorkshop private field
        private static bool PublishSuccess => __refget_m_publishSuccess != null ?__refget_m_publishSuccess(null) : true;
#endif

        public string Title { get { return m_title; } }
        public string ModPath { get { return m_modPath; } }

        public Uploader(WorkshopType type, string path, string[] tags = null, string[] ignoredExtensions = null, string[] ignoredPaths = null, bool compile = false, bool dryrun = false, bool development = false, PublishedFileVisibility? visibility = null, bool force = false, string previewFilename = null, string[] dlcs = null, ulong[] deps = null, string description = null, string changelog = null)
        {
            m_modPath = path;

            if (ulong.TryParse(m_modPath, out ulong id))
#if SE
                m_modId = new[] { new WorkshopId(id, MyGameService.GetDefaultUGC().ServiceName) };
#else
                m_modId = id;
#endif
            else
#if SE
                m_modId = MyWorkshop.GetWorkshopIdFromMod(m_modPath);
#else
                m_modId = MyWorkshop.GetWorkshopIdFromLocalMod(m_modPath) ?? 0;
#endif

            // Fill defaults before assigning user-defined ones
            FillPropertiesFromPublished();

            m_compile = compile;
            m_dryrun = dryrun;

            if (visibility != null)
                m_visibility = visibility;

            if (string.IsNullOrEmpty(m_title))
                m_title = Path.GetFileName(path);

            m_description = description;
            m_changelog = changelog;

            m_type = type;
            m_isDev = development;
            m_force = force;

            if(previewFilename != null)
                m_previewFilename = previewFilename;
#if SE
            var mappedlc = MapDLCStringsToInts(dlcs);

            // If user specified "0" or "none" for DLCs, remove all of them
            if (dlcs != null)
                m_dlcs = mappedlc;
#endif
            if (tags != null)
                m_tags = tags;

            if (deps != null)
            {
                // Any dependencies that existed, but weren't specified, will be removed
                if (m_deps != null)
                    m_depsToRemove = m_deps.Except(deps).ToArray();

                m_deps = deps;
            }

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

            if ( ignoredExtensions != null )
            {
                ignoredExtensions = ignoredExtensions.Select(s => "." + s.TrimStart(new[] { '.', '*' })).ToArray();
                ignoredExtensions.ForEach(s => m_ignoredExtensions.Add(s));
            }

            if (ignoredPaths != null)
            {
                ignoredPaths.ForEach(s => m_ignoredPaths.Add(s));
            }

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

            SetupReflection();
        }

#if SE
        private uint[] MapDLCStringsToInts(string[] stringdlcs)
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
                    Sandbox.Game.MyDLCs.MyDLC dlcvalue;
                    if (Sandbox.Game.MyDLCs.TryGetDLC(dlc, out dlcvalue))
                        dlcs.Add(dlcvalue.AppId);
                    else
                        MySandboxGame.Log.WriteLineAndConsole($"Invalid DLC specified: {dlc}");
                }
            }
            return dlcs.ToArray();
        }
#endif

        private void SetupReflection()
        {
            if (m_compile && m_type == WorkshopType.Mod)
            {
#if SE
                if (_scriptManager == null)
                    _scriptManager = new MyScriptManager();
#else
                if (_scriptManager == null)
                    _scriptManager = new MyModManager();
#endif
                if (_compileMethod == null)
                {
                    var compileMethod = _scriptManager.GetType().GetMethod("LoadScripts", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, null
#if SE
                        , new[] { typeof(string), typeof(MyModContext) }
#else
                        , new[] { typeof(MyModContext) }
#endif
                        , null);
                    MyDebug.AssertDebug(compileMethod != null);

                    if(compileMethod != null)
                        _compileMethod = Delegate.CreateDelegate(typeof(LoadScripts), _scriptManager, compileMethod, false) as LoadScripts;

                    if (_compileMethod == null)
                    {
                        MySandboxGame.Log.WriteLineAndConsole(string.Format(Constants.ERROR_Reflection, "LoadScripts"));
                    }
                }
            }

            var publishMethod = typeof(MyWorkshop).GetMethod("PublishItemBlocking", BindingFlags.Static | BindingFlags.NonPublic, Type.DefaultBinder, new Type[]
            {
                typeof(string),
                typeof(string),
                typeof(string),
                m_modId.GetType(),
                typeof(MyPublishedFileVisibility),
                typeof(string[]),
                typeof(HashSet<string>),
                typeof(HashSet<string>),
#if SE
                typeof(uint[]),
                typeof(MyWorkshopItem[]).MakeByRefType()
#endif
            }, null);

            MyDebug.AssertDebug(publishMethod != null);

            if (publishMethod != null)
                _publishMethod = Delegate.CreateDelegate(typeof(PublishItemBlocking), publishMethod, false) as PublishItemBlocking;

            if (_publishMethod == null)
            {
                MySandboxGame.Log.WriteLineAndConsole(string.Format(Constants.ERROR_Reflection, "PublishItemBlocking"));
            }

            if (_globalIgnoredExtensions == null)
                _globalIgnoredExtensions = (HashSet<string>)typeof(MyWorkshop).GetField("m_ignoredExecutableExtensions", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);

            if (_previewFileNames == null)
                _previewFileNames = (string[])typeof(MyWorkshop).GetField("m_previewFileNames", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);

            if (_previewFileNames == null)
                _previewFileNames = new string[] { "thumb.png", "thumb.jpg" };

#if !SE
            try
            {
                if (__refget_m_publishSuccess == null)
                    __refget_m_publishSuccess = MethodUtil.create_refgetter<MyWorkshop, bool>("m_publishSuccess", BindingFlags.NonPublic | BindingFlags.Static);
            }
            catch (Exception ex)
            {
                MySandboxGame.Log.WriteLineAndConsole(string.Format(Constants.ERROR_Reflection, "m_publishSuccess"));
                MySandboxGame.Log.WriteLine(ex.Message);
            }
#endif
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
                    if (_compileMethod != null)
                    {
                        MySandboxGame.Log.WriteLineAndConsole("Compiling...");
#if SE
                        var mod = new MyModContext();
                        mod.Init(m_title, null, m_modPath);
#else
                        var workshopItem = new MyLocalWorkshopItem(new VRage.ObjectBuilders.SerializableModReference(Path.GetFileName(m_modPath), 0));
                        var mod = new MyModContext(workshopItem, 0);
#endif
                        _compileMethod(
#if SE
                            m_modPath,
#endif
                            mod
                        );

                        // Process any errors
#if SE
                        var errors = MyDefinitionErrors.GetErrors();
#else
                        var compileMessages = _scriptManager.GetType().GetField("m_messages", BindingFlags.NonPublic | BindingFlags.Instance);
                        var errors = (compileMessages.GetValue(_scriptManager) as List<MyScriptCompiler.Message>) ?? new List<MyScriptCompiler.Message>();
#endif
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

                            if( errorCount > 0)
                                MySandboxGame.Log.WriteLineAndConsole(string.Format("There are {0} compile errors:", errorCount));
                            if (warningCount > 0)
                                MySandboxGame.Log.WriteLineAndConsole(string.Format("There are {0} compile warnings:", warningCount));

                            // Output raw message, which is usually in msbuild friendly format, for automated tools
                            foreach (var error in errors)
#if SE
                                System.Console.WriteLine(error.Message);
#else
                                System.Console.WriteLine(error.Text);
#endif

#if SE
                            MyDefinitionErrors.Clear();     // Clear old ones, so next mod starts fresh
#endif

                            if (errorCount > 0)
                            {
                                MySandboxGame.Log.WriteLineAndConsole("Compilation FAILED!");
                                return false;
                            }
                        }
                        MySandboxGame.Log.WriteLineAndConsole("Compilation successful!");
                    }
                    else
                    {
                        MySandboxGame.Log.WriteLineAndConsole(string.Format(Constants.ERROR_Reflection, "LoadScripts"));
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
                            MySandboxGame.Log.WriteLineAndConsole(msg.Text);

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
                MySandboxGame.Log.WriteLineAndConsole(string.Format("Directory does not exist {0}. Wrong option?", m_modPath ?? string.Empty));
                return false;
            }

            // Upload/Publish
#if SE
            if(((IMod)this).ModId == 0)
#else
            if (m_modId == 0)
#endif
            {
                MySandboxGame.Log.WriteLineAndConsole(string.Format("Uploading new {0}: {1}", m_type.ToString(), m_title));
                newMod = true;
            }
            else
            {
                MySandboxGame.Log.WriteLineAndConsole(string.Format("Updating {0}: {1}; {2}", m_type.ToString(), m_modId.AsString(), m_title));
            }

            // Add the global game filter for file extensions
            _globalIgnoredExtensions?.ForEach(s => m_ignoredExtensions.Add(s));

            // Process Tags
            ProcessTags();

            PrintItemDetails();

            MyWorkshopItem[] items = null;

            if (m_dryrun)
            {
                MySandboxGame.Log.WriteLineAndConsole("DRY-RUN; Publish skipped");
                return true;
            }
            else
            {
                if (_publishMethod != null)
                {
                    InjectedMethod.ChangeLog = m_changelog;
#if SE
                    var result = _publishMethod(m_modPath, m_title, m_description, m_modId, (MyPublishedFileVisibility)(m_visibility ?? PublishedFileVisibility.Private), m_tags, m_ignoredExtensions, m_ignoredPaths, m_dlcs, out items);
                    PublishSuccess = result.Item1 == MyGameServiceCallResult.OK;
#else
                    m_modId = _publishMethod(m_modPath, m_title, m_description, m_modId, (MyPublishedFileVisibility)(m_visibility ?? PublishedFileVisibility.Private), m_tags, m_ignoredExtensions, m_ignoredPaths);
#endif
                }
                else
                {
                    MySandboxGame.Log.WriteLineAndConsole(string.Format(Constants.ERROR_Reflection, "PublishItemBlocking"));
                }
                
                // SE libraries don't support updating dependencies, so we have to do that separately
                WorkshopHelper.PublishDependencies(m_modId, m_deps, m_depsToRemove);
            }
            if (((IMod)this).ModId == 0 || !PublishSuccess)
            {
                MySandboxGame.Log.WriteLineAndConsole("Upload/Publish FAILED!");
                return false;
            }
            else
            {
                MySandboxGame.Log.WriteLineAndConsole(string.Format("Upload/Publish success: {0}", m_modId.AsString()));
                if (newMod)
                {
#if SE
                    if (MyWorkshop.GenerateModInfo(m_modPath, items, MyGameService.UserId))
#else
                    if (MyWorkshop.UpdateModMetadata(m_modPath, m_modId, MySteam.UserId))
#endif
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
            var results = new List<MyWorkshopItem>();
#if SE
            if (MyWorkshop.GetItemsBlockingUGC(m_modId.ToList(), results))
#else
            if (MyWorkshop.GetItemsBlocking(new List<ulong>() { m_modId }, results))
#endif
            {
                System.Threading.Thread.Sleep(1000); // Fix for DLC not being filled in
                if (results.Count > 0)
                {
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
                        MySandboxGame.Log.WriteLineAndConsole(string.Format("Owner mismatch! Mod owner: {0}; Current user: {1}", owner, MyGameService.UserId));
                        MySandboxGame.Log.WriteLineAndConsole("Upload/Publish FAILED!");
                        return false;
                    }
                    return true;
                }
                return false;
            }
            return true;
        }

        void ProcessTags()
        {
            // TODO: This code could be better.

            // Get the list of existing tags, if there are any
            var existingTags = GetTags();
            var length = m_tags.Length;

            // Order or tag processing matters
            // 1) Copy mod type into tags
            var modtype = m_type.ToString();

            // 2) Verify the modtype matches what was listed in the workshop
            // TODO If type doesn't match, process as workshop type
            if (existingTags != null && existingTags.Length > 0)
                MyDebug.AssertRelease(existingTags.Contains(modtype, StringComparer.InvariantCultureIgnoreCase), string.Format("Mod type '{0}' does not match workshop '{1}'", modtype, existingTags[0]));

#if SE
            // 3a) check if user passed in the 'development' tag
            // If so, remove it, and mark the mod as 'dev' so it doesn't get flagged later
            if (m_tags.Contains(MyWorkshop.WORKSHOP_DEVELOPMENT_TAG))
            {
                m_tags = (from tag in m_tags where tag != MyWorkshop.WORKSHOP_DEVELOPMENT_TAG select tag).ToArray();
                m_isDev = true;
            }
#endif
            // 3b If tags contain mod type, remove it
            if (m_tags.Contains(modtype, StringComparer.InvariantCultureIgnoreCase))
            {
                m_tags = (from tag in m_tags where string.Compare(tag, modtype, true) != 0 select tag).ToArray();
            }

            // 4)
            if ( m_tags.Length == 1 && m_tags[0] == null && existingTags != null && existingTags.Length > 0)
            {
                // 4a) If user passed no tags, use existing ones
                Array.Resize(ref m_tags, existingTags.Length);
                Array.Copy(existingTags, m_tags, existingTags.Length);
            }
            else
            {
                // 4b) Verify passed in tags are valid for this mod type
                var validTags = new List<MyWorkshop.Category>()
                {
#if SE
                    // 'obsolete' tag is always available, as is 'No Mods' and 'experimental'
                    new MyWorkshop.Category() { Id = "obsolete" },
                    new MyWorkshop.Category() { Id = "no mods" },
                    new MyWorkshop.Category() { Id = "experimental" },
#endif
                };
                switch(m_type)
                {
                    case WorkshopType.Mod:
                        MyWorkshop.ModCategories.ForEach(c => validTags.Add(c));
                        // Mods have extra tags not in this list
#if SE
                        validTags.Add(new MyWorkshop.Category() { Id = "campaign" });
                        validTags.Add(new MyWorkshop.Category() { Id = "font" });
                        validTags.Add(new MyWorkshop.Category() { Id = "noscripts" });
#endif
                        break;
                    case WorkshopType.Blueprint:
                        MyWorkshop.BlueprintCategories.ForEach(c => validTags.Add(c));
#if SE
                        // Blueprints have extra tags not in this list
                        validTags.Add(new MyWorkshop.Category() { Id = "large_grid" });
                        validTags.Add(new MyWorkshop.Category() { Id = "small_grid" });
                        validTags.Add(new MyWorkshop.Category() { Id = "safe" });   // Mod.io only?
#endif
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

                // This query gets all the items in 'm_tags' that do *not* exist in 'validTags'
                // This is for detecting invalid tags passed in
                var invalidItems = from utag in m_tags
                                   where !(
                                        from tag in validTags
                                        select tag.Id
                                   ).Contains(utag, StringComparer.InvariantCultureIgnoreCase)
                                   select utag;

                if( invalidItems.Count() > 0 )
                {
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("{0} invalid tags: {1}", (m_force ? "Forced" : "Removing"), string.Join(", ", invalidItems)));

                    if (!m_force)
                        m_tags = (from tag in m_tags where !invalidItems.Contains(tag) select tag).ToArray();
                }

                // Now prepend the 'Type' tag
                string[] newTags = new string[m_tags.Length + 1];
                newTags[0] = m_type.ToString();

                var tags = from tag in validTags select tag.Id;

                // Convert all tags to proper-case
                for(var x = 0; x < m_tags.Length; x++)
                {
                    var tag = m_tags[x];
                    var newtag = (from vtag in tags where (string.Compare(vtag, tag, true) == 0) select vtag).FirstOrDefault();

                    if (!string.IsNullOrEmpty(newtag))
                        newTags[x + 1] = newtag;
                    else
                        newTags[x + 1] = m_tags[x];
                }

                m_tags = newTags;
            }
#if SE
            // 5) Set or clear development tag
            if (m_isDev)
            {
                // If user selected dev, add dev tag
                if (!m_tags.Contains(MyWorkshop.WORKSHOP_DEVELOPMENT_TAG))
                {
                    Array.Resize(ref m_tags, m_tags.Length + 1);
                    m_tags[m_tags.Length - 1] = MyWorkshop.WORKSHOP_DEVELOPMENT_TAG;
                }
            }
            else
            {
                // If not, remove tag
                if (m_tags.Contains(MyWorkshop.WORKSHOP_DEVELOPMENT_TAG))
                    m_tags = (from tag in m_tags where tag != MyWorkshop.WORKSHOP_DEVELOPMENT_TAG select tag).ToArray(); 
            }
#endif
            // 6) Strip empty values
            m_tags = m_tags.Where(x => !string.IsNullOrEmpty(x)).ToArray();

            // Done
        }

        string[] GetTags()
        {
            var results = new List<MyWorkshopItem>();

#if SE
            if (MyWorkshop.GetItemsBlockingUGC(m_modId.ToList(), results))
#else
            if (MyWorkshop.GetItemsBlocking(new List<ulong>() { m_modId }, results))
#endif
            {
                if (results.Count > 0)
                    return results[0].Tags.ToArray();
                else
                    return null;
            }

            return null;
        }

        uint[] GetDLC()
        {
#if SE
            var results = new List<MyWorkshopItem>();

            if (MyWorkshop.GetItemsBlockingUGC(m_modId.ToList(), results))
            {
                if (results.Count > 0)
                    return results[0].DLCs.ToArray();
                else
                    return null;
            }
#endif
            return null;
        }

        PublishedFileVisibility GetVisibility()
        {
            var results = new List<MyWorkshopItem>();

#if SE
            if (MyWorkshop.GetItemsBlockingUGC(m_modId.ToList(), results))
#else
            if (MyWorkshop.GetItemsBlocking(new List<ulong>() { m_modId }, results))
#endif
            {
                if (results.Count > 0)
                    return (PublishedFileVisibility)(int)results[0].Visibility;
                else
                    return PublishedFileVisibility.Private;
            }

            return PublishedFileVisibility.Private;
        }

#if !SE
        public bool UpdatePreviewFileOrTags()
        {
            return UpdatePreviewFileOrTags(ModId, MyGameService.CreateWorkshopPublisher());
        }
#endif

        public bool UpdatePreviewFileOrTags(ulong modId, MyWorkshopItemPublisher publisher)
        {
            ProcessTags();

            publisher.Id = modId;
            publisher.Title = Title;
            publisher.Visibility = (MyPublishedFileVisibility)(int)(m_visibility ?? GetVisibility());
            publisher.Thumbnail = m_previewFilename;
            publisher.Tags = new List<string>(m_tags);
#if SE
            if(m_dlcs != null)
                publisher.DLCs = new HashSet<uint>(m_dlcs);
#else
            publisher.Folder = m_modPath;
#endif
            if(m_deps != null)
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
                        MySandboxGame.Log.WriteLineAndConsole(string.Format("Error during publishing: {0}", (object)result));
                    resetEvent.Set();
                });

                PrintItemDetails();
                
                publisher.Publish();
                WorkshopHelper.PublishDependencies(m_modId, m_deps, m_depsToRemove);

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
                foreach(var filename in _previewFileNames)
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
                    MySandboxGame.Log.WriteLineAndConsole($"Thumbnail too large: Must be less than {MAX_SIZE} bytes; Size: {fileinfo.Length} bytes");
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
                var depItems = new List<MyWorkshopItem>();
#if SE
                var depIds = new List<WorkshopId>();
                foreach (var item in m_deps)
                    depIds.Add(new WorkshopId(item, MyGameService.GetDefaultUGC().ServiceName));

                if (MyWorkshop.GetItemsBlockingUGC(depIds, depItems))
#else
                if (MyWorkshop.GetItemsBlocking(m_deps, depItems))
#endif
                    depItems.ForEach(i => MySandboxGame.Log.WriteLineAndConsole(string.Format("{0,15} -> {1}",
                        i.Id, i.Title.Substring(0, Math.Min(i.Title.Length, Console.WindowWidth - 45)))));
                else
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("     {0}", string.Join(", ", m_deps)));
            }
            MySandboxGame.Log.WriteLineAndConsole(string.Format("Thumbnail: {0}", m_previewFilename ?? "No change"));
            ValidateThumbnail();
        }
    }
}
