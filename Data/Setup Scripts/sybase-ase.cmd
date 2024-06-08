ECHO OFF

REM try to remove existing container
docker stop sybase
docker rm -f sybase

REM use pull to get latest layers (run will use cached layers)
docker pull datagrip/sybase:16.0
docker run -d --name sybase -e SYBASE_DB=TestDataCore -p 5000:5000 datagrip/sybase:16.0

call wait sybase "SYBASE INITIALIZED"

REM enable utf8
docker exec -e SYBASE=/opt/sybase -e LD_LIBRARY_PATH=/opt/sybase/OCS-16_0/lib3p64/ sybase /opt/sybase/ASE-16_0/bin/charset -Usa -PmyPassword -SMYSYBASE binary.srt utf8

REM create additional database/enable utf8
docker cp sybase-utf8.sql sybase:/opt/sybase/sybase.sql
docker exec -e SYBASE=/opt/sybase sybase /opt/sybase/OCS-16_0/bin/isql -Usa -PmyPassword -SMYSYBASE -i"/opt/sybase/sybase.sql"

ECHO restarting

REM yep, "two" times: https://infocenter.sybase.com/help/index.jsp?topic=/com.sybase.infocenter.dc31654.1550/html/sag1/sag1475.htm
docker restart sybase
TIMEOUT 10
docker restart sybase
TIMEOUT 10

REM create additional database/enable utf8
docker cp sybase-db.sql sybase:/opt/sybase/sybase.sql
docker exec -e SYBASE=/opt/sybase sybase /opt/sybase/OCS-16_0/bin/isql -Usa -PmyPassword -SMYSYBASE -i"/opt/sybase/sybase.sql"


