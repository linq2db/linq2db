#!/bin/bash

. "$(dirname "$0")/ci-setvar.sh"
. "$(dirname "$0")/docker-liveness.sh"
ci_setvar TZ CET

# Oracle 21c (host port 1521) and 23c (host port 1522) run as concurrent lanes in one job.
docker run -d --name oracle21 -e ORACLE_PWD=oracle -p 1521:1521 container-registry.oracle.com/database/express:21.3.0-xe
docker run -d --name oracle23 -e ORACLE_PWD=oracle -p 1522:1521 container-registry.oracle.com/database/free:23.2.0.0
docker ps -a

echo -n 12345 > bfile.txt

# --- Oracle 21c ---
retries=0
until docker logs oracle21 | grep -q 'DATABASE IS READY TO USE!'; do
    sleep 10
    retries=`expr $retries + 1`
    echo waiting for oracle21 to start
    require_running oracle21
    # 300 retries, as oracle image is really slow to start
    if [ $retries -gt 300 ]; then
        echo oracle21 not started or takes too long to start
        docker logs oracle21
        exit 1
    fi;
done
docker cp bfile.txt oracle21:/home/oracle/bfile.txt

# --- Oracle 23c ---
retries=0
until docker logs oracle23 | grep -q 'DATABASE IS READY TO USE!'; do
    sleep 10
    retries=`expr $retries + 1`
    echo waiting for oracle23 to start
    require_running oracle23
    # 300 retries, as oracle image is really slow to start
    if [ $retries -gt 300 ]; then
        echo oracle23 not started or takes too long to start
        docker logs oracle23
        exit 1
    fi;
done
docker cp bfile.txt oracle23:/home/oracle/bfile.txt

# Both are ready, but the first can have died while the second was starting.
require_running oracle21
require_running oracle23

docker logs oracle21
docker logs oracle23
