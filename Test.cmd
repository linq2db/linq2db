cd /d "%~dp0"

ECHO OFF

SET CONFIG=%1
SET NETFX=%2
SET NET80=%3
SET NET90=%4
SET NET100=%5
SET EXTRA=%6

IF [%1] EQU [] (SET CONFIG=Debug)
IF [%2] EQU [] (SET NETFX=1)
IF [%3] EQU [] (SET NET80=1)
IF [%4] EQU [] (SET NET90=1)
IF [%5] EQU [] (SET NET100=1)

dotnet build linq2db.slnx -c %CONFIG% -v m

REM Tests run on Microsoft.Testing.Platform: the test projects are executables, so run them directly
REM (net462 produces an .exe; net8.0+ produce an apphost .exe). VSTest's "-l <format>" is replaced by
REM --report-trx; --settings makes NUnit honor .runsettings (AssemblySelectLimit etc.).
IF %NETFX%  NEQ 0 (.build\bin\Tests\%CONFIG%\net462\linq2db.Tests.exe   --settings .runsettings --report-trx --report-trx-filename net462.trx  --results-directory TestResults %EXTRA%)
IF %NET80%  NEQ 0 (.build\bin\Tests\%CONFIG%\net8.0\linq2db.Tests.exe   --settings .runsettings --report-trx --report-trx-filename net80.trx   --results-directory TestResults %EXTRA%)
IF %NET90%  NEQ 0 (.build\bin\Tests\%CONFIG%\net9.0\linq2db.Tests.exe   --settings .runsettings --report-trx --report-trx-filename net90.trx   --results-directory TestResults %EXTRA%)
IF %NET100% NEQ 0 (.build\bin\Tests\%CONFIG%\net10.0\linq2db.Tests.exe  --settings .runsettings --report-trx --report-trx-filename net100.trx  --results-directory TestResults %EXTRA%)

IF %NETFX%  NEQ 0 (.build\bin\Tests.EntityFrameworkCore.EF3\%CONFIG%\net462\linq2db.EntityFrameworkCore.Tests.exe    --settings .runsettings --report-trx --report-trx-filename net462.efcore.trx --results-directory TestResults %EXTRA%)
IF %NET80%  NEQ 0 (.build\bin\Tests.EntityFrameworkCore.EF8\%CONFIG%\net8.0\linq2db.EntityFrameworkCore.Tests.exe   --settings .runsettings --report-trx --report-trx-filename net80.efcore.trx  --results-directory TestResults %EXTRA%)
IF %NET90%  NEQ 0 (.build\bin\Tests.EntityFrameworkCore.EF9\%CONFIG%\net9.0\linq2db.EntityFrameworkCore.Tests.exe   --settings .runsettings --report-trx --report-trx-filename net90.efcore.trx  --results-directory TestResults %EXTRA%)
IF %NET100% NEQ 0 (.build\bin\Tests.EntityFrameworkCore.EF10\%CONFIG%\net10.0\linq2db.EntityFrameworkCore.Tests.exe --settings .runsettings --report-trx --report-trx-filename net100.efcore.trx --results-directory TestResults %EXTRA%)
