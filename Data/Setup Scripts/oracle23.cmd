ECHO OFF

REM try to remove existing container
docker stop oracle23
docker rm -f oracle23

REM use pull to get latest layers (run will use cached layers)
docker pull container-registry.oracle.com/database/free:23.2.0.0
docker run -d --name oracle23 -e ORACLE_PWD=password -p 1530:1521 container-registry.oracle.com/database/free:23.2.0.0

call wait oracle23 "DATABASE IS READY TO USE!"

REM create test database
docker cp oracle-setup.sql oracle23:/setup.sql
docker exec oracle23 sqlplus sys/password@FREEPDB1 as sysdba @/setup.sql

REM create test file
docker cp ../Oracle/bfile.txt oracle23:/home/oracle/bfile.txt

