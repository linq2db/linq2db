cd ..\..\linq2db\Source
call Compile.bat

cd ..\..\t4Models\NuGet

del *.nupkg

copy /b AddLinq2dbTool.txt + ..\Templates\LinqToDB.ttinclude LinqToDB.ttinclude

..\Redist\NuGet Pack linq2db.t4models.nuspec
rem rename linq2db.t4models.*.nupkg linq2db.t4models.nupkg

..\Redist\NuGet Pack linq2db.Access.nuspec
rem ..\Redist\NuGet Pack linq2db.Firebird.nuspec
..\Redist\NuGet Pack linq2db.SqlCe.nuspec
..\Redist\NuGet Pack linq2db.SqlServer.nuspec

del LinqToDB.ttinclude
