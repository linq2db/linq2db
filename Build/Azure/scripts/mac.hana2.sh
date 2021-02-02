#!/bin/bash

docker run -d --name hana2 -p 39017:39017 store/saplabs/hanaexpress:2.00.045.00.20200121.1 --agree-to-sap-license --passwords-url file:///hana/password.json

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
    if [ $retries -gt 500 ]; then
        echo hana2 not started or takes too long to start
        exit 1
    fi;
done

echo "hana started after ${retries} wait cycles"

docker logs hana2

# create test schema
~/linq2db_ci/providers/saphana/macos/HDBSQL/hdbsql -d HXE -n localhost:39017 -u SYSTEM -p Passw0rd CREATE SCHEMA TESTDB
# clear memory limits
~/linq2db_ci/providers/saphana/macos/HDBSQL/hdbsql -d HXE -n localhost:39017 -u SYSTEM -p Passw0rd ALTER USER SYSTEM CLEAR PARAMETER STATEMENT MEMORY LIMIT
# create linked server for FQN names testing
~/linq2db_ci/providers/saphana/macos/HDBSQL/hdbsql -d HXE -n localhost:39017 -u SYSTEM -p Passw0rd 'CREATE REMOTE SOURCE "LINKED_DB" ADAPTER "hanaodbc" CONFIGURATION '"'"'DRIVER=libodbcHDB.so;ServerNode=127.0.0.1:39017;'"'"''
~/linq2db_ci/providers/saphana/macos/HDBSQL/hdbsql -d HXE -n localhost:39017 -u SYSTEM -p Passw0rd 'CREATE CREDENTIAL FOR USER SYSTEM COMPONENT '"'"'SAPHANAFEDERATION'"'"' PURPOSE '"'"'LINKED_DB'"'"' TYPE '"'"'PASSWORD'"'"' USING '"'"'user=SYSTEM;password=Passw0rd'"'"''

cat <<-EOJSON > UserDataProviders.json
{
    "BASE.Azure": {
        "BasedOn": "AzureConnectionStrings",
        "DefaultConfiguration": "SQLite.MS",
        "TraceLevel": "Info",
        "Connections": {
            "SapHana.Odbc": {
                "ConnectionString": "Driver=$HOME/linq2db_ci/providers/saphana/macos/ODBC/libSQLDBCHDB.dylib;SERVERNODE=localhost:39017;databaseName=HXE;CS=TESTDB;UID=SYSTEM;PWD=Passw0rd;"
            }
        }
    },
    "CORE21.Azure": {
        "BasedOn": "BASE.Azure",
        "Providers": [
            "SapHana.Odbc"
        ]
    },
    "CORE31.Azure": {
        "BasedOn": "BASE.Azure",
        "Providers": [
            "SapHana.Odbc"
        ]
    },
    "NET50.Azure": {
        "BasedOn": "BASE.Azure",
        "Providers": [
            "SapHana.Odbc"
        ]
    }
}
EOJSON
