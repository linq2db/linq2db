#!/bin/bash
docker pull postgres:10
docker run -d --name pgsql postgres:10 -e POSTGRES_PASSWORD=Password12! -e POSTGRES_DB=testdata
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
