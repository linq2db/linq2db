ECHO OFF

REM try to remove existing container
docker stop oracle18
docker rm -f oracle18

REM use pull to get latest layers (run will use cached layers)
docker pull container-registry.oracle.com/database/express:18.4.0-xe
docker run -d --name oracle18 -e ORACLE_PWD=password -p 1528:1521 container-registry.oracle.com/database/express:18.4.0-xe

call wait oracle18 "DATABASE IS READY TO USE!"

REM create test database
docker cp oracle-setup.sql oracle18:/setup.sql
docker exec oracle18 sqlplus sys/password@XEPDB1 as sysdba @/setup.sql

REM create test file
docker cp ../Oracle/bfile.txt oracle18:/home/oracle/bfile.txt

