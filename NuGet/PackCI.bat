@rem Building packages for Azure
rmdir built /S /Q
md built

..\Redist\NuGet Pack linq2db.nuspec -OutputDirectory built
..\Redist\NuGet Pack linq2db.Tools.nuspec -OutputDirectory built

..\Redist\NuGet Pack linq2db.Access.nuspec -OutputDirectory built
..\Redist\NuGet Pack linq2db.DB2.nuspec -OutputDirectory built
..\Redist\NuGet Pack linq2db.DB2.Core.nuspec -OutputDirectory built
..\Redist\NuGet Pack linq2db.Firebird.nuspec -OutputDirectory built
..\Redist\NuGet Pack linq2db.Informix.nuspec -OutputDirectory built
..\Redist\NuGet Pack linq2db.Informix.Core.nuspec -OutputDirectory built
..\Redist\NuGet Pack linq2db.MySql.nuspec -OutputDirectory built
..\Redist\NuGet Pack linq2db.MySqlConnector.nuspec -OutputDirectory built
..\Redist\NuGet Pack linq2db.Oracle.Managed.nuspec -OutputDirectory built
..\Redist\NuGet Pack linq2db.Oracle.Unmanaged.nuspec -OutputDirectory built
..\Redist\NuGet Pack linq2db.PostgreSQL.nuspec -OutputDirectory built
..\Redist\NuGet Pack linq2db.SapHana.nuspec -OutputDirectory built
..\Redist\NuGet Pack linq2db.SqlCe.nuspec -OutputDirectory built
..\Redist\NuGet Pack linq2db.SQLite.nuspec -OutputDirectory built
..\Redist\NuGet Pack linq2db.SQLite.MS.nuspec -OutputDirectory built
..\Redist\NuGet Pack linq2db.SqlServer.nuspec -OutputDirectory built
..\Redist\NuGet Pack linq2db.SqlServer.MS.nuspec -OutputDirectory built
..\Redist\NuGet Pack linq2db.Sybase.nuspec -OutputDirectory built
..\Redist\NuGet Pack linq2db.Sybase.DataAction.nuspec -OutputDirectory built
..\Redist\NuGet Pack linq2db.t4models.nuspec -OutputDirectory built
