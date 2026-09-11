ECHO OFF

REM try to remove existing container
docker stop ydb
docker rm -f ydb

REM use pull to get latest layers (run will use cached layers)
docker pull ydbplatform/local-ydb:latest
REM YDB_USE_IN_MEMORY_PDISKS: RAM-backed storage instead of file-backed disks, so commits skip real
REM fsync/disk I/O - YDB's own recommended setting for test environments, where durability across
REM container restarts doesn't matter (test data gets recreated on every run anyway).
docker run -d --name ydb -p 2136:2136 -p 8765:8765 -e YDB_FEATURE_FLAGS=enable_temp_tables -e YDB_USE_IN_MEMORY_PDISKS=1 ydbplatform/local-ydb:latest

ECHO pause to wait for YDB startup completion
call wait-err ydb "Table profiles were not loaded"

REM UI at http://127.0.0.1:8765
