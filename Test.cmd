cd /d "%~dp0"

ECHO OFF

SET CONFIG=%1
SET NETFX=%2
SET NET80=%3
SET NET90=%4
SET NET100=%5
SET FORMAT=%6
SET EXTRA=%7

IF [%1] EQU [] (SET CONFIG=Debug)
IF [%2] EQU [] (SET NETFX=1)
IF [%3] EQU [] (SET NET80=1)
IF [%4] EQU [] (SET NET90=1)
IF [%5] EQU [] (SET NET100=1)
IF [%6] EQU [] (SET FORMAT=html)

dotnet build linq2db.slnx -c %CONFIG% -v m

IF %NETFX%  NEQ 0 (dotnet test .build/bin/Tests/%CONFIG%/net462/linq2db.Tests.dll -f net462 -l %FORMAT%;LogFileName=net462.%FORMAT% %EXTRA%)
IF %NET80%  NEQ 0 (dotnet test .build/bin/Tests/%CONFIG%/net8.0/linq2db.Tests.dll -f net8.0 -l %FORMAT%;LogFileName=net80.%FORMAT% %EXTRA%)
IF %NET90%  NEQ 0 (dotnet test .build/bin/Tests/%CONFIG%/net9.0/linq2db.Tests.dll -f net9.0 -l %FORMAT%;LogFileName=net90.%FORMAT% %EXTRA%)
IF %NET100% NEQ 0 (dotnet test .build/bin/Tests/%CONFIG%/net10.0/linq2db.Tests.dll -f net10.0 -l %FORMAT%;LogFileName=net100.%FORMAT% %EXTRA%)

IF %NETFX%  NEQ 0 (dotnet test .build/bin/Tests.EntityFrameworkCore.EF3/%CONFIG%/net462/linq2db.EntityFrameworkCore.Tests.dll -f net462 -l %FORMAT%;LogFileName=net462.efcore.%FORMAT% %EXTRA%)
IF %NET80%  NEQ 0 (dotnet test .build/bin/Tests.EntityFrameworkCore.EF8/%CONFIG%/net8.0/linq2db.EntityFrameworkCore.Tests.dll -f net8.0 -l %FORMAT%;LogFileName=net80.efcore.%FORMAT% %EXTRA%)
IF %NET90%  NEQ 0 (dotnet test .build/bin/Tests.EntityFrameworkCore.EF9/%CONFIG%/net9.0/linq2db.EntityFrameworkCore.Tests.dll -f net9.0 -l %FORMAT%;LogFileName=net90.efcore.%FORMAT% %EXTRA%)
IF %NET100% NEQ 0 (dotnet test .build/bin/Tests.EntityFrameworkCore.EF10/%CONFIG%/net10.0/linq2db.EntityFrameworkCore.Tests.dll -f net10.0 -l %FORMAT%;LogFileName=net100.efcore.%FORMAT% %EXTRA%)
