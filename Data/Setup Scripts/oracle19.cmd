ECHO OFF

REM try to remove existing container
docker stop oracle19
docker rm -f oracle19

REM use pull to get latest layers (run will use cached layers)
docker pull oracledb19c/oracle.19.3.0-ee:oracle19.3.0-ee
docker run -d --name oracle19 -e ORACLE_PWD=password -e ORACLE_SID=XE -p 1531:1521 oracledb19c/oracle.19.3.0-ee:oracle19.3.0-ee

call wait oracle19 "Completing Database Creation"

REM create test database
docker cp oracle-setup.sql oracle19:/setup.sql
docker exec oracle19 sqlplus sys/password@ORCLPDB1 as sysdba @/setup.sql

REM create test file
docker cp ../Oracle/bfile.txt oracle19:/home/oracle/bfile.txt
