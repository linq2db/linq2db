ECHO OFF

REM try to remove existing container
docker stop sql2025
docker rm -f sql2025

REM use pull to get latest layers (run will use cached layers)
docker pull linq2db/linq2db:mssql-2025
docker run -e ACCEPT_EULA=Y -e SA_PASSWORD=Password12! -p 1425:1433 --name sql2025 -d linq2db/linq2db:mssql-2025

call wait sql2025 "Recovery is complete"

REM create test databases
docker exec sql2025 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec sql2025 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"

