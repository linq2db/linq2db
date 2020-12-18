cd ..
call Build.cmd
cd NuGet

del *.nupkg
del *.snupkg

..\Redist\NuGet Pack linq2db.nuspec -Symbols -SymbolPackageFormat snupkg
..\Redist\NuGet Pack linq2db.AspNet.nuspec -Symbols -SymbolPackageFormat snupkg
..\Redist\NuGet Pack linq2db.Tools.nuspec -Symbols -SymbolPackageFormat snupkg

..\Redist\NuGet Pack linq2db.Access.nuspec
..\Redist\NuGet Pack linq2db.DB2.nuspec
..\Redist\NuGet Pack linq2db.DB2.Core.nuspec
..\Redist\NuGet Pack linq2db.Firebird.nuspec
..\Redist\NuGet Pack linq2db.Informix.nuspec
..\Redist\NuGet Pack linq2db.Informix.Core.nuspec
..\Redist\NuGet Pack linq2db.MySql.nuspec
..\Redist\NuGet Pack linq2db.MySqlConnector.nuspec
..\Redist\NuGet Pack linq2db.Oracle.Managed.nuspec
..\Redist\NuGet Pack linq2db.Oracle.Unmanaged.nuspec
..\Redist\NuGet Pack linq2db.PostgreSQL.nuspec
..\Redist\NuGet Pack linq2db.SapHana.nuspec
..\Redist\NuGet Pack linq2db.SqlCe.nuspec
..\Redist\NuGet Pack linq2db.SQLite.nuspec
..\Redist\NuGet Pack linq2db.SQLite.MS.nuspec
..\Redist\NuGet Pack linq2db.SqlServer.nuspec
..\Redist\NuGet Pack linq2db.SqlServer.MS.nuspec
..\Redist\NuGet Pack linq2db.Sybase.nuspec
..\Redist\NuGet Pack linq2db.Sybase.DataAction.nuspec
..\Redist\NuGet Pack linq2db.t4models.nuspec
