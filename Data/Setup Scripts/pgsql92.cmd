ECHO OFF

REM try to remove existing container
docker stop pgsql92
docker rm -f pgsql92

REM use pull to get latest layers (run will use cached layers)
docker pull postgres:9.2
docker run -d --name pgsql92 -e POSTGRES_PASSWORD=Password12! -p 5492:5432 -v /var/run/postgresql:/var/run/postgresql postgres:9.2

call wait pgsql92 "PostgreSQL init process complete"

call pgsql-createdb pgsql92
