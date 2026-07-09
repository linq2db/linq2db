rem SQL Server 2012 (host port 1412).
docker run -d -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Password12!" -p 1412:1433 -h mssql2012 --name=mssql2012 linq2db/linq2db:win-mssql-2012
docker ps -a

echo "Waiting for mssql2012 to accept connections"
set max=100
:repeat2012
set /a max=max-1
if %max% EQU 0 goto fail
sleep 1
docker exec mssql2012 sqlcmd -S localhost -U sa -P Password12! -Q "SELECT 1"
if %errorlevel% NEQ 0 goto repeat2012

docker exec mssql2012 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec mssql2012 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"

goto:eof

:fail
echo "Fail"
docker logs mssql2012
exit /b 1
