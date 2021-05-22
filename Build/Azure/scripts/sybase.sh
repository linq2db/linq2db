#!/bin/bash

docker run -d --name sybase -e SYBASE_DB=TestDataCore -p 5000:5000 datagrip/sybase:16.0
docker ps -a

# add trace flag to server startup procedure
cat <<-EOL > start_fixed.sh
#!/bin/sh
/opt/sybase/ASE-16_0/bin/dataserver -T11889 \
-d/opt/sybase/data/master.dat \
-e/opt/sybase/ASE-16_0/install/MYSYBASE.log \
-c/opt/sybase/ASE-16_0/MYSYBASE.cfg \
-M/opt/sybase/ASE-16_0 \
-N/opt/sybase/ASE-16_0/sysam/MYSYBASE.properties \
-i/opt/sybase \
-sMYSYBASE
EOL

docker cp start_fixed.sh sybase:/opt/sybase/ASE-16_0/install/RUN_MYSYBASE
docker exec sybase chmod +x /opt/sybase/ASE-16_0/install/RUN_MYSYBASE

# restart container to take effect
docker stop sybase
docker start sybase

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

docker logs sybase
