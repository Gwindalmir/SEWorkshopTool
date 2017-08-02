using Sandbox.Engine.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VRage;
using VRage.FileSystem;
#if SE
using MySubscribedItem = Sandbox.Engine.Networking.MySteamWorkshop.SubscribedItem;
#endif

namespace Phoenix.WorkshopTool
{
    class WorkshopHelper
    {
#if SE
        static MySteamService MySteam { get => (MySteamService)MyServiceManager.Instance.GetService<VRage.GameServices.IMyGameService>(); }
#endif
        public static MySubscribedItem GetSubscribedItem(ulong modid)
        {
            MySubscribedItem item = new MySubscribedItem();

            if (MySteam.API == null)
                return item;

            using (var mrEvent = new ManualResetEvent(false))
            {
                MySteam.API.RemoteStorage.GetPublishedFileDetails(modid, 0, (ioFailure, result) =>
                {
                    if (!ioFailure && result.Result == SteamSDK.Result.OK)
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
                    downloadPath = Path.Combine(MyFileSystem.UserDataPath, "Saves", MySteam.UserId.ToString());
                    break;
            }
            return downloadPath;
        }
    }
}
