ECHO OFF

REM try to remove existing container
docker stop sql2022
docker rm -f sql2022

REM use pull to get latest layers (run will use cached layers)
docker pull mcr.microsoft.com/mssql/server:2022-latest
docker run -e ACCEPT_EULA=Y -e SA_PASSWORD=Password12! -p 1422:1433 --name sql2022 -d mcr.microsoft.com/mssql/server:2022-latest

call wait sql2022 "Recovery is complete"

REM create test databases
docker exec sql2022 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec sql2022 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"

