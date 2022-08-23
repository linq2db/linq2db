ECHO OFF

REM try to remove existing container
docker stop pgsql12
docker rm -f pgsql12

REM use pull to get latest layers (run will use cached layers)
docker pull postgres:12
docker run -d --name pgsql12 -e POSTGRES_PASSWORD=Password12! -p 5412:5432 -v /var/run/postgresql:/var/run/postgresql postgres:12

ECHO pause to wait for PGSQL startup completion
timeout 5

REM create test database
docker exec pgsql12 psql -U postgres -c "create database testdata"
