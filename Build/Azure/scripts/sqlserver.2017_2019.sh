#!/bin/bash

# SQL Server 2017 (host port 1417) and 2019 (host port 1419) run as concurrent lanes in one job.
# Each SQL Server listens on 1433 inside its container; the host port differentiates them.

. "$(dirname "$0")/mssql-wait.sh"

docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Password12!' -p 1417:1433 -h mssql2017 --name=mssql2017 -d linq2db/linq2db:mssql-2017
docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Password12!' -p 1419:1433 -h mssql2019 --name=mssql2019 -d linq2db/linq2db:mssql-2019-fts
docker ps -a

wait_or_recreate mssql2017 1417 linq2db/linq2db:mssql-2017 || exit 1
wait_or_recreate mssql2019 1419 linq2db/linq2db:mssql-2019-fts || exit 1

docker exec mssql2017 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestData;'
docker exec mssql2017 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataMS;'

docker exec mssql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestData;'
docker exec mssql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataMS;'

# Northwind rides the 2019 server (SqlServer.Northwind/.MS point at port 1419). Case-sensitive and
# contained databases live in the SQL Server EXTRAS job.
docker exec mssql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE Northwind;'
docker exec mssql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE NorthwindMS;'
docker cp northwind.sql mssql2019:/northwind.sql
docker exec mssql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -d Northwind -i /northwind.sql
docker exec mssql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -d NorthwindMS -i /northwind.sql

# test-DB perf: SIMPLE recovery + delayed durability cut transaction-log-flush cost on the write-heavy suite
for c in mssql2017 mssql2019; do
    docker exec $c /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'ALTER DATABASE TestData SET RECOVERY SIMPLE; ALTER DATABASE TestData SET DELAYED_DURABILITY = FORCED;'
    docker exec $c /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'ALTER DATABASE TestDataMS SET RECOVERY SIMPLE; ALTER DATABASE TestDataMS SET DELAYED_DURABILITY = FORCED;'
done
