#!/bin/bash

echo "##vso[task.setvariable variable=TZ]CET"

# Oracle 18c (host port 1521) and 19c (host port 1522) run as concurrent lanes in one job.
docker run -d --name oracle18 -e ORACLE_PWD=oracle                 -p 1521:1521 container-registry.oracle.com/database/express:18.4.0-xe
docker run -d --name oracle19 -e ORACLE_PWD=oracle -e ORACLE_SID=XE -p 1522:1521 oracledb19c/oracle.19.3.0-ee:oracle19.3.0-ee
docker ps -a

echo -n 12345 > bfile.txt

# --- Oracle 18c ---
retries=0
until docker logs oracle18 | grep -q 'DATABASE IS READY TO USE!'; do
    sleep 10
    retries=`expr $retries + 1`
    echo waiting for oracle18 to start
    # 300 retries, as oracle image is really slow to start
    if [ $retries -gt 300 ]; then
        echo oracle18 not started or takes too long to start
        docker logs oracle18
        exit 1
    fi;
done
docker cp bfile.txt oracle18:/home/oracle/bfile.txt

# --- Oracle 19c ---
retries=0
until docker logs oracle19 | grep -q 'DATABASE IS READY TO USE!'; do
    sleep 10
    retries=`expr $retries + 1`
    echo waiting for oracle19 to start
    if [ $retries -gt 1000 ]; then
        echo oracle19 not started or takes too long to start
        docker logs oracle19
        exit 1
    fi;
done
docker cp bfile.txt oracle19:/home/oracle/bfile.txt

docker logs oracle18
docker logs oracle19
