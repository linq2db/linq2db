REM windows agent has only mysql command-line tools installed, no db itself
docker run -d --name mysql57 --net host mysql:5.7 --health-cmd='mysqladmin ping --silent' -e MYSQL_ROOT_PASSWORD=root
docker exec mysql57 mysql -e 'CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
