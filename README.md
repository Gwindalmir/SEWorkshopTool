[![Tests](https://github.com/Gwindalmir/SEWorkshopTool/actions/workflows/tests.yml/badge.svg)](https://github.com/Gwindalmir/SEWorkshopTool/actions/workflows/tests.yml)  
# Workshop Tool
This is a tool which allows batch upload and download of mods for both [Space Engineers](https://store.steampowered.com/app/244850) and [Medieval Engineers](https://store.steampowered.com/app/333950).  
It is also known as SEWT (for Space Engineers Workshop Tool), or MEWT (Medieval Engineers Workshop Tool).

# Introduction
This tools allows you to script mod uploads to the workshop. It takes various command-line options, which can be displayed by running **SEWorkshopTool.exe** without arguments.

Please note, this is a command-line application only. A GUI may be considered in the future, however at this time it is not planned.

# Building from source
> If you are not interested in building this from source, skip ahead to [Installation](#installation).

## Requirements
* [Build Tools for Visual Studio 2017](https://visualstudio.microsoft.com/vs/older-downloads/#visual-studio-2017-and-other-products) 15.7 or higher at a *minimum*.
  * Or the full [Visual Studio Community 2017](https://visualstudio.microsoft.com/vs/older-downloads/#visual-studio-2017-and-other-products) 15.7 or higher, if preferred instead
  * JetBrains Rider is reported as working by the community as another alternative
* .NET Framework 4.8 Targeting Pack for Visual Studio, or the [Developer Pack](https://dotnet.microsoft.com/download/dotnet-framework/net48) if using the Build Tools
  * SE targets this version
* [.NET Core SDK 2.1.300](https://www.microsoft.com/net/download/dotnet-core/sdk-2.1.300) or higher

### Optional
* A copy of [Space](https://store.steampowered.com/app/244850) (or [Medieval](https://store.steampowered.com/app/333950)) Engineers on Steam
  * If either Steam or the game aren't found, the binaries will be downloaded from the Dedicated Server depot.
  * A purchased copy of the game is still required to *run* the workshop tool, but not to build it.

## Build steps
These steps refer to Space Engineers. If you are working with Medieval Engineers, just subsititute as appropriate.
1. Clone repository.
1. If using Visual Studio
   * Open [**WorkshopTool.sln**](WorkshopTool.sln).
   * If Steam and Space Engineers (or Medieval) are installed, they should be detected during the build and the project references to the game DLLs should automatically update. If not, you will need to manually create the junction. If there is a problem, the error message in the build output will explain how to create that manually.
   * Select **Release**, **Any CPU**, and build the solution.
1. If using the Build Tools, or don't want to open the IDE
   * Execute [**build.bat**](build.bat) (Double click, or run from a command prompt)
1. Copy the following files into the Space Engineers *Bin64* directory above: *SEWorkshopTool.exe*, *SEWorkshopTool.exe.config*, *CommandLine.dll*, and *steam_appid.txt*.
   * Alternatively, just grab *SEWorkshopTool-latest.zip* in the output folder, and extract that to the Bin64 directory.
1. Open a command prompt in the Space Engineers *Bin64* directory as used above.
1. Run the tool with the appropriate options.

# Installation
## Requirements
1. A copy of [Space](https://store.steampowered.com/app/244850) (or [Medieval](https://store.steampowered.com/app/333950)) Engineers on Steam

## Steps
1. Either [build from source](#building-from-source), or download the most recent version over on the [releases](../../releases/latest) page.
1. Extract the contents of **SEWorkshopTool-*.zip** or **MEWorkshopTool-*.zip** (depending on the game you have) into the respective game's installation folder (e.g. *%ProgramFiles(x86)%\Steam\steamapps\common\SpaceEngineers\Bin64*).  
   * The files can also be placed in the parent folder, outside of *Bin64* (e.g. *steamapps\common\SpaceEngineers*)
   * Advanced users can extract the zip anywhere and create a junction to the game's Bin64 folder at the same level where the executable is located. The steps for that are beyond the scope of this document. *While this will work, it is not a currently supported installation method.*  
     This may be automated in the future.  
   * If there was a previous installation, and mod.io was used, save a copy of SEWorkshopTool.config *before* overwriting. This has your authentication token. If this is lost you will be required to re-authenticate to mod.io to upload there again.  
     You'll need to manually copy your authentication information from the old .config, and place it in the new one. See the section on [mod.io](#new-in-074) for an example of what to look for.  
1. Move [.wtignore](.wtignore) to *%AppData%\SpaceEngineers\Mods* or *%AppData%\MedievalEngineers\Mods*, as appropriate, and edit to taste.  
   * This can be skipped if this step was completed previously.  
1. Open a Command Prompt or Powershell prompt in the above installation location (where the zip was extracted).  
   * Hold down **Shift** and **Right-click** in a blank area to open the context menu in the folder, then select **Open command window here** or **Open PowerShell window here**.  
1. Execute `SEWorkshopTool.exe` (`.\SEWorkshopTool.exe` in PowerShell) to get started.
1. See the [examples](#usage-examples) below.

# Usage Examples
Want to skip ahead to the important bits?
* [Uploading a mod](#uploading-verb-push)
* [Changing an item's preview image (thumbnail)](#preview-image)
* [Setting item visibility](#visibility)
* [Adding workshop tags](#workshop-tags)
* Adding [dependencies](#dependencies) or [DLC](#dlc-requirements)
* [Setting a change note](#changelog)
* [Updating the workshop description](#workshop-description)
* [Excluding files from publish/push](#exclusion-list)
* [Downloading a mod or collection](#downloading-verb-get)

### Command-line changes as of 0.7.14!
Starting with 0.7.14, the command-line argument format has changed. The examples below have been updated to reflect this change.  
The previous command-line options will still be accepted at least until 0.8, and a warning will show if you attempt to use one, with the corrected format displayed.

In addition, some defaults changed:  
* **--compile** is now default (when appropriate), use --no-compile to disable compilation testing.
* **--extract** is now default, use --no-extract to disable automatic extraction to *%AppData%*.
* **--dev** has been removed. The **development** tag is no longer supported on mods, and will be removed from any updated mods. Using the option will not report an error, but it will not work. This argument only exists in the legacy (pre-0.7.14) command-line format, for compatibility, and will eventually be removed entirely.

## Command structure
Commands are broken down into several distinct categories:
* [get](#downloading-verb-get) - Download workshop items
* [push](#uploading-verb-push) - Push (upload) changes to workshop items
* [change](#changing-metadata-verb-change) - Push metadata changes to items
* [compile](#compilation-verb-compile) - Compile test local mod
* [cloud](#steam-cloud-verb-cloud) - Manage Steam Cloud storage

Each category corresponds to a command 'verb'. The various verbs are shown in more detail below.

Any command-line can be appended with **--help** to get a listing of the the arguments accepted by the program, or the specified verb.  

For example:  
* To show the list of verbs: `SEWorkshopTool --help`  
* To show the accepted arguments for the **push** verb: `SEWorkshopTool push --help`

## Downloading (verb: get)
* *Previously **--download***

To download a set of mods, use the **get** verb. Then supply a list of workshop IDs with the **--ids** argument.  
`SEWorkshopTool get --ids 754173702 681276386`

To download a collection or collections, use the exact same command as above. Then supply a list of collection IDs with the **--ids** argument.  
`SEWorkshopTool get --ids 814381863`

Workshop items will be automatically extracted to their respective locations in *%AppData%*.
If this is not desired, you can prevent this by passing **--no-extract**:  
`SEWorkshopTool get --no-extract --mods 754173702 681276386`

They will be extracted to their appropriate directory in the format: **[\_SEWT\_] _&lt;Workshop Title&gt;_ (_&lt;WorkshopId&gt;_)**

## Uploading (verb: push)
* *Previously **--upload***

To upload a mod, execute the following:  
`SEWorkshopTool push --mods %appdata%\SpaceEngineers\Mods\FTL`

To upload multiple mods at the same time, just append more directories to the command:  
`SEWorkshopTool push --mods %appdata%\SpaceEngineers\Mods\FTL %appdata%\SpaceEngineers\Mods\Stargate`

Relative paths are allowed and, if used, the default data path for that mod type will be prepended:  
`SEWorkshopTool push --mods FTL Stargate --script Off-Roading`

By default, **--compile** is enabled, if you want to publish a script even if it has compilaiton errors, you can use the **--no-compile** argument to disable the compilation check:  
`SEWorkshopTool push --no-compile --mods FTL %appdata%\SpaceEngineers\Mods\Stargate`

For compile testing, refer to the new [compile](#compilation-verb-compile) verb.

If you prefer to test publishing, but not actually publish, you can use the **--dry-run** argument:  
`SEWorkshopTool push --dry-run --mods FTL %appdata%\SpaceEngineers\Mods\Stargate`

If the mod directory or name contains spaces, surround the path with double-quotes:  
`SEWorkshopTool push --mods "%appdata%\SpaceEngineers\Mods\Mod Folder" "My Fabulous Mod"`

### Exclusion list
* **.wtignore** added in 0.7.5

You can pass custom file extensions to ignore during the upload with the **--exclude-ext** argument (with or without a leading dot):  
`SEWorkshopTool push --mods FTL --exclude-ext .fbx .pdb xml`  

You can pass custom paths to ignore during the upload with the **--exclude-path** argument:  
`SEWorkshopTool push --mods FTL --exclude-path bin obj`  

Note: The **--exclude-ext** and **--exclude-path** are recommended for one-time use. For a regular list of extensions or paths to be ignored for all publish operations, use a [**.wtignore**](.wtignore) file.

See the **change** verb below for more options that apply to this verb.

### Changelog
* Added in 0.7.8

The workshop changelog can also be set with the content changes, using the **--message** argument.

Assuming you have your changelog stored inside changelog.txt, setting the changelog for this *specific* operation is similiar:  
`SEWorkshopTool change --mods FTL --message changelog.txt`

If your changelog is short, you can specify the entire message as the argument, instead of a filename:  
`SEWorkshopTool change --mods FTL --message "Made a fix for the latest SE version."`

If you specify the changelog as a filename, it's recommended to add your changelog filename to **.wtignore** to avoid uploading it as a file in your mod content.

Notes:  
The changelog option first checks if the supplied argument is an existing file. If it is, it reads the contents. If not, it simply uses the argument as-is as the changelog note.  
Be sure to check the output for the message that will be sent to Steam (the log will show a shortened version, but the entire content will be sent to steam).  
It's important to surround the message with double-quotes.  

The changelog requires a content update and as such, it's not a valid option for metadata changes.

## Changing Metadata (verb: change)
* *Previously the default if neither **--upload** nor **--download** were specified.*

The **change** verb allows you change various metadata about a mod, without actually pushing a content change.  
All arguments below *also* apply to the **push** verb. They are just listed once, and separately, for brevity.

### Preview Image
* Added in 0.5.4

You can update the the preview image (*thumb.jpg*) separately, without reuploading the entire mod.
This also works for updating tags, DLC and dependencies as well.  

To update the thumbnail (any image steam supports, can be in *any* location):  
`SEWorkshopTool change --mods FTL --thumb "path\to\filename"`

By default, any *thumb.\** file (thumb.jpg, .png, etc.) in the mod folder will be automatically detected and used during normal **push** operations.  

### Visibility
To upload a with hidden visibility:  
`SEWorkshopTool change --mods "Mod Folder" --visibility Private`  

### Workshop Tags
You can specify the mod categories/tags with the **--tags** argument:  
`SEWorkshopTool change --mods FTL --tags block script`

Note with Tags:
If you do *not* specify any tags, then the existing tags on the workshop will be preserved (for updating mods). However if you specify *any* tags using **--tags**, then *all* tags will be replaced with what was entered.  
For example: If a workshop mod contains the *Block* and *Script* categories, and you pass only **--tags block**, then *Script* will be removed.
This does not apply to **--add-tags** or **--remove-tags**.

Starting with 0.7.14, tags can be added or removed selectively, with the **--add-tags** or **--remove-tags** options, respectively:  
`SEWorkshopTool change --mods FTL --add-tags script --remove-tags block`  
The above command will add the *script* tag, if it wasn't already present on the workshop item; it will also remove the 'block' tag if it was present. All other tags will remain untouched.

### DLC Requirements
* Added in 0.6.1

You can add DLC as a requirement for workshop items.  
This is *highly* recommended if you intend to upload mods using DLC assets.

To see the list of the available DLC:  
`SEWorkshopTool.exe --listdlc`

To set a mod as requiring the Style Pack DLC:  
`SEWorkshopTool change --mods FTL --dlc StylePack`  
or  
`SEWorkshopTool change --mods FTL --dlc 1084680`  

Notice that either the string name, or the AppID of the DLC can be specified.

To easily remove all DLC from an item, pass either *0* or *none* as the argument:  
`SEWorkshopTool change --mods FTL --dlc none`  

As with [tags](#workshop-tags), starting with 0.7.14, you can add or remove individual DLC items with **--add-dlc** and **--remove-dlc**, respectively, without affecting existing DLC listed on the item.

### Dependencies
* Added in 0.7.1

You can add other workshop items as a requirement for your items. This operates similar to the DLC requirement feature.

To set a mod as requiring another mod:  
`SEWorkshopTool change --mods FTL --dependencies 1992410560`  

To easily remove all dependencies from an item, pass either *0* or *none* as the argument:  
`SEWorkshopTool change --mods FTL --dependencies none`  

As with [tags](#workshop-tags), starting with 0.7.14, you can add or remove individual item dependencies with **--add-dependencies** or **--remove-dependencies**, respectively, without affecting existing dependencies listed on the item.  

For convenience, local mods can be specified, as long as they have *already* been published. The Workshop ID will be determined from the local mod or mods, if possible.  

Notes:  
Dependencies **must** be workshop items under the *Mod* category. *Blueprints*, *IngameScripts*, and other items cannot be specified as dependencies, and will cause errors in the game if used. This tool will enforce that restriction, and cannot be overridden.

### Workshop Description
* Added in 0.7.8

The entire workshop description can be set from the contents of a file using **--description**.

If you have a file in your mod directory named *description.txt*, you can set your workshop page to the contents of that file using the following:  
`SEWorkshopTool change --mods FTL --description description.txt`  

It is recommended to add your description file to **.wtignore**, so it isn't uploaded with the mod file contents (unless you want to do that).

Unlike the [changelog](#changelog) option, this requires a filename. Passing the content directly on the command-line is not allowed.  

Notes:  
Steam limits the workshop description to 8k bytes. For unicode, that means 4k characters.

## Compilation (verb: compile)
* *Previously just **--compile** or **--upload --compile --dry-run***

This command is useful for only compile testing mods, without uploading.  

This accepts a subset of the arguments of **push**, but only applies to Progammable Block (ingame) scripts, and mods.  

Example: `SEWorkshopTool compile --mods FTL %appdata%\SpaceEngineers\Mods\Stargate`


## Steam Cloud (verb: cloud)
* *Previously **--clearsteamcloud***
* Added in 0.6.0

To see a listing of all the files associated on your Steam cloud account:  
`SEWorkshopTool cloud --list`  
The result will display a tabular list of all the steam cloud files, their size, and status.

Example output:
```
Quota: total = 390,625 kiB, available = 389,692 kiB
Listing 13 cloud files
Filename                                  |Size (kiB)|In Cloud|Forgotten
------------------------------------------|----------|--------|---------
Blueprints/cloud/Large Grid 836/bp.sbc    |        2 |  True  |  N/A
Blueprints/cloud/Large Grid 836/thumb.png |      457 |  True  |  N/A
previewfile_1668739492.gif                |    1,018 |  False |  N/A
previewfile_754173702.jpg                 |       21 |  False |  N/A
previewfile_637504549.png                 |      219 |  True  |  N/A
previewfile_1992410560.png                |      219 |  False |  N/A
previewfile_714651334.png                 |      364 |  False |  N/A
previewfile_1701891397.jpg                |      312 |  False |  N/A
previewfile_1473969256.png                |      536 |  False |  N/A
previewfile_2200904480.jpg                |      182 |  False |  N/A
previewfile_2217821984.jpg                |      267 |  False |  N/A
previewfile_633376731.png                 |      364 |  False |  N/A
previewfile_2362147824.jpg                |      254 |  True  |  N/A
```

### Columns
1. Filename - The filename of the item. Use this name when referencing it for other operations.
2. Size - This is the size of the file, in kiB (base 2 kilobytes)
3. In Cloud - True or false if this file is actually stored on the cloud (consuming cloud storage)
   * True - File is stored on the cloud, and consuming cloud storage space
   * False - File is only stored locally, and is not consuming cloud storage space (deleting it won't free space)
4. Forgotten/Deleted - True or false if the file was successfully deleted or forgotten.
   * True - File was succesfully forgotten (deleted on cloud, but still stored locally), or deleted (deleted on cloud and locally).
   * False - File was **not** succesfully forgotten or deleted. Check steam logs.
   * N/A - This column is not applicable to the current operation. Ignore.

### *Deleted* vs *Forgotten*
This naming scheme comes from the Steamworks API for [FileForget](https://partner.steamgames.com/doc/api/ISteamRemoteStorage#FileForget) and [FileDelete](https://partner.steamgames.com/doc/api/ISteamRemoteStorage#FileDelete).  
* *Forget* means the file has been deleted from the cloud storage, but is still present in the local cache. This file will remain accessible in the API for the local machine only.  
* *Delete* means the file has been deleted from both cloud and local storage. The file is really gone.

To see a listing of all the files associated on your Steam cloud account:  
`SEWorkshopTool cloud --list`

To delete a file from the cloud, but keep it locally (*forget*):  
`SEWorkshopTool cloud --delete "Blueprints/cloud/Large Grid 836/bp.sbc"`  

To permanently delete a file both locally and on the cloud (*delete*):  
`SEWorkshopTool cloud --delete --force "Blueprints/cloud/Large Grid 836/bp.sbc"`  

Either of the above operations (*forget* or *delete*) will print the file list again, and populate the rightmost column in the list with `True` if the forget or delete operation was successful.

## Other Operations
### New in 0.7.5:
Support for .gitignore style ignore list (but simpler). This file is [.wtignore](.wtignore), and can be placed either in your mod directory, or in the parent Mods\ directory, ie. *%appdata%\SpaceEngineers\Mods*.
An example file is included with the release zip, just copy it to one of the directories above and edit to taste. Further documentation is in the [file](.wtignore) itself.

### New in 0.7.4:
Mod.io support, for uploading or downloading mods used by the XBox version.

**THIS IS EXPERIMENTAL AND BUGGY**

It can only upload and download mods. It cannot update tags. Thumbnail editing is untested.  
Authentication is required, this is done via email. SEWT does not ask for your account password!  
If you do not want to use SEWT for authentication, you might be able to manually create an OAuth2 token from the mod.io website, however this is untested.  
If so, add the following to SEWorkshopTool.exe.config, just below:

```xml
  <appSettings>
    <add key="auth-login" value="your mod.io email address" />
    <add key="auth-token" value="your mod.io token here" />
    <add key="auth-expires" value="unix timestamp expiration" />
  </appSettings>
```
To switch to mod.io, use the --modio option, in combination with all the other commands.  

For example, to upload a large ship blueprint named "My First Ship":  
`SEWorkshopTool.exe push --modio --blueprints "My First Ship" --tags ship,large_grid`
