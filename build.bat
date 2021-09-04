@echo off
setlocal enabledelayedexpansion

for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -version [15.0^,^) -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
	"%%i" "%~dp0\WorkshopTool.sln" /t:Build /p:Configuration=Release /p:ContinuousIntegrationBuild=true
	exit /b !errorlevel!
)
