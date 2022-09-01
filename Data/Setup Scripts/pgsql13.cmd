ECHO OFF

REM try to remove existing container
docker stop pgsql13
docker rm -f pgsql13

REM use pull to get latest layers (run will use cached layers)
docker pull postgres:13
docker run -d --name pgsql13 -e POSTGRES_PASSWORD=Password12! -p 5413:5432 -v /var/run/postgresql:/var/run/postgresql postgres:13

ECHO pause to wait for PGSQL startup completion
timeout 5

REM create test database
docker exec pgsql13 psql -U postgres -c "create database testdata"
