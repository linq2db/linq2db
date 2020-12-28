rmdir built /S /Q
md built

IF [%1] EQU [snupkg] (
nuget.exe Pack ..\BuiltNuGet\linq2db.nuspec -OutputDirectory ..\BuiltNuGet\built -Symbols -SymbolPackageFormat snupkg
nuget.exe Pack ..\BuiltNuGet\linq2db.AspNet.nuspec -OutputDirectory ..\BuiltNuGet\built -Symbols -SymbolPackageFormat snupkg
nuget.exe Pack ..\BuiltNuGet\linq2db.Tools.nuspec -OutputDirectory ..\BuiltNuGet\built -Symbols -SymbolPackageFormat snupkg
) ELSE (
REM Azure Artifacts doesn't support snupkg yet/still
REM https://developercommunity.visualstudio.com/idea/657354/add-snupkg-support-to-azure-devops-artifacts.html
nuget.exe Pack ..\BuiltNuGet\linq2db.nuspec -OutputDirectory ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.AspNet.nuspec -OutputDirectory ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.Tools.nuspec -OutputDirectory ..\BuiltNuGet\built
)

nuget.exe Pack ..\BuiltNuGet\linq2db.Access.nuspec -OutputDirectory ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.DB2.nuspec -OutputDirectory ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.DB2.Core.nuspec -OutputDirectory ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.Firebird.nuspec -OutputDirectory ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.Informix.nuspec -OutputDirectory ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.Informix.Core.nuspec -OutputDirectory ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.MySql.nuspec -OutputDirectory ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.MySqlConnector.nuspec -OutputDirectory ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.Oracle.Managed.nuspec -OutputDirectory ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.Oracle.Unmanaged.nuspec -OutputDirectory ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.PostgreSQL.nuspec -OutputDirectory ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.SapHana.nuspec -OutputDirectory ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.SqlCe.nuspec -OutputDirectory ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.SQLite.nuspec -OutputDirectory ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.SQLite.MS.nuspec -OutputDirectory ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.SqlServer.nuspec -OutputDirectory ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.SqlServer.MS.nuspec -OutputDirectory ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.Sybase.nuspec -OutputDirectory ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.Sybase.DataAction.nuspec -OutputDirectory ..\BuiltNuGet\built
nuget.exe Pack ..\BuiltNuGet\linq2db.t4models.nuspec -OutputDirectory ..\BuiltNuGet\built
