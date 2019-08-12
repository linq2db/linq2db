#!/bin/bash
#docker pull mariadb:latest
docker run -d --name mariadb mariadb:latest -e MYSQL_ROOT_PASSWORD=root -p 33060:3306 -v /var/lib/mysql:/var/lib/mysql
docker ps -a

retries=0
while ! mysql -p 33060 --host 127.0.0.1 --protocol TCP -uroot -proot -e "show databases;" > /dev/null 2>&1; do
    sleep 1
    retries=`expr $retries + 1`
    if [ $retries -gt 30 ]; then
        >&2 echo "Failed to wait for mariadb to start."
        docker ps -a
        docker logs mariadb
        exit 1
    fi;
done

docker exec mariadb mysql -e 'CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
