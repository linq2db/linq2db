cd /d "%~dp0"

ECHO OFF

SET CONFIG=%1
SET NET46=%2
SET NETCOREAPP21=%3
SET FORMAT=%4

IF [%1] EQU [] (SET CONFIG=Debug)
IF [%2] EQU [] (SET NET46=1)
IF [%3] EQU [] (SET NETCOREAPP21=1)
IF [%4] EQU [] (SET FORMAT=html)

ECHO Configuration=%CONFIG%, net46 enabled:%NET46%, netcoreapp2.1 enabled:%NETCOREAPP21%, format:%FORMAT%

"%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe" linq2db.sln /p:Configuration=%CONFIG% /t:Restore;Build /v:m

IF %NET46%        NEQ 0 (dotnet vstest ./Tests/Linq/bin/%CONFIG%/net46/linq2db.Tests.dll         /Framework:.NETFramework,Version=v4.6 /logger:%FORMAT%;LogFileName=net46.%FORMAT%)
IF %NETCOREAPP21% NEQ 0 (dotnet vstest ./Tests/Linq/bin/%CONFIG%/netcoreapp2.1/linq2db.Tests.dll /Framework:.NETCoreApp,Version=v2.1   /logger:%FORMAT%;LogFileName=netcoreapp21.%FORMAT%)
