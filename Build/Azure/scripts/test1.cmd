rem change password if it will work
docker run -d --name mysql -h mysql -p 3306:3306 petrjahoda/mariadb-nanoserver:latest
docker ps -a

echo "Waiting"
set max = 100
:repeat
echo pinging
sleep 1
docker exec mysql mysql --protocol TCP -uroot -proot -e "show databases;"
set /a max=max-1
if %max% EQU 0 goto fail
if %errorlevel% NEQ 0 goto repeat
echo "Container is UP"

docker exec mysql mysql --protocol TCP -uroot -p54321 -e "ALTER USER 'root'@'%' IDENTIFIED BY 'root';"
docker exec mysql mysql -e "CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;" -uroot -proot
docker exec mysql mysql -e "CREATE DATABASE testdata2 DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;" -uroot -proot
docker exec mysql mysql -e "SELECT VERSION();" -uroot -proot
docker logs mysql
goto:eof

:fail
echo "Fail"
docker logs mysql
