#!/bin/bash

docker run -d --name sybase -e SYBASE_DB=TestDataCore -p 5000:5000 datagrip/sybase:16.0
docker ps -a
sleep 45

retries=0
until docker logs sybase | grep -q 'SYBASE INITIALIZED'; do
    sleep 5
    retries=`expr $retries + 1`
    if [ $retries -gt 30 ]; then
        >&2 echo 'Failed to init sybase'
        exit 1
    fi;

    echo 'Waiting for sybase'
done

echo Generate CREATE DATABASE script
cat <<-EOSQL > sybase_init.sql
USE master
GO
disk resize name='master', size='200m'
GO
IF EXISTS(SELECT * FROM dbo.sysdatabases WHERE name = 'TestDataCore')
  DROP DATABASE TestDataCore
GO
CREATE DATABASE TestDataCore ON master = '102400K'
GO
EOSQL

cat sybase_init.sql
docker cp sybase_init.sql sybase:/init.sql
docker exec -e SYBASE=/opt/sybase sybase /opt/sybase/OCS-16_0/bin/isql -Usa -PmyPassword -SMYSYBASE -i"/init.sql" -e --retserverror

echo PRINTING DOCKER LOGS
docker logs sybase
