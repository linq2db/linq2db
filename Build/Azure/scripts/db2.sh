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
