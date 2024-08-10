SET NUSPECS="..\.build\nuspecs"
SET NUGETS="..\.build\nugets"

RMDIR %NUGETS% /S /Q
MD %NUGETS%

IF [%1] EQU [snupkg] (
nuget.exe Pack %NUSPECS%\linq2db.nuspec -OutputDirectory %NUGETS% -Symbols -SymbolPackageFormat snupkg
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.Extensions.nuspec -OutputDirectory %NUGETS% -Symbols -SymbolPackageFormat snupkg
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.Tools.nuspec -OutputDirectory %NUGETS% -Symbols -SymbolPackageFormat snupkg
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.Remote.Grpc.nuspec -OutputDirectory %NUGETS% -Symbols -SymbolPackageFormat snupkg
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.Remote.Wcf.nuspec -OutputDirectory %NUGETS% -Symbols -SymbolPackageFormat snupkg
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.FSharp.nuspec -OutputDirectory %NUGETS% -Symbols -SymbolPackageFormat snupkg
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.EntityFrameworkCore.v3.nuspec -OutputDirectory %NUGETS% -Symbols -SymbolPackageFormat snupkg
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.EntityFrameworkCore.v6.nuspec -OutputDirectory %NUGETS% -Symbols -SymbolPackageFormat snupkg
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.EntityFrameworkCore.v8.nuspec -OutputDirectory %NUGETS% -Symbols -SymbolPackageFormat snupkg
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
) ELSE (
REM Azure Artifacts doesn't support snupkg yet/still
REM https://developercommunity.visualstudio.com/idea/657354/add-snupkg-support-to-azure-devops-artifacts.html
nuget.exe Pack %NUSPECS%\linq2db.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.Extensions.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.Tools.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.Remote.Grpc.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.Remote.Wcf.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.FSharp.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.EntityFrameworkCore.v3.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.EntityFrameworkCore.v6.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.EntityFrameworkCore.v8.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
)

nuget.exe Pack %NUSPECS%\linq2db.cli.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%

nuget.exe Pack %NUSPECS%\linq2db.Access.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.DB2.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.DB2.Core.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.Firebird.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.Informix.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.Informix.Core.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.MySql.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.MySqlConnector.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.Oracle.Managed.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.Oracle.Unmanaged.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.PostgreSQL.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.SapHana.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.SqlCe.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.SQLite.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.SQLite.MS.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.SqlServer.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.SqlServer.MS.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.Sybase.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.Sybase.DataAction.nuspec -OutputDirectory %NUGETS%
IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
nuget.exe Pack %NUSPECS%\linq2db.t4models.nuspec -OutputDirectory %NUGETS%
