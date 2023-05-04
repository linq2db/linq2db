ECHO OFF

REM try to remove existing container
docker stop oracle12
docker rm -f oracle12

REM use pull to get latest layers (run will use cached layers)
docker pull datagrip/oracle:12.2.0.1-se2-directio
docker run -d --name oracle12 -e ORACLE_PWD=oracle -e ORACLE_SID=ORC12 -p 1522:1521 datagrip/oracle:12.2.0.1-se2-directio

call wait oracle12 "DATABASE IS READY TO USE!"

REM create test database
docker cp oracle-setup.sql oracle12:/setup.sql
docker exec oracle12 sqlplus sys/password@XEPDB1 as sysdba @/setup.sql

REM create test file
docker cp ../Oracle/bfile.txt oracle12:/home/oracle/bfile.txt

