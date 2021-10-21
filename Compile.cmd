@echo off

cd /d "%~dp0"

if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise" (
	echo.
	echo Using Visual Studio 2019 Enterprise
	echo.
	set "msbuild_path=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
) else if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community" (
	echo.
	echo Using Visual Studio 2019 Community
	echo.
	set "msbuild_path=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
) else (
	echo Could not find an installed version of Visual Studio 2019 Enterprise or Community
	exit
)

"%msbuild_path%" /v:m /target:Clean Source\LinqToDB\LinqToDB.csproj /property:Configuration=Debug
"%msbuild_path%" /v:m /target:Clean Source\LinqToDB\LinqToDB.csproj /property:Configuration=Release
"%msbuild_path%" /v:m Source\LinqToDB\LinqToDB.csproj /property:Configuration=Debug
"%msbuild_path%" /v:m Source\LinqToDB\LinqToDB.csproj /property:Configuration=Release