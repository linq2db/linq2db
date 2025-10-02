ECHO OFF

REM try to remove existing container
docker stop pgsql12
docker rm -f pgsql12

REM use pull to get latest layers (run will use cached layers)
docker pull postgres:12
docker run -d --name pgsql12 -e POSTGRES_PASSWORD=Password12! -p 5412:5432 -v /var/run/postgresql:/var/run/postgresql postgres:12

call wait pgsql12 "server started"

call pgsql-createdb pgsql12
