ECHO OFF

REM try to remove existing container
docker stop oracle11
docker rm -f oracle11

REM use pull to get latest layers (run will use cached layers)
docker pull datagrip/oracle:11.2
docker run -d --name oracle11 -p 1521:1521 datagrip/oracle:11.2

call wait oracle11 "Database ready to use"

REM create test database
docker cp oracle11-setup.sql oracle11:/setup.sql
docker exec oracle11 sqlplus sys/oracle@localhost as sysdba @/setup.sql

REM create test file
docker exec oracle11 mkdir /home/oracle
docker cp ../Oracle/bfile.txt oracle11:/home/oracle/bfile.txt

