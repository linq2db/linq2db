ECHO OFF

REM try to remove existing container
docker stop informix14
docker rm -f informix14

REM Port 9089 conflicts with VMware Converter Service. Use 9189.

docker pull icr.io/informix/informix-developer-database:latest
docker run -d --name informix14 -e INIT_FILE=linq2db.sql -e LICENSE=ACCEPT -p 9189:9089 -p 9088:9088 icr.io/informix/informix-developer-database:latest

docker cp informix_init.sql informix14:/opt/ibm/config/linq2db.sql

call wait informix14 "Informix container login information"
