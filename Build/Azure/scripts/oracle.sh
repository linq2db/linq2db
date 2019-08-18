#!/bin/bash

docker run -d --name oracle --net host -p 1521:1521 datagrip/oracle:11.2
sleep 80
docker ps -a
docker logs oracle
docker run oracle cat /u01/app/oracle/product/11.2.0/xe/config/log
