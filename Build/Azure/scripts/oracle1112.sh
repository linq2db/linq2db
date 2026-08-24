#!/bin/bash

echo "##vso[task.setvariable variable=TZ]CET"

# Oracle 11g (host port 1521) and 12c (host port 1522) run as concurrent lanes in one job.
# Each Oracle listens on 1521 inside its container; the host port differentiates them.
docker run -d --name oracle11                                    -p 1521:1521 datagrip/oracle:11.2
docker run -d --name oracle12 -e ORACLE_PWD=oracle -e ORACLE_SID=ORC12 -p 1522:1521 datagrip/oracle:12.2.0.1-se2-directio
docker ps -a

echo -n 12345 > bfile.txt

# --- Oracle 11g ---
retries=0
until docker logs oracle11 | grep -q 'Database ready to use'; do
    sleep 5
    retries=`expr $retries + 1`
    echo waiting for oracle11 to start
    if [ $retries -gt 200 ]; then
        echo oracle11 not started or takes too long to start
        docker logs oracle11
        exit 1
    fi;
done

cat <<-EOL > setup.sql
create user test identified by test;
grant unlimited  tablespace to test;
grant all privileges to test identified by test;
GRANT SELECT ON sys.dba_users TO test;
EXIT;
EOL
docker cp setup.sql oracle11:/setup.sql
docker exec oracle11 sqlplus sys/oracle@localhost as sysdba @/setup.sql
docker exec oracle11 mkdir /home/oracle
docker cp bfile.txt oracle11:/home/oracle/bfile.txt

# --- Oracle 12c ---
retries=0
until docker logs oracle12 | grep -q 'DATABASE IS READY TO USE!'; do
    sleep 10
    retries=`expr $retries + 1`
    echo waiting for oracle12 to start
    # 300 retries, as oracle image is really slow to start
    if [ $retries -gt 300 ]; then
        echo oracle12 not started or takes too long to start
        docker logs oracle12
        exit 1
    fi;
done

docker cp bfile.txt oracle12:/home/oracle/bfile.txt

docker logs oracle11
docker logs oracle12
