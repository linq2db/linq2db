cd /d "%~dp0"

ECHO OFF

SET CONFIG=%1
SET NET46=%2
SET NETCOREAPP20=%3

IF [%1] EQU [] (SET CONFIG=Debug)
IF [%2] EQU [] (SET NET46=1)
IF [%3] EQU [] (SET NETCOREAPP20=1)

ECHO Configuration=%CONFIG%, net46 enabled:%NET46%, netcoreapp2.0 enabled:%NETCOREAPP20%

"%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe" linq2db.sln /p:Configuration=%CONFIG% /t:Restore;Build /v:m

IF %NET46%        NEQ 0 (dotnet vstest ./Tests/Linq/bin/%CONFIG%/net46/linq2db.Tests.dll         /Framework:.NETFramework,Version=v4.6 /logger:trx;LogFileName=net46.trx)
IF %NETCOREAPP20% NEQ 0 (dotnet vstest ./Tests/Linq/bin/%CONFIG%/netcoreapp2.0/linq2db.Tests.dll /Framework:.NETCoreApp,Version=v2.0   /logger:trx;LogFileName=netcoreapp20.trx)
