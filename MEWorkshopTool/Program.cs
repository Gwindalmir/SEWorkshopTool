using Phoenix.WorkshopTool;
using System;
using System.Diagnostics;

namespace Phoenix.MEWorkshopTool
{
    public class Program
    {
        public static int Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += GameBase.CurrentDomain_AssemblyResolve;

            if (args != null)
            {
                foreach (string arg in args)
                {
                    if (arg == "--vrage-error-log-upload")
                        return 0;
                }
            }

            var game = new MedievalGame();
            int resultCode = game.InitGame(args);
            return resultCode;
        }
    }
}
