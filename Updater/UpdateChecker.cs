using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.IO;

namespace Gwindalmir.Updater
{
    public class UpdateChecker
    {
        const string ROOT_URL = "https://api.github.com/repos/{0}/{1}/";
        const string AUTHOR = "Gwindalmir";
        const string PROJECT = "SEWorkshopTool";
        private static readonly HttpClient _client = new HttpClient();

        private string m_author;
        private string m_project;
        private bool m_prereleases;
        private Release m_latest;
        
        static UpdateChecker()
        {
            _client.DefaultRequestHeaders.UserAgent.ParseAdd($"{Assembly.GetExecutingAssembly().GetName().Name}/{Assembly.GetExecutingAssembly().GetName().Version.ToString()}");
            _client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        }

        /// <summary>
        /// Initializes a new instance of <see cref="UpdateChecker"/>.
        /// </summary>
        /// <param name="author">Project owner</param>
        /// <param name="project">Project name</param>
        /// <param name="prereleases">Include pre-releases</param>
        public UpdateChecker(string author = AUTHOR, string project = PROJECT, bool prereleases = false)
        {
            m_author = author;
            m_project = project;
            m_prereleases = prereleases;
        }

        /// <summary>
        /// Gets the latest release on GitHub.
        /// </summary>
        /// <returns></returns>
        public Release GetLatestRelease()
        {
            if (m_latest == null)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, 
                    m_prereleases ? (string.Format(ROOT_URL, m_author, m_project) + "releases?per_page=1&pages=1") :
                    (string.Format(ROOT_URL, m_author, m_project) + "releases/latest"));

                var response = _client.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(response));
                var ser = new DataContractJsonSerializer(typeof(List<Release>), new DataContractJsonSerializerSettings()
                {
                    DateTimeFormat = new System.Runtime.Serialization.DateTimeFormat("yyyy-MM-ddTHH:mm:ssK")
                });

                var release = ser.ReadObject(ms) as List<Release>;

                m_latest = release.Count > 0 ? release[0] : default(Release);
            }
            return m_latest;
        }

        public bool IsNewerThan(Version version)
        {
            if (version is null)
                return false;

            // Get the release version, and filter out the version
            // The version is assumed to follow reasonable semantic versioning,
            // with optional prefix (such as 'v', 'v0.7.9-alpha').
            var release = GetLatestRelease();
            var start = release.TagName.IndexOfAny(new[] { '0', '1', '2', '3', '4', '5', '6', '8', '8', '9' });
            var end = release.TagName.IndexOfAny(new[] { '-', '+' });

            if (end == -1)
                end = release.TagName.Length;

            var releaseVersion = new Version(release.TagName.Substring(start, end - start));
            var currentVersion = new Version(version.Major, version.Minor, version.Build);

            return releaseVersion > currentVersion;
        }
    }
}
