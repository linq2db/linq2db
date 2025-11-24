ECHO OFF

SET VERSION=%1
SET EF3_VERSION=%2
SET EF8_VERSION=%3
SET EF9_VERSION=%4
SET EF10_VERSION=%5
IF [%1] EQU [] (SET VERSION=6.0.0-local.1)
IF [%2] EQU [] (SET EF3_VERSION=3.0.0-local.1)
IF [%3] EQU [] (SET EF8_VERSION=8.0.0-local.1)
IF [%4] EQU [] (SET EF9_VERSION=9.0.0-local.1)
IF [%5] EQU [] (SET EF10_VERSION=10.0.0-local.1)

cd ..
call Build.cmd
cd NuGet

dotnet tool install -g dotnet-script
dotnet script BuildNuspecs.csx /path:**\*.nuspec /buildPath:..\.build\nuspecs /version:%VERSION% /clean:true
dotnet script BuildNuspecs.csx /path:linq2db.EntityFrameworkCore.v3.nuspec /buildPath:..\.build\nuspecs /version:%EF3_VERSION% /linq2DbVersion:%VERSION%
dotnet script BuildNuspecs.csx /path:linq2db.EntityFrameworkCore.v8.nuspec /buildPath:..\.build\nuspecs /version:%EF8_VERSION% /linq2DbVersion:%VERSION%
dotnet script BuildNuspecs.csx /path:linq2db.EntityFrameworkCore.v9.nuspec /buildPath:..\.build\nuspecs /version:%EF9_VERSION% /linq2DbVersion:%VERSION%
dotnet script BuildNuspecs.csx /path:linq2db.EntityFrameworkCore.v10.nuspec /buildPath:..\.build\nuspecs /version:%EF10_VERSION% /linq2DbVersion:%VERSION%

call Pack.cmd
pause
