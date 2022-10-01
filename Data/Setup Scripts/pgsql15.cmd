ECHO OFF

REM try to remove existing container
docker stop pgsql15
docker rm -f pgsql15

REM use pull to get latest layers (run will use cached layers)
docker pull postgres:15rc1
docker run -d --name pgsql15 -e POSTGRES_PASSWORD=Password12! -p 5415:5432 -v /var/run/postgresql:/var/run/postgresql postgres:15rc1

ECHO pause to wait for PGSQL startup completion
timeout 5

REM create test database
docker exec pgsql15 psql -U postgres -c "create database testdata"
