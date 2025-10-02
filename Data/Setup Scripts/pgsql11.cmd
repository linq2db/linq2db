ECHO OFF

REM try to remove existing container
docker stop pgsql11
docker rm -f pgsql11

REM use pull to get latest layers (run will use cached layers)
docker pull postgres:11
docker run -d --name pgsql11 -e POSTGRES_PASSWORD=Password12! -p 5411:5432 -v /var/run/postgresql:/var/run/postgresql postgres:11

call wait pgsql11 "server started"

call pgsql-createdb pgsql11
