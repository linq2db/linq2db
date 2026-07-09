rem SQL Server 2022 (host port 1422) and 2025 (host port 1425) run as concurrent lanes in one job.
rem Each SQL Server listens on 1433 inside its container; the host port differentiates them.
docker run -d -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Password12!" -p 1422:1433 -h mssql2022 --name=mssql2022 linq2db/linq2db:win-mssql-2022
docker run -d -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Password12!" -p 1425:1433 -h mssql2025 --name=mssql2025 linq2db/linq2db:win-mssql-2025
docker ps -a

echo "Waiting for mssql2022 to accept connections"
set max=100
:repeat2022
set /a max=max-1
if %max% EQU 0 goto fail
sleep 1
docker exec mssql2022 sqlcmd -S localhost -U sa -P Password12! -Q "SELECT 1"
if %errorlevel% NEQ 0 goto repeat2022

echo "Waiting for mssql2025 to accept connections"
set max=100
:repeat2025
set /a max=max-1
if %max% EQU 0 goto fail
sleep 1
rem SQL Server 2025 image ships mssql-tools18 sqlcmd, which defaults to encrypted
rem connections and rejects the self-signed cert without -C (trust server certificate).
docker exec mssql2025 sqlcmd -S localhost -U sa -P Password12! -Q "SELECT 1" -C
if %errorlevel% NEQ 0 goto repeat2025

docker exec mssql2022 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec mssql2022 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"
docker exec mssql2025 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;" -C
docker exec mssql2025 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;" -C

goto:eof

:fail
echo "Fail"
docker logs mssql2022
docker logs mssql2025
exit /b 1
