set tries=0
:retry

docker run -d -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Password12!" -p 1433:1433 -h mssql --name=mssql linq2db/linq2db:win-mssql-2022
docker ps -a

echo "Waiting for SQL Server to accept connections"
set max=100
:repeat
set /a max=max-1
if %max% EQU 0 goto fail
echo pinging sql server
sleep 1

docker logs %1 2>nul | FINDSTR /C:"Error response from daemon: No such container: mssql"

IF %ERRORLEVEL% EQU 0 (
    SET /a tries=tries+1
    if %tries% EQU 5 (
        ECHO cannot start container
        GOTO:fail
    )
    ECHO failed to start container
    GOTO:retry
)

docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "SELECT 1"
if %errorlevel% NEQ 0 goto repeat
echo "SQL Server is operational"

docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "SELECT @@Version"

docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"

REM FTS required
goto:eof
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE Northwind;"
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE NorthwindMS;"
docker cp northwind.sql mssql:northwind.sql
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -d Northwind -i northwind.sql
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -d NorthwindMS -i northwind.sql

goto:eof

:fail
echo "Fail"
docker logs mssql
exit /b 1
