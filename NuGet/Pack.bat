cd ..\..\linq2db\Source
call Compile.bat

cd ..\..\linq2db.t4models\NuGet

del *.nupkg

copy /b AddTool.Linq2db.txt        + ..\Templates\LinqToDB.ttinclude            LinqToDB.ttinclude
copy /b AddTool.SQLite.txt         + ..\Templates\LinqToDB.SQLite.ttinclude     LinqToDB.SQLite.ttinclude
copy /b AddTool.PostgreSQL.txt     + ..\Templates\LinqToDB.PostgreSQL.ttinclude LinqToDB.PostgreSQL.ttinclude
copy /b AddTool.Firebird.txt       + ..\Templates\LinqToDB.Firebird.ttinclude   LinqToDB.Firebird.ttinclude
copy /b AddTool.MySql.txt          + ..\Templates\LinqToDB.MySql.ttinclude      LinqToDB.MySql.ttinclude
copy /b AddTool.SqlCe.txt          + ..\Templates\LinqToDB.SqlCe.ttinclude      LinqToDB.SqlCe.ttinclude
copy /b AddTool.SqlServer.txt      + ..\Templates\LinqToDB.SqlServer.ttinclude  LinqToDB.SqlServer.ttinclude
copy /b AddTool.Sybase.txt         + ..\Templates\LinqToDB.Sybase.ttinclude     LinqToDB.Sybase.ttinclude
copy /b AddTool.Oracle.x86.txt     + ..\Templates\LinqToDB.Oracle.ttinclude     LinqToDB.Oracle.x86.ttinclude
copy /b AddTool.Oracle.x64.txt     + ..\Templates\LinqToDB.Oracle.ttinclude     LinqToDB.Oracle.x64.ttinclude
copy /b AddTool.Oracle.Managed.txt + ..\Templates\LinqToDB.Oracle.ttinclude     LinqToDB.Oracle.Managed.ttinclude

..\Redist\NuGet Pack linq2db.t4models.nuspec

..\Redist\NuGet Pack linq2db.Access.nuspec
..\Redist\NuGet Pack linq2db.Firebird.nuspec
..\Redist\NuGet Pack linq2db.MySql.nuspec
..\Redist\NuGet Pack linq2db.SqlCe.nuspec
..\Redist\NuGet Pack linq2db.SQLite.nuspec
..\Redist\NuGet Pack linq2db.PostgreSQL.nuspec
..\Redist\NuGet Pack linq2db.SqlServer.nuspec
..\Redist\NuGet Pack linq2db.Sybase.nuspec
..\Redist\NuGet Pack linq2db.Oracle.x86.nuspec
..\Redist\NuGet Pack linq2db.Oracle.x64.nuspec
..\Redist\NuGet Pack linq2db.Oracle.Managed.nuspec

del *.ttinclude
