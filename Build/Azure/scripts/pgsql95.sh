#!/bin/bash
#docker pull postgres:9.5
docker run -d --name pgsql --net host -e POSTGRES_PASSWORD=Password12! -p 5432:5432 -v /var/run/postgresql:/var/run/postgresql postgres:9.5
until docker exec pgsql psql -U postgres -c '\l'; do
>&2 echo "Postgres is unavailable - sleeping"
sleep 1
done
until docker exec pgsql psql -U postgres -c 'create database testdata'; do
>&2 echo "Postgres is unavailable - sleeping"
sleep 1
done
docker exec pgsql psql -U postgres -c '\l'
