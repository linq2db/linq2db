REM TODO: use our own docker images. for now let's just grab anything suitable temporary
docker run -d --name pgsql --net host -p 5432:5432 steeltoeoss/postgresql:10 -e POSTGRES_PASSWORD=Password12!
docker exec pgsql pgsql -U postgres -c 'create database testdata'
