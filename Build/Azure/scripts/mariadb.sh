#!/bin/bash
docker pull mariadb:latest
docker run -d --name mariadb mariadb:latest --health-cmd='mysqladmin ping --silent' -e MYSQL_ROOT_PASSWORD=root -p:33060:3306
docker ps -a
docker exec mariadb mysql -e 'CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
