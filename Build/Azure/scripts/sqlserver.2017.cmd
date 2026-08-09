rem SQL Server 2017 on host port 1417 (the container always listens on 1433 internally).
docker run -d -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Password12!" -p 1417:1433 -h mssql2017 --name=mssql2017 linq2db/linq2db:win-mssql-2017
docker ps -a

echo "Waiting for mssql2017 to accept connections"
set max=100
:repeat2017
set /a max=max-1
if %max% EQU 0 goto fail
sleep 1
docker exec mssql2017 sqlcmd -S localhost -U sa -P Password12! -Q "SELECT 1"
if %errorlevel% NEQ 0 goto repeat2017

docker exec mssql2017 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec mssql2017 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"
REM test-DB perf: SIMPLE recovery + delayed durability cut transaction-log-flush cost on the write-heavy suite
docker exec mssql2017 sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestData SET RECOVERY SIMPLE; ALTER DATABASE TestData SET DELAYED_DURABILITY = FORCED;"
docker exec mssql2017 sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestDataMS SET RECOVERY SIMPLE; ALTER DATABASE TestDataMS SET DELAYED_DURABILITY = FORCED;"

goto:eof

:fail
echo "Fail"
docker logs mssql2017
exit /b 1
