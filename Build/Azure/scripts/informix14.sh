#!/bin/bash


cp -f ./IBM.Data.DB2.Core-lnx/lib/netstandard2.0/IBM.Data.DB2.Core.dll ./IBM.Data.DB2.Core.dll
rm -rf ./clidriver/*
rm ./IBM.Data.DB2.Core.dll
echo list .
ls .
cp -a ./IBM.Data.DB2.Core-lnx/build/clidriver/. ./clidriver/
echo list .
ls .

echo $PATH
export PATH=`$PATH:$PWD/clidriver/bin:$PWD/clidriver/lib`
echo $PATH

docker run -d --name informix -e INIT_FILE=linq2db.sql -e LICENSE=ACCEPT -p 9089:9089 ibmcom/informix-developer-database:14.10.FC1DE

echo Generate CREATE DATABASE script
cat <<-EOSQL > informix_init.sql
CREATE DATABASE testdb WITH BUFFERED LOG
EOSQL

cat informix_init.sql
docker cp informix_init.sql informix:/opt/ibm/config/linq2db.sql

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
