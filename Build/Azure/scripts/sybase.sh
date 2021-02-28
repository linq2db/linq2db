#!/bin/bash

docker run -d --name sybase -e SYBASE_DB=TestDataCore -p 5000:5000 datagrip/sybase:16.0
docker ps -a
sleep 45

retries=0
until docker logs sybase | grep -q 'SYBASE INITIALIZED'; do
    sleep 5
    retries=`expr $retries + 1`
    if [ $retries -gt 30 ]; then
        >&2 echo 'Failed to init sybase'
        exit 1
    fi;

    echo 'Waiting for sybase'
done

# temporary fix for https://github.com/linq2db/linq2db/issues/2858
docker exec sybase pkill dataserver

cat <<-EOL > start_fixed.sh
#!/bin/sh
/opt/sybase/ASE-16_0/bin/dataserver \
-d/opt/sybase/data/master.dat \
-e/opt/sybase/ASE-16_0/install/MYSYBASE.log \
-c/opt/sybase/ASE-16_0/MYSYBASE.cfg \
-M/opt/sybase/ASE-16_0 \
-N/opt/sybase/ASE-16_0/sysam/MYSYBASE.properties \
-i/opt/sybase \
-sMYSYBASE \
-T11889
EOL

docker cp start_fixed.sh sybase:/opt/sybase/ASE-16_0/install/start_fixed.sh

docker exec sybase chmod +x /opt/sybase/ASE-16_0/install/start_fixed.sh

docker exec -d sybase bash -c 'export SYBASE=/opt/sybase && source /opt/sybase/SYBASE.sh && sh /opt/sybase/SYBASE.sh && sh /opt/sybase/ASE-16_0/install/start_fixed.sh'

docker logs sybase
