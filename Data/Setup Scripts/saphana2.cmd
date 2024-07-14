ECHO OFF

REM try to remove existing container
docker stop hana2
docker rm -f hana2

REM use pull to get latest layers (run will use cached layers)
docker pull saplabs/hanaexpress:latest
docker run -h hxehost -d --name hana2 -p 39017:39017 saplabs/hanaexpress:latest --agree-to-sap-license --passwords-url file:///hana/password.json

docker cp hana-password.json hana2:/hana/password.json
docker exec hana2 sudo chmod 600 /hana/password.json
docker exec hana2 sudo chown 12000:79 /hana/password.json

call wait hana2 "Startup finished"

SET hdbsql="..\..\Redist\SapHana\hdbsql.exe"

REM create test schema
%hdbsql% -n localhost:39017 -u SYSTEM -p Passw0rd "CREATE SCHEMA TESTDB"
REM clear memory limits
%hdbsql% -n localhost:39017 -u SYSTEM -p Passw0rd "ALTER USER SYSTEM CLEAR PARAMETER STATEMENT MEMORY LIMIT"
REM create linked server for FQN names testing
%hdbsql% -n localhost:39017 -u SYSTEM -p Passw0rd "CREATE REMOTE SOURCE ""LINKED_DB"" ADAPTER ""hanaodbc"" CONFIGURATION 'DRIVER=libodbcHDB.so;ServerNode=127.0.0.1:39017;'"
%hdbsql% -n localhost:39017 -u SYSTEM -p Passw0rd "CREATE CREDENTIAL FOR USER SYSTEM COMPONENT 'SAPHANAFEDERATION' PURPOSE 'LINKED_DB' TYPE 'PASSWORD' USING 'user=SYSTEM;password=Passw0rd'"

REM free some memory (diserver ~300mb, webdispatcher ~500m), so we can run tests
%hdbsql% -n localhost:39017 -u SYSTEM -p Passw0rd "ALTER SYSTEM ALTER CONFIGURATION ('daemon.ini','host','hxehost') UNSET ('diserver','instances') WITH RECONFIGURE"
%hdbsql% -n localhost:39017 -u SYSTEM -p Passw0rd "ALTER SYSTEM ALTER CONFIGURATION ('daemon.ini','host','hxehost') SET ('webdispatcher','instances') = '0' WITH RECONFIGURE"
%hdbsql% -n localhost:39017 -u SYSTEM -p Passw0rd "ALTER SYSTEM ALTER CONFIGURATION ('daemon.ini','host','hxehost') SET ('preprocessor','instances') = '0' WITH RECONFIGURE"
