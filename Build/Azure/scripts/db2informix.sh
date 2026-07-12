#!/bin/bash

# DB2 (50000) and Informix (9089) run as concurrent lanes in a single CI job - distinct ports, no
# conflict. Both use log-grep waits that are timing-insensitive, so the containers are started up
# front and waited on in turn. Split out of the former db2+informix+ydb+sybase job so a single agent
# no longer runs four DB engines at once.

docker run -d --name db2      --privileged -e LICENSE=accept -e DB2INST1_PASSWORD=Password12! -e DBNAME=testdb -p 50000:50000 icr.io/db2_community/db2:latest
docker run -d --name informix -e INIT_FILE=linq2db.sql -e LICENSE=ACCEPT -p 9089:9089 icr.io/informix/informix-developer-database:latest

echo Generate Informix CREATE DATABASE script
cat <<-EOSQL > informix_init.sql
CREATE DATABASE testdb WITH BUFFERED LOG
EOSQL
cat informix_init.sql
docker cp informix_init.sql informix:/opt/ibm/config/linq2db.sql

docker ps -a

# wait for DB2
retries=0
until docker logs db2 | grep -q 'Setup has completed'; do
    sleep 5
    retries=`expr $retries + 1`
    echo waiting for db2 to start
    if [ $retries -gt 100 ]; then
        echo db2 not started or takes too long to start
        docker logs db2
        exit 1
    fi;
done

# wait for Informix
retries=0
until docker logs informix | grep -q 'Informix container login information'; do
    sleep 5
    retries=`expr $retries + 1`
    echo waiting for informix to start
    if [ $retries -gt 300 ]; then
        echo informix not started or takes too long to start
        docker logs informix
        exit 1
    fi;
done

docker logs db2
docker logs informix
