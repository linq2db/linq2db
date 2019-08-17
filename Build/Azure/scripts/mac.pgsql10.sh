#!/bin/bash
chmod +x mac.docker.sh
mac.docker.sh
ret=$?
if [ $ret -ne 0 ]; then
    echo 'Docker install failed'
    exit 1
fi

#docker pull postgres:10
docker run -d --name pgsql -h pgsql -e POSTGRES_PASSWORD=Password12! -p 5432:5432 -v pgdb:/var/run/postgresql postgres:10
until docker exec pgsql psql -U postgres -c '\l'; do
>&2 echo "Postgres is unavailable - sleeping"
sleep 1
done
docker exec pgsql psql -U postgres -c 'create database testdata'
docker exec pgsql psql -U postgres -c '\l'
