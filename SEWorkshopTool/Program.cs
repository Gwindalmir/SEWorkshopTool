using Sandbox;
using Sandbox.Engine.Utils;
using SpaceEngineers.Game;
using System;
using System.Reflection;
using VRage.FileSystem;
using VRage.Utils;
using Sandbox.Engine.Platform;
using Sandbox.Engine.Networking;
using SteamSDK;
using System.IO;
using ParallelTasks;
using Sandbox.Game;
using System.Windows.Forms;
using System.Threading;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Sandbox.Game.World;
using VRage.Game;
using VRage.Scripting;
using System.Collections.Generic;
using System.Linq;
using VRageRender;
using Phoenix.WorkshopTool;

namespace Phoenix.SEWorkshopTool
{
    class Program
    {
        public static int Main(string[] args)
        {
            var game = new SpaceGame();
            return game.InitGame(args);
        }
    }
}
