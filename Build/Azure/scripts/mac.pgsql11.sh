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

docker pull postgres:11
docker run -d --name pgsql -p 5432:5432 postgres:11 -e POSTGRES_PASSWORD=Password12! --net host
docker ps -a

# Wait for start
echo "Waiting for PostgreSQL started"
docker exec pgsql psql -U postgres -c 'select 1'
is_up=$?
while [ $is_up -ne 0 ] ; do
    docker exec pgsql psql -U postgres -c 'select 1'
    is_up=$?
done
echo "PostgreSQL is operational"

docker exec pgsql psql -U postgres -c 'create database testdata'
