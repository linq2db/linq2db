rem SQL Server 2014 (host port 1414) and 2016 (host port 1416) run as concurrent lanes in one job.
rem Each SQL Server listens on 1433 inside its container; the host port differentiates them.
docker run -d -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Password12!" -p 1414:1433 -h mssql2014 --name=mssql2014 linq2db/linq2db:win-mssql-2014
docker run -d -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Password12!" -p 1416:1433 -h mssql2016 --name=mssql2016 linq2db/linq2db:win-mssql-2016
docker ps -a

echo "Waiting for mssql2014 to accept connections"
set max=100
:repeat2014
set /a max=max-1
if %max% EQU 0 goto fail
sleep 1
docker exec mssql2014 sqlcmd -S localhost -U sa -P Password12! -Q "SELECT 1"
if %errorlevel% NEQ 0 goto repeat2014

echo "Waiting for mssql2016 to accept connections"
set max=100
:repeat2016
set /a max=max-1
if %max% EQU 0 goto fail
sleep 1
docker exec mssql2016 sqlcmd -S localhost -U sa -P Password12! -Q "SELECT 1"
if %errorlevel% NEQ 0 goto repeat2016

docker exec mssql2014 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec mssql2014 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"
docker exec mssql2016 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec mssql2016 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"

goto:eof

:fail
echo "Fail"
docker logs mssql2014
docker logs mssql2016
exit /b 1
