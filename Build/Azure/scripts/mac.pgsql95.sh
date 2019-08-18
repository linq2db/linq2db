#!/bin/bash
chmod +x scripts/mac.docker.sh
scripts/mac.docker.sh
ret=$?
if [ $ret -ne 0 ]; then
    echo 'Docker install failed'
    exit 1
fi

#docker pull postgres:9.5
docker run -d --name pgsql -h pgsql -e POSTGRES_PASSWORD=Password12! -p 5432:5432 -v pgdb:/var/run/postgresql postgres:9.5
until docker exec pgsql psql -U postgres -c '\l'; do
>&2 echo "Postgres is unavailable - sleeping"
sleep 1
done
sleep 5
docker exec pgsql psql -U postgres -c 'create database testdata'
docker exec pgsql psql -U postgres -c '\l'
