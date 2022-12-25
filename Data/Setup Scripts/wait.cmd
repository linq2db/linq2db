ECHO OFF
SET tries=0

:repeat

docker logs %1 | FINDSTR /C:%2

IF %ERRORLEVEL% NEQ 0 (
    SET /a tries=tries+1
    if %tries% EQU 100 (
        SET tries=0
        ECHO check failed 100 times
    )
    GOTO:repeat
)
