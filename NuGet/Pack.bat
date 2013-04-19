cd ..\..\linq2db\Source
call Compile.bat

cd ..\..\linq2db.t4models\NuGet

del *.nupkg

copy /b AddTool.Linq2db.txt   + ..\Templates\LinqToDB.ttinclude           LinqToDB.ttinclude
copy /b AddTool.SQLite.txt    + ..\Templates\LinqToDB.SQLite.ttinclude    LinqToDB.SQLite.ttinclude
copy /b AddTool.MySql.txt     + ..\Templates\LinqToDB.MySql.ttinclude     LinqToDB.MySql.ttinclude
copy /b AddTool.SqlServer.txt + ..\Templates\LinqToDB.SqlServer.ttinclude LinqToDB.SqlServer.ttinclude
copy /b AddTool.SqlCe.txt     + ..\Templates\LinqToDB.SqlCe.ttinclude     LinqToDB.SqlCe.ttinclude

..\Redist\NuGet Pack linq2db.t4models.nuspec
rem rename linq2db.t4models.*.nupkg linq2db.t4models.nupkg

..\Redist\NuGet Pack linq2db.Access.nuspec
rem ..\Redist\NuGet Pack linq2db.Firebird.nuspec
..\Redist\NuGet Pack linq2db.MySql.nuspec
..\Redist\NuGet Pack linq2db.SqlCe.nuspec
..\Redist\NuGet Pack linq2db.SQLite.nuspec
..\Redist\NuGet Pack linq2db.SqlServer.nuspec

del *.ttinclude
