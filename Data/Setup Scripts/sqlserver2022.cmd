ECHO OFF

REM try to remove existing container
docker stop sql2022
docker rm -f sql2022

REM use pull to get latest layers (run will use cached layers)
docker pull mcr.microsoft.com/mssql/server:2022-latest
docker run -e ACCEPT_EULA=Y -e SA_PASSWORD=Password12! -p 1422:1433 --name sql2022 -d mcr.microsoft.com/mssql/server:2022-latest

ECHO pause to wait for SQL Server startup completion
timeout 15

REM create test databases
REM collation added temporary due to issues with image
docker exec sql2022 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData  COLLATE SQL_Latin1_General_CP1_CI_AS;"
docker exec sql2022 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS  COLLATE SQL_Latin1_General_CP1_CI_AS;"

