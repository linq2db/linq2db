rmdir ..\BuiltNuGet\built /S /Q
md ..\BuiltNuGet\built

nuget.exe Pack ..\BuiltNuGet\linq2db.cli.nuspec -OutputDirectory ..\BuiltNuGet\built

