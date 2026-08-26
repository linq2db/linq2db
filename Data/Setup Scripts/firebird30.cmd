ECHO OFF

REM try to remove existing container
docker stop firebird30
docker rm -f firebird30

docker rm -f firebird30
docker pull firebirdsql/firebird:3
docker run -d --name firebird30 -e FIREBIRD_ROOT_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb30.fdb -e FIREBIRD_USE_LEGACY_AUTH=true -e FIREBIRD_DATABASE_DEFAULT_CHARSET=UTF8 -p 3030:3050 firebirdsql/firebird:3
