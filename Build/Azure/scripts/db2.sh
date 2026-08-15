#!/bin/bash

docker run -d --name db2 --privileged -e LICENSE=accept -e DB2INST1_PASSWORD=Password12! -e DBNAME=testdb -p 50000:50000 icr.io/db2_community/db2:latest

docker ps -a

retries=0
status="1"
until docker logs db2 | grep -q 'Setup has completed'; do
    sleep 5
    retries=`expr $retries + 1`
    echo waiting for db2 to start
    if [ $retries -gt 100 ]; then
        echo db2 not started or takes too long to start
        exit 1
    fi;
done

docker logs db2

# AUTO_STMT_STATS (real-time statistics) makes the optimizer synchronously profile a table's stats
# during query compilation whenever they're missing/stale - our tests constantly create/drop small
# tables and immediately query them, so this fires on nearly every fresh table. Measured 3.7x slower
# with it on (confirmed via a controlled run: identical test counts, only this setting differed).
docker exec -u db2inst1 db2 bash -lc "db2 connect to testdb && db2 UPDATE DB CFG FOR testdb USING AUTO_STMT_STATS OFF"
