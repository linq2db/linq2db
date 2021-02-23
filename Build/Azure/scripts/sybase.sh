#!/bin/bash

docker run -d --name sybase -e SYBASE_DB=TestDataCore -p 5000:5000 datagrip/sybase:16.0
docker ps -a
sleep 45

retries=0
until docker logs sybase | grep -q 'SYBASE INITIALIZED'; do
    sleep 5
    retries=`expr $retries + 1`
    if [ $retries -gt 30 ]; then
        >&2 echo 'Failed to init sybase'
        exit 1
    fi;

    echo 'Waiting for sybase'
done

echo PRINTING DOCKER LOGS
docker logs sybase
