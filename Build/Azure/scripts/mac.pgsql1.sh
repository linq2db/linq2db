#!/bin/bash

# macOS variant of pgsql1.sh: older PostgreSQL versions (9.2-12), each in its own container on a
# distinct host port (54NN scheme). macOS CI legs are currently disabled, but this mirrors the linux
# script so the matrix entry stays valid if they are re-enabled.

# "<docker tag>:<host port>" pairs.
versions="9.2:5492 9.3:5493 9.5:5495 10:5410 11:5411 12:5412"

for vp in $versions; do
    ver="${vp%%:*}"
    port="${vp##*:}"
    name="pgsql${ver//./}"
    docker run -d --name "$name" -h "$name" -e POSTGRES_PASSWORD=Password12! -p "${port}:5432" "postgres:${ver}"
done

for vp in $versions; do
    ver="${vp%%:*}"
    port="${vp##*:}"
    name="pgsql${ver//./}"
    retries=0
    until docker exec "$name" psql -U postgres -c '\l' | grep -q 'testdata'; do
        sleep 1
        retries=$((retries + 1))
        docker exec "$name" psql -U postgres -c 'create database testdata' >/dev/null 2>&1
        if [ "$retries" -gt 120 ]; then
            echo "postgres $ver ($name) failed to start or create database"
            docker logs "$name"
            exit 1
        fi
    done
    echo "postgres $ver ready on port $port"
done
