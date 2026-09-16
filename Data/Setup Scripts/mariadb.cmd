ECHO OFF

REM try to remove existing container
docker stop mariadb
docker rm -f mariadb

REM use pull to get latest layers (run will use cached layers)
REM pinned to the major: MariaDB 13 adds UPDATE ... RETURNING, which the MariaDB10 dialect cannot
REM declare. Unpin once #5933 adds a MariaDB13 dialect.
docker pull mariadb:12
docker run -d --name mariadb -e MARIADB_ROOT_PASSWORD=root -e MARIADB_DATABASE=testdata -p 3316:3306 mariadb:12

call wait-err mariadb "3306  mariadb.org"
