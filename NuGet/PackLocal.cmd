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

cd ..
call Build.cmd
cd NuGet

dotnet tool install -g dotnet-script
dotnet script BuildNuspecs.csx /path:**\*.nuspec /buildPath:..\.build\nuspecs /version:%VERSION% /clean:true
dotnet script BuildNuspecs.csx /path:linq2db.EntityFrameworkCore.v3.nuspec /buildPath:..\.build\nuspecs /version:%EF3_VERSION% /linq2DbVersion:%VERSION%
dotnet script BuildNuspecs.csx /path:linq2db.EntityFrameworkCore.v6.nuspec /buildPath:..\.build\nuspecs /version:%EF6_VERSION% /linq2DbVersion:%VERSION%
dotnet script BuildNuspecs.csx /path:linq2db.EntityFrameworkCore.v8.nuspec /buildPath:..\.build\nuspecs /version:%EF8_VERSION% /linq2DbVersion:%VERSION%
dotnet script BuildNuspecs.csx /path:linq2db.EntityFrameworkCore.v9.nuspec /buildPath:..\.build\nuspecs /version:%EF9_VERSION% /linq2DbVersion:%VERSION%

call Pack.cmd %SNUPKG%
pause
