#!/bin/bash

# macOS variant of mysql.sh: all three MySQL-family servers on distinct host ports (3306/3307/3316).
# macOS CI legs are currently disabled, but this mirrors the linux script so the merged matrix entry
# stays valid if they are re-enabled.
docker run -d --name mysql   -e MYSQL_ROOT_PASSWORD=root -p 3306:3306 mysql:latest
docker run -d --name mysql57 -e MYSQL_ROOT_PASSWORD=root -p 3307:3306 mysql:5.7
docker run -d --name mariadb -e MYSQL_ROOT_PASSWORD=root -p 3316:3306 mariadb:latest
docker ps -a

# mysql:latest -> MySql.8.0 / MySqlConnector.8.0 (port 3306)
retries=0
until docker exec mysql mysql --protocol TCP -uroot -proot -e "show databases;"; do
    sleep 3
    retries=`expr $retries + 1`
    if [ $retries -gt 90 ]; then
        >&2 echo "Failed to wait for mysql to start."
        docker logs mysql
        exit 1
    fi;
done
docker exec mysql mysql -e 'CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
docker exec mysql mysql -e 'CREATE DATABASE testdataconnector DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
docker exec mysql mysql -e 'SET GLOBAL local_infile=1;' -uroot -proot

# mysql:5.7 -> MySql.5.7 / MySqlConnector.5.7 (port 3307)
retries=0
until docker exec mysql57 mysql --protocol TCP -uroot -proot -e "show databases;"; do
    sleep 3
    retries=`expr $retries + 1`
    if [ $retries -gt 90 ]; then
        >&2 echo "Failed to wait for mysql57 to start."
        docker logs mysql57
        exit 1
    fi;
done
docker exec mysql57 mysql -e 'CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
docker exec mysql57 mysql -e 'CREATE DATABASE testdataconnector DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot

# mariadb:latest -> MariaDB.11 (port 3316)
retries=0
until docker exec mariadb mariadb --protocol TCP -uroot -proot -e "show databases;"; do
    sleep 3
    retries=`expr $retries + 1`
    if [ $retries -gt 90 ]; then
        >&2 echo "Failed to wait for mariadb to start."
        docker logs mariadb
        exit 1
    fi;
done
docker exec mariadb mariadb -e 'CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
docker exec mariadb mariadb -e 'CREATE DATABASE testdataconnector DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
