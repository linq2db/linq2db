#!/bin/bash

# SQL Server 2022 (host port 1422) and 2025 (host port 1425) run as concurrent lanes in one job.
# Each SQL Server listens on 1433 inside its container; the host port differentiates them.
docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Password12!' -p 1422:1433 -h mssql2022 --name=mssql2022 -d linq2db/linq2db:mssql-2022
docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Password12!' -p 1425:1433 -h mssql2025 --name=mssql2025 -d linq2db/linq2db:mssql-2025
docker ps -a

for c in mssql2022 mssql2025; do
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
