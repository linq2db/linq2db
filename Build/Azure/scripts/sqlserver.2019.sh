#!/bin/bash

docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Password12!' -p 1433:1433 -h mssql --name=mssql -d linq2db/linq2db:mssql-2019-fts
docker ps -a

# Wait for start
echo "Waiting for SQL Server to accept connections"
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "SELECT 1"
is_up=$?
while [ $is_up -ne 0 ] ; do
    docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "SELECT 1"
    is_up=$?
done
echo "SQL Server is operational"

docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'SELECT @@Version'

docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestData COLLATE Latin1_General_CS_AS WITH CATALOG_COLLATION = SQL_Latin1_General_CP1_CI_AS;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataMS COLLATE Latin1_General_CS_AS WITH CATALOG_COLLATION = SQL_Latin1_General_CP1_CI_AS;'

docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataSA;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataMSSA;'

docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'sp_configure '"'"'contained database authentication'"'"', 1;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'RECONFIGURE;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataContained CONTAINMENT = PARTIAL;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataMSContained CONTAINMENT = PARTIAL;'

# test-DB perf: SIMPLE recovery + delayed durability cut transaction-log-flush cost on the write-heavy suite
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'ALTER DATABASE TestData SET RECOVERY SIMPLE; ALTER DATABASE TestData SET DELAYED_DURABILITY = FORCED;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'ALTER DATABASE TestDataMS SET RECOVERY SIMPLE; ALTER DATABASE TestDataMS SET DELAYED_DURABILITY = FORCED;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'ALTER DATABASE TestDataSA SET RECOVERY SIMPLE; ALTER DATABASE TestDataSA SET DELAYED_DURABILITY = FORCED;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'ALTER DATABASE TestDataMSSA SET RECOVERY SIMPLE; ALTER DATABASE TestDataMSSA SET DELAYED_DURABILITY = FORCED;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'ALTER DATABASE TestDataContained SET RECOVERY SIMPLE; ALTER DATABASE TestDataContained SET DELAYED_DURABILITY = FORCED;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'ALTER DATABASE TestDataMSContained SET RECOVERY SIMPLE; ALTER DATABASE TestDataMSContained SET DELAYED_DURABILITY = FORCED;'

docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE Northwind;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE NorthwindMS;'

docker cp northwind.sql mssql:/northwind.sql
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -d Northwind -i /northwind.sql
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -d NorthwindMS -i /northwind.sql
