rem this is the only image for windows on dockerhub that worked (mostly due to azure agents issues)
rem we will use it for now while we don't have our images
rem runs pgsql 10.5
docker run -d --name pgsql -h pgsql -e POSTGRES_PASSWORD=Password12! -p 5432:5432 dominiquesavoie/postgres:windows-latest
docker ps -a

echo "Waiting"
set max = 100
:repeat
set /a max=max-1
if %max% EQU 0 goto fail
echo pinging postgres
sleep 1
docker exec pgsql psql -U postgres -c "\l"
if %errorlevel% NEQ 0 goto repeat
echo "postgres is UP"

docker exec pgsql psql -U postgres -c "create database testdata"
docker exec pgsql psql -U postgres -c "\l"
docker exec pgsql psql -U postgres -c "SELECT version();"
goto:eof

:fail
echo "Fail"
docker logs pgsql
