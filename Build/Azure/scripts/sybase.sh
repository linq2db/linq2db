#!/bin/bash

docker run -d --name sybase -e SYBASE_DB=TestDataCore -p 5000:5000 datagrip/sybase:16.0
docker ps -a
sleep 45

echo Generate CREATE DATABASE script
# sometimes it fails to create user and db, so we need to do it manually
# https://github.com/DataGrip/docker-env/issues/8
# we just need to create db if it is missing, nothing else
cat <<-EOSQL > sybase_init.sql
USE master
GO
disk resize name='master', size='200m'
GO
IF NOT EXISTS(SELECT * FROM dbo.sysdatabases WHERE name = 'TestDataCore')
  CREATE DATABASE TestDataCore ON master = '102400K'
GO
EOSQL

cat sybase_init.sql
docker cp sybase_init.sql sybase:/init.sql

retries=0
until docker exec -e SYBASE=/opt/sybase sybase /opt/sybase/OCS-16_0/bin/isql -Usa -PmyPassword -SMYSYBASE -i"/init.sql" -e --retserverror ; do
    sleep 5
    retries=`expr $retries + 1`
    if [ $retries -gt 30 ]; then
        >&2 echo 'Failed to init sybase'
        exit 1
    fi;

    echo 'Waiting for sybase'
done

echo PRINTING DOCKER LOGS
docker logs sybase
