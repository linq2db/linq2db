ECHO OFF

IF [%1] EQU [] (
ECHO Version required
GOTO :EOF
)

SET VERSION=0.0.%1

powershell ..\Build\BuildNuspecs.ps1 -path linq2db.cli.nuspec -buildPath ..\BuiltNuGet -version %VERSION%
RMDIR ..\BuiltNuGet\built /S /Q
MD ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.cli.nuspec -OutputDirectory ..\BuiltNuGet\built

dotnet tool install -g --add-source ..\BuiltNuGet\built linq2db.cli
dotnet tool update -g --add-source ..\BuiltNuGet\built linq2db.cli
