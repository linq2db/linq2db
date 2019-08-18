#!/bin/bash
chmod +x scripts/mac.docker.sh
scripts/mac.docker.sh
ret=$?
if [ $ret -ne 0 ]; then
    echo 'Docker install failed'
    exit 1
fi

#docker pull mariadb:latest
docker run -d --name mariadb -e MYSQL_ROOT_PASSWORD=root -p 3306:3306 mariadb:latest
docker ps -a

retries=0
until docker exec mariadb mysql --protocol TCP -uroot -proot -e "show databases;"; do
    sleep 3
    retries=`expr $retries + 1`
    if [ $retries -gt 90 ]; then
        >&2 echo "Failed to wait for mariadb to start."
        docker ps -a
        docker logs mariadb
        exit 1
    fi;
done

docker exec mariadb mysql -e 'CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
