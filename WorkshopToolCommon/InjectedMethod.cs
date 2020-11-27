using Sandbox;
using Sandbox.Engine.Networking;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using VRage;
using VRage.GameServices;
#if SE
using VRage.Http;
using MySteam = Sandbox.Engine.Networking.MyGameService;
#endif
namespace Phoenix.WorkshopTool
{
    class InjectedMethod
    {
#if SE
        static IMyGameService MySteam { get => (IMyGameService)MyServiceManager.Instance.GetService<IMyGameService>(); }
#endif
        delegate void SubmitItemUpdateResult(SubmitItemUpdateResult_t result, bool ioFailure);
#if SE
        static readonly Assembly steamAssembly = typeof(VRage.Steam.MySteamGameService).Assembly;
        public static readonly Type MySteamWorkshopItemPublisherType = steamAssembly.GetType("VRage.Steam.MySteamWorkshopItemPublisher");
        public static readonly Type MySteamHelperType = steamAssembly.GetType("VRage.Steam.MySteamHelper");
#else
        public static readonly Type MySteamWorkshopItemPublisherType = typeof(VRage.Steam.MySteamWorkshopItemPublisher);
        public static readonly Type MySteamHelperType = typeof(VRage.Steam.MySteamHelper);
#endif
        // THIS IS A HACK TO GET A CHANGELOG TO THIS CODE
        public static string ChangeLog = null;

#if SE
        private void UpdatePublishedItem()
#else
        public bool UpdatePublishedItem(VRage.Steam.MySteamWorkshopItemPublisher.PublishFields fieldsToPublish, string changeNotes = "")
#endif
        {
            // dynamic slows things down, but it saves coding time
            dynamic thisobj = this;

#if !SE
            if (thisobj.Id == 0)
                return false;
#endif

            var steamService = MySteamWorkshopItemPublisherType.GetField("m_steamService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(thisobj);
            var steamUGC = MySteamWorkshopItemPublisherType.GetProperty("SteamUGC", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(thisobj);
            var appid = (AppId_t)steamService.GetType().GetField("SteamAppId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(steamService);
            UGCUpdateHandle_t ugcUpdateHandleT = SteamUGC.StartItemUpdate(appid, (PublishedFileId_t)thisobj.Id);

            if (!string.IsNullOrEmpty(thisobj.Title))
                SteamUGC.SetItemTitle(ugcUpdateHandleT, string.IsNullOrWhiteSpace(thisobj.Title) ? string.Format("Item {0}", (object)thisobj.Id) : thisobj.Title);

            if (thisobj.Tags != null)
                SteamUGC.SetItemTags(ugcUpdateHandleT, (IList<string>)thisobj.Tags);

            SteamUGC.SetItemVisibility(ugcUpdateHandleT, (ERemoteStoragePublishedFileVisibility)MySteamHelperType.GetMethod("ToSteam", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { thisobj.Visibility }));

            if (!string.IsNullOrWhiteSpace(thisobj.Description))
                SteamUGC.SetItemDescription(ugcUpdateHandleT, thisobj.Description);
            if (!string.IsNullOrWhiteSpace(thisobj.Folder))
                SteamUGC.SetItemContent(ugcUpdateHandleT, thisobj.Folder);
            if (!string.IsNullOrWhiteSpace(thisobj.Thumbnail))
                SteamUGC.SetItemPreview(ugcUpdateHandleT, thisobj.Thumbnail);
            if (thisobj.Metadata != null)
                SteamUGC.SetItemMetadata(ugcUpdateHandleT, MyModMetadataLoader.Serialize((ModMetadataFile)thisobj.Metadata));

            dynamic submitItemUpdateResult = MySteamWorkshopItemPublisherType.GetField("m_submitItemUpdateResult", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(thisobj);
            var SubmitItemUpdateResult = MySteamWorkshopItemPublisherType.GetMethod("SubmitItemUpdateResult", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            var SubmitItemUpdateResultMethod = (SubmitItemUpdateResult)Delegate.CreateDelegate(typeof(SubmitItemUpdateResult), thisobj, SubmitItemUpdateResult);
            submitItemUpdateResult.Set(SteamUGC.SubmitItemUpdate(ugcUpdateHandleT, ChangeLog), new CallResult<SubmitItemUpdateResult_t>.APIDispatchDelegate(SubmitItemUpdateResultMethod));

#if !SE
            return true;
#endif
        }

#if SE
        private static readonly int m_bufferSize = 1 * 1024 * 1024; // buffer size for copying files

        // The keen original method has a dependency on the UI, for reporting feedback on mod progress.
        // We don't care about that, so this method will be injected in its place to replace it.
        private static string WriteAndShareFileBlocking(string localFileFullPath)
        {
            var steamFileName = Path.GetFileName(localFileFullPath).ToLower();
            MySandboxGame.Log.WriteLine(string.Format("Writing and sharing file '{0}' - START", steamFileName));

            if (!MyGameService.IsOnline)
                return null;


            using (var fs = new FileStream(localFileFullPath, FileMode.Open, FileAccess.Read))
            {
                if (MySteam.FileExists(steamFileName))
                {
                    var size = MySteam.GetFileSize(steamFileName);
                    MySandboxGame.Log.WriteLine(string.Format("File already exists '{0}', size: {1}", steamFileName, size));
                }
                ulong handle = MySteam.FileWriteStreamOpen(steamFileName);
                byte[] buffer = new byte[m_bufferSize];
                int bytesRead = 0;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    MySteam.FileWriteStreamWriteChunk(handle, buffer, bytesRead);
                MySteam.FileWriteStreamClose(handle);
            }


            bool fileShareSuccess = false;

            MyGameServiceCallResult result = MyGameServiceCallResult.Fail;
            using (var mrEvent = new ManualResetEvent(false))
            {
                MySteam.FileShare(steamFileName, delegate (bool ioFailure, MyRemoteStorageFileShareResult data)
                {
                    fileShareSuccess = !ioFailure && data.Result == MyGameServiceCallResult.OK;
                    result = data.Result;
                    if (fileShareSuccess)
                        MySandboxGame.Log.WriteLine(string.Format("File shared"));
                    else
                        MySandboxGame.Log.WriteLine(string.Format("Error sharing the file: {0}", GetErrorString(ioFailure, data.Result)));
                    mrEvent.Set();
                });
                mrEvent.WaitOne();
                mrEvent.Reset();
            }

            MySandboxGame.Log.WriteLine(string.Format("Writing and sharing file '{0}' - END", steamFileName));

            if (!fileShareSuccess && result != MyGameServiceCallResult.FileNotFound)
                return null;
            else if (result == MyGameServiceCallResult.FileNotFound)
                return result.ToString();

            return steamFileName;
        }
#endif

        private static string GetErrorString(bool ioFailure, MyGameServiceCallResult result)
        {
            return ioFailure ? "IO Failure" : result.ToString();
        }

        public static ulong UpdateModThumbnailTags(ulong? workshopId, string thumbnailFilename = null, string[] tags = null)
        {
#if SE
            var localPreviewFileFullPath = thumbnailFilename;
            string tempFileFullPath = null;
            string steamPreviewFileName = null;
            MyGameServiceCallResult publishResult;
            ulong publishedFileId = 0;

            if (File.Exists(localPreviewFileFullPath))
            {
                tempFileFullPath = Path.GetTempFileName();
                File.Copy(localPreviewFileFullPath, tempFileFullPath, true);
                steamPreviewFileName = WriteAndShareFileBlocking(tempFileFullPath);
                File.Delete(tempFileFullPath);

                if (steamPreviewFileName == null)
                {
                    MySandboxGame.Log.WriteLine(string.Format("Could not share preview file = '{0}'", localPreviewFileFullPath));
                    return 0;
                }
            }

            MySandboxGame.Log.WriteLine("Publishing - START");
            using (var mrEvent = new ManualResetEvent(false))
            {
                // Update item if it has already been published, otherwise publish it.
                bool publishedFileNotFound = true;
                if (workshopId.HasValue && workshopId != 0)
                {
                    MySandboxGame.Log.WriteLine("File appears to be published already. Attempting to update workshop file.");
                    ulong updateHandle = MySteam.CreatePublishedFileUpdateRequest(workshopId.Value);

                    if(thumbnailFilename != null)
                        MySteam.UpdatePublishedFilePreviewFile(updateHandle, steamPreviewFileName);

                    if (tags != null)
                        MySteam.UpdatePublishedFileTags(updateHandle, tags);

                    MySteam.CommitPublishedFileUpdate(updateHandle, delegate (bool ioFailure, MyRemoteStorageUpdatePublishedFileResult data)
                    {
                        publishResult = data.Result;
                        bool success = !ioFailure && data.Result == MyGameServiceCallResult.OK;
                        if (success)
                            MySandboxGame.Log.WriteLine("Published file update successful");
                        else
                            MySandboxGame.Log.WriteLine(string.Format("Error during publishing: {0}", GetErrorString(ioFailure, data.Result)));
                        publishedFileId = data.PublishedFileId;
                        publishedFileNotFound = data.Result == MyGameServiceCallResult.FileNotFound;
                        mrEvent.Set();
                    });
                    mrEvent.WaitOne();
                    mrEvent.Reset();
                }
            }

            MySandboxGame.Log.WriteLine("Publishing - END");

            // Erasing temporary file. No need for it to take up cloud storage anymore.
            MySandboxGame.Log.WriteLine("Deleting cloud files - START");
            if(steamPreviewFileName != null)
                MySteam.FileDelete(steamPreviewFileName);
            MySandboxGame.Log.WriteLine("Deleting cloud files - END");
#endif
            return workshopId.Value;
        }

        public static bool RestartAppIfNecessary(AppId_t unOwnAppID)
        {
            return false;
        }

#if SE
        // Vanilla method fails to URLEncode the ticket data, triggering error 422
        private static /*MyModIo.MyRequestSetup*/ object CreateRequest(
          string function,
          HttpMethod method,
          string contentType,
          params string[] p)
        {
            var clsMyModIo = typeof(VRage.Mod.Io.MyModIoService).Assembly.GetType("VRage.Mod.Io.MyModIo");
            var clsMyRequestSetup = clsMyModIo?.GetNestedType("MyRequestSetup", System.Reflection.BindingFlags.NonPublic);

            if (clsMyRequestSetup != null)
            {
                var mtdGetUrl = clsMyModIo.GetMethod("GetUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                var mtdAddAuthorizationHeader = clsMyModIo.GetMethod("AddAuthorizationHeader", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                var request = Activator.CreateInstance(clsMyRequestSetup, true);
                var parameters = request.GetType().GetField("Parameters", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                parameters.SetValue(request, new List<HttpData>()
                {
                    new HttpData("Accept", (object) "application/json", HttpDataType.HttpHeader)
                });

                // Fix data to be urlencoded
                if (contentType == "application/x-www-form-urlencoded")
                {
                    var httpparams = parameters.GetValue(request) as List<HttpData>;
                    for (var idx = 0; idx < p.Length; idx++)
                    {
                        var param = p[idx].Split(new[] { '=' }, 2);
                        param[1] = System.Net.WebUtility.UrlEncode(param[1]);
                        p[idx] = string.Join("=", param[0], param[1]);

                        httpparams.Add(new HttpData(param[0], param[1], HttpDataType.RequestBody));
                    }
                }

                request.GetType().GetField("Url", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                    .SetValue(request, (string)mtdGetUrl.Invoke(null, new object[] { function, p }));
                request.GetType().GetField("Method", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                    .SetValue(request, method);

                if (contentType != null)
                    (parameters.GetValue(request) as List<HttpData>)
                        .Add(new HttpData("Content-Type", (object)contentType, HttpDataType.HttpHeader));

                mtdAddAuthorizationHeader.Invoke(null, new[] { request });
                return request;
            }

            return null;
        }
#endif
    }
}
