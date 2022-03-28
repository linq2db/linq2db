#!/bin/bash

rm -rf ./clidriver/*

if [ -e ./IBM.Data.Db2.dll ]
then
    nuget install Net.IBM.Data.Db2-lnx -Version 6.0.0.200 -ExcludeVersion
    rm ./IBM.Data.Db2.dll
    cp -a ./Net.IBM.Data.Db2-lnx/buildTransitive/clidriver/. ./clidriver/
    cp -f ./Net.IBM.Data.Db2-lnx/lib/net6.0/IBM.Data.Db2.dll ./IBM.Data.Db2.dll
else
    nuget install IBM.Data.DB2.Core-lnx -Version 3.1.0.500 -ExcludeVersion
    rm ./IBM.Data.DB2.Core.dll
    cp -a ./IBM.Data.DB2.Core-lnx/buildTransitive/clidriver/. ./clidriver/
    cp -f ./IBM.Data.DB2.Core-lnx/lib/netstandard2.1/IBM.Data.DB2.Core.dll ./IBM.Data.DB2.Core.dll
fi

echo "##vso[task.setvariable variable=PATH]$PATH:$PWD/clidriver/bin:$PWD/clidriver/lib"
echo "##vso[task.setvariable variable=LD_LIBRARY_PATH]$PWD/clidriver/lib/"
