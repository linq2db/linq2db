#!/bin/bash

rm -rf ./clidriver/*
rm ./IBM.Data.DB2.Core.dll
cp -a ./IBM.Data.DB2.Core-lnx/build/clidriver/. ./clidriver/
cp -f ./IBM.Data.DB2.Core-lnx/lib/netstandard2.0/IBM.Data.DB2.Core.dll ./IBM.Data.DB2.Core.dll

sudo apt-get install libxml2

export LD_LIBRARY_PATH=$PWD/clidriver/lib/
export PATH=$PATH:$PWD/clidriver/bin:$PWD/clidriver/lib
echo $LD_LIBRARY_PATH
echo $PATH

docker run -d --name informix -e SIZE=custom -e LICENSE=ACCEPT -p 9089:9089 ibmcom/informix-developer-database:12.10.FC12W1DE

echo Generate CREATE DATABASE script
cat <<-EOSQL > informix_init.sql
CREATE DATABASE testdb WITH BUFFERED LOG
EOSQL

cat informix_init.sql
docker cp informix_init.sql informix:/opt/ibm/config/sch_init_informix.custom.sql
docker exec informix cp /opt/ibm/data/informix_config.small /opt/ibm/data/informix_config.custom

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
