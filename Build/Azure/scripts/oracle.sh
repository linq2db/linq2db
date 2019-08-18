#!/bin/bash

docker run -d --name oracle -p 1521:1521 -p 8080:8080 datagrip/oracle:11.2
#sleep 100
scripts/wait-for-it.sh localhost:8080 --timeout=600
docker ps -a
docker logs oracle
#docker exec oracle ls -a /u01/app/oracle/product/11.2.0/xe/config/log
#docker exec oracle cat /u01/app/oracle/product/11.2.0/xe/config/log
