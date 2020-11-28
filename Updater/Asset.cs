using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Gwindalmir.Updater
{
    [DataContract]
    public class Asset
    {
        [DataMember(Name = "id")]
        public uint Id;
        [DataMember(Name = "browser_download_url")]
        public string Url;
        [DataMember(Name = "name")]
        public string Name;
        [DataMember(Name = "label")]
        public string Label;

        [DataMember(Name = "size")]
        public ulong Size;

        [DataMember(Name = "created_at")]
        public DateTime Created;
        [DataMember(Name = "published_at")]
        public DateTime Published;
    }
}
