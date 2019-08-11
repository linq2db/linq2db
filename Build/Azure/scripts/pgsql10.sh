#!/bin/bash
docker run -d --name pgsql --net host -e POSTGRES_PASSWORD=Password12! -p 5432:5432 -v /var/run/postgresql:/var/run/postgresql postgres:10 -e POSTGRES_DB=testdata
until docker exec pgsql psql -U postgres -c '\l'; do
>&2 echo "Postgres is unavailable - sleeping"
sleep 1
done
docker exec pgsql psql -U postgres -c '\l'
