using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using VRage.FileSystem;
using Sandbox;
using VRage.GameServices;
using VRage.Game;
using System.Reflection;
using VRage.Utils;
using VRage;
using Phoenix.WorkshopTool.Extensions;
using Sandbox.Engine.Networking;
using Sandbox.Graphics.GUI;
#if SE
using Sandbox.Game.World;
using MyDebug = Phoenix.WorkshopTool.Extensions.MyDebug;
using Error = VRage.Game.MyDefinitionErrors.Error;
using CancelToken = Sandbox.Engine.Networking.MyWorkshop.CancelToken;
#else
using MySteam = VRage.GameServices.MyGameService;
using MyScriptManager = VRage.Session.MyModManager;
using Error = VRage.Scripting.MyScriptCompiler.Message;
using WorkshopId = System.UInt64;
using VRage.Scripting;
using VRage.Session;
#endif

namespace Phoenix.WorkshopTool
{
    // This class is meant to consolidate all the game-specific workshop logic, to keep the rest of the app cleaner.
    public class WorkshopHelper
    {
#if SE
        static IMyGameService MySteam { get => (IMyGameService)MyServiceManager.Instance.GetService<IMyGameService>(); }
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
                    downloadPath = Path.Combine(MyFileSystem.UserDataPath, "Saves", MySteam.UserId.ToString());
                    break;
            }
            return downloadPath;
        }

        public static bool GenerateModInfo(string modPath, MyWorkshopItem[] publishedFiles, WorkshopId[] modIds, ulong steamIDOwner)
        {
#if SE
            return MyWorkshop.GenerateModInfo(modPath, publishedFiles, steamIDOwner);
#else
            return MyWorkshop.UpdateModMetadata(modPath, modIds[0], steamIDOwner);
#endif
        }

        public static WorkshopId[] GetWorkshopIdFromMod(string modFolder)
        {
#if SE
            return MyWorkshop.GetWorkshopIdFromMod(modFolder);
#else
            return new[] { MyWorkshop.GetWorkshopIdFromLocalMod(modFolder) ?? 0 };
#endif
        }

#if SE
        public static List<MyWorkshopItem> GetItemsBlocking(ICollection<WorkshopId> ids)
        {
            var results = new List<MyWorkshopItem>();
            MyWorkshop.GetItemsBlockingUGC(ids.ToList(), results);
            return results;
        }
