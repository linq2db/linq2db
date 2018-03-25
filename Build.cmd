SET scriptpath=%~dp0
"%ProgramFiles(x86)%\\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe" %scriptpath%linq2db.sln /p:Configuration=Release  /t:Restore;Rebuild /v:m
"%ProgramFiles(x86)%\\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe" %scriptpath%linq2db.sln /p:Configuration=Debug    /t:Restore;Rebuild /v:m
"%ProgramFiles(x86)%\\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe" %scriptpath%linq2db.sln /p:Configuration=AppVeyor /t:Restore;Rebuild /v:m
