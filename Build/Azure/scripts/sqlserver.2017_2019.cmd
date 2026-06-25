rem SQL Server 2017 (host port 1417) and 2019 (host port 1419) run as concurrent lanes in one job.
rem Each SQL Server listens on 1433 inside its container; the host port differentiates them.
docker run -d -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Password12!" -p 1417:1433 -h mssql2017 --name=mssql2017 linq2db/linq2db:win-mssql-2017
docker run -d -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Password12!" -p 1419:1433 -h mssql2019 --name=mssql2019 linq2db/linq2db:win-mssql-2019
docker ps -a

echo "Waiting for mssql2017 to accept connections"
set max=100
:repeat2017
set /a max=max-1
if %max% EQU 0 goto fail
sleep 1
docker exec mssql2017 sqlcmd -S localhost -U sa -P Password12! -Q "SELECT 1"
if %errorlevel% NEQ 0 goto repeat2017

echo "Waiting for mssql2019 to accept connections"
set max=100
:repeat2019
set /a max=max-1
if %max% EQU 0 goto fail
sleep 1
docker exec mssql2019 sqlcmd -S localhost -U sa -P Password12! -Q "SELECT 1"
if %errorlevel% NEQ 0 goto repeat2019

docker exec mssql2017 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec mssql2017 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"
docker exec mssql2019 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec mssql2019 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"

goto:eof

:fail
echo "Fail"
docker logs mssql2017
docker logs mssql2019
exit /b 1