#endif

        public static MyWorkshopItem GetItemBlocking(ulong id)
        {
            return GetItemsBlocking(new[] { id })?.FirstOrDefault();
        }

        public static List<MyWorkshopItem> GetItemsBlocking(ICollection<ulong> ids)
        {
            if (ids == null)
                return null;
#if SE
            var workshopIds = new List<WorkshopId>();
            ids.ForEach(i => workshopIds.Add(new WorkshopId(i, MyGameService.GetDefaultUGC().ServiceName)));
            return GetItemsBlocking(workshopIds.ToArray());
#else
            var results = new List<MyWorkshopItem>();

            // We have to execute the query manually, since ME doesn't have a DS compatible version of the method.
            var query = MyGameService.CreateWorkshopQuery();
            query.ItemIds = ids.ToList();
            query.CacheExpirationTime = 300;
            using (var resetEvent = new AutoResetEvent(false))
            {
                query.QueryCompleted += (q, r) =>
                {
                    if (r == MyGameServiceCallResult.OK)
                    {
                        foreach (var item in q.Items)
                            results.Add(item);

                        results.Sort();
                    }
                    resetEvent.Set();
                };

                query.Run();
                resetEvent.WaitOne();
            }
            return results;
#endif
        }

        public static MyModContext GetContext(string modPath, MyWorkshopItem workshopItem, WorkshopId[] modId, string title = null)
        {
#if SE
            var mod = new MyModContext();

            // Because of a regression in SE, we need to create a checkpoint ModItem to set the Id.
            var modob = new MyObjectBuilder_Checkpoint.ModItem();
            modob.Name = Path.GetFileName(modPath);
                        
            if (modId.Length > 0)
            {
                modob.PublishedFileId = workshopItem.Id;
                modob.PublishedServiceName = workshopItem.ServiceName;
                modob.FriendlyName = workshopItem.Title;
                modob.SetModData(workshopItem);
            }
            else
            {
                // Fake it, so the compile still works
                modob.PublishedFileId = 0;
                modob.PublishedServiceName = MyGameService.GetDefaultUGC().ServiceName;
                modob.FriendlyName = title;
            }
            mod.Init(modob);

            // Call init again, to make sure the path in set properly to the local mod directory
            mod.Init(title, null, modPath);
#else
            var item = new MyLocalWorkshopItem(new VRage.ObjectBuilders.SerializableModReference(Path.GetFileName(modPath), 0));
            var mod = new MyModContext(item, 0);
#endif
            return mod;
        }

        #region Publishing
        public static MyWorkshopItemPublisher GetPublisher(WorkshopId modId)
        {
#if SE
            return MyGameService.GetUGC(modId.ServiceName).CreateWorkshopPublisher();
#else
            return MyGameService.CreateWorkshopPublisher();
#endif
        }

        public static void PublishDependencies(ulong[] modId, ulong[] dependenciesToAdd, ulong[] dependenciesToRemove = null)
        {
            dependenciesToRemove?.ForEach(id => Steamworks.SteamUGC.RemoveDependency((Steamworks.PublishedFileId_t)modId[0], (Steamworks.PublishedFileId_t)id));
            dependenciesToAdd?.ForEach(id => Steamworks.SteamUGC.AddDependency((Steamworks.PublishedFileId_t)modId[0], (Steamworks.PublishedFileId_t)id));
        }

        public static void PublishDLC(ulong[] modId, uint[] dependenciesToAdd, uint[] dependenciesToRemove = null)
        {
            dependenciesToRemove?.ForEach(id => Steamworks.SteamUGC.RemoveAppDependency((Steamworks.PublishedFileId_t)modId[0], (Steamworks.AppId_t)id));
            dependenciesToAdd?.ForEach(id => Steamworks.SteamUGC.AddAppDependency((Steamworks.PublishedFileId_t)modId[0], (Steamworks.AppId_t)id));
        }
#if SE
        public static void PublishDependencies(WorkshopId[] modId, ulong[] dependenciesToAdd, ulong[] dependenciesToRemove = null)
        {
            foreach (var item in modId)
            {
                if (item.ServiceName == "Steam")
                {
                    dependenciesToRemove?.ForEach(id => Steamworks.SteamUGC.RemoveDependency((Steamworks.PublishedFileId_t)item.Id, (Steamworks.PublishedFileId_t)id));
                    dependenciesToAdd?.ForEach(id => Steamworks.SteamUGC.AddDependency((Steamworks.PublishedFileId_t)item.Id, (Steamworks.PublishedFileId_t)id));
                }
                else if (item.ServiceName == "mod.io")
                {
                    throw new NotImplementedException("Setting mod dependencies on mod.io is not implemented yet.");
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(item.ServiceName), $"Unknown service: {item.ServiceName}");
                }
            }
        }

        public static void PublishDLC(WorkshopId[] modId, uint[] dependenciesToAdd, uint[] dependenciesToRemove = null)
        {
            foreach (var item in modId)
            {
                if (item.ServiceName == "Steam")
                {
                    dependenciesToRemove?.ForEach(id => Steamworks.SteamUGC.RemoveAppDependency((Steamworks.PublishedFileId_t)item.Id, (Steamworks.AppId_t)id));
                    dependenciesToAdd?.ForEach(id => Steamworks.SteamUGC.AddAppDependency((Steamworks.PublishedFileId_t)item.Id, (Steamworks.AppId_t)id));
                }
                else if (item.ServiceName == "mod.io")
                {
                    throw new NotImplementedException("Setting mod DLC on mod.io is not implemented yet.");
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(item.ServiceName), $"Unknown service: {item.ServiceName}");
                }
            }
        }
