cd /d "%~dp0"

ECHO OFF

SET CONFIG=%1
SET NETFX=%2
SET NET100=%3
SET NET110=%4
SET EXTRA=%5

IF [%1] EQU [] (SET CONFIG=Debug)
IF [%2] EQU [] (SET NETFX=1)
IF [%3] EQU [] (SET NET100=1)
IF [%4] EQU [] (SET NET110=1)

dotnet build linq2db.slnx -c %CONFIG% -v m

REM Tests run on Microsoft.Testing.Platform: the test projects are executables, so run them directly
REM (net462 produces an .exe; net8.0+ produce an apphost .exe). VSTest's "-l <format>" is replaced by
REM --report-trx; --settings makes NUnit honor .runsettings (AssemblySelectLimit etc.).
IF %NETFX%  NEQ 0 (.build\bin\Tests\%CONFIG%\net462\linq2db.Tests.exe   --settings .runsettings --report-trx --report-trx-filename net462.trx --results-directory TestResults %EXTRA%)
IF %NET100% NEQ 0 (.build\bin\Tests\%CONFIG%\net10.0\linq2db.Tests.exe  --settings .runsettings --report-trx --report-trx-filename net100.trx --results-directory TestResults %EXTRA%)
IF %NET110% NEQ 0 (.build\bin\Tests\%CONFIG%\net11.0\linq2db.Tests.exe  --settings .runsettings --report-trx --report-trx-filename net110.trx --results-directory TestResults %EXTRA%)

IF %NETFX%  NEQ 0 (.build\bin\Tests.EntityFrameworkCore.EF3\%CONFIG%\net462\linq2db.EntityFrameworkCore.Tests.exe    --settings .runsettings --report-trx --report-trx-filename net462.efcore.trx --results-directory TestResults %EXTRA%)
IF %NET100% NEQ 0 (.build\bin\Tests.EntityFrameworkCore.EF10\%CONFIG%\net10.0\linq2db.EntityFrameworkCore.Tests.exe --settings .runsettings --report-trx --report-trx-filename net100.efcore.trx --results-directory TestResults %EXTRA%)
IF %NET110% NEQ 0 (.build\bin\Tests.EntityFrameworkCore.EF11\%CONFIG%\net11.0\linq2db.EntityFrameworkCore.Tests.exe --settings .runsettings --report-trx --report-trx-filename net110.efcore.trx --results-directory TestResults %EXTRA%)
