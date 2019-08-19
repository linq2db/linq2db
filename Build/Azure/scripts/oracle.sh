#!/bin/bash

docker run -d --name oracle -p 1521:1521 -p 8080:8080 datagrip/oracle:11.2

#sleep 100
#chmod +x scripts/wait-for-it.sh
#scripts/wait-for-it.sh localhost:8080 --timeout=600

docker cp scripts/ping-oracle.sql oracle:/test.sql

retries=0
status="1"
while [ "$status" != "0" ]
do
    sleep 5
    retries=`expr $retries + 1`
    docker exec oracle sqlplus /nolog @/test.sql
    status=$?
    if [ $retries -gt 100 ]; then
        >&2 echo 'Failed to start oracle'
        exit 1
    fi;
done

docker ps -a
docker logs oracle
