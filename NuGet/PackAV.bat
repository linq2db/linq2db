@rem Building packages for appveyor
@rem
@rem first  parameter - target package version
@rem second parameter - target linq2db version

set version=%1%

echo packages version: %version% for linq2db version %2%

del *.nupkg

..\Redist\NuGet Pack linq2db.nuspec                  -Version %version%
..\Redist\NuGet Pack linq2db.t4models.nuspec         -Version %version%

..\Redist\NuGet Pack linq2db.Access.nuspec           -Version %version%
..\Redist\NuGet Pack linq2db.DB2.nuspec              -Version %version%
..\Redist\NuGet Pack linq2db.Firebird.nuspec         -Version %version%
..\Redist\NuGet Pack linq2db.Informix.nuspec         -Version %version%
..\Redist\NuGet Pack linq2db.MySql.nuspec            -Version %version%
..\Redist\NuGet Pack linq2db.Oracle.Managed.nuspec   -Version %version%
..\Redist\NuGet Pack linq2db.Oracle.Unmanaged.nuspec -Version %version%
..\Redist\NuGet Pack linq2db.PostgreSQL.nuspec       -Version %version%
..\Redist\NuGet Pack linq2db.SapHana.nuspec          -Version %version%
..\Redist\NuGet Pack linq2db.SqlCe.nuspec            -Version %version%
..\Redist\NuGet Pack linq2db.SQLite.nuspec           -Version %version%
..\Redist\NuGet Pack linq2db.SqlServer.nuspec        -Version %version%
..\Redist\NuGet Pack linq2db.Sybase.nuspec           -Version %version%
