#!/bin/bash
docker run -d --name firebird  -e FIREBIRD_ROOT_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb.fdb -e FIREBIRD_USE_LEGACY_AUTH=true -e FIREBIRD_DATABASE_DEFAULT_CHARSET=UTF8 -p 3050:3050 firebirdsql/firebird:6-snapshot
docker ps -a
sleep 15

docker logs firebird
