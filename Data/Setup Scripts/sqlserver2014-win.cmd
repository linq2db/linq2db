ECHO OFF

REM try to remove existing container
docker stop sql2014
docker rm -f sql2014

REM use pull to get latest layers (run will use cached layers)
docker pull linq2db/linq2db:win-mssql-2014
docker run -d -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Password12!" -p 1414:1433 --name sql2014 linq2db/linq2db:win-mssql-2014

ECHO pause to wait for SQL Server startup completion
timeout 90

REM create test databases
docker exec sql2014 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec sql2014 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"
