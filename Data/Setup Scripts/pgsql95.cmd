ECHO OFF

REM try to remove existing container
docker stop pgsql95
docker rm -f pgsql95

REM use pull to get latest layers (run will use cached layers)
docker pull postgres:9.5
docker run -d --name pgsql95 -e POSTGRES_PASSWORD=Password12! -p 5495:5432 -v /var/run/postgresql:/var/run/postgresql postgres:9.5

call wait pgsql95 "PostgreSQL init process complete"

call pgsql-createdb pgsql95
