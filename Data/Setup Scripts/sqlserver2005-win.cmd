ECHO OFF

REM try to remove existing container
docker stop sql2005
docker rm -f sql2005

REM use pull to get latest layers (run will use cached layers)
docker pull linq2db/linq2db:win-mssql-2005
docker run -d -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Password12!" -p 1433:1433 -h mssql --name=mssql linq2db/linq2db:win-mssql-2005

ECHO pause to wait for SQL Server startup completion
timeout 90

REM create test databases
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"