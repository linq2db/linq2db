#!/bin/bash
docker run -d --name firebird  -e ISC_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb.fdb -e EnableLegacyClientAuth=true -p 3050:3050 jacobalberty/firebird:v4.0.0rc1
docker ps -a
sleep 15

docker logs firebird
