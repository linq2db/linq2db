#!/bin/bash
docker run -d --name firebird -e ISC_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb.fdb -e EnableLegacyClientAuth=true -p 3051:3050 jacobalberty/firebird:3.0
docker ps -a
sleep 5
docker logs firebird
