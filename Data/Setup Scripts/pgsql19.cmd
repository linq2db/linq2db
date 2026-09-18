ECHO OFF

REM try to remove existing container
docker stop pgsql19
docker rm -f pgsql19

REM use pull to get latest layers (run will use cached layers)
docker pull postgres:19beta1
docker run -d --name pgsql19 -e POSTGRES_PASSWORD=Password12! -p 5419:5432 -v /var/run/postgresql:/var/run/postgresql postgres:19beta1

call wait pgsql19 "server started"

call pgsql-createdb pgsql19
