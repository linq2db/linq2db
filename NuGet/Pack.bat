cd ..\Source
call Compile.bat

cd ..\NuGet

del *.nupkg

..\Redist\NuGet Pack linq2db.nuspec
