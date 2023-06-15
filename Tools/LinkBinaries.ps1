[cmdletBinding(SupportsShouldProcess=$false)]
param(
    [Parameter(Mandatory=$true)]
    [System.UInt32]$AppId = 244850
,
    [Parameter(Mandatory=$false)]
    [System.String]$LinkDestination = $null
,
    [switch]$Force = $false
)

function Get-ViaDepotDownloader {
    $dsrootdir = Resolve-Path -Path ([System.IO.Path]::Combine($LinkDestination, ".."))
    $dsdir = [System.IO.Path]::Combine($dsrootdir, "DedicatedServer64")

    $serverAppId = $AppId

    if($AppId -eq 244850) { $serverAppId = 298740 }
    elseif($AppId -eq 333950) { $serverAppId = 367970 }

    $depotId = $serverAppId + 1

    $ver = "2.5.0"
    $name = "depotdownloader-$($ver)"
    if (-not(Test-Path -Path "$($PSScriptRoot)\$($name)\DepotDownloader.exe" -PathType Leaf)) {
        # Don't allow parallel builds to each trigger this download
        $lockfile = $null
        $lockname = "$($PSScriptRoot)\.downloader.lock"
        try {
            $lockfile = [System.IO.File]::Open($lockname,'Append','Write', 'None')
            Write-Host "Fetching DepotDownloader..."
            Invoke-WebRequest -Uri "https://github.com/SteamRE/DepotDownloader/releases/download/DepotDownloader_$($ver)/$($name).zip" -OutFile "$($PSScriptRoot)\$($name).zip"
            Write-Host "Expanding DepotDownloader..."
            Expand-Archive "$($PSScriptRoot)\$($name).zip" -DestinationPath "$($PSScriptRoot)\$($name)"
        } catch {
        } finally {
            if($lockfile -ne $null) {
                $lockfile.Dispose()
                Remove-Item -Path $lockname
            }
            while(Test-Path -Path $lockname -PathType Leaf) { Start-Sleep 1 }
        }
    }

    $beta = ""
    Write-Host "Fetching Dedicated Server binaries to '$dsrootdir'..."
    if ($AppId -eq 333950) {
        $beta = "-beta communityedition"
    }
    & "$($PSScriptRoot)\$($name)\DepotDownloader.exe" -app $serverAppId -depot $depotId -dir $dsrootdir -filelist "$($PSScriptRoot)\filelist_ds.txt" $beta | Write-Verbose

    return $dsdir
}

if ($LinkDestination -and (Test-Path -Path $LinkDestination)) {
    if ($Force) {
        # -Force and -Recurse are required to delete a junction. However it won't delete the contents, as expected.
        Remove-Item $LinkDestination -Force -Recurse
    }
    else {
        Write-Verbose "Destination '$($LinkDestination)' already exists, skipping"
        exit 0
    }
}

$GamePath = $null

try {
    Import-Module $PSScriptRoot\Modules\SteamTools

    # Get steam library locations
    $steamPath = Get-SteamPath
    Write-Verbose "Steam Path: $($steamPath)"

    [array]$steamLibraries += $steamPath
    $librarys = ConvertFrom-VDF -InputObject (Get-Content "$($steamPath)\steamapps\libraryfolders.vdf")

    for ($i = 1; $true; $i++) {
        if ($librarys.LibraryFolders."$i" -eq $null) {
            break
        }

        $path = $librarys.LibraryFolders."$i".path.Replace("\\","\")
        Write-Verbose "Additional Steam library found in '$($path)'"
        [array]$steamLibraries += $path
    }

    ForEach ($library in ($steamLibraries) ) {
        Write-Verbose ("Checking library: " + $library)
        $manifest = "$($library)\SteamApps\appmanifest_$($AppId).acf"
        if (-not (Test-Path -Path $manifest -PathType Leaf)) { continue }
        $acf = ConvertFrom-VDF -InputObject (Get-Content $manifest -Encoding UTF8)
        if ($acf.AppState.appID -eq $AppId) {
            $GamePath = "$($library)\SteamApps\common\$($acf.AppState.InstallDir)\Bin64"
        }
    }
    if (-not $GamePath) { throw New-Object System.IO.DirectoryNotFoundException }
} catch {
    Write-Error $_
    Write-Information "Could not determine game install location."
    if($LinkDestination) {
        Write-Output "Getting DS from Steam instead..."
        $GamePath = Get-ViaDepotDownloader
    }
}

if ($GamePath) {
    if ($LinkDestination) {
        Write-Output ("Found Game: $($GamePath)")
        New-Item -Path $LinkDestination -ItemType Junction -Value "$($GamePath)" | Out-Null
        Write-Output ("Created symbolic link: $($LinkDestination) <-> $($GamePath)")
    }
    else {
        Write-Output $GamePath
    }
}
else {
    if ($AppId -eq 244850) {
        $path = Resolve-Path -Path "$($PSScriptRoot)\..\SEWorkshopTool"
        $msg = ("`nCould not find Space Engineers installation. Manually create a junction to the Bin64 directory.`n" + 
                "mklink /j '$($path)\Bin64' '<Location of SpaceEngineers\Bin64>'`n-")
        Write-Error $msg
    }
    elseif ($AppId -eq 333950) {
        $path = Resolve-Path -Path "$($PSScriptRoot)\..\MEWorkshopTool"
        $msg = ("`nCould not find Medieval Engineers installation. Manually create a junction to the Bin64 directory.`n" + 
                "mklink /j '$($path)\Bin64' '<Location of MedievalEngineers\Bin64>'`n-")
        Write-Error $msg
    }
    exit 1
}

exit 0
