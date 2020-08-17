docker run -d --name pgsql -p 5432:5432 stellirin/postgres-windows:10 -e POSTGRES_PASSWORD=Password12!
docker ps -a

echo "Waiting for PGSQL to start"
:repeat
echo pinging pgsql
docker exec pgsql psql -U postgres -c "select 1"
if %errorlevel% NEQ 0 goto repeat
echo "PGSQL is operational"

docker exec pgsql psql -U postgres -c "create database testdata"
