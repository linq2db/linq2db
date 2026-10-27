# Sourced by the sqlserver.*.sh setup scripts.
#
# Wait until $name accepts connections. The mssql images intermittently fail to come up on the CI
# agents (the SQL Server process crashes during startup, or the container exits early); when that
# happens, recreate the container - up to 3 attempts total - before giving up. On the happy path the
# container from the caller's initial `docker run` is already starting, so attempt 1 just waits on it
# and this adds no extra latency.
wait_or_recreate() {
    local name=$1
    local hostport=$2
    local image=$3

    local attempt
    for attempt in 1 2 3; do
        if [ $attempt -gt 1 ]; then
            >&2 echo "Recreating $name (attempt $attempt/3)"
            docker logs $name 2>&1 | tail -n 40 || true
            docker rm -f $name > /dev/null 2>&1 || true
            docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Password12!' -p $hostport:1433 -h $name --name=$name -d $image
        fi

        local retries=0
        while true; do
            if docker exec $name /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q "SELECT 1"; then
                echo "$name is operational (attempt $attempt)"
                return 0
            fi

            # A crashed/exited container will never accept connections: stop waiting immediately and
            # recreate it instead of burning the whole timeout on a dead container.
            if [ "$(docker inspect -f '{{.State.Running}}' $name 2> /dev/null)" != "true" ]; then
                echo "$name is not running (crashed or exited during startup)"
                break
            fi

            sleep 1
            retries=`expr $retries + 1`
            if [ $retries -gt 120 ]; then
                echo "$name did not accept connections within 120s"
                break
            fi
        done
    done

    >&2 echo "Failed to start $name after 3 attempts."
    docker logs $name || true
    return 1
}
