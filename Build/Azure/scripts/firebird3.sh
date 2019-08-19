#!/bin/bash
docker run -d --name firebird  -e ISC_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb.fdb -e EnableLegacyClientAuth=true -p 3050:3050 jacobalberty/firebird:3.0
docker ps -a
sleep 15
docker exec firebird ls -a /firebird/data

docker exec firebird ls -a /firebird/log

docker logs firebird

