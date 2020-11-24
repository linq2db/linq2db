cd /d "%~dp0"

ECHO OFF

SET CONFIG=%1
SET NET472=%2
SET NETCOREAPP21=%3
SET NETCOREAPP31=%4
SET NET50=%5
SET FORMAT=%6

IF [%1] EQU [] (SET CONFIG=Debug)
IF [%2] EQU [] (SET NET472=1)
IF [%3] EQU [] (SET NETCOREAPP21=1)
IF [%4] EQU [] (SET NETCOREAPP31=1)
IF [%5] EQU [] (SET NET50=1)
IF [%6] EQU [] (SET FORMAT=html)

ECHO Configuration=%CONFIG%, net472 enabled:%NET472%, netcoreapp2.1 enabled:%NETCOREAPP21%, format:%FORMAT%

"%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe" linq2db.sln /p:Configuration=%CONFIG% /t:Restore;Build /v:m

IF %NET472%       NEQ 0 (dotnet vstest ./Tests/Linq/bin/%CONFIG%/net472/linq2db.Tests.dll        /Framework:.NETFramework,Version=v4.7.2 /logger:%FORMAT%;LogFileName=net472.%FORMAT%)
IF %NETCOREAPP21% NEQ 0 (dotnet vstest ./Tests/Linq/bin/%CONFIG%/netcoreapp2.1/linq2db.Tests.dll /Framework:.NETCoreApp,Version=v2.1     /logger:%FORMAT%;LogFileName=netcoreapp21.%FORMAT%)
IF %NETCOREAPP31% NEQ 0 (dotnet vstest ./Tests/Linq/bin/%CONFIG%/netcoreapp3.1/linq2db.Tests.dll /Framework:.NETCoreApp,Version=v3.1     /logger:%FORMAT%;LogFileName=netcoreapp31.%FORMAT%)
IF %NET50%        NEQ 0 (dotnet vstest ./Tests/Linq/bin/%CONFIG%/net5.0/linq2db.Tests.dll        /Framework:net5.0                       /logger:%FORMAT%;LogFileName=net50.%FORMAT%)
