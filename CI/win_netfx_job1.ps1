Move-Item -Path "win_netfx_job1.json" -Destination "DataProviders.json" -Force

#install postgresql
#  $env:PGUSER="postgres"
#  $env:PGPASSWORD="Password12!"
#  $dbName = "TestDatanet45"

#  choco install postgresql9 --force --params '/Password:Password12!'
#  set PATH=%PATH%;"C:\Program Files\PostgreSQL\9.6\bin;C:\Program Files\PostgreSQL\9.6\lib"
#  $cmd = '"C:\Program Files\PostgreSQL\9.6\bin\createdb" $dbName'
#  iex "& $cmd"

docker run -d --name postgres10 -e POSTGRES_PASSWORD=Password12! -p 5432:5432 -v /var/run/postgresql:/var/run/postgresql postgres:10
until docker exec postgres10 psql -U postgres -c '\q'; do
  >&2 echo "Postgres is unavailable - sleeping"
  sleep 1
done
docker exec postgres10 psql -U postgres -c "create database testdata"