ECHO OFF

REM try to remove existing container
docker stop pgsql18
docker rm -f pgsql18

REM use pull to get latest layers (run will use cached layers)
docker pull postgres:18rc1
docker run -d --name pgsql18 -e POSTGRES_PASSWORD=Password12! -p 5418:5432 -v /var/run/postgresql:/var/run/postgresql postgres:18rc1

call wait pgsql18 "server started"

call pgsql-createdb pgsql18
