#!/bin/bash
docker pull postgres:10
docker run -d --name pgsql -p 5432:5432 postgres:10 -e POSTGRES_PASSWORD=Password12!
docker ps -a

# Wait for start
echo "Waiting for PostgreSQL started"
docker exec pgsql psql -U postgres -c '\l'
is_up=$?
while [ $is_up -ne 0 ] ; do
    docker exec pgsql psql -U postgres -c '\l'
    is_up=$?
done
echo "PostgreSQL is operational"

docker exec pgsql pgsql -U postgres -c 'create database testdata'
