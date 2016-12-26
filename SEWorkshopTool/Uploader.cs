using Sandbox;
using Sandbox.Engine.Networking;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Scripting;
using VRage.Utils;

namespace SEWorkshopTool
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
        readonly string[] m_ignoredExtensions;

        string m_modPath;
        bool m_compile;
        bool m_dryrun;
        ulong m_modId = 0;
        string m_title;
        SteamSDK.PublishedFileVisibility m_visibility;
        WorkshopType m_type;
        string[] m_tags = new string[1];
        bool m_isDev = false;
        bool m_force;

        private static MyScriptManager _scriptManager;
        private static MethodInfo _publishMethod;
        private static MethodInfo _compileMethod;

        public string Title { get { return m_title; } }
        public ulong ModId { get { return m_modId; } }
        public string ModPath { get { return m_modPath; } }

        public Uploader(WorkshopType type, string path, string[] tags = null, string[] ignoredExtensions = null, bool compile = false, bool dryrun = false, bool development = false, SteamSDK.PublishedFileVisibility visibility = SteamSDK.PublishedFileVisibility.Public, bool force = false)
        {
            m_modPath = path;
            m_compile = compile;
            m_dryrun = dryrun;
            m_visibility = visibility;
            m_title = Path.GetFileName(path);
            m_modId = MySteamWorkshop.GetWorkshopIdFromLocalMod(m_modPath);
            m_type = type;
            m_isDev = development;
            m_force = force;

            if( tags != null )
                m_tags = tags;

            // This file list should match the PublishXXXAsync methods in MySteamWorkshop
            switch(m_type)
            {
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
            }

            if ( ignoredExtensions != null )
            {
                ignoredExtensions = ignoredExtensions.Select(s => "." + s.TrimStart(new[]{ '.', '*'})).ToArray();
                string[] allIgnoredExtensions = new string[m_ignoredExtensions.Length + ignoredExtensions.Length];
                ignoredExtensions.CopyTo(allIgnoredExtensions, 0);
                m_ignoredExtensions.CopyTo(allIgnoredExtensions, ignoredExtensions.Length);
                m_ignoredExtensions = allIgnoredExtensions;
            }

            SetupReflection();
        }

        private void SetupReflection()
        {
            if (m_compile && m_type == WorkshopType.Mod)
            {
                if (_scriptManager == null)
                    _scriptManager = new MyScriptManager();

                if (_compileMethod == null)
                {
                    _compileMethod = typeof(MyScriptManager).GetMethod("LoadScripts", BindingFlags.NonPublic | BindingFlags.Instance);
                    MyDebug.AssertDebug(_compileMethod != null);

                    if (_compileMethod != null)
                    {
                        var parameters = _compileMethod.GetParameters();
                        MyDebug.AssertDebug(parameters.Count() == 2);
                        MyDebug.AssertDebug(parameters[0].ParameterType == typeof(string));
                        MyDebug.AssertDebug(parameters[1].ParameterType == typeof(MyModContext));

                        if (!(parameters.Count() == 2 && parameters[0].ParameterType == typeof(string) && parameters[1].ParameterType == typeof(MyModContext)))
                        {
                            _compileMethod = null;
                            MySandboxGame.Log.WriteLineAndConsole(string.Format(Constants.ERROR_Reflection, "LoadScripts"));
                        }
                    }
                }
            }

            if (!m_dryrun)
            {
                _publishMethod = typeof(MySteamWorkshop).GetMethod("PublishItemBlocking", BindingFlags.Static | BindingFlags.NonPublic);
                MyDebug.AssertDebug(_publishMethod != null);

                if (_publishMethod != null)
                {
                    var parameters = _publishMethod.GetParameters();
                    MyDebug.AssertDebug(parameters.Count() == 7);
                    MyDebug.AssertDebug(parameters[0].ParameterType == typeof(string));
                    MyDebug.AssertDebug(parameters[1].ParameterType == typeof(string));
                    MyDebug.AssertDebug(parameters[2].ParameterType == typeof(string));
                    MyDebug.AssertDebug(parameters[3].ParameterType == typeof(ulong?));
                    MyDebug.AssertDebug(parameters[4].ParameterType == typeof(SteamSDK.PublishedFileVisibility));
                    MyDebug.AssertDebug(parameters[5].ParameterType == typeof(string[]));
                    MyDebug.AssertDebug(parameters[6].ParameterType == typeof(string[]));

                    if (!(parameters.Count() == 7 &&
                        parameters[0].ParameterType == typeof(string) &&
                        parameters[1].ParameterType == typeof(string) &&
                        parameters[2].ParameterType == typeof(string) &&
                        parameters[3].ParameterType == typeof(ulong?) &&
                        parameters[4].ParameterType == typeof(SteamSDK.PublishedFileVisibility) &&
                        parameters[5].ParameterType == typeof(string[]) &&
                        parameters[6].ParameterType == typeof(string[])))
                    {
                        _publishMethod = null;
                        MySandboxGame.Log.WriteLineAndConsole(string.Format(Constants.ERROR_Reflection, "PublishItemBlocking"));
                    }
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
                        var mod = new MyModContext();
                        mod.Init(m_title, null, m_modPath);
                        _compileMethod.Invoke(_scriptManager, new object[]
                        {
                        m_modPath,
                        mod
                        });

                        // Process any errors
                        var errors = MyDefinitionErrors.GetErrors();
                        if (errors.Count > 0)
                        {
                            MySandboxGame.Log.WriteLineAndConsole(string.Format("There are {0} compile errors:", errors.Count));
                            foreach (var error in errors)
                                MySandboxGame.Log.WriteLineAndConsole(string.Format("{0}: {1}", error.ModName, error.Message));

                            MyDefinitionErrors.Clear();     // Clear old ones, so next mod starts fresh
                            MySandboxGame.Log.WriteLineAndConsole("Compilation FAILED!");
                            return false;
                        }
                        MySandboxGame.Log.WriteLineAndConsole("Compilation successful!");
                    }
                    else
                    {
                        MySandboxGame.Log.WriteLineAndConsole(string.Format(Constants.ERROR_Reflection, "LoadScripts"));
                    }
                }
                else if(m_type == WorkshopType.IngameScript)
                {
                    // Load the ingame script from the disk
                    // I don't like this, but meh
                    var input = new StreamReader(Path.Combine(m_modPath, "Script.cs"));
                    var program = input.ReadToEnd();
                    input.Close();
                    var ingamescript = MyScriptCompiler.Static.GetIngameScript(program, "Program", typeof(Sandbox.ModAPI.Ingame.MyGridProgram).Name, "sealed partial");
                    var messages = new List<MyScriptCompiler.Message>();
                    var assembly = MyScriptCompiler.Static.Compile(MyApiTarget.Ingame, null, ingamescript, messages).Result;

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

            if( MySteam.API == null )
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
                var title = m_title;
                var item = WorkshopHelper.GetSubscribedItem(m_modId);

                if (item != null)
                    title = item.Title;

                // Check if the mod owner in the sbmi matches steam owner
                MyDebug.AssertDebug(item.SteamIDOwner == MySteam.UserId);
                if(item.SteamIDOwner != MySteam.UserId)
                {
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("Owner mismatch! Mod owner: {0}; Current user: {1}", item.SteamIDOwner, MySteam.UserId));
                    MySandboxGame.Log.WriteLineAndConsole("Upload/Publish FAILED!");
                    return false;
                }
                MySandboxGame.Log.WriteLineAndConsole(string.Format("Updating {0}: {1}; {2}", m_type.ToString(), m_modId, title));
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
                    var ret = _publishMethod.Invoke(null, new object[]
                    {
                        m_modPath,
                        m_title,
                        null,
                        new ulong?(m_modId),
                        m_visibility,
                        m_tags,
                        m_ignoredExtensions
                    });

                    MyDebug.AssertDebug(ret is ulong);
                    m_modId = (ulong)ret;
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
                    if (MySteamWorkshop.GenerateModInfo(m_modPath, m_modId, MySteam.UserId))
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
                MyDebug.AssertDebug(existingTags.Contains(modtype), string.Format("Mod type '{0}' does not match workshop '{1}'", modtype, existingTags[0]));

            // 3a) check if user passed in the 'development' tag
            // If so, remove it, and mark the mod as 'dev' so it doesn't get flagged later
            if (m_tags.Contains(MySteamWorkshop.WORKSHOP_DEVELOPMENT_TAG))
            {
                m_tags = (from tag in m_tags where tag != MySteamWorkshop.WORKSHOP_DEVELOPMENT_TAG select tag).ToArray();
                m_isDev = true;
            }

            // 3b If tags contain mod type, remove it
            if (m_tags.Contains(modtype))
            {
                m_tags = (from tag in m_tags where tag != modtype select tag).ToArray();
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
                MySteamWorkshop.Category[] validTags = new MySteamWorkshop.Category[0];
                switch(m_type)
                {
                    case WorkshopType.Mod:
                        validTags = MySteamWorkshop.ModCategories;
                        break;
                    case WorkshopType.Blueprint:
                        validTags = MySteamWorkshop.BlueprintCategories;
                        break;
                    case WorkshopType.Scenario:
                        validTags = MySteamWorkshop.ScenarioCategories;
                        break;
                    case WorkshopType.World:
                        validTags = MySteamWorkshop.WorldCategories;
                        break;
                    case WorkshopType.IngameScript:
                        //tags = new MySteamWorkshop.Category[0];     // There are none currently
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
                                   ).Contains(utag)
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
                Array.Copy(m_tags, 0, newTags, 1, m_tags.Length);
                m_tags = newTags;
            }

            // 5) Set or clear development tag
            if (m_isDev)
            {
                // If user selected dev, add dev tag
                if (!m_tags.Contains(MySteamWorkshop.WORKSHOP_DEVELOPMENT_TAG))
                {
                    Array.Resize(ref m_tags, m_tags.Length + 1);
                    m_tags[m_tags.Length - 1] = MySteamWorkshop.WORKSHOP_DEVELOPMENT_TAG;
                }
            }
            else
            {
                // If not, remove tag
                if (m_tags.Contains(MySteamWorkshop.WORKSHOP_DEVELOPMENT_TAG))
                    m_tags = (from tag in m_tags where tag != MySteamWorkshop.WORKSHOP_DEVELOPMENT_TAG select tag).ToArray(); 
            }

            // 6) Strip empty values
            m_tags = m_tags.Where(x => !string.IsNullOrEmpty(x)).ToArray();

            // Done
            MySandboxGame.Log.WriteLineAndConsole(string.Format("Publishing with tags: {0}", string.Join(", ", m_tags)));
        }

        string[] GetTags()
        {
            return WorkshopHelper.GetSubscribedItem(m_modId).Tags;
        }
    }
}
