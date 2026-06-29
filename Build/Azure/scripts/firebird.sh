#!/bin/bash

# Start all four Firebird versions, each in its own container on a distinct host port and database
# file (matching CommonConnectionStrings: 2.5 -> 3025/testdb25, 3 -> 3030/testdb30, 4 -> 3040/testdb40,
# 5 -> 3050/testdb50), so all four Firebird providers run as concurrent lanes within a single CI job.
# Firebird listens on 3050 inside every container; the host port differentiates them.
docker run -d --name firebird25 -e ISC_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb25.fdb                              -p 3025:3050 jacobalberty/firebird:2.5-sc
docker run -d --name firebird3  -e ISC_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb30.fdb -e EnableLegacyClientAuth=true -p 3030:3050 jacobalberty/firebird:v3
docker run -d --name firebird4  -e ISC_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb40.fdb -e EnableLegacyClientAuth=true -p 3040:3050 jacobalberty/firebird:v4
docker run -d --name firebird5  -e ISC_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb50.fdb -e EnableLegacyClientAuth=true -p 3050:3050 jacobalberty/firebird:v5
docker ps -a

sleep 20

docker logs firebird25
docker logs firebird3
docker logs firebird4
docker logs firebird5
