#!/bin/bash
docker run -d --name firebird --net host -e ISC_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb -p 3050:3050 jacobalberty/firebird:2.5-ss
docker ps -a
sleep 5
docker logs firebird
