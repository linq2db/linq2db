ECHO OFF

SET VERSION=%1
SET SNUPKG=%2
SET EF3_VERSION=%3
SET EF6_VERSION=%4
SET EF8_VERSION=%5
IF [%1] EQU [] (SET VERSION=6.0.0-local.1)
IF [%2] EQU [] (SET SNUPKG=)
IF [%3] EQU [] (SET EF3_VERSION=3.0.0-local.1)
IF [%4] EQU [] (SET EF6_VERSION=6.0.0-local.1)
IF [%5] EQU [] (SET EF8_VERSION=8.0.0-local.1)

cd ..\Redist\
WHERE nuget.exe >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
ECHO Cannot find nuget.exe. Add it to PATH or place it to current folder
ECHO nuget.exe could be downloaded from https://dist.nuget.org/win-x86-commandline/latest/nuget.exe
GOTO :EOF
)
cd ..\NuGet\

cd ..
rem call Build.cmd

dotnet tool install -g dotnet-script
dotnet script BuildNuspecs.csx /path:**\*.nuspec /buildPath:..\.build\nuspecs /version:%VERSION%



/*
powershell ..\Build\BuildNuspecs.ps1 -path *.nuspec -buildPath ..\.build\nuspecs -version %VERSION% -clean true

powershell ..\Build\BuildNuspecs.ps1 -path linq2db.EntityFrameworkCore.v3.nuspec -buildPath ..\.build\nuspecs -version %EF3_VERSION% -linq2DbVersion %VERSION%
powershell ..\Build\BuildNuspecs.ps1 -path linq2db.EntityFrameworkCore.v6.nuspec -buildPath ..\.build\nuspecs -version %EF6_VERSION% -linq2DbVersion %VERSION%
powershell ..\Build\BuildNuspecs.ps1 -path linq2db.EntityFrameworkCore.v8.nuspec -buildPath ..\.build\nuspecs -version %EF8_VERSION% -linq2DbVersion %VERSION%

*/



call Pack.cmd %SNUPKG%
pause
