#!/bin/bash -v

rm -rf ./clidriver/*
if [ $? != 0 ]; then exit 1; fi

nuget install Net.IBM.Data.Db2-lnx -Version 7.0.0.400 -ExcludeVersion
if [ $? != 0 ]; then exit 1; fi
rm ./IBM.Data.Db2.dll
if [ $? != 0 ]; then exit 1; fi
ls
cp -a ./Net.IBM.Data.Db2-lnx/buildTransitive/clidriver/. ./clidriver/
if [ $? != 0 ]; then exit 1; fi
cp -f ./Net.IBM.Data.Db2-lnx/lib/net6.0/IBM.Data.Db2.dll ./IBM.Data.Db2.dll
if [ $? != 0 ]; then exit 1; fi

echo "##vso[task.setvariable variable=PATH]$PATH:$PWD/clidriver/bin:$PWD/clidriver/lib"
echo "##vso[task.setvariable variable=LD_LIBRARY_PATH]$PWD/clidriver/lib/"
