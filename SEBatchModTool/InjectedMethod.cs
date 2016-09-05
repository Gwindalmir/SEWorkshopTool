using Sandbox;
using Sandbox.Engine.Networking;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SEBatchModTool
{
    class InjectedMethod
    {
        private static readonly int m_bufferSize = 1 * 1024 * 1024; // buffer size for copying files

        // The keen original method has a dependency on the UI, for reporting feedback on mod progress.
        // We don't care about that, so this method will be injected in its place to replace it.
        private static string WriteAndShareFileBlocking(string localFileFullPath)
        {
            var steam = MySteam.API;
            var steamFileName = Path.GetFileName(localFileFullPath).ToLower();
            MySandboxGame.Log.WriteLine(string.Format("Writing and sharing file '{0}' - START", steamFileName));

            if (!steam.IsOnline())
                return null;

            using (var fs = new FileStream(localFileFullPath, FileMode.Open, FileAccess.Read))
            {
                if (steam.RemoteStorage.FileExists(steamFileName))
                {
                    var size = steam.RemoteStorage.GetFileSize(steamFileName);
                    MySandboxGame.Log.WriteLine(string.Format("File already exists '{0}', size: {1}", steamFileName, size));
                }
                ulong handle = steam.RemoteStorage.FileWriteStreamOpen(steamFileName);
                byte[] buffer = new byte[m_bufferSize];
                int bytesRead = 0;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    steam.RemoteStorage.FileWriteStreamWriteChunk(handle, buffer, bytesRead);
                steam.RemoteStorage.FileWriteStreamClose(handle);
            }


            bool fileShareSuccess = false;

            Result result = Result.Fail;
            using (var mrEvent = new ManualResetEvent(false))
            {
                steam.RemoteStorage.FileShare(steamFileName, delegate (bool ioFailure, RemoteStorageFileShareResult data)
                {
                    fileShareSuccess = !ioFailure && data.Result == Result.OK;
                    result = data.Result;
                    if (fileShareSuccess)
                    {
                        MySandboxGame.Log.WriteLine(string.Format("File shared"));
                    }
                    else
                        MySandboxGame.Log.WriteLine(string.Format("Error sharing the file: {0}", GetErrorString(ioFailure, data.Result)));
                    mrEvent.Set();
                });
                mrEvent.WaitOne();
                mrEvent.Reset();
            }

            MySandboxGame.Log.WriteLine(string.Format("Writing and sharing file '{0}' - END", steamFileName));

            if (!fileShareSuccess && result != Result.FileNotFound)
                return null;
            else if (result == Result.FileNotFound)
                return result.ToString();

            return steamFileName;
        }

        private static string GetErrorString(bool ioFailure, Result result)
        {
            return ioFailure ? "IO Failure" : result.ToString();
        }
    }
}
