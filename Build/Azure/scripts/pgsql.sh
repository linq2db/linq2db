#!/bin/bash
set -x
docker run -d --name pgsql --net host -p 5432:5432 postgres:11 -e POSTGRES_PASSWORD=Password12!
until docker exec pgsql psql -U postgres -c '\l'; do
    >&2 echo "waiting for pgsql..."
    sleep 1
done
docker exec pgsql pgsql -U postgres -c 'create database testdata'
