docker run -d -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Password12!" -p 1433:1433 -h mssql --name=mssql linq2db/linq2db:win-mssql-2019
docker ps -a

echo "Waiting for SQL Server to accept connections"
set max=100
:repeat
set /a max=max-1
if %max% EQU 0 goto fail
echo pinging sql server
sleep 1
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "SELECT 1"
if %errorlevel% NEQ 0 goto repeat
echo "SQL Server is operational"

docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "SELECT @@Version"

docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData COLLATE Latin1_General_CS_AS WITH CATALOG_COLLATION = SQL_Latin1_General_CP1_CI_AS;"
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS COLLATE Latin1_General_CS_AS WITH CATALOG_COLLATION = SQL_Latin1_General_CP1_CI_AS;"

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
