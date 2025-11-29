ECHO OFF
SET TreatWarningsAsErrors=true
SET NUSPECS=../.build/nuspecs
SET NUGETS=../.build/nugets

IF EXIST "%NUGETS%/" RMDIR "%NUGETS%" /S /Q
MD "%NUGETS%"

DIR "%NUSPECS%"

cmd /c "exit /b 0"

ECHO build nugets
FOR %%n IN (%NUSPECS%/*.nuspec) DO (
    ECHO %%n
    dotnet pack %NUSPECS%/%%n -o %NUGETS%
    IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%
)

DIR "%NUGETS%"
