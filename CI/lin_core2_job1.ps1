Move-Item -Path "lin_core2_job1.json" -Destination "DataProviders.json" -Force

docker run -d --name postgres10 -e POSTGRES_PASSWORD=Password12! -p 5432:5432 -v /var/run/postgresql:/var/run/postgresql postgres:10
until docker exec postgres10 psql -U postgres -c '\q'; do
  >&2 echo "Postgres is unavailable - sleeping"
  sleep 1
done
docker exec postgres10 psql -U postgres -c "create database testdata"