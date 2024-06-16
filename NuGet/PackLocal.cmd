ECHO OFF

SET VERSION=%1
SET SNUPKG=%2
IF [%1] EQU [] (SET VERSION=6.0.0-local.1)
IF [%2] EQU [] (SET SNUPKG=)

WHERE nuget.exe >nul 2>&1
IF %errorlevel% NEQ 0 (
ECHO Cannot find nuget.exe. Add it to PATH or place it to current folder
ECHO nuget.exe could be downloaded from https://dist.nuget.org/win-x86-commandline/latest/nuget.exe
GOTO :EOF
)

cd ..
rem call Build.cmd
cd NuGet

dotnet script BuildNuspecs.csx /path:*.nuspec /buildPath:..\.build\nuspecs /version:%VERSION%
call Pack.cmd %SNUPKG%
pause
