rem without real 2005 image we use 2012 in compatibility mode
docker run -d -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Password12!" -p 1433:1433 -h mssql --name=mssql dbafromthecold/sqlserver2012express:rtm
docker ps -a

echo "Waiting for SQL Server to accept connections"
:repeat
echo pinging sql server
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "SELECT 1"
if %errorlevel% NEQ 0 goto repeat
echo "SQL Server is operational"

docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "SELECT @@Version"
echo "create TestData"
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;ALTER DATABASE TestData SET COMPATIBILITY_LEVEL = 90;"
echo "create TestData2005"
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData2005;ALTER DATABASE TestData2005 SET COMPATIBILITY_LEVEL = 90;"
