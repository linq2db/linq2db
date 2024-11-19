ECHO OFF
SET tries=0

:repeat

docker ps 2>nul | FINDSTR /C:%1

IF %ERRORLEVEL% EQU 0 (
    SET /a tries=tries+1
    if %tries% EQU 100 (
        SET tries=0
        ECHO check failed 100 times
    )
    GOTO:repeat
)
