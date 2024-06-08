ECHO OFF

REM try to remove existing container
docker stop firebird25
docker rm -f firebird25

docker rm -f firebird25
docker pull jacobalberty/firebird:2.5-sc
docker run -d --name firebird25 -e ISC_PASSWORD=masterkey -e FIREBIRD_DATABASE=testdb25.fdb -p 3025:3050 jacobalberty/firebird:2.5-sc
