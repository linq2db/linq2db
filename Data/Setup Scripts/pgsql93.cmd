ECHO OFF

REM try to remove existing container
docker stop pgsql93
docker rm -f pgsql93

REM use pull to get latest layers (run will use cached layers)
docker pull postgres:9.3
docker run -d --name pgsql93 -e POSTGRES_PASSWORD=Password12! -p 5493:5432 -v /var/run/postgresql:/var/run/postgresql postgres:9.3

ECHO pause to wait for PGSQL startup completion
timeout 5

REM create test database
docker exec pgsql93 psql -U postgres -c "create database testdata"
