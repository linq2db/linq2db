#!/bin/bash

#docker pull postgres:9.3
docker run -d --name pgsql -h pgsql -e POSTGRES_PASSWORD=Password12! -p 5432:5432 -v pgdb:/var/run/postgresql postgres:9.3
until docker exec pgsql psql -U postgres -c '\l'; do
>&2 echo "Postgres is unavailable - sleeping"
sleep 1
done
sleep 5
docker exec pgsql psql -U postgres -c 'create database testdata'
docker exec pgsql psql -U postgres -c '\l'