#endif
        #endregion Publishing

        #region Downloading
        public static MyWorkshop.ResultData DownloadModsBlocking(List<MyWorkshopItem> mods, CancelToken cancelToken = null)
        {
#if SE
            return MyWorkshop.DownloadModsBlockingUGC(mods, cancelToken);
#else
            return MyWorkshop.DownloadModsBlocking(mods, cancelToken);
#endif
        }

        public static bool DownloadBlueprintBlocking(MyWorkshopItem item, bool check = true)
        {
#if SE
            return MyWorkshop.DownloadBlueprintBlockingUGC(item, check);
#else
            return MyWorkshop.DownloadBlueprintBlocking(item, null, check);
#endif
        }

        public static bool TryCreateWorldInstanceBlocking(WorkshopType type, MyWorkshopItem world, out string sessionPath, bool overwrite)
        {
#if SE
            MyWorkshop.MyWorkshopPathInfo pathinfo = type == WorkshopType.World ?
                                                    MyWorkshop.MyWorkshopPathInfo.CreateWorldInfo() :
                                                    MyWorkshop.MyWorkshopPathInfo.CreateScenarioInfo();

            return MyWorkshop.TryCreateWorldInstanceBlocking(world, pathinfo, out sessionPath, overwrite);
#else
            return MyWorkshop.TryCreateWorldInstanceBlocking(world, out sessionPath, overwrite, null);
#endif
        }

        #endregion Downloading

        #region Reflection
#if SE
        private delegate ValueTuple<MyGameServiceCallResult, string> PublishItemBlockingDelegate(string localFolder, string publishedTitle, string publishedDescription, WorkshopId[] workshopId, MyPublishedFileVisibility visibility, string[] tags, HashSet<string> ignoredExtensions, HashSet<string> ignoredPaths, uint[] requiredDLCs, out MyWorkshopItem[] outIds);
        private delegate void LoadScriptsDelegate(string path, MyModContext mod = null);
        internal static bool PublishSuccess { get; set; }
#else
        private delegate ulong PublishItemBlockingDelegate(string localFolder, string publishedTitle, string publishedDescription, ulong? workshopId, MyPublishedFileVisibility visibility, string[] tags, HashSet<string> ignoredExtensions = null, HashSet<string> ignoredPaths = null);
        private delegate void LoadScriptsDelegate(MyModContext mod = null);

        // Static delegate instance of ref-getter method, statically initialized.
        // Requires an 'OfInterestClass' instance argument to be provided by caller.
        static MethodUtil.RefGetter<MyWorkshop, bool> __refget_m_publishSuccess;
        // Default returns true, as a reflection error doesn't necessarily mean the publish failed.
        // Check log file for error.
        // This is a dynamic getter for the MyWorkshop private field
        internal static bool PublishSuccess => __refget_m_publishSuccess != null ? __refget_m_publishSuccess(null) : true;
