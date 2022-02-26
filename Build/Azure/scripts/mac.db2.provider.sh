#!/bin/bash

nuget install IBM.Data.DB2.Core-lnx -Version 3.1.0.500 -ExcludeVersion

rm -rf ./clidriver/
cp -f ./IBM.Data.DB2.Core-osx/lib/netstandard2.1/IBM.Data.DB2.Core.dll ./IBM.Data.DB2.Core.dll
cp -rf ./IBM.Data.DB2.Core-osx/buildTransitive/clidriver/ ./clidriver/

