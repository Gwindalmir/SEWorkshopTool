# SE Workshop Tool
Tool which allows batch upload and download of mods for Space Engineers.

# Introduction
This tools allows you to script mod uploads to the workshop.
It takes various command-line options, which can be displayed by running **SEWorkshopTool.exe** without arguments.

Please note, this is a command-line application only. A GUI may be considered in the future, however at this time it is not planned.

# Requirements
1. A copy of Space Engineers on Steam
2. Visual Studio 2013 Update 4 or higher

# Build Steps
1. Open **SEWorkshopTool.sln**
2. Change the references for *CommandLine.dll*, *Sandbox.Game.dll*, *SpaceEngineers.Game.dll*, *Vrage.dll*, *VRage.Game.dll*, *VRage.Library.dll*, and *VRage.Scripting.dll* to point to the location where Space Engineers is installed, typically *&lt;SteamApps&gt;\Common\SpaceEngineers\Bin64*
3. Select **Release**, **Any CPU**, and build the solution.
4. Copy the following files into the Space Engineers *Bin64* directory above: *SEWorkshopTool.exe*, and *SEWorkshopTool.exe.config*.
5. Open a command prompt in the Space Engineers *Bin64* directory as used above.
6. Run the tool with the appropriate options.

# Examples

## Uploading
To upload a mod, execute the following:  
`SEWorkshopTool --mods %appdata%\SpaceEngineers\Mods\FTL`

To upload multiple mods at the same time, just append more directories to the command:  
`SEWorkshopTool --mods %appdata%\SpaceEngineers\Mods\FTL %appdata%\SpaceEngineers\Mods\Stargate`

For additional checks, you can also compile the mods with the **--compile** argument, to verify they are valid before uploading:  
`SEWorkshopTool --compile --mods %appdata%\SpaceEngineers\Mods\FTL %appdata%\SpaceEngineers\Mods\Stargate`

If you just prefer to test the compilation, without uploading to the workshop, you can use the **--dry-run** argument:  
`SEWorkshopTool --dry-run --compile --mods %appdata%\SpaceEngineers\Mods\FTL %appdata%\SpaceEngineers\Mods\Stargate`

To upload a mod tagged for the development branch, use the **--dev** argument (can be combined with above options):  
`SEWorkshopTool --dev --mods %appdata%\SpaceEngineers\Mods\FTL`

If the mod directory contains spaces, surround the path with double-quotes:  
`SEWorkshopTool --dev --mods "%appdata%\SpaceEngineers\Mods\Mod Folder"`

You can also specify the mod categories with the **--tags** argument:  
`SEWorkshopTool --mods %appdata%\SpaceEngineers\Mods\FTL --tags block script`

Note with Tags:
If you do *not* specify any tags, then the existing tags on the workshop will be preserved (for updating mods). However if you specify *any* tags, then *all* tags will be replaced with what was entered.

For example: If a workshop mod contains the *Block* and *Script* categories, and you pass only **--tags block**, then *Script* will be removed.

## Downloading
To download a set of mods, use the **--download** argument. Then supply a list of workshop IDs with the **--mods** argument.  
`SEWorkshopTool --download --mods 754173702 681276386`

If desired, you can also automatically extract the mods to your local *%appdata%\SpaceEngineers\Mods* directory with the **--extract** argument:  
`SEWorkshopTool --extract --download --mods 754173702 681276386`

They will be extracted to their appropriate directory in the format: *[\_SEWT\_] &lt;Mod Title&gt; (&lt;WorkshopId&gt;)*

