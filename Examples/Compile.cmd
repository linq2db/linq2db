@echo off

cd /d "%~dp0"

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
) else if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Preview" (
    echo.
    echo Using Visual Studio 2022 Preview
    echo.
    set "root_path=%ProgramFiles%\Microsoft Visual Studio\2022\Preview"
) else (
    echo Could not find an installed version of Visual Studio 2022 or Build Tools
    exit
)

set "msbuild_path=%root_path%\MSBuild\Current\Bin\amd64\MSBuild.exe"

REM Grpc
"%msbuild_path%" /v:m /target:Clean Remote\Grpc\Grpc.sln /property:Configuration=Debug
"%msbuild_path%" /v:m /target:Clean Remote\Grpc\Grpc.sln /property:Configuration=Release
"%msbuild_path%" /v:m Remote\Grpc\Grpc.sln /property:Configuration=Debug
"%msbuild_path%" /v:m Remote\Grpc\Grpc.sln /property:Configuration=Release

REM Soap
"%msbuild_path%" /v:m /target:Clean Remote\Soap\Soap.sln /property:Configuration=Debug
"%msbuild_path%" /v:m /target:Clean Remote\Soap\Soap.sln /property:Configuration=Release
"%msbuild_path%" /v:m Remote\Grpc\Grpc.sln /property:Configuration=Debug
"%msbuild_path%" /v:m Remote\Grpc\Grpc.sln /property:Configuration=Release

REM Wcf
"%msbuild_path%" /v:m /target:Clean Remote\Wcf\Wcf.sln /property:Configuration=Debug
"%msbuild_path%" /v:m /target:Clean Remote\Wcf\Wcf.sln /property:Configuration=Release
"%msbuild_path%" /v:m Remote\Wcf\Wcf.sln /property:Configuration=Debug
"%msbuild_path%" /v:m Remote\Wcf\Wcf.sln /property:Configuration=Release
