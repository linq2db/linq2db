ECHO OFF

REM try to remove existing container
docker stop sql2019
docker rm -f sql2019

REM use pull to get latest layers (run will use cached layers)
REM docker pull linq2db/linq2db:mssql-2019-fts
docker run -e ACCEPT_EULA=Y -e SA_PASSWORD=Password12! -p 1419:1433 --name sql2019 -d linq2db/linq2db:mssql-2019-fts

call wait sql2019 "Recovery is complete"

REM create test databases
REM docker exec sql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
REM docker exec sql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"

docker exec sql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData COLLATE Latin1_General_CS_AS WITH CATALOG_COLLATION = SQL_Latin1_General_CP1_CI_AS;"
docker exec sql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS COLLATE Latin1_General_CS_AS WITH CATALOG_COLLATION = SQL_Latin1_General_CP1_CI_AS;"

docker exec sql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataSA;"
docker exec sql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMSSA;"

docker exec sql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "sp_configure 'contained database authentication', 1;"
docker exec sql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "RECONFIGURE;"
docker exec sql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataContained CONTAINMENT = PARTIAL;"
docker exec sql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMSContained CONTAINMENT = PARTIAL;"

docker exec sql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "CREATE DATABASE Northwind;"
docker exec sql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "CREATE DATABASE NorthwindMS;"

docker cp "../Create Scripts/Northwind.sql" sql2019:/northwind.sql

docker exec sql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -d Northwind -i /northwind.sql
docker exec sql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -d NorthwindMS -i /northwind.sql


