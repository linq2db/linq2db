ECHO OFF
SET TreatWarningsAsErrors=true
SET NUGETS=../.build/../NuGet
SET SNUPKG_EXPECTED=1221
SET EXPECTED=13

REM dotnet pack doesn't set exit code, so we need to count generated nugets
set ACTUAL=0
for %%x in ("%NUGETS%/*.nuspec") do @(SET /a ACTUAL+=1 >NUL)
IF %ACTUAL% NEQ %EXPECTED% (
    ECHO "Expected %EXPECTED% nugets but created %ACTUAL%"
    EXIT /b 1
)
set ACTUAL=0
for %%x in ("%NUGETS%/*.nuspec") do @(SET /a ACTUAL+=1 >NUL)
IF %ACTUAL% NEQ %SNUPKG_EXPECTED% (
    ECHO "Expected %SNUPKG_EXPECTED% nugets but created %ACTUAL%"
    EXIT /b 1
)

