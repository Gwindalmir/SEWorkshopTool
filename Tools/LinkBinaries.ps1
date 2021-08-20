[cmdletBinding(SupportsShouldProcess=$false)]
param(
	[Parameter(Position=0, Mandatory=$true)]
	[System.UInt32]$AppId = 244850
,
	[Parameter(Position=1, Mandatory=$false)]
	[System.String]$LinkDestination = $null
,
	[switch]$Force = $false
)

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

	$path = $librarys.LibraryFolders."$i".Replace("\\","\")
	Write-Verbose "Additional Steam library found in '$($path)'"
	[array]$steamLibraries += $path
}

$GamePath = $null

ForEach ($library in ($steamLibraries) ) {
	Write-Verbose ("Checking library: " + $library)
	ForEach ($file in (Get-ChildItem "$($library)\SteamApps\*.acf") ) {
		$acf = ConvertFrom-VDF (Get-Content $file -Encoding UTF8)
		if ($acf.AppState.appID -eq $AppId) {
			$GamePath = "$($library)\SteamApps\common\$($acf.AppState.InstallDir)"
		}
	}
}

if ($GamePath) {
	if ($LinkDestination) {
		Write-Output ("Found Game: $($GamePath)")
		New-Item -Path $LinkDestination -ItemType Junction -Value "$($GamePath)\Bin64"
	}
	else {
		Write-Output $GamePath
	}
}
else {
	if ($AppId -eq 244850) {
		$path = Resolve-Path -Path '$($PSScriptRoot)\..\SEWorkshopTool'
		$msg = ("`nCould not find Space Engineers installation. Manually create a junction to the Bin64 directory.`n" + 
				"mklink /j '$($path)\Bin64' '<Location of SpaceEngineers\Bin64>'`n-")
		Write-Error $msg
	}
	elseif ($AppId -eq 333950) {
		$path = Resolve-Path -Path '$($PSScriptRoot)\..\MEWorkshopTool'
		$msg = ("`nCould not find Medieval Engineers installation. Manually create a junction to the Bin64 directory.`n" + 
				"mklink /j '$($path)\Bin64' '<Location of MedievalEngineers\Bin64>'`n-")
		Write-Error $msg
	}
	exit 1
}

exit 0
