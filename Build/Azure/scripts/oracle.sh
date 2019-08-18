#!/bin/bash

docker run -d --name oracle --net host -p 1521:1521 datagrip/oracle:11.2
sleep 60
docker ps -a
docker logs oracle
