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
docker run -d --name mysql mysql:latest -e MYSQL_ROOT_PASSWORD=root -p 33060:3306
docker ps -a

# Wait for start
echo "Waiting for MySQL started"
docker exec mysql mysql --protocol TCP -uroot -proot -e "show databases;"
is_up=$?
while [ $is_up -ne 0 ] ; do
    docker exec mysql mysql --protocol TCP -uroot -proot -e "show databases;"
    is_up=$?
done
echo "MySQL is operational"

docker exec mysql mysql -e 'CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
docker exec mysql mysql -e 'CREATE DATABASE testdata2 DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
