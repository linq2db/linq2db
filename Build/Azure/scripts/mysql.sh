#!/bin/bash
docker pull mysql:latest
docker run -d --name mysql mysql:latest --health-cmd='mysqladmin ping --silent' -e MYSQL_ROOT_PASSWORD=root -p 33060:3306
docker ps -a
docker exec mysql mysql -e 'CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
docker exec mysql mysql -e 'CREATE DATABASE testdata2 DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
