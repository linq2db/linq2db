#!/bin/bash

echo "##vso[task.setvariable variable=TZ]CET"

docker run -d --name oracle -p 1521:1521 datagrip/oracle:11.2

docker ps -a

retries=0
status="1"
until docker logs oracle | grep -q 'Database ready to use'; do
    sleep 5
    retries=`expr $retries + 1`
    echo waiting for oracle to start
    if [ $retries -gt 200 ]; then
        echo oracle not started or takes too long to start
        exit 1
    fi;
done

docker logs oracle
