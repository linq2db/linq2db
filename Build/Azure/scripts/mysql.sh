#!/bin/bash
set -x
docker run -d --name mysql mysql:5.7 --health-cmd='mysqladmin ping --silent' -e MYSQL_ROOT_PASSWORD=root
docker exec mysql mysql -e 'CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
