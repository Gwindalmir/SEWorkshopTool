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

#if SE
using Sandbox.Game.World;
using VRage.Scripting;
using MySubscribedItem = Sandbox.Engine.Networking.MyWorkshop.SubscribedItem;
#else
using VRage.Session;
using MySubscribedItem = VRage.GameServices.MyWorkshopItem;
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
        static MySteamService MySteam { get => (MySteamService)MyServiceManager.Instance.GetService<VRage.GameServices.IMyGameService>(); }
#if SE
        readonly string[] m_ignoredExtensions;
#else
        readonly HashSet<string> m_ignoredExtensions = new HashSet<string>();
        readonly HashSet<string> m_ignoredPaths = new HashSet<string>();
#endif
        string m_modPath;
        bool m_compile;
        bool m_dryrun;
        ulong m_modId = 0;
        string m_title;
        MyPublishedFileVisibility? m_visibility;
        WorkshopType m_type;
        string[] m_tags = new string[1];
        bool m_isDev = false;
        bool m_force;
        string m_previewFilename;

        private static object _scriptManager;
        private static PublishItemBlocking _publishMethod;
        private static LoadScripts _compileMethod;

#if SE
        private delegate ulong PublishItemBlocking(string localFolder, string publishedTitle, string publishedDescription, ulong? workshopId, MyPublishedFileVisibility visibility, string[] tags, string[] ignoredExtensions);
        private delegate void LoadScripts(string path, MyModContext mod = null);
#else
        private delegate ulong PublishItemBlocking(string localFolder, string publishedTitle, string publishedDescription, ulong? workshopId, MyPublishedFileVisibility visibility, string[] tags, HashSet<string> ignoredExtensions = null, HashSet<string> ignoredPaths = null);
        private delegate void LoadScripts(MyModContext mod = null);
#endif

        public string Title { get { return m_title; } }
        public ulong ModId { get { return m_modId; } }
        public string ModPath { get { return m_modPath; } }

        public Uploader(WorkshopType type, string path, string[] tags = null, string[] ignoredExtensions = null, bool compile = false, bool dryrun = false, bool development = false, MyPublishedFileVisibility? visibility = null, bool force = false, string previewFilename = null)
        {
            m_modPath = path;
            m_compile = compile;
            m_dryrun = dryrun;
            m_visibility = visibility;
            m_title = Path.GetFileName(path);
            m_modId = MyWorkshop.GetWorkshopIdFromLocalMod(m_modPath);
            m_type = type;
            m_isDev = development;
            m_force = force;
            m_previewFilename = previewFilename;

            if( tags != null )
                m_tags = tags;

            // This file list should match the PublishXXXAsync methods in MyWorkshop
            switch(m_type)
            {
#if SE
                case WorkshopType.Mod:
                    m_ignoredExtensions = new string[] { ".sbmi" };
                    break;
                case WorkshopType.IngameScript:
                    m_ignoredExtensions = new string[] { ".sbmi", ".png", ".jpg" };
                    break;
                case WorkshopType.World:
                    m_ignoredExtensions = new string[] { ".xmlcache", ".png" };
                    break;
                case WorkshopType.Blueprint:
                    m_ignoredExtensions = new string[] { };
                    break;
                case WorkshopType.Scenario:
                    m_ignoredExtensions = new string[] { };
                    break;
#else
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
#endif
            }

            if ( ignoredExtensions != null )
            {
                ignoredExtensions = ignoredExtensions.Select(s => "." + s.TrimStart(new[] { '.', '*' })).ToArray();
#if SE
                string[] allIgnoredExtensions = new string[m_ignoredExtensions.Length + ignoredExtensions.Length];
                ignoredExtensions.CopyTo(allIgnoredExtensions, 0);
                m_ignoredExtensions.CopyTo(allIgnoredExtensions, ignoredExtensions.Length);
                m_ignoredExtensions = allIgnoredExtensions;
#else
                ignoredExtensions.ForEach(s => m_ignoredExtensions.Add(s));
#endif
            }

            SetupReflection();
        }

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
                    var compileMethod = _scriptManager.GetType().GetMethod("LoadScripts", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(string), typeof(MyModContext) }, null);
                    MyDebug.AssertDebug(compileMethod != null);

                    if(compileMethod != null)
                        _compileMethod = Delegate.CreateDelegate(typeof(LoadScripts), _scriptManager, compileMethod, false) as LoadScripts;

                    if (_compileMethod == null)
                    {
                        MySandboxGame.Log.WriteLineAndConsole(string.Format(Constants.ERROR_Reflection, "LoadScripts"));
                    }
                }
            }

            if (!m_dryrun)
            {
                var publishMethod = typeof(MyWorkshop).GetMethod("PublishItemBlocking", BindingFlags.Static | BindingFlags.NonPublic);
                MyDebug.AssertDebug(publishMethod != null);

                if (publishMethod != null)
                    _publishMethod = Delegate.CreateDelegate(typeof(PublishItemBlocking), publishMethod, false) as PublishItemBlocking;

                if (_publishMethod == null)
                {
                    MySandboxGame.Log.WriteLineAndConsole(string.Format(Constants.ERROR_Reflection, "PublishItemBlocking"));
                }
            }
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
                        var workshopItem = new MyLocalWorkshopItem(new VRage.ObjectBuilders.SerializableModReference(m_title, 0, m_title));
                        var mod = new MyModContext(workshopItem, 0);
