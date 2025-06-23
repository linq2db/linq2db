#!/bin/bash -v

rm -rf ./clidriver/*
if [ $? != 0 ]; then exit 1; fi
rm ./IBM.Data.Db2.dll
if [ $? != 0 ]; then exit 1; fi

# use wget+unzip instead of "nuget install" as it is not available anymore
wget https://www.nuget.org/api/v2/package/Net.IBM.Data.Db2-lnx/9.0.0.200
if [ $? != 0 ]; then exit 1; fi

unzip 9.0.0.200 -d Net.IBM.Data.Db2-lnx
if [ $? != 0 ]; then exit 1; fi

cp -a ./Net.IBM.Data.Db2-lnx/buildTransitive/clidriver/. ./clidriver/
if [ $? != 0 ]; then exit 1; fi
cp -f ./Net.IBM.Data.Db2-lnx/lib/net8.0/IBM.Data.Db2.dll ./IBM.Data.Db2.dll
if [ $? != 0 ]; then exit 1; fi

newPath = $PATH:$PWD/clidriver/bin:$PWD/clidriver/lib
newLdPath = $PWD/clidriver/lib/

# debug
echo "Current PATH=$PATH, New=$newPath"
echo "Current LD_LIBRARY_PATH=$LD_LIBRARY_PATH, New=$newLdPath"

echo "##vso[task.setvariable variable=PATH]$newPath"
echo "##vso[task.setvariable variable=LD_LIBRARY_PATH]$newLdPath"
