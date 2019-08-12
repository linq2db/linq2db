#!/bin/bash
#https://github.com/microsoft/azure-pipelines-image-generation/issues/738
brew cask install docker
sudo /Applications/Docker.app/Contents/MacOS/Docker --quit-after-install --unattended
/Applications/Docker.app/Contents/MacOS/Docker --unattended &
while ! docker info 2>/dev/null ; do
sleep 5
if pgrep -xq -- "Docker"; then
    echo docker still running
else
    echo docker not running, restart
    /Applications/Docker.app/Contents/MacOS/Docker --unattended &
fi
echo "Waiting for docker service to be in the running state"
done

#docker pull postgres:11
docker run -d --name pgsql -h pgsql -e POSTGRES_PASSWORD=Password12! -p 5432:5432 -v /var/run/postgresql:/var/run/postgresql postgres:11
until docker exec pgsql psql -U postgres -c '\l'; do
>&2 echo "Postgres is unavailable - sleeping"
sleep 5
done
docker exec pgsql psql -U postgres -c 'create database testdata'
docker exec pgsql psql -U postgres -c '\l'
