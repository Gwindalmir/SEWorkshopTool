using Sandbox.Engine.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SEBatchModTool
{
    class WorkshopHelper
    {
        public static MySteamWorkshop.SubscribedItem GetSubscribedItem(ulong modid)
        {
            MySteamWorkshop.SubscribedItem item = new MySteamWorkshop.SubscribedItem();

            using (var mrEvent = new ManualResetEvent(false))
            {
                MySteam.API.RemoteStorage.GetPublishedFileDetails(modid, 0, (ioFailure, result) =>
                {
                    if (!ioFailure)
                    {
                        item.Description = result.Description;
                        item.Title = result.Title;
                        item.UGCHandle = result.FileHandle;
                        item.Tags = result.Tags.Split(',');
                        item.SteamIDOwner = result.SteamIDOwner;
                        item.TimeUpdated = result.TimeUpdated;
                        item.PublishedFileId = result.PublishedFileId;
                    }
                    mrEvent.Set();
                });

                mrEvent.WaitOne();
                mrEvent.Reset();
            }
            return item;
        }
    }
}
