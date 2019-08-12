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

#docker pull mariadb:latest
docker run -d --name mariadb mariadb:latest -e MYSQL_ROOT_PASSWORD=root -p 33060:3306 -v mysql:/var/lib/mysql --net host
docker ps -a

# Wait for start
echo "Waiting for MariaDB started"
docker exec mariadb mysql --protocol TCP -uroot -proot -e "show databases;"
is_up=$?
while [ $is_up -ne 0 ] ; do
    docker exec mariadb mysql --protocol TCP -uroot -proot -e "show databases;"
    is_up=$?
done
echo "MariaDB is operational"

docker exec mariadb mysql -e 'CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
