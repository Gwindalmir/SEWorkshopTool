@echo off
setlocal enabledelayedexpansion

call :FindVS

msbuild "%~dp0\WorkshopTool.sln" /m /t:Restore,Build /p:Configuration=Release /p:ContinuousIntegrationBuild=true
exit /b %ERRORLEVEL%

goto :EOF

:FindVS
if NOT "%VSINSTALLDIR%"=="" goto :EOF

for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -version [15.0^,^) -products * -requires Microsoft.Component.MSBuild -property installationPath`) do (
	call "%%i\Common7\Tools\vsdevcmd.bat"
	goto :EOF
)
goto :EOF
