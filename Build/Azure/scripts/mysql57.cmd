docker pull steeltoeoss/mysql:5.7
docker run -d --name mysql steeltoeoss/mysql:5.7 -e MYSQL_ROOT_PASSWORD=root -p 33060:3306
docker ps -a

echo "Waiting for MySQL to start"
:repeat
echo pinging MySQL
docker exec mysql mysql --protocol TCP -uroot -proot -e "show databases;"
if %errorlevel% NEQ 0 goto repeat
echo "MySQL is operational"

docker exec mysql mysql -e "CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;" -uroot -proot
