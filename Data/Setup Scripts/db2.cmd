ECHO OFF

REM try to remove existing container
docker stop db2
docker rm -f db2

REM use pull to get latest layers (run will use cached layers)
docker pull icr.io/db2_community/db2:latest
docker run -d --name db2 --privileged -e LICENSE=accept -e DB2INST1_PASSWORD=Password12! -e DBNAME=testdb -p 50000:50000 icr.io/db2_community/db2:latest

call wait db2 "Setup has completed"

REM ---------------------------------------------------------------------------
REM Patch the container's restart script to fix the docker-stop/start failure.
REM
REM Without this patch, `docker stop db2` followed by `docker start db2` exits
REM the container with code 1, error chain:
REM   - db2icrt fails (DBI1264E) because fencedid is owned root:root instead of
REM     root:db2iadm1 (DBI20187E in db2icrt.log)
REM   - the image's setup_db2_instance.sh has a chown bug at this exact spot
REM     (references `.fenced` with a leading dot; actual file is `fencedid`)
REM
REM Fix: insert a `chown root:db2iadm1 .../fencedid` line just before the
REM `create_instance` call so the file has the right ownership before db2icrt
REM checks it. Patch persists in the container's writable layer until
REM `docker rm` — re-run db2.cmd if the container is recreated.
REM
REM IBM thread (DBI20187E):
REM   https://community.ibm.com/community/user/discussion/121-container-community-edition-docker-start-fails-dbi20187e
REM ---------------------------------------------------------------------------

docker exec db2 sed -i "/^if ! create_instance; then$/i [ -f /database/config/db2inst1/sqllib/adm/fencedid ] && chown root:db2iadm1 /database/config/db2inst1/sqllib/adm/fencedid" /var/db2_setup/lib/setup_db2_instance.sh
