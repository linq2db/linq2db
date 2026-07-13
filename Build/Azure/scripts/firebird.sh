#!/bin/bash

# Start all four Firebird versions, each in its own container on a distinct host port and database
# file (matching CommonConnectionStrings: 2.5 -> 3025/testdb25, 3 -> 3030/testdb30, 4 -> 3040/testdb40,
# 5 -> 3050/testdb50), so all four Firebird providers run as concurrent lanes within a single CI job.
# Firebird listens on 3050 inside every container; the host port differentiates them.
# 3/4/5 use the official firebirdsql/firebird images (newer point releases; DB created under
# /var/lib/firebird/data). 2.5 is EOL with no official image, so it stays on jacobalberty
# (DB under /firebird/data). Paths are reflected in DataProviders.json connection strings.
docker run -d --name firebird25 -e ISC_PASSWORD=masterkey        -e FIREBIRD_DATABASE=testdb25.fdb                                                                            -p 3025:3050 jacobalberty/firebird:2.5-sc
docker run -d --name firebird3  -e FIREBIRD_ROOT_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb30.fdb -e FIREBIRD_USE_LEGACY_AUTH=true -e FIREBIRD_DATABASE_DEFAULT_CHARSET=UTF8 -p 3030:3050 firebirdsql/firebird:3
docker run -d --name firebird4  -e FIREBIRD_ROOT_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb40.fdb -e FIREBIRD_USE_LEGACY_AUTH=true -e FIREBIRD_DATABASE_DEFAULT_CHARSET=UTF8 -p 3040:3050 firebirdsql/firebird:4
docker run -d --name firebird5  -e FIREBIRD_ROOT_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb50.fdb -e FIREBIRD_USE_LEGACY_AUTH=true -e FIREBIRD_DATABASE_DEFAULT_CHARSET=UTF8 -p 3050:3050 firebirdsql/firebird:5
docker ps -a

sleep 20

docker logs firebird25
docker logs firebird3
docker logs firebird4
docker logs firebird5
