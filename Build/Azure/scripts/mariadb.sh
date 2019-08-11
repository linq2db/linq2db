#!/bin/bash
docker pull mariadb:latest
docker run -d --name mariadb mariadb:latest -e MYSQL_ROOT_PASSWORD=root -p 33060:3306
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
