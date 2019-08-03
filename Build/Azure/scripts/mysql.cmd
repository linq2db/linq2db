REM windows agent has only mysql command-line tools installed, no db itself
REM TODO: use our own docker images. for now let's just grab anything suitable temporary
docker run -d --name mysql --net host steeltoeoss/mysql:5.7 --health-cmd='mysqladmin ping --silent' -e MYSQL_ROOT_PASSWORD=root
docker exec mysql mysql -e 'CREATE DATABASE testdata DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;' -uroot -proot
