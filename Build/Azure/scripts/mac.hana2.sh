#!/bin/bash

docker run -d --name hana2 -p 39013:39013 store/saplabs/hanaexpress:2.00.040.00.20190729.1 --agree-to-sap-license --passwords-url file:///password.json

echo Generate password file
cat <<-EOJSON > hana_password.json
{"master_password": "password"}
EOJSON

docker cp hana_password.json hana2:/password.json

docker ps -a

retries=0
status="1"
until docker logs hana2 | grep -q 'cannot remove'; do
    sleep 5
    retries=`expr $retries + 1`
    echo waiting for hana2 to complain about password.json
    if [ $retries -gt 100 ]; then
        echo hana2 not started or takes too long to start
        exit 1
    fi;
done

docker exec hana2 sudo rm /password.json

retries=0
status="1"
until docker logs hana2 | grep -q 'Startup finished'; do
    sleep 5
    retries=`expr $retries + 1`
    echo waiting for hana2 to start
    if [ $retries -gt 100 ]; then
        echo hana2 not started or takes too long to start
        exit 1
    fi;
done

docker logs hana2
