SET VERSION=%1
SET SNUPKG=%2
IF [%1] EQU [] (SET VERSION=3.0.0-local1)
IF [%2] EQU [] (SET SNUPKG=)

cd ..
call Build.cmd
cd NuGet

powershell ..\Build\BuildNuspecs.ps1 -path *.nuspec -buildPath ..\BuiltNuGet -version %VERSION%
call Pack.bat %SNUPKG%
