#!/bin/bash

# TODO: update tag to 15 when it is released
# port 5415 port used instead of default one to avoid connection string conflicts with multiple versions of servers on same machine in local testing
docker run -d --name pgsql --net host -e POSTGRES_PASSWORD=Password12! -p 5415:5432 -v /var/run/postgresql:/var/run/postgresql postgres:15beta3

retries=0
until docker exec pgsql psql -U postgres -c '\l' | grep -q 'testdata'; do
    sleep 1
    retries=`expr $retries + 1`
    docker exec pgsql psql -U postgres -c 'create database testdata'
    if [ $retries -gt 100 ]; then
        echo postgres not started or database failed to create
        exit 1
    fi;
done

docker logs pgsql
