ECHO OFF

IF [%1] EQU [] (
ECHO Version required
GOTO :EOF
)

SET NUSPECS="..\.build\nuspecs"
SET NUGETS="..\.build\nugets"

SET VERSION=0.0.%1

dotnet tool install -g dotnet-script
dotnet script BuildNuspecs.csx /path:linq2db.cli.nuspec /buildPath:%NUSPECS% /version:%VERSION%

RMDIR %NUGETS% /S /Q
MD %NUGETS%
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.cli.nuspec -OutputDirectory %NUGETS%

dotnet tool uninstall linq2db.cli -g
dotnet tool install -g --add-source %NUGETS% linq2db.cli --version %VERSION%