#endif
                        _compileMethod(
#if SE
                            m_modPath,
#endif
                            mod
                        );

                        // Process any errors
                        var errors = MyDefinitionErrors.GetErrors();
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
                                System.Console.WriteLine(error.Message);

                            MyDefinitionErrors.Clear();     // Clear old ones, so next mod starts fresh

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
                    var ingamescript = MyScriptCompiler.Static.GetIngameScript(program, "Program", typeof(Sandbox.ModAPI.Ingame.MyGridProgram).Name, "sealed partial");
                    var messages = new List<MyScriptCompiler.Message>();
                    var assembly = MyScriptCompiler.Static.Compile(MyApiTarget.Ingame, null, ingamescript, messages, null).Result;

                    if (messages.Count > 0)
                    {
                        MySandboxGame.Log.WriteLineAndConsole(string.Format("There are {0} compile messages:", messages.Count));
                        int errors = 0;
                        foreach (var msg in messages)
                        {
                            MySandboxGame.Log.WriteLineAndConsole(msg.Text);

                            if (msg.Severity > TErrorSeverity.Warning)
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

            if( !Steamworks.SteamAPI.IsSteamRunning() )
            {
                MySandboxGame.Log.WriteLineAndConsole("Cannot publish, Steam not detected!");
                return false;
            }

            // Upload/Publish
            if (m_modId == 0)
            {
                MySandboxGame.Log.WriteLineAndConsole(string.Format("Uploading new {0}: {1}", m_type.ToString(), m_title));
                newMod = true;
            }
            else
            {
                if(FillPropertiesFromPublished())
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("Updating {0}: {1}; {2}", m_type.ToString(), m_modId, m_title));
            }

            // Process Tags
            ProcessTags();

            if (m_dryrun)
            {
                MySandboxGame.Log.WriteLineAndConsole("DRY-RUN; Publish skipped");
            }
            else
            {
                if (_publishMethod != null)
                {
                    m_modId = _publishMethod(m_modPath, m_title, null, m_modId, m_visibility ?? MyPublishedFileVisibility.Public, m_tags, m_ignoredExtensions
#if !SE
                        , m_ignoredPaths
#endif
                        );
                }
                else
                {
                    MySandboxGame.Log.WriteLineAndConsole(string.Format(Constants.ERROR_Reflection, "PublishItemBlocking"));
                }
            }
            if (m_modId == 0)
            {
                MySandboxGame.Log.WriteLineAndConsole("Upload/Publish FAILED!");
                return false;
            }
            else
            {
                MySandboxGame.Log.WriteLineAndConsole(string.Format("Upload/Publish success: {0}", m_modId));
                if (newMod)
                {
                    if (MyWorkshop.GenerateModInfo(m_modPath, m_modId, MySteam.UserId))
                    {
                        MySandboxGame.Log.WriteLineAndConsole(string.Format("Create modinfo.sbmi success: {0}", m_modId));
                    }
                    else
                    {
                        MySandboxGame.Log.WriteLineAndConsole(string.Format("Create modinfo.sbmi FAILED: {0}", m_modId));
                        return false;
                    }
                }
            }
            return true;
        }

        bool FillPropertiesFromPublished()
        {
            var results = new List<MySubscribedItem>();
#if SE
            if (MyWorkshop.GetItemsBlocking(results, new List<ulong>() { m_modId }))
#else
            if (MyWorkshop.GetItemsBlocking(new List<ulong>() { m_modId }, results))
#endif
            {
                if (results.Count > 0)
                    m_title = results[0].Title;

                // Check if the mod owner in the sbmi matches steam owner
#if SE
                var owner = results[0].SteamIDOwner;
#else
                var owner = results[0].OwnerId;

                if(m_visibility == null)
                    m_visibility = results[0].Visibility;
#endif

                MyDebug.AssertDebug(owner == MySteam.UserId);
                if (owner != MySteam.UserId)
                {
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("Owner mismatch! Mod owner: {0}; Current user: {1}", owner, MySteam.UserId));
                    MySandboxGame.Log.WriteLineAndConsole("Upload/Publish FAILED!");
                    return false;
                }
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
                MyWorkshop.Category[] validTags = new MyWorkshop.Category[0];
                switch(m_type)
                {
                    case WorkshopType.Mod:
                        validTags = MyWorkshop.ModCategories;
                        break;
                    case WorkshopType.Blueprint:
                        validTags = MyWorkshop.BlueprintCategories;
                        break;
                    case WorkshopType.Scenario:
                        validTags = MyWorkshop.ScenarioCategories;
                        break;
                    case WorkshopType.World:
                        validTags = MyWorkshop.WorldCategories;
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
            MySandboxGame.Log.WriteLineAndConsole(string.Format("Publishing with tags: {0}", string.Join(", ", m_tags)));
        }

        string[] GetTags()
        {
            var results = new List<MySubscribedItem>();

#if SE
            if (MyWorkshop.GetItemsBlocking(results, new List<ulong>() { m_modId }))
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

        public bool UpdatePreviewFileOrTags()
        {
            ProcessTags();
#if SE
            if(InjectedMethod.UpdateModThumbnailTags(ModId, m_previewFilename, m_tags) != 0)
            {
                if(!string.IsNullOrEmpty(m_previewFilename))
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("Updated thumbnail: {0}", Title));
                return true;
            }
            return false;
#else
            FillPropertiesFromPublished();

            var publisher = MySteam.CreateWorkshopPublisher();
            publisher.Id = ModId;
            publisher.Title = Title;
            publisher.Visibility = m_visibility ?? MyPublishedFileVisibility.Public;
            publisher.Thumbnail = m_previewFilename;
            publisher.Tags = new List<string>(m_tags);
            publisher.Publish();

            if(!string.IsNullOrEmpty(m_previewFilename))
                MySandboxGame.Log.WriteLineAndConsole(string.Format("Updated thumbnail: {0}", Title));

            return true;

#endif
        }
    }
}
