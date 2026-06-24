#!/bin/bash

# Starts every supported PostgreSQL version, each in its own container on a distinct host port
# (the 54NN scheme from CommonConnectionStrings: 9.2 -> 5492, 10 -> 5410, 18 -> 5418), so they
# run as concurrent provider lanes within a single CI job. TCP only: no --net host (can't remap
# ports) and no shared /var/run/postgresql socket mount (it would collide across containers).

# "<docker tag>:<host port>" pairs.
versions="9.2:5492 9.3:5493 9.5:5495 10:5410 11:5411 12:5412 13:5413 14:5414 15:5415 16:5416 17:5417 18:5418"

# start every container (image is pulled on demand by docker run)
for vp in $versions; do
    ver="${vp%%:*}"
    port="${vp##*:}"
    name="pgsql${ver//./}"
    docker run -d --name "$name" -e POSTGRES_PASSWORD=Password12! -p "${port}:5432" "postgres:${ver}"
done

# wait for each server to accept connections and create the testdata database
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
