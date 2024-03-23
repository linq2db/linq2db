ECHO OFF

REM try to remove existing container
docker stop mariadb
docker rm -f mariadb

REM use pull to get latest layers (run will use cached layers)
docker pull mariadb:latest
docker run -d --name mariadb -e MARIADB_ROOT_PASSWORD=root -e MARIADB_DATABASE=testdata -p 3316:3306 mariadb:latest

call wait-err mariadb "3306  mariadb.org"
