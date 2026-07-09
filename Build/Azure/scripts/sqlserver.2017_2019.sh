#!/bin/bash

# SQL Server 2017 (host port 1417) and 2019 (host port 1419) run as concurrent lanes in one job.
# Each SQL Server listens on 1433 inside its container; the host port differentiates them.
docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Password12!' -p 1417:1433 -h mssql2017 --name=mssql2017 -d linq2db/linq2db:mssql-2017
docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Password12!' -p 1419:1433 -h mssql2019 --name=mssql2019 -d linq2db/linq2db:mssql-2019
docker ps -a

for c in mssql2017 mssql2019; do
    echo "Waiting for $c to accept connections"
    retries=0
    until docker exec $c /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "SELECT 1"; do
        sleep 1
        retries=`expr $retries + 1`
        if [ $retries -gt 120 ]; then
            >&2 echo "Failed to wait for $c to start."
            docker logs $c
            exit 1
        fi;
    done
    echo "$c is operational"
    docker exec $c /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestData;'
    docker exec $c /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataMS;'
done
