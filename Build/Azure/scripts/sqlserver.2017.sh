#!/bin/bash
docker pull mcr.microsoft.com/mssql/server:2017-latest
docker run -it -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Password12!' -p 1433:1433 -h mssql --name=mssql -d mcr.microsoft.com/mssql/server:2017-latest
docker ps -a
sleep 5
docker exec -it mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password12! -Q 'select @@Version'
docker exec -it mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestData;'
docker exec -it mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestData2017;'
docker exec -it mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password12! -Q 'CREATE DATABASE NorthwindDB;'
docker cp scripts/sql/northwind.sql mssql:/northwind.sql
docker exec -it mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password12! -i /northwind.sql
