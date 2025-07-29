#!/bin/bash

docker run -d --name pgsql -e POSTGRES_PASSWORD=Password12! -p 5432:5432 -v /var/run/postgresql:/var/run/postgresql postgres:18beta2

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
