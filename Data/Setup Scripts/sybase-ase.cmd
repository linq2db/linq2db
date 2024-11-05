ECHO OFF

REM try to remove existing container
docker stop sybase
docker rm -f sybase

REM use pull to get latest layers (run will use cached layers)
docker pull linq2db/linq2db:ase-16
docker run -d --name sybase -p 5000:5000 linq2db/linq2db:ase-16

call wait sybase "SYBASE CONFIGURED"
call wait-exit sybase

docker start sybase
call wait-exit sybase

docker start sybase
call wait sybase "SYBASE STARTED"
timeout 5
docker cp ase.sql sybase:/opt/sap/ase.sql
docker exec -e SYBASE=/opt/sap sybase bash -c "source /opt/sap/SYBASE.sh && /opt/sap/OCS-16_0/bin/isql -Usa -PmyPassword -SMYSYBASE -i/opt/sap/ase.sql"

