#!/bin/bash
docker pull postgres:11
docker run -d --name pgsql -p 5432:5432 postgres:11 -e POSTGRES_PASSWORD=Password12!
docker ps -a

# Wait for start
echo "Waiting for PostgreSQL started"
docker exec pgsql psql -U postgres -c 'select 1'
is_up=$?
while [ $is_up -ne 0 ] ; do
    docker exec pgsql psql -U postgres -c 'select 1'
    is_up=$?
done
echo "PostgreSQL is operational"

docker exec pgsql psql -U postgres -c 'create database testdata'
