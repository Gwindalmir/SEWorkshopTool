using Phoenix.WorkshopTool;
using System;

namespace Phoenix.SEWorkshopTool
{
    class Program
    {
        public static int Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += GameBase.CurrentDomain_AssemblyResolve;

            var game = new SpaceGame();
            return game.InitGame(args);
        }
    }
}
