ECHO OFF

REM try to remove existing container
docker stop firebird40
docker rm -f firebird40

docker rm -f firebird40
docker pull jacobalberty/firebird:v4
docker run -d --name firebird40 -e ISC_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb40.fdb -e EnableLegacyClientAuth=true -p 3040:3050 jacobalberty/firebird:v4
