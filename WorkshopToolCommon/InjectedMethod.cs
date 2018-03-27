using Sandbox;
using Sandbox.Engine.Networking;
using System.IO;
using System.Threading;
using VRage;
using VRage.GameServices;

namespace Phoenix.WorkshopTool
{
    class InjectedMethod
    {
        static MySteamService MySteam { get => (MySteamService)MyServiceManager.Instance.GetService<VRage.GameServices.IMyGameService>(); }

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
    }
}
