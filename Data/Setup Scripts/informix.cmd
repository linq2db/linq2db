ECHO OFF

REM try to remove existing container
docker stop informix
docker rm -f informix

REM Port 9089 conflicts with VMware Converter Service. Use 9189.

docker pull ibmcom/informix-developer-database:latest
docker run -d --name informix -e INIT_FILE=linq2db.sql -e LICENSE=ACCEPT -p 9189:9089 -p 9088:9088 ibmcom/informix-developer-database:latest

docker cp informix_init.sql informix:/opt/ibm/config/linq2db.sql

call wait informix "Informix container login information"
