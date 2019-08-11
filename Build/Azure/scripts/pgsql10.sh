#!/bin/bash
docker run -d --name postgres10 --net host -e POSTGRES_PASSWORD=Password12! -p 5432:5432 -v /var/run/postgresql:/var/run/postgresql postgres:10
until docker exec postgres10 psql -U postgres -c '\l'; do
>&2 echo "Postgres is unavailable - sleeping"
sleep 5
done
docker exec postgres10 psql -U postgres -c 'create database testdata'
docker exec postgres10 psql -U postgres -c '\l'
