#!/bin/bash

docker run -d --name sybase -e SYBASE_DB=TestDataCore -p 5000:5000 datagrip/sybase162
docker ps -a

#retries=0
#until docker exec sybase /opt/sybase/OCS-16_0/bin/isql -Usa -PmyPassword -SMYSYBASE -i"./init1.sql"
    #sleep 3
    #retries=`expr $retries + 1`
    #if [ $retries -gt 90 ]; then
        #>&2 echo "Failed to wait for mysql to start."
        #docker ps -a
        #docker logs mysql
        #exit 1
    #fi;
#done
