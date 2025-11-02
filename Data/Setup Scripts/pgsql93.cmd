ECHO OFF

REM try to remove existing container
docker stop pgsql93
docker rm -f pgsql93

REM use pull to get latest layers (run will use cached layers)
docker pull postgres:9.3
docker run -d --name pgsql93 -e POSTGRES_PASSWORD=Password12! -p 5493:5432 -v /var/run/postgresql:/var/run/postgresql postgres:9.3

call wait pgsql93 "PostgreSQL init process complete"

call pgsql-createdb pgsql93
