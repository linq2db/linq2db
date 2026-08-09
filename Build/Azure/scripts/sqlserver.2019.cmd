rem SQL Server 2019 on host port 1419 (the container always listens on 1433 internally).
rem The Windows leg runs only SqlServer.2019/.MS, so no 1433 publish or extra databases are needed
rem here - unlike the Linux/macOS leg, which also serves the SA / Contained / Northwind providers.
docker run -d -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Password12!" -p 1419:1433 -h mssql2019 --name=mssql2019 linq2db/linq2db:win-mssql-2019
docker ps -a

echo "Waiting for mssql2019 to accept connections"
set max=100
:repeat2019
set /a max=max-1
if %max% EQU 0 goto fail
sleep 1
docker exec mssql2019 sqlcmd -S localhost -U sa -P Password12! -Q "SELECT 1"
if %errorlevel% NEQ 0 goto repeat2019

docker exec mssql2019 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec mssql2019 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"
REM test-DB perf: SIMPLE recovery + delayed durability cut transaction-log-flush cost on the write-heavy suite
docker exec mssql2019 sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestData SET RECOVERY SIMPLE; ALTER DATABASE TestData SET DELAYED_DURABILITY = FORCED;"
docker exec mssql2019 sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestDataMS SET RECOVERY SIMPLE; ALTER DATABASE TestDataMS SET DELAYED_DURABILITY = FORCED;"

goto:eof

:fail
echo "Fail"
docker logs mssql2019
exit /b 1
