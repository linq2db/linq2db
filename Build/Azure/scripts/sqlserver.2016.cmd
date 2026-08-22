rem SQL Server 2016 (host port 1416).
docker run -d -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Password12!" -p 1416:1433 -h mssql2016 --name=mssql2016 linq2db/linq2db:win-mssql-2016
docker ps -a

echo "Waiting for mssql2016 to accept connections"
set max=100
:repeat2016
set /a max=max-1
if %max% EQU 0 goto fail
sleep 1
docker exec mssql2016 sqlcmd -S localhost -U sa -P Password12! -Q "SELECT 1"
if %errorlevel% NEQ 0 goto repeat2016

docker exec mssql2016 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec mssql2016 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"
REM test-DB perf: SIMPLE recovery + delayed durability cut transaction-log-flush cost on the write-heavy suite
docker exec mssql2016 sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestData SET RECOVERY SIMPLE; ALTER DATABASE TestData SET DELAYED_DURABILITY = FORCED;"
docker exec mssql2016 sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestDataMS SET RECOVERY SIMPLE; ALTER DATABASE TestDataMS SET DELAYED_DURABILITY = FORCED;"

goto:eof

:fail
echo "Fail"
docker logs mssql2016
exit /b 1
