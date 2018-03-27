using Sandbox.Engine.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using VRage.FileSystem;
#if SE
using VRage;
using VRage.Utils;
using MySubscribedItem = Sandbox.Engine.Networking.MyWorkshop.SubscribedItem;
#else
using VRage.Library.Logging;
using MySubscribedItem = VRage.GameServices.MyWorkshopItem;
#endif

namespace Phoenix.WorkshopTool
{
    class WorkshopHelper
    {
#if SE
        static MySteamService MySteam { get => (MySteamService)MyServiceManager.Instance.GetService<VRage.GameServices.IMyGameService>(); }
#endif
        static private Dictionary<uint, Action<bool, string>> m_callbacks = new Dictionary<uint, Action<bool, string>>();
        static string _requestURL = "https://api.steampowered.com/{0}/{1}/v{2:0000}/?format=xml";

        public static string GetWorkshopItemPath(WorkshopType type, bool local = true)
        {
            // Get proper path to download to
            var downloadPath = MyFileSystem.ModsPath;
            switch (type)
            {
                case WorkshopType.Blueprint:
                    downloadPath = Path.Combine(MyFileSystem.UserDataPath, "Blueprints", local ? "local" : "workshop");
                    break;
#if SE
                case WorkshopType.IngameScript:
                    downloadPath = Path.Combine(MyFileSystem.UserDataPath, Sandbox.Game.Gui.MyGuiIngameScriptsPage.SCRIPTS_DIRECTORY, local ? "local" : "workshop");
                    break;
#endif
                case WorkshopType.World:
                case WorkshopType.Scenario:
                    downloadPath = Path.Combine(MyFileSystem.UserDataPath, "Saves", MySteamService.Static.UserId.ToString());
                    break;
            }
            return downloadPath;
        }

#region Collections
        public static IEnumerable<MySubscribedItem> GetCollectionDetails(ulong modid)
        {
            IEnumerable<MySubscribedItem> details = new List<MySubscribedItem>();

            MyLog.Default.WriteLineAndConsole("Begin processing collections");

            using (var mrEvent = new ManualResetEvent(false))
            {
                GetCollectionDetails(new List<ulong>() { modid }, (IOFailure, result) =>
                {
                    if (!IOFailure)
                    {
                        details = result;
                    }
                    mrEvent.Set();
                });

                mrEvent.WaitOne();
                mrEvent.Reset();
            }

            MyLog.Default.WriteLineAndConsole("End processing collections");

            return details;
        }

        // code from Rexxar, modified to use XML
        public static bool GetCollectionDetails(IEnumerable<ulong> publishedFileIds, Action<bool, IEnumerable<MySubscribedItem>> callback)
        {
            string xml = "";
            var modsInCollection = new List<MySubscribedItem>();
            bool failure = false;
            MyLog.Default.IncreaseIndent();
            try
            {
                var request = WebRequest.Create(string.Format(_requestURL, "ISteamRemoteStorage", "GetCollectionDetails", 1));
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";

                StringBuilder sb = new StringBuilder();
                sb.Append("?&collectioncount=").Append(publishedFileIds.Count());
                int i = 0;

                foreach (var id in publishedFileIds)
                    sb.AppendFormat("&publishedfileids[{0}]={1}", i++, id);

                var d = Encoding.UTF8.GetBytes(sb.ToString());
                request.ContentLength = d.Length;
                using (var rs = request.GetRequestStream())
                    rs.Write(d, 0, d.Length);

                var response = request.GetResponse();

                var sbr = new StringBuilder(100);
                var buffer = new byte[1024];
                int count;

                while ((count = response.GetResponseStream().Read(buffer, 0, 1024)) > 0)
                {
                    sbr.Append(Encoding.UTF8.GetString(buffer, 0, count));
                }
                xml = sbr.ToString();

                System.Xml.XmlReaderSettings settings = new System.Xml.XmlReaderSettings()
                {
                    DtdProcessing = System.Xml.DtdProcessing.Ignore,
                };

                using (System.Xml.XmlReader reader = System.Xml.XmlReader.Create(new StringReader(xml), settings))
                {
                    reader.ReadToFollowing("result");

                    var xmlResult = reader.ReadElementContentAsInt();
                    if (xmlResult != 1 /* OK */)
                    {
                        MyLog.Default.WriteLine(string.Format("Failed to download collections: result = {0}", xmlResult));
                        failure = true;
                    }

                    reader.ReadToFollowing("resultcount");
                    count = reader.ReadElementContentAsInt();

                    if (count != publishedFileIds.Count())
                    {
                        MyLog.Default.WriteLine(string.Format("Failed to download collection details: Expected {0} results, got {1}", publishedFileIds.Count(), count));
                    }

                    var processed = new List<ulong>(publishedFileIds.Count());

                    for (i = 0; i < publishedFileIds.Count(); ++i)
                    {
                        reader.ReadToFollowing("publishedfileid");
                        ulong publishedFileId = Convert.ToUInt64(reader.ReadElementContentAsString());

                        reader.ReadToFollowing("result");
                        xmlResult = reader.ReadElementContentAsInt();

                        if (xmlResult == 1 /* OK */)
                        {
                            MyLog.Default.WriteLineAndConsole(string.Format("Collection {0} contains the following items:", publishedFileId.ToString()));

                            reader.ReadToFollowing("children");
                            using (var sub = reader.ReadSubtree())
                            {
                                while (sub.ReadToFollowing("publishedfileid"))
                                {
                                    var results = new List<MySubscribedItem>();

                                    // SE and ME have the argument flipped, why?
#if SE
                                    if (MyWorkshop.GetItemsBlocking(results, new List<ulong>() { Convert.ToUInt64(sub.ReadElementContentAsString()) }))
#else
                                    if (MyWorkshop.GetItemsBlocking(new List<ulong>() { Convert.ToUInt64(sub.ReadElementContentAsString()) }, results))
#endif
                                    {
                                        var item = results[0];

                                        MyLog.Default.WriteLineAndConsole(string.Format("Id - {0}, title - {1}",
#if SE
                                            item.PublishedFileId
#else
                                            item.Id
#endif
                                            , item.Title));
                                        modsInCollection.Add(item);
                                    }
                                }
                            }

                            failure = false;
                        }
                        else
                        {
                            MyLog.Default.WriteLineAndConsole(string.Format("Item {0} returned the following error: {1}", publishedFileId.ToString(), (Steamworks.EResult)xmlResult));
                            failure = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine(ex);
                return false;
            }
            finally
            {
                MyLog.Default.DecreaseIndent();
                callback(failure, modsInCollection);
            }
            return failure;
        }
#endregion Collections
    }
}
