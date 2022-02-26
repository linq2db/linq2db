#!/bin/bash

nuget install IBM.Data.DB2.Core-lnx -Version 3.1.0.500 -ExcludeVersion

rm ./IBM.Data.DB2.Core.dll
rm -rf ./clidriver/*
cp -a ./IBM.Data.DB2.Core-lnx/buildTransitive/clidriver/. ./clidriver/
cp -f ./IBM.Data.DB2.Core-lnx/lib/netstandard2.1/IBM.Data.DB2.Core.dll ./IBM.Data.DB2.Core.dll

echo "##vso[task.setvariable variable=PATH]$PATH:$PWD/clidriver/bin:$PWD/clidriver/lib"
echo "##vso[task.setvariable variable=LD_LIBRARY_PATH]$PWD/clidriver/lib/"

