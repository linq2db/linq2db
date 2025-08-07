#!/bin/bash

docker run -d --name ydb -p 2136:2136 -e YDB_FEATURE_FLAGS=enable_parameterized_decimal,enable_temp_tables ydbplatform/local-ydb:latest

retries=0
until docker logs ydb 2>&1 | grep -q 'Table profiles were not loaded'; do
    sleep 5
    retries=`expr $retries + 1`
    echo waiting for YDB to start
    if [ $retries -gt 50 ]; then
        echo YDB not started or takes too long to start
        docker logs ydb
        exit 1
    fi;
done

docker logs ydb
