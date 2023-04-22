ECHO OFF

REM try to remove existing container
docker stop sql2017
docker rm -f sql2017

REM use pull to get latest layers (run will use cached layers)
docker pull mcr.microsoft.com/mssql/server:2017-latest
docker run -e ACCEPT_EULA=Y -e SA_PASSWORD=Password12! -p 1417:1433 --name sql2017 -d mcr.microsoft.com/mssql/server:2017-latest

call wait sql2017 "Recovery is complete"

REM create test databases
docker exec sql2017 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec sql2017 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"

