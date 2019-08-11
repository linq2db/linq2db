#!/bin/bash
docker pull postgres:10
docker run -d --name pgsql --net host postgres:10 -e POSTGRES_PASSWORD=Password12! -e POSTGRES_DB=testdata -p 5432:5432 -v /var/run/postgresql:/var/run/postgresql
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
