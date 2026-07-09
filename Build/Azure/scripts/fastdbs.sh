#!/bin/bash

# DB2 (50000), Informix (9089), YDB (2136) and Sybase (5000) run as concurrent lanes in a single
# CI job - distinct ports, no conflict. DB2/Informix/YDB use log-grep waits that are timing-
# insensitive, so their containers are started up front and waited on in turn. Sybase runs last as
# a self-contained block because its "configure, stop twice, restart" choreography is timing-
# sensitive and must run contiguously after its own `docker run`.

# --- start DB2 / Informix / YDB ---
docker run -d --name db2      --privileged -e LICENSE=accept -e DB2INST1_PASSWORD=Password12! -e DBNAME=testdb -p 50000:50000 icr.io/db2_community/db2:latest
docker run -d --name informix -e INIT_FILE=linq2db.sql -e LICENSE=ACCEPT -p 9089:9089 icr.io/informix/informix-developer-database:latest
docker run -d --name ydb      -p 2136:2136 -e YDB_FEATURE_FLAGS=enable_temp_tables ydbplatform/local-ydb:latest

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

# wait for YDB
retries=0
until docker logs ydb 2>&1 | grep -q 'Table profiles were not loaded'; do
    sleep 5
    retries=`expr $retries + 1`
    echo waiting for YDB to start
    if [ $retries -gt 50 ]; then
        echo YDB not started or takes too long to start
        docker logs ydb
        exit 1
    fi;
done

# --- Sybase (self-contained: start + configure + stop twice + restart + seed) ---
docker run -d --name sybase -e SYBASE_DB=TestDataCore -p 5000:5000 linq2db/linq2db:ase-16.1
docker ps -a

retries=0
until docker logs sybase | grep -q 'SYBASE CONFIGURED'; do
    sleep 5
    retries=`expr $retries + 1`
    if [ $retries -gt 100 ]; then
        >&2 echo 'Failed to init sybase'
        exit 1
    fi;

    echo 'Waiting for sybase'
done

# sybase will stop twice to finish encoding setup
retries=0
while docker ps | grep -q 'sybase'; do
    sleep 5
    retries=`expr $retries + 1`
    if [ $retries -gt 30 ]; then
        >&2 echo 'Wait for container to stop failed'
        exit 1
    fi;

    echo 'Waiting for container to stop'
done


docker start sybase
retries=0
while docker ps | grep -q 'sybase'; do
    sleep 5
    retries=`expr $retries + 1`
    if [ $retries -gt 30 ]; then
        >&2 echo 'Wait for container to stop failed'
        exit 1
    fi;

    echo 'Waiting for container to stop'
done

docker start sybase

retries=0
until docker logs sybase | grep -q 'SYBASE STARTED'; do
    sleep 5
    retries=`expr $retries + 1`
    if [ $retries -gt 30 ]; then
        >&2 echo 'Failed to start sybase'
        exit 1
    fi;

    echo 'Waiting for sybase'
done

sleep 5

cat <<-EOL > ase.sql
use master
go
sp_dboption tempdb, 'ddl in tran', 'true'
go
disk resize name='master', size='100m'
go
create database TestData on default
go
create database TestDataCore on default
go
EOL

docker cp ase.sql sybase:/opt/sap/ase.sql
docker exec -e SYBASE=/opt/sap sybase bash -c 'source /opt/sap/SYBASE.sh && /opt/sap/OCS-16_1/bin/isql -Usa -PmyPassword -SMYSYBASE -i/opt/sap/ase.sql'

docker logs db2
docker logs informix
docker logs ydb
docker logs sybase
