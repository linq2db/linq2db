ECHO OFF

REM try to remove existing container
docker stop firebird50
docker rm -f firebird50

docker rm -f firebird50
docker pull firebirdsql/firebird:5
docker run -d --name firebird50 -e FIREBIRD_ROOT_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb50.fdb -e FIREBIRD_USE_LEGACY_AUTH=true -e FIREBIRD_DATABASE_DEFAULT_CHARSET=UTF8 -p 3050:3050 firebirdsql/firebird:5
