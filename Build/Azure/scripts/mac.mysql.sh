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

#docker pull mysql:latest
docker run -d --name mysql -e MYSQL_ROOT_PASSWORD=root -p 3306:3306 mysql:latest
docker ps -a

retries=0
until docker exec mysql mysql --protocol TCP -uroot -proot -e "show databases;"; do
    sleep 1
    retries=`expr $retries + 1`
    if [ $retries -gt 90 ]; then
        >&2 echo "Failed to wait for mysql to start."
        docker ps -a
        docker logs mysql
        exit 1
    fi;
done


docker exec mysql mysql -e 'CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
docker exec mysql mysql -e 'CREATE DATABASE testdata2 DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