#endif

        private static object _scriptManager = new MyScriptManager();
        private static PublishItemBlockingDelegate _publishMethod;
        private static LoadScriptsDelegate _compileMethod;

        private static string[] _previewFileNames;
        public static ICollection<string> PreviewFileNames
        {
            get
            {
                if (_previewFileNames == null)
                    _previewFileNames = typeof(MyWorkshop).GetField("m_previewFileNames", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as string[];

                if (_previewFileNames == null)
                    _previewFileNames = new string[] { "thumb.png", "thumb.jpg" };

                return _previewFileNames;
            }
        }

        private static HashSet<string> _globalIgnoredExtensions;
        public static ICollection<string> IgnoredExtensions
        {
            get
            {
                if (_globalIgnoredExtensions == null)
                    _globalIgnoredExtensions = typeof(MyWorkshop).GetField("m_ignoredExecutableExtensions", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as HashSet<string>;

                return _globalIgnoredExtensions;
            }
        }

        public static bool LoadScripts(string path, MyModContext mod = null)
        {
            if (_compileMethod == null)
            {
                _compileMethod = ReflectLoadScripts();
                if (_compileMethod == null)
                {
                    MySandboxGame.Log.WriteLineError(string.Format(Constants.ERROR_Reflection, "LoadScripts"));
                    return false;
                }
            }
#if SE
            _compileMethod(path, mod);
#else
            _compileMethod(mod);
#endif
            return true;
        }

        public static bool PublishItemBlocking(string localFolder, string publishedTitle, string publishedDescription, WorkshopId[] workshopId, MyPublishedFileVisibility visibility, string[] tags, HashSet<string> ignoredExtensions, HashSet<string> ignoredPaths, uint[] requiredDLCs, out MyWorkshopItem[] outIds)
        {
            if (_publishMethod == null)
            {
                _publishMethod = ReflectPublishItemBlocking();

                if (_publishMethod == null)
                {
                    MySandboxGame.Log.WriteLineError(string.Format(Constants.ERROR_Reflection, "PublishItemBlocking"));
                    outIds = null;
                    return false;
                }

#if !SE
                try
                {
                    if (__refget_m_publishSuccess == null)
                        __refget_m_publishSuccess = ReflectPublishSuccess();
                }
                catch (Exception ex)
                {
                    MySandboxGame.Log.WriteLineError(string.Format(Constants.ERROR_Reflection, "m_publishSuccess"));
                    MySandboxGame.Log.WriteLine(ex.Message);
                    outIds = null;
                    return false;
                }
#endif
            }
#if SE
            var result = _publishMethod(localFolder, publishedTitle, publishedDescription, workshopId, visibility, tags, ignoredExtensions, ignoredPaths, requiredDLCs, out outIds);
            PublishSuccess = result.Item1 == MyGameServiceCallResult.OK;
#else
            var result = _publishMethod(localFolder, publishedTitle, publishedDescription, workshopId[0], visibility, tags, ignoredExtensions, ignoredPaths);
            if (result > 0)
                outIds = new[] { GetItemBlocking(result) };
            else
                outIds = null;
#endif
            return PublishSuccess;
        }

        public static void ClearErrors()
        {
#if SE
            MyDefinitionErrors.Clear();     // Clear old ones, so next mod starts fresh
#endif
        }

        public static List<Error> GetErrors()
        {
#if SE
            var errors = MyDefinitionErrors.GetErrors();
#else
            var compileMessages = ReflectCompileMessages();
            var errors = (compileMessages.GetValue(_scriptManager) as List<MyScriptCompiler.Message>) ?? new List<MyScriptCompiler.Message>();
#endif
            return errors.ToList();
        }

        private static LoadScriptsDelegate ReflectLoadScripts()
        {
            var compileMethod = _scriptManager.GetType().GetMethod("LoadScripts", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, null
#if SE
                        , new[] { typeof(string), typeof(MyModContext) }
#else
                        , new[] { typeof(MyModContext) }
#endif
                        , null);
            MyDebug.AssertDebug(compileMethod != null);

            if (compileMethod != null)
                return Delegate.CreateDelegate(typeof(LoadScriptsDelegate), _scriptManager, compileMethod, false) as LoadScriptsDelegate;
            return null;
        }

        private static PublishItemBlockingDelegate ReflectPublishItemBlocking()
        {
            var publishMethod = typeof(MyWorkshop).GetMethod("PublishItemBlocking", BindingFlags.Static | BindingFlags.NonPublic, Type.DefaultBinder, new Type[]
            {
                    typeof(string),
                    typeof(string),
                    typeof(string),
#if SE
                    typeof(WorkshopId[]),
#else
                    typeof(ulong),
#endif
                    typeof(MyPublishedFileVisibility),
                    typeof(string[]),
                    typeof(HashSet<string>),
                    typeof(HashSet<string>),
#if SE
                    typeof(uint[]),
                    typeof(MyWorkshopItem[]).MakeByRefType()
#endif
            }, null);

            MyDebug.AssertDebug(publishMethod != null);

            if (publishMethod != null)
                return Delegate.CreateDelegate(typeof(PublishItemBlockingDelegate), publishMethod, false) as PublishItemBlockingDelegate;
            return null;
        }

        public static MethodInfo ReflectInitSteamWorkshop()
        {
#if SE
            return typeof(SpaceEngineers.Game.SpaceEngineersGame).GetMethod("InitSteamWorkshop", BindingFlags.NonPublic | BindingFlags.Instance);
#else
            return typeof(Medieval.MyMedievalGame).GetMethod("InitSteamWorkshop", BindingFlags.NonPublic | BindingFlags.Instance);
#endif
        }

        public static MethodInfo ReflectSteamWorkshopItemPublisherMethod(string method, BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic)
        {
            return ReflectionHelper.GetMethod(InjectedMethod.MySteamWorkshopItemPublisherType, method, flags);
        }

        public static FieldInfo ReflectSteamWorkshopItemPublisherField(string method, BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic)
        {
            return InjectedMethod.MySteamWorkshopItemPublisherType.GetField(method, flags);
        }

        public static PropertyInfo ReflectSteamWorkshopItemPublisherProperty(string method, BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic)
        {
            return InjectedMethod.MySteamWorkshopItemPublisherType.GetProperty(method, flags);
        }

        public static MethodInfo ReflectToService()
        {
            return ReflectionHelper.GetMethod(InjectedMethod.MySteamHelperType, "ToService", BindingFlags.Static | BindingFlags.Public);
        }

        public static MethodInfo ReflectToSteam()
        {
            return ReflectionHelper.GetMethod(InjectedMethod.MySteamHelperType, "ToSteam", BindingFlags.Static | BindingFlags.Public);
        }

#if SE
        public static MethodInfo ReflectCreateRequest()
        {
            return ReflectionHelper.GetMethod(typeof(VRage.Mod.Io.MyModIoService).Assembly.GetType("VRage.Mod.Io.MyModIo"), "CreateRequest", BindingFlags.Static | BindingFlags.NonPublic);
        }

#else
        internal static MethodUtil.RefGetter<MyWorkshop, bool> ReflectPublishSuccess()
        {
            return MethodUtil.create_refgetter<MyWorkshop, bool>("m_publishSuccess", BindingFlags.NonPublic | BindingFlags.Static);
        }

        internal static FieldInfo ReflectCompileMessages()
        {
            return _scriptManager.GetType().GetField("m_messages", BindingFlags.NonPublic | BindingFlags.Instance);
        }
#endif

#if !SE
        public static MethodInfo ReflectMySteamUgcInstance()
        {
            var propertyInfo = typeof(VRage.Steam.MySteamService).Assembly.GetType("VRage.Steam.Steamworks.MySteamUgc").GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);

            MyDebug.AssertDebug(propertyInfo != null);

            var getMethod = propertyInfo?.GetGetMethod();
            return getMethod;
        }
#endif
        #endregion Reflection

        #region Collections
        public static IEnumerable<MyWorkshopItem> GetCollectionDetails(ulong modid)
        {
            IEnumerable<MyWorkshopItem> details = new List<MyWorkshopItem>();

            MySandboxGame.Log.WriteLineAndConsole("Begin processing collections");

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

            MySandboxGame.Log.WriteLineAndConsole("End processing collections");

            return details;
        }

        // code from Rexxar, modified to use XML
        public static bool GetCollectionDetails(IEnumerable<ulong> publishedFileIds, Action<bool, IEnumerable<MyWorkshopItem>> callback)
        {
            string xml = "";
            var modsInCollection = new List<MyWorkshopItem>();
            bool failure = false;
            MySandboxGame.Log.IncreaseIndent();
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
                        MySandboxGame.Log.WriteLine(string.Format("Failed to download collections: result = {0}", xmlResult));
                        failure = true;
                    }

                    reader.ReadToFollowing("resultcount");
                    count = reader.ReadElementContentAsInt();

                    if (count != publishedFileIds.Count())
                    {
                        MySandboxGame.Log.WriteLine(string.Format("Failed to download collection details: Expected {0} results, got {1}", publishedFileIds.Count(), count));
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
                            MySandboxGame.Log.WriteLineAndConsole(string.Format("Collection {0} contains the following items:", publishedFileId.ToString()));

                            reader.ReadToFollowing("children");
                            using (var sub = reader.ReadSubtree())
                            {
                                while (sub.ReadToFollowing("publishedfileid"))
                                {
                                    var results = GetItemsBlocking(new [] { Convert.ToUInt64(sub.ReadElementContentAsString()).ToWorkshopId() });
                                    if(results?.Count > 0)
                                    {
                                        var item = results[0];

                                        MySandboxGame.Log.WriteLineAndConsole(string.Format("Id - {0}, title - {1}", item.Id, item.Title));
                                        modsInCollection.Add(item);
                                    }
                                }
                            }

                            failure = false;
                        }
                        else
                        {
                            MySandboxGame.Log.WriteLineAndConsole(string.Format("Item {0} returned the following error: {1}", publishedFileId.ToString(), (Steamworks.EResult)xmlResult));
                            failure = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MySandboxGame.Log.WriteLine(ex);
                return false;
            }
            finally
            {
                MySandboxGame.Log.DecreaseIndent();
                callback(failure, modsInCollection);
            }
            return failure;
        }
        #endregion Collections
    }
}
