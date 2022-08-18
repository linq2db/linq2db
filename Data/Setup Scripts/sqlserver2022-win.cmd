ECHO OFF

REM try to remove existing container
docker stop sql2022
docker rm -f sql2022

REM use pull to get latest layers (run will use cached layers)
docker pull cagrin/mssql-server-ltsc2022:2022-preview
docker run -e ACCEPT_EULA=Y -e SA_PASSWORD=Password12! -p 1422:1433 --name sql2022 -d cagrin/mssql-server-ltsc2022:2022-preview

ECHO pause to wait for SQL Server startup completion
timeout 15

REM create test databases
docker exec sql2022 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec sql2022 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"

