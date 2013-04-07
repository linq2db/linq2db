cd ..\..\linq2db\Source
call Compile.bat

cd ..\..\t4Models\NuGet

del *.nupkg

copy /b AddLinq2dbTool.txt + ..\Templates\LinqToDB.ttinclude LinqToDB.ttinclude

NuGet Pack linq2db.t4models.nuspec
rem rename linq2db.t4models.*.nupkg linq2db.t4models.nupkg

NuGet Pack linq2db.SqlServer.nuspec

del LinqToDB.ttinclude