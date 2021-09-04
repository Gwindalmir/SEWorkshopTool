using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.GameServices;

namespace Phoenix.WorkshopTool.Extensions
{
    static class GameServiceExtensions
    {
        public static bool GetRemoteStorageQuota(this IMyGameService service, out ulong totalBytes, out ulong availableBytes)
        {
            return SteamRemoteStorage.GetQuota(out totalBytes, out availableBytes);
        }

        public static int GetRemoteStorageFileCount(this IMyGameService service)
        {
            return SteamRemoteStorage.GetFileCount();
        }

        public static string GetRemoteStorageFileNameAndSize(this IMyGameService service, int fileIndex, out int fileSizeInBytes)
        {
            return SteamRemoteStorage.GetFileNameAndSize(fileIndex, out fileSizeInBytes);
        }

        public static bool IsRemoteStorageFilePersisted(this IMyGameService service, string file)
        {
            return SteamRemoteStorage.FilePersisted(file);
        }

        public static bool RemoteStorageFileForget(this IMyGameService service, string file)
        {
            return SteamRemoteStorage.FileForget(file);
        }

        public static bool DeleteFromCloud(this IMyGameService service, string fileName)
        {
            return SteamRemoteStorage.FileDelete(fileName);
        }
    }
}
