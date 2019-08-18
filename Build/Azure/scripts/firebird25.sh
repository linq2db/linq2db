#!/bin/bash
docker run -d --name firebird -e ISC_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb.fdb -p 3051:3050 jacobalberty/firebird:2.5-sc
docker ps -a
sleep 5
docker logs firebird
