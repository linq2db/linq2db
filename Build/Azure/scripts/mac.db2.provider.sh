#!/bin/bash

rm -rf ./clidriver/

if [ -e ./IBM.Data.Db2.dll ]
then
    nuget install Net.IBM.Data.Db2-osx -Version 6.0.0.200 -ExcludeVersion
    cp -f ./Net.IBM.Data.Db2-osx/lib/net6.0/IBM.Data.Db2.dll ./IBM.Data.Db2.dll
    cp -rf ./Net.IBM.Data.Db2-osx/buildTransitive/clidriver/ ./clidriver/
else
    nuget install IBM.Data.DB2.Core-lnx -Version 3.1.0.500 -ExcludeVersion
    cp -f ./IBM.Data.DB2.Core-osx/lib/netstandard2.1/IBM.Data.DB2.Core.dll ./IBM.Data.DB2.Core.dll
    cp -rf ./IBM.Data.DB2.Core-osx/buildTransitive/clidriver/ ./clidriver/
fi
