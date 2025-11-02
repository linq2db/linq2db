ECHO OFF
SET tries=0

:repeat

REM try to create test database
docker exec %1 psql -U postgres -c "create database testdata"
docker exec %1 psql -U postgres -c \l | FINDSTR /C:testdata

IF %ERRORLEVEL% NEQ 0 (
    SET /a tries=tries+1
    if %tries% EQU 100 (
        SET tries=0
        ECHO check failed 100 times
    )
    GOTO:repeat
)
