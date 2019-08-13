docker run -d --name mysql -h mysql -e MYSQL_ROOT_PASSWORD=root -p 3306:3306 steeltoeoss/mysql:latest
docker ps -a

echo "Waiting"
set max = 100
:repeat
echo pinging
sleep 1
mysql mysql --protocol TCP -uroot -proot -e "show databases;" -h mysql
set /a max=max-1
if %max% EQU 0 goto fail
if %errorlevel% NEQ 0 goto repeat
echo "Container is UP"

mysql mysql -e "CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;" -uroot -proot -h mysql
mysql mysql -e "CREATE DATABASE testdata2 DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;" -uroot -proot -h mysql
mysql mysql -e "SELECT VERSION();" -uroot -proot -h mysql
docker logs mysql
goto:eof

:fail
echo "Fail"
docker logs mysql
