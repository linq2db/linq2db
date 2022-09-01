ECHO OFF

REM try to remove existing container
docker stop pgsql10
docker rm -f pgsql10

REM use pull to get latest layers (run will use cached layers)
docker pull postgres:10
docker run -d --name pgsql10 -e POSTGRES_PASSWORD=Password12! -p 5410:5432 -v /var/run/postgresql:/var/run/postgresql postgres:10

ECHO pause to wait for PGSQL startup completion
timeout 5

REM create test database
docker exec pgsql10 psql -U postgres -c "create database testdata"
