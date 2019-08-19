#!/bin/bash

docker run -d --name sybase -e SYBASE_DB=TestDataCore -p 5000:5000 datagrip/sybase:16.0
docker ps -a
sleep 45

docker logs sybase

# sometimes it fails to create user and db, so we need to do it manually
# https://github.com/DataGrip/docker-env/issues/8
# we just need to create db if it is missing, nothing else
cat <<-EOSQL > sybase_init.sql
use master
go
IF NOT EXISTS(SELECT * FROM dbo.sysdatabases WHERE name = 'TestDataCore')
  CREATE DATABASE TestDataCore
EOSQL

docker cp sybase_init.sql sybase:/init.sql
docker exec sybase /opt/sybase/OCS-16_0/bin/isql -Usa -PmyPassword -SMYSYBASE -i"/init.sql"

docker logs sybase
