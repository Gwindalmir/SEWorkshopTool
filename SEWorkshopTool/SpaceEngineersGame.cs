using System;

namespace Phoenix.SEWorkshopTool
{
    public class SpaceEngineersGame : SpaceEngineers.Game.SpaceEngineersGame
    {
        public SpaceEngineersGame(string[] commandlineArgs) : base(commandlineArgs)
        {
        }

        // This method must be overriden to avoid the render thread from being created.
        // Otherwise it will just crash, since we don't have a render component.
        protected override void InitializeRender(IntPtr windowHandle)
        {
            // do nothing
        }
    }
}
