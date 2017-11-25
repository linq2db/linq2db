cd ..
call Build.cmd

cd NuGet

del *.nupkg

..\Redist\NuGet Pack linq2db.nuspec
