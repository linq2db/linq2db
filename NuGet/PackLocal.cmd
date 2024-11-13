ECHO OFF

SET VERSION=%1
SET SNUPKG=%2
SET EF3_VERSION=%3
SET EF6_VERSION=%4
SET EF8_VERSION=%5
SET EF9_VERSION=%6
IF [%1] EQU [] (SET VERSION=6.0.0-local.1)
IF [%2] EQU [] (SET SNUPKG=)
IF [%3] EQU [] (SET EF3_VERSION=3.0.0-local.1)
IF [%4] EQU [] (SET EF6_VERSION=6.0.0-local.1)
IF [%5] EQU [] (SET EF8_VERSION=8.0.0-local.1)
IF [%6] EQU [] (SET EF9_VERSION=9.0.0-local.1)

WHERE nuget.exe >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
ECHO Cannot find nuget.exe. Add it to PATH or place it to current folder
ECHO nuget.exe could be downloaded from https://dist.nuget.org/win-x86-commandline/latest/nuget.exe
GOTO :EOF
)

CD ..
CALL Build.cmd
CD NuGet

powershell ..\Build\BuildNuspecs.ps1 -path *.nuspec -buildPath ..\.build\nuspecs -version %VERSION% -clean true

powershell ..\Build\BuildNuspecs.ps1 -path linq2db.EntityFrameworkCore.v3.nuspec -buildPath ..\.build\nuspecs -version %EF3_VERSION% -linq2DbVersion %VERSION%
powershell ..\Build\BuildNuspecs.ps1 -path linq2db.EntityFrameworkCore.v6.nuspec -buildPath ..\.build\nuspecs -version %EF6_VERSION% -linq2DbVersion %VERSION%
powershell ..\Build\BuildNuspecs.ps1 -path linq2db.EntityFrameworkCore.v8.nuspec -buildPath ..\.build\nuspecs -version %EF8_VERSION% -linq2DbVersion %VERSION%
powershell ..\Build\BuildNuspecs.ps1 -path linq2db.EntityFrameworkCore.v9.nuspec -buildPath ..\.build\nuspecs -version %EF9_VERSION% -linq2DbVersion %VERSION%

CALL Pack.cmd %SNUPKG%
