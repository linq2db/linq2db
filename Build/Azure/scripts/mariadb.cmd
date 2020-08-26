rem change password if it will work
#docker run -d --name mysql -h mysql -p 3306:3306 petrjahoda/mariadb-nanoserver:latest
#docker run -d --name mysql -h mysql -p 3306:3306 philotimo/mariadb-windows:10.3.16-nanoserver-1809
#docker run -d --name mysql -h mysql -p 3306:3306 connorlanigan/mariadb-windows:10.3.7
docker run -d --name mysql -h mysql -p 3306:3306 kiazhi/nanoserver.mariadb:latest
docker ps -a

echo "Waiting"
set max = 100
:repeat
set /a max=max-1
if %max% EQU 0 goto fail
echo pinging
sleep 1
docker exec mysql mysql --protocol TCP -uroot -p54321 -e "show databases;"
if %errorlevel% NEQ 0 goto repeat
echo "Container is UP"

docker exec mysql mysql -e "CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;" -uroot -p54321
docker exec mysql mysql -e "CREATE DATABASE testdata2 DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;" -uroot -p54321
docker exec mysql mysql -e "SELECT VERSION();" -uroot -p54321
docker logs mysql
goto:eof

:fail
echo "Fail"
docker logs mysql
