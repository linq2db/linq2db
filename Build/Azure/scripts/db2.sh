#!/bin/bash

rm -rf ./clidriver/*
rm ./IBM.Data.DB2.Core.dll
cp -a ./IBM.Data.DB2.Core-lnx/build/clidriver/. ./clidriver/
cp -f ./IBM.Data.DB2.Core-lnx/lib/netstandard2.0/IBM.Data.DB2.Core.dll ./IBM.Data.DB2.Core.dll

echo "##vso[task.setvariable variable=PATH]$PATH:$PWD/clidriver/bin:$PWD/clidriver/lib"
echo "##vso[task.setvariable variable=LD_LIBRARY_PATH]$PWD/clidriver/lib/"

docker run -d --name db2 --privileged -e LICENSE=accept -e DB2INST1_PASSWORD=Password12! -e DBNAME=testdb -p 50000:50000 ibmcom/db2:11.5.0.0a

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
