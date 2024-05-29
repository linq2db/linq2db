ECHO OFF

REM try to remove existing container
docker stop firebird50
docker rm -f firebird50

docker rm -f firebird50
docker pull jacobalberty/firebird:v5
docker run -d --name firebird50 -e ISC_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb50.fdb -e EnableLegacyClientAuth=true -p 3050:3050 jacobalberty/firebird:v5
