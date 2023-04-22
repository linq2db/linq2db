ECHO OFF

REM try to remove existing container
docker stop pgsql14
docker rm -f pgsql14

REM use pull to get latest layers (run will use cached layers)
docker pull postgres:14
docker run -d --name pgsql14 -e POSTGRES_PASSWORD=Password12! -p 5414:5432 -v /var/run/postgresql:/var/run/postgresql postgres:14

call wait pgsql14 "server started"

REM create test database
docker exec pgsql14 psql -U postgres -c "create database testdata"
