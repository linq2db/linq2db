docker run -d --name pgsql -h pgsql -e POSTGRES_PASSWORD=Password12! -p 5432:5432 stefanscherer/postgres-windows:10.2-insider
docker ps -a

echo "Waiting"
set max = 100
:repeat
echo pinging sql server
sleep 1
docker exec pgsql psql -U postgres -c "\l"
set /a max=max-1
if %max% EQU 0 goto fail
if %errorlevel% NEQ 0 goto repeat
echo "Container is UP"

docker exec pgsql psql -U postgres -c "create database testdata"
docker exec pgsql psql -U postgres -c "\l"
docker exec pgsql psql -U postgres -c "SELECT version();"
docker logs pgsql
goto:eof

:fail
echo "Fail"
docker logs pgsql
