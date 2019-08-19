#!/bin/bash
docker run -d --name firebird -eFIREBIRD_USER=test -e FIREBIRD_PASSWORD=test -e FIREBIRD_DATABASE=testdb.fdb -e EnableLegacyClientAuth=true -p 3050:3050 jacobalberty/firebird:3.0
docker ps -a
sleep 15
docker exec firebird ls -a /firebird/data
docker logs firebird

