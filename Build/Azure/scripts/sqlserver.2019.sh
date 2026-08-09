#!/bin/bash

# SQL Server 2019 (full-text-search image), published on two host ports because the connection
# strings for this one server are split across both:
#   1417/1419 scheme  -> SqlServer.2019 / SqlServer.2019.MS use "Server=localhost,1419"
#   default 1433      -> SqlServer.SA / .Contained / .Northwind still use bare "Server=localhost"
# Publishing only 1419 leaves those six providers pointing at a port nothing listens on, which does
# not fail fast - every connection attempt burns the SqlClient connect timeout instead.

# Wait until $name accepts connections. The mssql-2017/2019 images intermittently fail to come up
# on the CI agents (the SQL Server process crashes during startup, or the container exits early);
# when that happens, recreate the container - up to 3 attempts total - before giving up. On the
# happy path the container from the initial `docker run` below is already starting, so attempt 1
# just waits on it and this adds no extra latency.
wait_or_recreate() {
    local name=$1
    local portargs=$2
    local image=$3

    local attempt
    for attempt in 1 2 3; do
        if [ $attempt -gt 1 ]; then
            >&2 echo "Recreating $name (attempt $attempt/3)"
            docker logs $name 2>&1 | tail -n 40 || true
            docker rm -f $name > /dev/null 2>&1 || true
            docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Password12!' $portargs -h $name --name=$name -d $image
        fi

        local retries=0
        while true; do
            if docker exec $name /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "SELECT 1"; then
                echo "$name is operational (attempt $attempt)"
                return 0
            fi

            # A crashed/exited container will never accept connections: stop waiting immediately and
            # recreate it instead of burning the whole timeout on a dead container.
            if [ "$(docker inspect -f '{{.State.Running}}' $name 2> /dev/null)" != "true" ]; then
                echo "$name is not running (crashed or exited during startup)"
                break
            fi

            sleep 1
            retries=`expr $retries + 1`
            if [ $retries -gt 120 ]; then
                echo "$name did not accept connections within 120s"
                break
            fi
        done
    done

    >&2 echo "Failed to start $name after 3 attempts."
    docker logs $name || true
    return 1
}

docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Password12!' -p 1419:1433 -p 1433:1433 -h mssql2019 --name=mssql2019 -d linq2db/linq2db:mssql-2019-fts
docker ps -a

wait_or_recreate mssql2019 "-p 1419:1433 -p 1433:1433" linq2db/linq2db:mssql-2019-fts || exit 1

docker exec mssql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'SELECT @@Version'

docker exec mssql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestData COLLATE Latin1_General_CS_AS WITH CATALOG_COLLATION = SQL_Latin1_General_CP1_CI_AS;'
docker exec mssql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataMS COLLATE Latin1_General_CS_AS WITH CATALOG_COLLATION = SQL_Latin1_General_CP1_CI_AS;'

docker exec mssql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataSA;'
docker exec mssql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataMSSA;'

docker exec mssql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'sp_configure '"'"'contained database authentication'"'"', 1;'
docker exec mssql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'RECONFIGURE;'
docker exec mssql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataContained CONTAINMENT = PARTIAL;'
docker exec mssql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataMSContained CONTAINMENT = PARTIAL;'

docker exec mssql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE Northwind;'
docker exec mssql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE NorthwindMS;'

docker cp northwind.sql mssql2019:/northwind.sql
docker exec mssql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -d Northwind -i /northwind.sql
docker exec mssql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -d NorthwindMS -i /northwind.sql

# test-DB perf: SIMPLE recovery + delayed durability cut transaction-log-flush cost on the write-heavy suite
for db in TestData TestDataMS TestDataSA TestDataMSSA TestDataContained TestDataMSContained; do
    docker exec mssql2019 /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "ALTER DATABASE $db SET RECOVERY SIMPLE; ALTER DATABASE $db SET DELAYED_DURABILITY = FORCED;"
done
