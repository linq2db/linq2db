ECHO OFF

REM try to remove existing container
docker stop mysql
docker rm -f mysql

REM use pull to get latest layers (run will use cached layers)
docker pull mysql:latest
docker run -d --name mysql -e MYSQL_ROOT_PASSWORD=root -p 3306:3306 mysql:latest

call wait-err mysql "3306  MySQL"

REM create test database
docker exec mysql mysql -e "CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;" -uroot -proot
docker exec mysql mysql -e "CREATE DATABASE testdataconnector DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;" -uroot -proot

REM Suppress ERROR 3948 (42000)
docker exec mysql mysql -e "SET GLOBAL local_infile=1;" -uroot -proot
