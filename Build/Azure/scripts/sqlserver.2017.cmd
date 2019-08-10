docker pull microsoft/mssql-server-windows-developer:2017-latest
docker run -d -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Password12!" -p 1433:1433 -h mssql --name=mssql microsoft/mssql-server-windows-developer:2017-latest
docker ps -a

echo "Waiting for SQL Server to accept connections"
:repeat
echo pinging sql server
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "SELECT 1"
if %errorlevel% NEQ 0 goto repeat
echo "SQL Server is operational"

echo "create TestData"
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
echo "create TestData2017"
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData2017;"
echo "copy Northwind"
docker cp scripts/northwind.sql mssql:/northwind.sql
echo "create Northwind"
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -i /northwind.sql
