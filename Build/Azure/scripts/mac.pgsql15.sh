#!/bin/bash

# TODO: update tag to 15 when it is released
docker run -d --name pgsql -h pgsql -e POSTGRES_PASSWORD=Password12! -p 5432:5432 -v pgdb:/var/run/postgresql postgres:15rc1

until docker exec pgsql psql -U postgres -c '\l'; do
>&2 echo "Postgres is unavailable - sleeping"
sleep 5
done
docker exec pgsql psql -U postgres -c 'create database testdata'
docker exec pgsql psql -U postgres -c '\l'
