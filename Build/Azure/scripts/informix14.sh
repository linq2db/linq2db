#!/bin/bash

docker run -d --name informix -e INIT_FILE=linq2db.sql -e LICENSE=ACCEPT -p 9089:9089 icr.io/informix/informix-developer-database:latest

echo Generate CREATE DATABASE script
cat <<-EOSQL > informix_init.sql
CREATE DATABASE testdb WITH BUFFERED LOG
EOSQL

cat informix_init.sql
docker cp informix_init.sql informix:/opt/ibm/config/linq2db.sql

docker ps -a

retries=0
status="1"
until docker logs informix | grep -q 'Informix container login information'; do
    sleep 5
    retries=`expr $retries + 1`
    echo waiting for informix to start
    if [ $retries -gt 300 ]; then
        echo informix not started or takes too long to start
        exit 1
    fi;
done

docker logs informix
