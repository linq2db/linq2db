ECHO OFF

REM try to remove existing container
docker stop mysql55
docker rm -f mysql55

REM use pull to get latest layers (run will use cached layers)
docker pull mysql55:5.5
docker run -d --name mysql55 -e MYSQL_ROOT_PASSWORD=root -p 3305:3306 mysql:5.5

call wait-err mysql55 "3306  MySQL"

REM create test database
docker exec mysql55 mysql -e "CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;" -uroot -proot
docker exec mysql55 mysql -e "CREATE DATABASE testdataconnector DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;" -uroot -proot

