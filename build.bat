@echo off
setlocal enabledelayedexpansion
set CONFIG=Release

call :FindVS

msbuild "%~dp0\WorkshopTool.sln" /m /t:Restore,Build /p:Configuration=%CONFIG%
vstest.console "%~dp0\Tests\bin\%CONFIG%\net461\Phoenix.WorkshopTool.Tests.dll" /Settings:"%~dp0\Tests\tests.runsettings"
exit /b %ERRORLEVEL%

goto :EOF

:FindVS
if NOT "%VSINSTALLDIR%"=="" goto :EOF

for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -version [15.0^,^) -products * -requires Microsoft.Component.MSBuild -property installationPath`) do (
	call "%%i\Common7\Tools\vsdevcmd.bat"
	goto :EOF
)
goto :EOF
