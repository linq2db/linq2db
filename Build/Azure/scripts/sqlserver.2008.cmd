rem SQL Server 2008 (host port 1408).
docker run -d -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Password12!" -p 1408:1433 -h mssql2008 --name=mssql2008 linq2db/linq2db:win-mssql-2008
docker ps -a

echo "Waiting for mssql2008 to accept connections"
set max=100
:repeat2008
set /a max=max-1
if %max% EQU 0 goto fail
sleep 1
docker exec mssql2008 sqlcmd -S localhost -U sa -P Password12! -Q "SELECT 1"
if %errorlevel% NEQ 0 goto repeat2008

docker exec mssql2008 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec mssql2008 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"
REM test-DB perf: SIMPLE recovery cuts transaction-log overhead (DELAYED_DURABILITY needs SQL 2014+, N/A here)
docker exec mssql2008 sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestData SET RECOVERY SIMPLE;"
docker exec mssql2008 sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestDataMS SET RECOVERY SIMPLE;"

goto:eof

:fail
echo "Fail"
docker logs mssql2008
exit /b 1
