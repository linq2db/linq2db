#!/bin/bash

docker run -d --name oracle -p 1521:1521 -p 8080:8080 datagrip/oracle:11.2

#docker cp scripts/ping-oracle.sql oracle:/test.sql

# do oracle ping 3 times as oralce goes up and down multiple times during start for no GOOD reason
#retries=0
#status="1"
#while [ "$status" != "0" ]
#do
 #   sleep 5
  #  retries=`expr $retries + 1`
   # docker exec oracle sqlplus /nolog @/test.sql
    #status=$?
    #if [ $retries -gt 100 ]; then
     #   >&2 echo 'Failed to start oracle'
      #  exit 1
    #fi;
#done

docker ps -a

retries=0
status="1"
until docker logs oracle | grep -q 'Database ready to use. Enjoy'; do
    sleep 5
    retries=`expr $retries + 1`
    echo waiting for oracle to start
    if [ $retries -gt 100 ]; then
        echo oracle not started or takes too long to start
        exit 1
    fi;
done

docker logs oracle
