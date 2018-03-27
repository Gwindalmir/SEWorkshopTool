using Phoenix.WorkshopTool;
using System;

namespace Phoenix.MEWorkshopTool
{
    class Program
    {
        public static int Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += GameBase.CurrentDomain_AssemblyResolve;

            var game = new MedievalGame();
            return game.InitGame(args);
        }
    }
}
