ECHO OFF

REM try to remove existing container
docker stop sql2025
docker rm -f sql2025

REM use pull to get latest layers (run will use cached layers)
docker pull linq2db/linq2db:win-mssql-2025
docker run -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=Password12! -p 1425:1433 --name sql2025 -d linq2db/linq2db:win-mssql-2025

ECHO pause to wait for SQL Server startup completion
timeout 15

REM create test databases
docker exec sql2025 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;" -C
docker exec sql2025 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;" -C

REM SqlServer.CS: case-sensitive data and catalog
docker exec sql2025 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataCS COLLATE Latin1_General_CS_AS;" -C
docker exec sql2025 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMSCS COLLATE Latin1_General_CS_AS;" -C

REM SqlServer.Contained: case-sensitive data, case-insensitive catalog
docker exec sql2025 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password12! -Q "sp_configure 'contained database authentication', 1;" -C
docker exec sql2025 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password12! -Q "RECONFIGURE;" -C
docker exec sql2025 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataContained CONTAINMENT = PARTIAL COLLATE Latin1_General_CS_AS;" -C
docker exec sql2025 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMSContained CONTAINMENT = PARTIAL COLLATE Latin1_General_CS_AS;" -C

