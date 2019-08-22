using Sandbox;
using System.IO;
using System.Linq;
using VRage.FileSystem;
using VRage.GameServices;

#if SE
using VRage.Compression;
#else
using VRage.Library.Compression;
#endif

namespace Phoenix.WorkshopTool
{
    class Downloader : IMod
    {
        string m_modPath;
        string m_extractPath;
        ulong m_modId = 0;
        string m_title;
        string[] m_tags = new string[0];

        public string Title { get { return m_title; } }
        public ulong ModId { get { return m_modId; } }
        public string ModPath { get { return m_modPath; } }

        public Downloader(string extractPath, MyWorkshopItem item)
        {
            m_modId = item.Id;
            m_title = item.Title;
            m_modPath = item.Folder;
            m_extractPath = extractPath;
            m_tags = item.Tags.ToArray();
        }

        public bool Extract()
        {
            var sanitizedTitle = Path.GetInvalidFileNameChars().Aggregate(Title, (current, c) => current.Replace(c.ToString(), "_"));
            var dest = Path.Combine(m_extractPath, string.Format("{0} {1} ({2})", Constants.SEWT_Prefix, sanitizedTitle, m_modId.ToString()));

            MySandboxGame.Log.WriteLineAndConsole(string.Format("Extracting item: '{0}' to: \"{1}\"", m_title, dest));
            if (Directory.Exists(m_modPath))
                MyFileSystem.CopyAll(m_modPath, dest);
            else
                MyZipArchive.ExtractToDirectory(m_modPath, dest);

            return true;
        }
    }
}
