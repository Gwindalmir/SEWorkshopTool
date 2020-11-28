using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Gwindalmir.Updater
{
    [DataContract]
    public class Release
    {
        [DataMember(Name = "id")]
        public uint Id;
        [DataMember(Name = "name")]
        public string Name;
        [DataMember(Name = "url")]
        public string Url;
        [DataMember(Name = "tag_name")]
        public string TagName;

        [DataMember(Name = "prerelease")]
        public bool Prerelease;
        [DataMember(Name = "draft")]
        public bool Draft;

        [DataMember(Name = "body")]
        public string Body;

        [DataMember(Name = "created_at")]
        public DateTime Created;
        [DataMember(Name = "published_at")]
        public DateTime Published;

        [DataMember(Name = "assets")]
        public List<Asset> Assets;

        /// <summary>
        /// Return first asset that contains a specified string.
        /// </summary>
        /// <param name="namepart">Text to search for in the asset name.</param>
        /// <returns></returns>
        public Asset GetMatchingAsset(string namepart)
        {
            if (Assets?.Count > 0)
            {
                foreach (var asset in Assets)
                {
                    if (asset.Name.Contains(namepart))
                        return asset;
                }
            }

            return default(Asset);
        }
    }
}
