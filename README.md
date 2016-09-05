# SEBatchModTool
Tool which allows batch upload and download of mods for Space Engineers.

# Introduction
This tools allows you to script mod uploads to the workshop.
It takes various command-line options, which can be displayed by running **SEBatchModTool.exe** without arguments.

Please note, this is a command-line application only. A GUI may be considered in the future, however at this time it is not planned.

# Requirements
1. A copy of Space Engineers on Steam
2. Visual Studio 2013 Update 4 or higher

# Build Steps
1. Open **SEBatchModTool.sln**
2. Change the references for *Sandbox.Game.dll*, *SpaceEngineers.Game.dll*, *Vrage.dll*, *VRage.Game.dll*, *VRage.Library.dll*, and *VRage.Scripting.dll* to point to the location where Space Engineers is installed (typically *&lt;SteamApps&gt;\Common\SpaceEngineers\Bin64*
3. Select **Release**, **Any CPU**, and build the solution.
4. Copy the following files into the Space Engineers *Bin64* directory above: *CommandLine.dll*, *SEBatchModTool.exe*, and *SEBatchModTool.exe.config*.
5. Open a command prompt in the Space Engineers *Bin64* directory as used above.
6. Run the tool with the appropriate options.

# Example
To upload a mod, execute the following:
`SEBatchModTool --mods %appdata%\SpaceEngineers\Mods\FTL`

To upload multiple mods at the same time, just append more directories to the command:
`SEBatchModTool --mods %appdata%\SpaceEngineers\Mods\FTL %appdata%\SpaceEngineers\Mods\Stargate`

For additional checks, you can also compile the mods with the **--compile** argument, to verify they are valid before uploading:
`SEBatchModTool --compile --mods %appdata%\SpaceEngineers\Mods\FTL %appdata%\SpaceEngineers\Mods\Stargate`

If you just prefer to test the compilation, without uploading to the workshop, you can use the **--dry-run** argument:
`SEBatchModTool --dry-run --compile --mods %appdata%\SpaceEngineers\Mods\FTL %appdata%\SpaceEngineers\Mods\Stargate`

To upload a mod tagged for the development branch, use the **--dev** argument (can be combined with above options):
`SEBatchModTool --dev --mods %appdata%\SpaceEngineers\Mods\FTL`

