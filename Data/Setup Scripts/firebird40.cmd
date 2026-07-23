ECHO OFF

REM try to remove existing container
docker stop firebird40
docker rm -f firebird40

docker rm -f firebird40
docker pull firebirdsql/firebird:4
docker run -d --name firebird40 -e FIREBIRD_ROOT_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb40.fdb -e FIREBIRD_USE_LEGACY_AUTH=true -e FIREBIRD_DATABASE_DEFAULT_CHARSET=UTF8 -p 3040:3050 firebirdsql/firebird:4
