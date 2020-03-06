using Phoenix.WorkshopTool;
using System;

namespace Phoenix.SEWorkshopTool
{
    public class Program
    {
        public static int Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += GameBase.CurrentDomain_AssemblyResolve;
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += GameBase.CurrentDomain_ReflectionOnlyAssemblyResolve;

            var game = new SpaceGame();
            return game.InitGame(args);
        }
    }
}
