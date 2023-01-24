ECHO OFF

REM try to remove existing container
docker stop informix
docker rm -f informix

docker pull ibmcom/informix-developer-database:latest
docker run -d --name informix -e INIT_FILE=linq2db.sql -e LICENSE=ACCEPT -p 9089:9089 -p 9088:9088 ibmcom/informix-developer-database:latest

docker cp informix_init.sql informix:/opt/ibm/config/linq2db.sql

call wait informix "Informix container login information"
