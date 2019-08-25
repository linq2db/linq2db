#!/bin/bash

NuGet.exe install IBM.Data.DB2.Core-osx -ExcludeVersion
cp -f IBM.Data.DB2.Core-lnx/lib/netstandard2.0/IBM.Data.DB2.Core.dll ../IBM.Data.DB2.Core.dll
rm -rf ../clidriver/
cp -rf IBM.Data.DB2.Core-lnx/build/clidriver/ ../clidriver/

docker run -d --name db2 -e LICENSE=accept -e DB2INST1_PASSWORD=Password12! -e DBNAME=testdb -p 50000:50000 ibmcom/db2:11.5.0.0a

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
