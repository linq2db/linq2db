ECHO OFF

REM try to remove existing container
docker stop mariadb
docker rm -f mariadb

REM use pull to get latest layers (run will use cached layers)
docker pull mariadb:latest
docker run -d --name mariadb -e MYSQL_ROOT_PASSWORD=root -p 3316:3306 mariadb:latest

call wait-err mariadb "3306  mariadb.org"

REM create test database
docker exec mariadb mariadb -e "CREATE DATABASE testdata DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;" -uroot -proot

