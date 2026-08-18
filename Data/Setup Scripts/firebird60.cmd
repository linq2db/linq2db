ECHO OFF

REM try to remove existing container
docker stop firebird60
docker rm -f firebird60

docker rm -f firebird60
docker pull firebirdsql/firebird:6-snapshot
docker run -d --name firebird60 -e FIREBIRD_ROOT_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb60.fdb -e FIREBIRD_USE_LEGACY_AUTH=true -e FIREBIRD_DATABASE_DEFAULT_CHARSET=UTF8 -p 3060:3050 firebirdsql/firebird:6-snapshot
