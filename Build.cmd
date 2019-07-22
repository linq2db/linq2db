cd /d "%~dp0"
"%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe" linq2db.sln /p:Configuration=Release /t:Restore;Rebuild /v:m
"%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe" linq2db.sln /p:Configuration=Debug   /t:Restore;Rebuild /v:m
"%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe" linq2db.sln /p:Configuration=Azure   /t:Restore;Rebuild /v:m

