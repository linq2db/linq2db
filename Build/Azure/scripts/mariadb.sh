#!/bin/bash
sudo systemctl stop mysql

docker run -d --name mariadb -e MYSQL_ROOT_PASSWORD=root -p 3306:3306 mariadb:latest
docker ps -a

retries=0
until docker exec mariadb mariadb --protocol TCP -uroot -proot -e "show databases;"; do
    sleep 3
    retries=`expr $retries + 1`
    if [ $retries -gt 90 ]; then
        >&2 echo "Failed to wait for mariadb to start."
        docker ps -a
        docker logs mariadb
        exit 1
    fi;
done

docker exec mariadb mariadb -e 'CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
docker exec mariadb mariadb -e 'CREATE DATABASE testdataconnector DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
