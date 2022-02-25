cd /d "%~dp0"

ECHO OFF

SET CONFIG=%1
SET NET472=%2
SET NETCOREAPP31=%3
SET NET50=%4
SET NET60=%5
SET FORMAT=%6
SET EXTRA=%7

IF [%1] EQU [] (SET CONFIG=Debug)
IF [%2] EQU [] (SET NET472=1)
IF [%3] EQU [] (SET NETCOREAPP31=1)
IF [%4] EQU [] (SET NET50=1)
IF [%5] EQU [] (SET NET60=1)
IF [%6] EQU [] (SET FORMAT=html)

if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2022\BuildTools" (
    echo.
    echo Using Visual Studio 2022 Build Tools
    echo.
    set "root_path=%ProgramFiles(x86)%\Microsoft Visual Studio\2022\BuildTools"
) else if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise" (
    echo.
    echo Using Visual Studio 2022 Enterprise
    echo.
    set "root_path=%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise"
) else if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Professional" (
    echo.
    echo Using Visual Studio 2022 Professional
    echo.
    set "root_path=%ProgramFiles%\Microsoft Visual Studio\2022\Professional"
) else if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Community" (
    echo.
    echo Using Visual Studio 2022 Community
    echo.
    set "root_path=%ProgramFiles%\Microsoft Visual Studio\2022\Community"
) else (
    echo Could not find an installed version of Visual Studio 2022 or Build Tools
    exit
)

set "msbuild_path=%root_path%\MSBuild\Current\Bin\amd64\MSBuild.exe"

"%msbuild_path%" linq2db.sln /p:Configuration=%CONFIG% /t:Restore;Build /v:m

IF %NET472%       NEQ 0 (dotnet test ./Tests/Linq/bin/%CONFIG%/net472/linq2db.Tests.dll        -f net472        -l %FORMAT%;LogFileName=net472.%FORMAT% %EXTRA%)
IF %NETCOREAPP31% NEQ 0 (dotnet test ./Tests/Linq/bin/%CONFIG%/netcoreapp3.1/linq2db.Tests.dll -f netcoreapp3.1 -l %FORMAT%;LogFileName=netcoreapp31.%FORMAT% %EXTRA%)
IF %NET50%        NEQ 0 (dotnet test ./Tests/Linq/bin/%CONFIG%/net5.0/linq2db.Tests.dll        -f net5.0        -l %FORMAT%;LogFileName=net50.%FORMAT% %EXTRA%)
IF %NET60%        NEQ 0 (dotnet test ./Tests/Linq/bin/%CONFIG%/net6.0/linq2db.Tests.dll        -f net6.0        -l %FORMAT%;LogFileName=net60.%FORMAT% %EXTRA%)

