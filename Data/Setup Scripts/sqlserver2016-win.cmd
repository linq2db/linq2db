ECHO OFF

REM try to remove existing container
docker stop sql2016
docker rm -f sql2016

REM use pull to get latest layers (run will use cached layers)
docker pull microsoft/mssql-server-2016-express-windows
docker run -e ACCEPT_EULA=Y -e SA_PASSWORD=Password12! -p 1416:1433 --name sql2016 -d microsoft/mssql-server-2016-express-windows

ECHO pause to wait for SQL Server startup completion
timeout 15

REM create test databases
docker exec sql2016 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec sql2016 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"

