#!/bin/bash

# Net.IBM.Data.Db2-osx ships one TFM build per major version (9.x -> net8.0, 10.x -> net10.0).
# The version picked here MUST match Net.IBM.Data.Db2-osx in Directory.Packages.props for this TFM
# so the swapped-in osx DLL matches the assembly the tests were compiled against.
# Bump these in lockstep with Directory.Packages.props.
TFM=$(basename "$(dirname "$(dirname "$PWD")")")
if [ "$TFM" = "net10.0" ]; then
	DB2_PKG_VERSION=10.0.0.100
else
	DB2_PKG_VERSION=9.0.0.400
fi

rm -rf ./clidriver/
if [ $? != 0 ]; then exit 1; fi

# use wget+unzip instead of "nuget install" as it is not available anymore
wget https://www.nuget.org/api/v2/package/Net.IBM.Data.Db2-osx/$DB2_PKG_VERSION
if [ $? != 0 ]; then exit 1; fi

unzip $DB2_PKG_VERSION -d Net.IBM.Data.Db2-osx
if [ $? != 0 ]; then exit 1; fi

# the package contains a single lib/<tfm> build for this version; copy whichever it ships
cp -f "$(find ./Net.IBM.Data.Db2-osx/lib -name IBM.Data.Db2.dll | head -1)" ./IBM.Data.Db2.dll
if [ $? != 0 ]; then exit 1; fi

cp -rf ./Net.IBM.Data.Db2-osx/buildTransitive/clidriver/ ./clidriver/
if [ $? != 0 ]; then exit 1; fi
