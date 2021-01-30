#!/bin/bash
docker run -d --name firebird -e ISC_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb.fdb -p 3050:3050 jacobalberty/firebird:2.5-sc
docker ps -a
sleep 5

# create Dialect1 database
cat <<EOF > create-test-db
SET SQL DIALECT 1;CREATE DATABASE '/firebird/data/testdbd1.fdb' USER 'SYSDBA' PASSWORD 'masterkey' DEFAULT CHARACTER SET UTF8;QUIT;
EOF
docker cp create-test-db firebird:/firebird/data/create-test-db
docker exec firebird /usr/local/firebird/bin/isql -i /firebird/data/create-test-db

docker logs firebird
