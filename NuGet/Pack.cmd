SET NUSPECS="..\.build\nuspecs"
SET NUGETS="..\.build\nugets"

rmdir %NUGETS% /S /Q
md %NUGETS%

cmd /c "exit /b 0"

IF [%1] EQU [snupkg] (
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.nuspec -OutputDirectory %NUGETS% -Symbols -SymbolPackageFormat snupkg
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.Extensions.nuspec -OutputDirectory %NUGETS% -Symbols -SymbolPackageFormat snupkg
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.Tools.nuspec -OutputDirectory %NUGETS% -Symbols -SymbolPackageFormat snupkg
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.Remote.Grpc.nuspec -OutputDirectory %NUGETS% -Symbols -SymbolPackageFormat snupkg
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.Remote.Wcf.nuspec -OutputDirectory %NUGETS% -Symbols -SymbolPackageFormat snupkg
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.FSharp.nuspec -OutputDirectory %NUGETS% -Symbols -SymbolPackageFormat snupkg
if %errorlevel% neq 0 exit
) ELSE (
REM Azure Artifacts doesn't support snupkg yet/still
REM https://developercommunity.visualstudio.com/idea/657354/add-snupkg-support-to-azure-devops-artifacts.html
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.Extensions.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.Tools.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.Remote.Grpc.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.Remote.Wcf.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.FSharp.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
)

..\Redist\nuget.exe Pack %NUSPECS%\linq2db.cli.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit

..\Redist\nuget.exe Pack %NUSPECS%\linq2db.Access.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.DB2.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.DB2.Core.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.ClickHouse.Client.nuspec         -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.ClickHouse.MySqlConnector.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.ClickHouse.Octonica.nuspec       -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.Firebird.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.Informix.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.Informix.Core.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.MySql.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.MySqlConnector.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.Oracle.Managed.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.Oracle.Unmanaged.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.PostgreSQL.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.SapHana.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.SqlCe.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.SQLite.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.SQLite.MS.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.SqlServer.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.SqlServer.MS.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.Sybase.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.Sybase.DataAction.nuspec -OutputDirectory %NUGETS%
if %errorlevel% neq 0 exit
..\Redist\nuget.exe Pack %NUSPECS%\linq2db.t4models.nuspec -OutputDirectory %NUGETS%
