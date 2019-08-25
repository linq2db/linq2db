#!/bin/bash

#NuGet.exe install IBM.Data.DB2.Core-lnx -ExcludeVersion
cp -f IBM.Data.DB2.Core-lnx/lib/netstandard2.0/IBM.Data.DB2.Core.dll ../IBM.Data.DB2.Core.dll
rm -rf ../clidriver/
cp -rf IBM.Data.DB2.Core-lnx/build/clidriver/ ../clidriver/

docker run -d --name informix -e INIT_FILE=linq2db.sql -e LICENSE=ACCEPT -p 9088:9088 ibmcom/informix-developer-database:12.10.FC12W1DE

echo Generate CREATE DATABASE script
cat <<-EOSQL > informix_init.sql
CREATE DATABASE testdb
EOSQL

cat informix_init.sql
docker cp informix_init.sql informix:linq2db.sql

docker ps -a

retries=0
status="1"
until docker logs informix | grep -q 'Informix container login Information'; do
    sleep 5
    retries=`expr $retries + 1`
    echo waiting for informix to start
    if [ $retries -gt 100 ]; then
        echo informix not started or takes too long to start
        exit 1
    fi;
done

docker logs informix
