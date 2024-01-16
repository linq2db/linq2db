cd /d "%~dp0"

ECHO OFF

SET CONFIG=%1
SET NETFX=%2
SET NET60=%3
SET NET80=%4
SET FORMAT=%5
SET EXTRA=%6

IF [%1] EQU [] (SET CONFIG=Debug)
IF [%2] EQU [] (SET NETFX=1)
IF [%3] EQU [] (SET NET60=1)
IF [%4] EQU [] (SET NET80=1)
IF [%5] EQU [] (SET FORMAT=html)

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

IF %NETFX% NEQ 0 (dotnet test ./Tests/Linq/bin/%CONFIG%/net462/linq2db.Tests.dll -f net462 -l %FORMAT%;LogFileName=net462.%FORMAT% %EXTRA%)
IF %NET60%  NEQ 0 (dotnet test ./Tests/Linq/bin/%CONFIG%/net6.0/linq2db.Tests.dll -f net6.0 -l %FORMAT%;LogFileName=net60.%FORMAT% %EXTRA%)
IF %NET80%  NEQ 0 (dotnet test ./Tests/Linq/bin/%CONFIG%/net8.0/linq2db.Tests.dll -f net8.0 -l %FORMAT%;LogFileName=net80.%FORMAT% %EXTRA%)

