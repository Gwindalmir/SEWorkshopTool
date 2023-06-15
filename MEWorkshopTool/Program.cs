using Phoenix.WorkshopTool;
using System;

namespace Phoenix.MEWorkshopTool
{
    public class Program : ProgramBase
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

            try
            {
                var game = new MedievalGame();
                int resultCode = game.InitGame(args);
                return resultCode;
            }
            catch (Exception e)
            {
                CheckForUpdate(e);
                throw;
            }
        }
    }
}
