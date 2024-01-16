#!/bin/bash

rm -rf ./clidriver/*

nuget install Net.IBM.Data.Db2-lnx -Version 7.0.0.200 -ExcludeVersion
rm ./IBM.Data.Db2.dll
cp -a ./Net.IBM.Data.Db2-lnx/buildTransitive/clidriver/. ./clidriver/
cp -f ./Net.IBM.Data.Db2-lnx/lib/net6.0/IBM.Data.Db2.dll ./IBM.Data.Db2.dll

echo "##vso[task.setvariable variable=PATH]$PATH:$PWD/clidriver/bin:$PWD/clidriver/lib"
echo "##vso[task.setvariable variable=LD_LIBRARY_PATH]$PWD/clidriver/lib/"
