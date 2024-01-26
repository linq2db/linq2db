ECHO OFF

REM try to remove existing container
docker stop firebird30
docker rm -f firebird30

docker rm -f firebird30
docker pull jacobalberty/firebird:v3
docker run -d --name firebird30 -e ISC_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb30.fdb -e EnableLegacyClientAuth=true -p 3030:3050 jacobalberty/firebird:v3
