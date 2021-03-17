:: This script creates a symlink to the game binaries to account for different installation directories on different systems.

@echo off

:Again1
set /p path="Please enter the folder location of your SpaceEngineers.exe: "
cd %~dp0
mklink /J SpaceEngineersBin64 "%path%"
if errorlevel 1 goto Error1
echo Done! - You can now open the solution without issue.
goto End1
:Error1
echo An error occured creating the symlink.
goto Again1
:End1
pause
