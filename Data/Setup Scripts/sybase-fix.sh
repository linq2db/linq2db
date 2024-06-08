#!/bin/sh
/opt/sybase/ASE-16_0/bin/dataserver -T11889 \
-d/opt/sybase/data/master.dat \
-e/opt/sybase/ASE-16_0/install/MYSYBASE.log \
-c/opt/sybase/ASE-16_0/MYSYBASE.cfg \
-M/opt/sybase/ASE-16_0 \
-N/opt/sybase/ASE-16_0/sysam/MYSYBASE.properties \
-i/opt/sybase \
-sMYSYBASE
