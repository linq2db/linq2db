#!/bin/bash

# SQL Server 2017 (host port 1417) and 2019 (host port 1419) run as concurrent lanes in one job.
# Each SQL Server listens on 1433 inside its container; the host port differentiates them.

# Wait until $name accepts connections. The mssql-2017/2019 images intermittently fail to come up
# on the CI agents (the SQL Server process crashes during startup, or the container exits early);
# when that happens, recreate the container - up to 3 attempts total - before giving up. On the
# happy path the container from the initial `docker run` below is already starting, so attempt 1
# just waits on it and this adds no extra latency.
wait_or_recreate() {
    local name=$1
    local hostport=$2
    local image=$3

    local attempt
    for attempt in 1 2 3; do
        if [ $attempt -gt 1 ]; then
            >&2 echo "Recreating $name (attempt $attempt/3)"
            docker logs $name 2>&1 | tail -n 40 || true
            docker rm -f $name > /dev/null 2>&1 || true
            docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Password12!' -p $hostport:1433 -h $name --name=$name -d $image
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

docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Password12!' -p 1417:1433 -h mssql2017 --name=mssql2017 -d linq2db/linq2db:mssql-2017
docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Password12!' -p 1419:1433 -h mssql2019 --name=mssql2019 -d linq2db/linq2db:mssql-2019
docker ps -a

wait_or_recreate mssql2017 1417 linq2db/linq2db:mssql-2017 || exit 1
wait_or_recreate mssql2019 1419 linq2db/linq2db:mssql-2019 || exit 1

for c in mssql2017 mssql2019; do
    docker exec $c /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestData;'
    docker exec $c /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataMS;'
    # test-DB perf: SIMPLE recovery + delayed durability cut transaction-log-flush cost on the write-heavy suite
    docker exec $c /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'ALTER DATABASE TestData SET RECOVERY SIMPLE; ALTER DATABASE TestData SET DELAYED_DURABILITY = FORCED;'
    docker exec $c /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'ALTER DATABASE TestDataMS SET RECOVERY SIMPLE; ALTER DATABASE TestDataMS SET DELAYED_DURABILITY = FORCED;'
done
