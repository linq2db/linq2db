ECHO OFF

REM try to remove existing container
docker stop oracle21
docker rm -f oracle21

REM use pull to get latest layers (run will use cached layers)
docker pull container-registry.oracle.com/database/express:21.3.0-xe
docker run -d --name oracle21 -e ORACLE_PWD=password -p 1529:1521 container-registry.oracle.com/database/express:21.3.0-xe

call wait oracle21 "DATABASE IS READY TO USE!"

REM create test database
docker cp oracle-setup.sql oracle21:/setup.sql
docker exec oracle21 sqlplus sys/password@XEPDB1 as sysdba @/setup.sql

REM create test file
docker cp ../Oracle/bfile.txt oracle21:/home/oracle/bfile.txt

