ECHO OFF
SET TreatWarningsAsErrors=true
SET NUSPECS=../.build/nuspecs
SET NUGETS=../.build/nugets

IF EXIST "%NUGETS%/" RMDIR "%NUGETS%" /S /Q
MD "%NUGETS%"

DIR "%NUSPECS%"

cmd /c "exit /b 0"

dotnet restore empty/empty.csproj

ECHO build binary nugets (with debug support)
FOR %%n IN (linq2db linq2db.Extensions linq2db.Compat linq2db.Tools linq2db.Scaffold linq2db.Remote.Grpc linq2db.Remote.Wcf linq2db.Remote.SignalR linq2db.Remote.HttpClient.Client linq2db.Remote.HttpClient.Server linq2db.FSharp linq2db.EntityFrameworkCore.v3 linq2db.EntityFrameworkCore.v8 linq2db.EntityFrameworkCore.v9) DO (
    ECHO %NUSPECS%/%%n.nuspec
    IF [%1] EQU [snupkg] (
        dotnet pack empty/empty.csproj --no-build -p:NuspecFile=../%NUSPECS%/%%n.nuspec -o %NUGETS% --include-symbols -p:SymbolPackageFormat=snupkg
        IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
    ) ELSE (
        REM Azure Artifacts doesn't support snupkg yet/still
        REM https://developercommunity.visualstudio.com/idea/657354/add-snupkg-support-to-azure-devops-artifacts.html
        dotnet pack empty/empty.csproj --no-build -p:NuspecFile=../%NUSPECS%/%%n.nuspec -o %NUGETS%
        IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
    )
)

ECHO build cli/t4 nugets (no debug support required)
FOR %%n IN (cli Access ClickHouse DB2 Firebird Informix MySql Oracle PostgreSQL SapHana SqlCe SQLite SqlServer Sybase t4models) DO (
    ECHO %NUSPECS%/%%n.nuspec
    dotnet pack empty/empty.csproj --no-build -p:NuspecFile=../%NUSPECS%/linq2db.%%n.nuspec -o %NUGETS%
    IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
)

DIR "%NUGETS%"
