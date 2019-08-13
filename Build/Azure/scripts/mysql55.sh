#!/bin/bash
sudo systemctl stop mysql

#docker pull mysql:5.5
docker run -d --name mysql -e MYSQL_ROOT_PASSWORD=root -p 3306:3306 mysql:5.5
docker ps -a

retries=0
until docker exec mysql mysql --protocol TCP -uroot -proot -e "show databases;"; do
    sleep 3
    retries=`expr $retries + 1`
    if [ $retries -gt 90 ]; then
        >&2 echo "Failed to wait for mysql to start."
        docker ps -a
        docker logs mysql
        exit 1
    fi;
done

docker exec mysql mysql -e 'CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
