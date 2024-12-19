#!/bin/bash

docker run -d --name clickhouse --ulimit nofile=262144:262144 -p 8123:8123 -p 9000:9000 -p 9004:9004 -p 9005:9005 -e CLICKHOUSE_USER=testuser -e CLICKHOUSE_PASSWORD=testuser clickhouse/clickhouse-server:latest

docker ps -a

echo Patching ClickHouse settings...
docker exec clickhouse sed -i "0,/<\/default>/{s/<\/default>/<join_use_nulls>1<\/join_use_nulls><mutations_sync>1<\/mutations_sync><allow_experimental_object_type>1<\/allow_experimental_object_type><allow_experimental_geo_types>1<\/allow_experimental_geo_types><allow_experimental_json_type>1<\/allow_experimental_json_type><\/default>/}" /etc/clickhouse-server/users.xml
docker restart clickhouse

retries=0
until docker logs clickhouse | grep -q 'create new user'; do
    sleep 5
    retries=`expr $retries + 1`
    echo waiting for ClickHouse to start
    if [ $retries -gt 50 ]; then
        echo ClickHouse not started or takes too long to start
        docker logs clickhouse
        exit 1
    fi;
done

sleep 5


docker exec clickhouse clickhouse-client --multiquery --host 127.0.0.1 -u testuser --password testuser -q "CREATE DATABASE testdb1"
docker exec clickhouse clickhouse-client --multiquery --host 127.0.0.1 -u testuser --password testuser -q "CREATE DATABASE testdb2"
docker exec clickhouse clickhouse-client --multiquery --host 127.0.0.1 -u testuser --password testuser -q "CREATE DATABASE testdb3"
# pgsql interface unusable currently https://github.com/ClickHouse/ClickHouse/issues/18611
#docker exec clickhouse clickhouse-client --multiquery --host 127.0.0.1 -u testuser --password testuser -q "CREATE DATABASE testdb4"

docker logs clickhouse
