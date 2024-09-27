SET NUSPECS="..\.build\nuspecs"
SET NUGETS="..\.build\nugets"

RMDIR %NUGETS% /S /Q
MD %NUGETS%

REM build binary nugets (with debug support)
FOR %%n IN (linq2db linq2db.Extensions linq2db.Tools linq2db.Remote.Grpc linq2db.Remote.Wcf linq2db.FSharp linq2db.EntityFrameworkCore.v3 linq2db.EntityFrameworkCore.v6 linq2db.EntityFrameworkCore.v8) DO (
    IF [%1] EQU [snupkg] (
        nuget.exe Pack %NUSPECS%\%%n.nuspec -OutputDirectory %NUGETS% -Symbols -SymbolPackageFormat snupkg
        IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
    ) ELSE (
        REM Azure Artifacts doesn't support snupkg yet/still
        REM https://developercommunity.visualstudio.com/idea/657354/add-snupkg-support-to-azure-devops-artifacts.html
        nuget.exe Pack %NUSPECS%\%%n.nuspec -OutputDirectory %NUGETS%
        IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
    )
)

REM build cli/t4 nugets (no debug support required)
FOR %%n IN (cli Access DB2 DB2.Core Firebird Informix Informix.Core MySql MySqlConnector Oracle.Managed Oracle.Unmanaged PostgreSQL SapHana SqlCe SQLite SQLite.MS SqlServer SqlServer.MS Sybase Sybase.DataAction t4models) DO (
    nuget.exe Pack %NUSPECS%\linq2db.%%n.nuspec -OutputDirectory %NUGETS%
    IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
)
