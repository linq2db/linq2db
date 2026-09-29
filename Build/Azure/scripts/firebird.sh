#!/bin/bash

# Start all four Firebird versions, each in its own container on a distinct host port and database
# file (matching CommonConnectionStrings: 2.5 -> 3025/testdb25, 3 -> 3030/testdb30, 4 -> 3040/testdb40,
# 5 -> 3050/testdb50), so all four Firebird providers run as concurrent lanes within a single CI job.
# Firebird listens on 3050 inside every container; the host port differentiates them.
# 3/4/5 use the official firebirdsql/firebird images (newer point releases; DB created under
# /var/lib/firebird/data). 2.5 is EOL with no official image, so it stays on jacobalberty
# (DB under /firebird/data). Paths are reflected in DataProviders.json connection strings.
docker run -d --name firebird25 -e ISC_PASSWORD=masterkey        -e FIREBIRD_DATABASE=testdb25.fdb                                                                            -p 3025:3050 jacobalberty/firebird:2.5-sc
docker run -d --name firebird3  -e FIREBIRD_ROOT_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb30.fdb -e FIREBIRD_USE_LEGACY_AUTH=true -e FIREBIRD_DATABASE_DEFAULT_CHARSET=UTF8 -p 3030:3050 firebirdsql/firebird:3
docker run -d --name firebird4  -e FIREBIRD_ROOT_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb40.fdb -e FIREBIRD_USE_LEGACY_AUTH=true -e FIREBIRD_DATABASE_DEFAULT_CHARSET=UTF8 -p 3040:3050 firebirdsql/firebird:4
docker run -d --name firebird5  -e FIREBIRD_ROOT_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb50.fdb -e FIREBIRD_USE_LEGACY_AUTH=true -e FIREBIRD_DATABASE_DEFAULT_CHARSET=UTF8 -p 3050:3050 firebirdsql/firebird:5
docker ps -a

# Readiness is probed with isql rather than grepped from the container log the way the other merged setup
# scripts do it: a successful connect proves the database exists and the engine accepts connections, which
# no log marker does, and it holds whatever a given image chooses to log - this script spans two image
# families whose logging differs. Four containers now initialise concurrently on one agent, so the
# pre-merge fixed waits (5s for 2.5, 15s for 3/4/5, one container per job) no longer bound readiness, and
# there was no failure detection at all: a container that died during startup produced no diagnostic and
# the leg failed later against something that never came up.
# isql lives in /usr/local/firebird/bin in the jacobalberty images and /opt/firebird/bin in the
# firebirdsql ones, and is not on PATH in the former, so both are added.
wait_for_firebird () {
    name=$1
    db=$2
    retries=0

    until docker exec "$name" sh -c "PATH=\$PATH:/usr/local/firebird/bin:/opt/firebird/bin; isql -u SYSDBA -p masterkey '$db' -q -i /dev/null" > /dev/null 2>&1; do
        if [ "$(docker inspect -f '{{.State.Running}}' "$name" 2> /dev/null)" != "true" ]; then
            echo "$name is not running (crashed or exited during startup)"
            docker logs "$name"
            exit 1
        fi

        sleep 2
        retries=`expr $retries + 1`
        echo waiting for $name to start
        if [ $retries -gt 60 ]; then
            echo "$name not started or takes too long to start"
            docker logs "$name"
            exit 1
        fi
    done

    echo "$name is ready"
}

wait_for_firebird firebird25 /firebird/data/testdb25.fdb
wait_for_firebird firebird3  /var/lib/firebird/data/testdb30.fdb
wait_for_firebird firebird4  /var/lib/firebird/data/testdb40.fdb
wait_for_firebird firebird5  /var/lib/firebird/data/testdb50.fdb

docker logs firebird25
docker logs firebird3
docker logs firebird4
docker logs firebird5
