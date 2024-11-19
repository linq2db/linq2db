ECHO OFF

REM try to remove existing container
docker stop mysql57
docker rm -f mysql57

REM use pull to get latest layers (run will use cached layers)
docker pull mysql57:5.7
docker run -d --name mysql57 -e MYSQL_ROOT_PASSWORD=root -p 3307:3306 mysql:5.7

call wait-err mysql57 "3306  MySQL"

REM create test database
docker exec mysql57 mysql -e "CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;" -uroot -proot
docker exec mysql57 mysql -e "CREATE DATABASE testdataconnector DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;" -uroot -proot

