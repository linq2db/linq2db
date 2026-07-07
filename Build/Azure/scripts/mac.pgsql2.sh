#!/bin/bash

# macOS variant of pgsql2.sh: newer PostgreSQL versions (13-19), each in its own container on a
# distinct host port (54NN scheme). macOS CI legs are currently disabled, but this mirrors the linux
# script so the matrix entry stays valid if they are re-enabled.

# "<version>:<host port>" pairs.
versions="13:5413 14:5414 15:5415 16:5416 17:5417 18:5418 19:5419"

for vp in $versions; do
    ver="${vp%%:*}"
    port="${vp##*:}"
    name="pgsql${ver//./}"
    # PostgreSQL 19 ships only as a prerelease image tag; other versions match the version tag.
    image="postgres:${ver}"
    [ "$ver" = "19" ] && image="postgres:19beta1"
    docker run -d --name "$name" -h "$name" -e POSTGRES_PASSWORD=Password12! -p "${port}:5432" "$image"
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
