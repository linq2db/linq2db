#!/bin/bash

echo "##vso[task.setvariable variable=TZ]CET"

docker run -d --name oracle -e ORACLE_PWD=oracle -p 1521:1521 container-registry.oracle.com/database/express:18.4.0-xe

docker ps -a

retries=0
status="1"
until docker logs oracle | grep -q 'DATABASE IS READY TO USE!'; do
    sleep 10
    retries=`expr $retries + 1`
    echo waiting for oracle to start
    # 300 retries, as oracle image is really slow to start
    if [ $retries -gt 300 ]; then
        echo oracle not started or takes too long to start
        docker logs oracle
        exit 1
    fi;
done

echo -n 12345 > bfile.txt
docker cp bfile.txt oracle:/home/oracle/bfile.txt

docker logs oracle
