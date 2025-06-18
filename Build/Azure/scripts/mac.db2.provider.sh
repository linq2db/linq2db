#!/bin/bash

rm -rf ./clidriver/
if [ $? != 0 ]; then exit 1; fi

# use wget+unzip instead of "nuget install" as it is not available anymore
wget https://www.nuget.org/api/v2/package/Net.IBM.Data.Db2-osx/9.0.0.100
if [ $? != 0 ]; then exit 1; fi

unzip 9.0.0.100 -d Net.IBM.Data.Db2-osx
if [ $? != 0 ]; then exit 1; fi

cp -f ./Net.IBM.Data.Db2-osx/lib/net9.0/IBM.Data.Db2.dll ./IBM.Data.Db2.dll
if [ $? != 0 ]; then exit 1; fi

cp -rf ./Net.IBM.Data.Db2-osx/buildTransitive/clidriver/ ./clidriver/
if [ $? != 0 ]; then exit 1; fi
