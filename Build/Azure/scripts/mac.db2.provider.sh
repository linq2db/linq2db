#!/bin/bash

rm -rf ./clidriver/

nuget install Net.IBM.Data.Db2-osx -Version 7.0.0.200 -ExcludeVersion
cp -f ./Net.IBM.Data.Db2-osx/lib/net6.0/IBM.Data.Db2.dll ./IBM.Data.Db2.dll
cp -rf ./Net.IBM.Data.Db2-osx/buildTransitive/clidriver/ ./clidriver/
