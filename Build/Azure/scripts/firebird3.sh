#!/bin/bash
docker run -d --name firebird -eFIREBIRD_USER=test -e ISC_PASSWORD=test -e FIREBIRD_DATABASE=testdb.fdb -e EnableWireCrypt=true -p 3050:3050 jacobalberty/firebird:3.0
docker ps -a
sleep 15
docker logs firebird
