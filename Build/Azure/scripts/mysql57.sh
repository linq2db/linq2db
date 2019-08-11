#!/bin/bash
docker pull mysql:5.7
docker run -d --name mysql mysql:5.7 -e MYSQL_ROOT_PASSWORD=root -p 33060:3306
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
