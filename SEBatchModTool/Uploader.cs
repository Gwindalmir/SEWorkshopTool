using Sandbox;
using Sandbox.Engine.Networking;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Utils;

namespace SEBatchModTool
{
    class Uploader : IMod
    {
        string m_modPath;
        bool m_compile;
        bool m_dryrun;
        ulong m_modId = 0;
        string m_title;
        SteamSDK.PublishedFileVisibility m_visibility;
        readonly string[] m_tags = { MySteamWorkshop.WORKSHOP_MOD_TAG };
        readonly string[] m_ignoredExtensions = { ".sbmi" };

        private static MyScriptManager _scriptManager;
        private static MethodInfo _publishMethod;
        private static MethodInfo _compileMethod;

        public string Title { get { return m_title; } }
        public ulong ModId { get { return m_modId; } }

        public string ModPath { get { return m_modPath; } }

        public Uploader(string path, bool compile = false, bool dryrun = false, bool development = false, SteamSDK.PublishedFileVisibility visibility = SteamSDK.PublishedFileVisibility.Public)
        {
            m_modPath = path;
            m_compile = compile;
            m_dryrun = dryrun;
            m_visibility = visibility;
            m_title = Path.GetFileName(path);
            m_modId = MySteamWorkshop.GetWorkshopIdFromLocalMod(m_modPath);

            if ( m_compile )
            {
                if (_scriptManager == null)
                    _scriptManager = new MyScriptManager();

                if (_compileMethod == null)
                    _compileMethod = typeof(MyScriptManager).GetMethod("LoadScripts", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if( !m_dryrun )
            {
                _publishMethod = typeof(MySteamWorkshop).GetMethod("PublishItemBlocking", BindingFlags.Static | BindingFlags.NonPublic);
            }

            if (development)
                m_tags[m_tags.Length - 1] = MySteamWorkshop.WORKSHOP_DEVELOPMENT_TAG;

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
                    MySandboxGame.Log.WriteLineAndConsole(string.Empty);
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

            // Upload/Publish
            if (m_modId == 0)
            {
                MySandboxGame.Log.WriteLineAndConsole(string.Format("Uploading new mod: {0}", m_title));
                newMod = true;
            }
            else
            {
                MySandboxGame.Log.WriteLineAndConsole(string.Format("Updating mod: {0}; {1}", m_title, m_modId));
            }

            if (m_dryrun)
            {
                MySandboxGame.Log.WriteLineAndConsole("DRY-RUN; No action taken");
            }
            else
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
            }
            return true;
        }
    }
}
