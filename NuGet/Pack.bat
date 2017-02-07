@rem cd ..\..\linq2db\Source
@rem call Compile.bat

cd ..\..\linq2db.t4models\ToolsGenerator

%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ToolsGenerator.csproj /property:Configuration=Release

cd ..\NuGet

del *.nupkg

..\ToolsGenerator\bin\Release\ToolsGenerator.exe

..\Redist\NuGet Pack linq2db.t4models.nuspec

..\Redist\NuGet Pack linq2db.Access.nuspec
..\Redist\NuGet Pack linq2db.Firebird.nuspec
..\Redist\NuGet Pack linq2db.MySql.nuspec
..\Redist\NuGet Pack linq2db.SqlCe.nuspec
..\Redist\NuGet Pack linq2db.SQLite.nuspec
..\Redist\NuGet Pack linq2db.PostgreSQL.nuspec
..\Redist\NuGet Pack linq2db.SqlServer.nuspec
..\Redist\NuGet Pack linq2db.Sybase.nuspec
..\Redist\NuGet Pack linq2db.SapHana.nuspec
..\Redist\NuGet Pack linq2db.Oracle.x86.nuspec
..\Redist\NuGet Pack linq2db.Oracle.x64.nuspec
..\Redist\NuGet Pack linq2db.Oracle.Managed.nuspec
..\Redist\NuGet Pack linq2db.DB2.nuspec
..\Redist\NuGet Pack linq2db.Informix.nuspec

del *.ttinclude
