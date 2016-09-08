using Sandbox;
using Sandbox.Engine.Networking;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VRage.Compression;
using VRage.Game;
using VRage.Utils;

namespace SEWorkshopTool
{
    class Downloader : IMod
    {
        string m_modPath;
        ulong m_modId = 0;
        string m_title;
        readonly string[] m_tags = { MySteamWorkshop.WORKSHOP_MOD_TAG };

        public string Title { get { return m_title; } }
        public ulong ModId { get { return m_modId; } }
        public string ModPath { get { return m_modPath; } }

        public Downloader(string path, ulong modid, string title, string[] tags)
        {
            m_modId = modid;
            m_title = title;
            m_modPath = path;
        }

        public bool Extract()
        {
            string ext = ".sbm";

            if (m_tags.Contains(MySteamWorkshop.WORKSHOP_MOD_TAG))
                ext = ".sbm";
            else if (m_tags.Contains(MySteamWorkshop.WORKSHOP_BLUEPRINT_TAG))
                ext = ".sbb";
            else if (m_tags.Contains(MySteamWorkshop.WORKSHOP_SCENARIO_TAG))
                ext = ".sbs";
            else if (m_tags.Contains(MySteamWorkshop.WORKSHOP_WORLD_TAG))
                ext = ".sbw";

            var sanitizedTitle = Path.GetInvalidFileNameChars().Aggregate(Title, (current, c) => current.Replace(c.ToString(), "_"));
            var source = Path.Combine(m_modPath, m_modId.ToString() + ext);
            var dest = Path.Combine(m_modPath, string.Format("{0} {1} ({2})", Constants.SEWT_Prefix, sanitizedTitle, m_modId.ToString()));
            MySandboxGame.Log.WriteLineAndConsole(string.Format("Extracting mod: '{0}' to: \"{1}\"", sanitizedTitle, dest));
            MyZipArchive.ExtractToDirectory(source, dest);
            return true;
        }
    }
}
