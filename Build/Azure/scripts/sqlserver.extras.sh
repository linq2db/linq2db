#!/bin/bash

# Wait until $name accepts connections, recreating the container (up to 3 attempts total) when it crashes
# or exits during startup - same helper as sqlserver.2022_2025.sh, which runs the same image.
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

docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Password12!' -p 1433:1433 -h mssql --name=mssql -d linq2db/linq2db:mssql-2025
docker ps -a

wait_or_recreate mssql 1433 linq2db/linq2db:mssql-2025 || exit 1

docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'SELECT @@Version'

# SqlServer.CS: case-sensitive data and catalog (no CATALOG_COLLATION - catalog follows database collation)
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataCS COLLATE Latin1_General_CS_AS;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataMSCS COLLATE Latin1_General_CS_AS;'

# SqlServer.Contained: case-sensitive data, contained database catalog is always case-insensitive
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'sp_configure '"'"'contained database authentication'"'"', 1;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'RECONFIGURE;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataContained CONTAINMENT = PARTIAL COLLATE Latin1_General_CS_AS;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataMSContained CONTAINMENT = PARTIAL COLLATE Latin1_General_CS_AS;'

# test-DB perf: SIMPLE recovery + delayed durability cut transaction-log-flush cost on the write-heavy suite
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'ALTER DATABASE TestDataCS SET RECOVERY SIMPLE; ALTER DATABASE TestDataCS SET DELAYED_DURABILITY = FORCED;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'ALTER DATABASE TestDataMSCS SET RECOVERY SIMPLE; ALTER DATABASE TestDataMSCS SET DELAYED_DURABILITY = FORCED;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'ALTER DATABASE TestDataContained SET RECOVERY SIMPLE; ALTER DATABASE TestDataContained SET DELAYED_DURABILITY = FORCED;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'ALTER DATABASE TestDataMSContained SET RECOVERY SIMPLE; ALTER DATABASE TestDataMSContained SET DELAYED_DURABILITY = FORCED;'
