docker pull steeltoeoss/mysql:5.7
docker run -d --name mysql steeltoeoss/mysql:5.7 --health-cmd="mysqladmin ping --silent" -e MYSQL_ROOT_PASSWORD=root -p:33060:3306
docker ps -a
docker exec mysql mysql -e "CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;" -uroot -proot
