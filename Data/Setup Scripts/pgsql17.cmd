ECHO OFF

REM try to remove existing container
docker stop pgsql17
docker rm -f pgsql17

REM use pull to get latest layers (run will use cached layers)
docker pull postgres:17
docker run -d --name pgsql17 -e POSTGRES_PASSWORD=Password12! -p 5417:5432 -v /var/run/postgresql:/var/run/postgresql postgres:17

call wait pgsql17 "server started"

call pgsql-createdb pgsql17
