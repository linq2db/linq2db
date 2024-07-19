#!/bin/bash

docker run -h hxehost -d --name hana2 -p 39017:39017 saplabs/hanaexpress:latest --agree-to-sap-license --passwords-url file:///hana/password.json
#echo Generate password file
cat <<-EOJSON > hana_password.json
{"master_password": "Passw0rd"}
EOJSON

docker cp hana_password.json hana2:/hana/password.json
docker exec hana2 sudo chmod 600 /hana/password.json
docker exec hana2 sudo chown 12000:79 /hana/password.json

docker ps -a

git clone https://github.com/linq2db/linq2db.ci.git ~/linq2db_ci

retries=0
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

# create test schema
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd CREATE SCHEMA TESTDB
# clear memory limits
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd ALTER USER SYSTEM CLEAR PARAMETER STATEMENT MEMORY LIMIT
# create linked server for FQN names testing
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'CREATE REMOTE SOURCE "LINKED_DB" ADAPTER "hanaodbc" CONFIGURATION '"'"'DRIVER=libodbcHDB.so;ServerNode=127.0.0.1:39017;'"'"''
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'CREATE CREDENTIAL FOR USER SYSTEM COMPONENT '"'"'SAPHANAFEDERATION'"'"' PURPOSE '"'"'LINKED_DB'"'"' TYPE '"'"'PASSWORD'"'"' USING '"'"'user=SYSTEM;password=Passw0rd'"'"''

# free some memory (diserver ~300mb, webdispatcher ~500m), so we can run tests
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'daemon.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') UNSET ('"'"'diserver'"'"','"'"'instances'"'"') WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'daemon.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'webdispatcher'"'"','"'"'instances'"'"') = '"'"'0'"'"' WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'daemon.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'preprocessor'"'"','"'"'instances'"'"') = '"'"'0'"'"' WITH RECONFIGURE'

# additional memory tuning
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'nameserver.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'dynamic_result_cache'"'"','"'"'total_size'"'"') = '"'"'1'"'"' WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'nameserver.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'result_cache'"'"','"'"'total_size'"'"') = '"'"'1'"'"' WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'indexserver.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'dynamic_result_cache'"'"','"'"'total_size'"'"') = '"'"'1'"'"' WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'indexserver.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'result_cache'"'"','"'"'total_size'"'"') = '"'"'1'"'"' WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'nameserver.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'load_trace'"'"','"'"'enable'"'"') = '"'"'false'"'"' WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'indexserver.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'load_trace'"'"','"'"'enable'"'"') = '"'"'false'"'"' WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'nameserver.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'mergedog'"'"','"'"'unload_check_interval'"'"') = '"'"'5000'"'"' WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'indexserver.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'mergedog'"'"','"'"'unload_check_interval'"'"') = '"'"'5000'"'"' WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'nameserver.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'session'"'"','"'"'connection_history_maximum_size'"'"') = '"'"'10'"'"' WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'indexserver.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'session'"'"','"'"'connection_history_maximum_size'"'"') = '"'"'10'"'"' WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'nameserver.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'session'"'"','"'"'result_buffer_reclaim_threshold'"'"') = '"'"'1000'"'"' WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'indexserver.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'session'"'"','"'"'result_buffer_reclaim_threshold'"'"') = '"'"'1000'"'"' WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'nameserver.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'sql'"'"','"'"'plan_cache_enabled'"'"') = '"'"'false'"'"' WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'indexserver.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'sql'"'"','"'"'plan_cache_enabled'"'"') = '"'"'false'"'"' WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'nameserver.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'sql'"'"','"'"'plan_cache_statistics_enabled'"'"') = '"'"'false'"'"' WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'indexserver.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'sql'"'"','"'"'plan_cache_statistics_enabled'"'"') = '"'"'false'"'"' WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'nameserver.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'sql'"'"','"'"'plan_statistics_enabled'"'"') = '"'"'false'"'"' WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'indexserver.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'sql'"'"','"'"'plan_statistics_enabled'"'"') = '"'"'false'"'"' WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'nameserver.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'debugger'"'"','"'"'enabled'"'"') = '"'"'false'"'"' WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'nameserver.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'unload_trace'"'"','"'"'enable'"'"') = '"'"'false'"'"' WITH RECONFIGURE'
~/linq2db_ci/providers/saphana/linux/HDBSQL/hdbsql -n localhost:39017 -u SYSTEM -p Passw0rd 'ALTER SYSTEM ALTER CONFIGURATION ('"'"'indexserver.ini'"'"','"'"'host'"'"','"'"'hxehost'"'"') SET ('"'"'unload_trace'"'"','"'"'enable'"'"') = '"'"'false'"'"' WITH RECONFIGURE'

cat <<-EOJSON > HanaDataProviders.json
{
    "BASE.Azure": {
        "BasedOn": "AzureConnectionStrings",
        "DefaultConfiguration": "SQLite.MS",
        "TraceLevel": "Info",
        "Connections": {
            "SapHana.Odbc": {
                "ConnectionString": "Driver=$HOME/linq2db_ci/providers/saphana/linux/ODBC/libodbcHDB.so;SERVERNODE=localhost:39017;CS=TESTDB;UID=SYSTEM;PWD=Passw0rd;"
            }
        }
    },
    "NET60.Azure": {
        "BasedOn": "BASE.Azure",
        "Providers": [
            "SapHana.Odbc"
        ]
    },
    "NET80.Azure": {
        "BasedOn": "BASE.Azure",
        "Providers": [
            "SapHana.Odbc"
        ]
    }
}
EOJSON
