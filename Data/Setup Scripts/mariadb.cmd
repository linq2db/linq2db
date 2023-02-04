ECHO OFF

REM try to remove existing container
docker stop mariadb
docker rm -f mariadb

REM use pull to get latest layers (run will use cached layers)
docker pull mariadb:latest
docker run -d --name mariadb -e MYSQL_ROOT_PASSWORD=root -p 3316:3306 mariadb:latest

call wait-err mariadb "3306  mariadb.org"

REM create test database
docker exec mariadb mysql -e "CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;" -uroot -proot
docker exec mariadb mysql -e "CREATE DATABASE testdataconnector DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;" -uroot -proot

