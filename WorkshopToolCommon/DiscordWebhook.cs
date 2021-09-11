using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Phoenix.WorkshopTool
{
    class DiscordWebhook
    {
        WorkshopType m_type;
        string m_title;
        string m_changelog;

        //it is a small payload
        static readonly string _payload = @"{{""embeds"":[ {{""author"": {{""name"": ""{0}""}}, ""title"": ""{1}"", ""description"": ""{2}""}} ]}}";

        //Discord webhook format
        static readonly Regex _urlValidator = new Regex("https://discord.com/api/webhooks/[0-9]+/[a-zA-Z0-9-]+", RegexOptions.Compiled, new TimeSpan(0, 0, 1));

        public DiscordWebhook(WorkshopType type, string title, string changelog)
        {
            m_type = type;
            m_title = title;
            m_changelog = changelog;
        }

        public bool Call(string url, out string error)
        {
            if (_urlValidator.IsMatch(url))
            {
                string requestPayload = string.Format(_payload, m_type, m_title.Replace("\r\n", "\\n"), m_changelog.Replace("\r\n", "\\n"));
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.ContentType = "application/json";
                request.Method = "POST";

                byte[] data = Encoding.UTF8.GetBytes(requestPayload);
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
                request.GetResponse();

                error = string.Empty;
                return true;
            } 
            else
                error = "Invalid webhook Url";

            return false;
        }

    }
}
