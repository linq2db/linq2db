SET NUSPECS="..\.build\nuspecs"
SET NUGETS="..\.build\nugets"

dir ..\Redist\*.*

RMDIR %NUGETS% /S /Q
MD %NUGETS%

DIR %NUGETS%
DIR %NUGETS%

rem cmd /c "exit /b 0"


ECHO build binary nugets (with debug support)
FOR %%n IN (linq2db linq2db.Extensions linq2db.Tools linq2db.Remote.Grpc linq2db.Remote.Wcf linq2db.FSharp linq2db.EntityFrameworkCore.v3 linq2db.EntityFrameworkCore.v6 linq2db.EntityFrameworkCore.v8 linq2db.EntityFrameworkCore.v9) DO (
    ECHO %NUSPECS%\%%n.nuspec
    IF [%1] EQU [snupkg] (
        ..\Redist\nuget.exe Pack %NUSPECS%\%%n.nuspec -OutputDirectory %NUGETS% -Symbols -SymbolPackageFormat snupkg
        IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
    ) ELSE (
        REM Azure Artifacts doesn't support snupkg yet/still
        REM https://developercommunity.visualstudio.com/idea/657354/add-snupkg-support-to-azure-devops-artifacts.html
        ..\Redist\nuget.exe Pack %NUSPECS%\%%n.nuspec -OutputDirectory %NUGETS%
        IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
    )
)

ECHO build cli/t4 nugets (no debug support required)
FOR %%n IN (cli Access ClickHouse DB2 Firebird Informix MySql Oracle PostgreSQL SapHana SqlCe SQLite SqlServer Sybase t4models) DO (
    ECHO %NUSPECS%\%%n.nuspec
    ..\Redist\nuget.exe Pack %NUSPECS%\linq2db.%%n.nuspec -OutputDirectory %NUGETS%
    IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
)
