ECHO OFF

REM try to remove existing container
docker stop clickhouse
docker rm -f clickhouse

REM use pull to get latest layers (run will use cached layers)
docker pull clickhouse/clickhouse-server:latest
docker run -d --name clickhouse --ulimit nofile=262144:262144 -p 8123:8123 -p 9000:9000 -p 9004:9004 -p 9005:9005 -e CLICKHOUSE_USER=testuser -e CLICKHOUSE_PASSWORD=testuser clickhouse/clickhouse-server:latest

REM update settings and restart server
docker exec clickhouse sed -i "0,/<\/default>/{s/<\/default>/<join_use_nulls>1<\/join_use_nulls><mutations_sync>1<\/mutations_sync><allow_experimental_object_type>1<\/allow_experimental_object_type><allow_experimental_geo_types>1<\/allow_experimental_geo_types><allow_experimental_json_type>1<\/allow_experimental_json_type><allow_experimental_correlated_subqueries>1<\/allow_experimental_correlated_subqueries><\/default>/}" /etc/clickhouse-server/users.xml
docker restart clickhouse

ECHO pause to wait for ClickHouse startup completion
call wait clickhouse "create new user"
timeout 10

REM create test databases for all providers
docker exec clickhouse clickhouse-client --multiquery --host 127.0.0.1 -u testuser --password testuser -q "CREATE DATABASE testdb1"
docker exec clickhouse clickhouse-client --multiquery --host 127.0.0.1 -u testuser --password testuser -q "CREATE DATABASE testdb2"
docker exec clickhouse clickhouse-client --multiquery --host 127.0.0.1 -u testuser --password testuser -q "CREATE DATABASE testdb3"

