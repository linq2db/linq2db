#!/bin/bash

echo "##vso[task.setvariable variable=TZ]CET"

docker run -d --name oracle -p 1521:1521 datagrip/oracle:12.2.0.1-se2-directio

docker ps -a

retries=0
status="1"
until docker logs oracle | grep -q 'Database ready to use. Enjoy'; do
    sleep 5
    retries=`expr $retries + 1`
    echo waiting for oracle to start
    if [ $retries -gt 100 ]; then
        echo oracle not started or takes too long to start
        docker logs oracle
        exit 0
    fi;
done

docker logs oracle
