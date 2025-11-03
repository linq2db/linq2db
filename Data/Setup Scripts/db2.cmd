ECHO OFF

REM try to remove existing container
docker stop db2
docker rm -f db2

REM use pull to get latest layers (run will use cached layers)
docker pull icr.io/db2_community/db2:latest
docker run -d --name db2 --privileged -e LICENSE=accept -e DB2INST1_PASSWORD=Password12! -e DBNAME=testdb -p 50000:50000 icr.io/db2_community/db2:latest

call wait db2 "Setup has completed"
