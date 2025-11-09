ECHO OFF

REM try to remove existing container
docker stop pgsql16
docker rm -f pgsql16

REM use pull to get latest layers (run will use cached layers)
docker pull postgres:16
docker run -d --name pgsql16 -e POSTGRES_PASSWORD=Password12! -p 5416:5432 -v /var/run/postgresql:/var/run/postgresql postgres:16

call wait pgsql16 "server started"

call pgsql-createdb pgsql16
