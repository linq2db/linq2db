#!/bin/bash

docker run -d --name oracle -e ORACLE_USER=orauser -e ORACLE_PASSWORD=password1! -p 8080:8080 -p 1521:1521 datagrip/oracle:11.2
docker ps -a
docker logs oracle
