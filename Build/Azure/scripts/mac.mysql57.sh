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

#docker pull mysql:5.7
docker run -d --name mysql mysql:5.7 -e MYSQL_ROOT_PASSWORD=root -p 33060:3306 -v mysql:/var/lib/mysql --net host
docker ps -a

retries=0
while ! mysql -p 33060 --host 127.0.0.1 --protocol TCP -uroot -proot -e "show databases;" > /dev/null 2>&1; do
    sleep 1
    retries=`expr $retries + 1`
    if [ $retries -gt 30 ]; then
        >&2 echo "Failed to wait for mysql to start."
        docker ps -a
        docker logs mysql
        exit 1
    fi;
done

docker exec mysql mysql -e 'CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
