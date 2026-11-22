#!/bin/bash
# Not -v: it echoes sourced files' text into step output, where the Azure agent parses any
# task.setvariable it finds - including one merely mentioned in a comment. Failed build 23225.

# The version picked here MUST match Net.IBM.Data.Db2-lnx in Directory.Packages.props
# so the swapped-in linux DLL matches the assembly the tests were compiled against.
# Bump it in lockstep with Directory.Packages.props.
DB2_PKG_VERSION=10.0.0.400

rm -rf ./clidriver/*
if [ $? != 0 ]; then exit 1; fi
rm ./IBM.Data.Db2.dll
if [ $? != 0 ]; then exit 1; fi

# use wget+unzip instead of "nuget install" as it is not available anymore
wget https://www.nuget.org/api/v2/package/Net.IBM.Data.Db2-lnx/$DB2_PKG_VERSION
if [ $? != 0 ]; then exit 1; fi

unzip $DB2_PKG_VERSION -d Net.IBM.Data.Db2-lnx
if [ $? != 0 ]; then exit 1; fi

cp -a ./Net.IBM.Data.Db2-lnx/buildTransitive/clidriver/. ./clidriver/
if [ $? != 0 ]; then exit 1; fi
# the package contains a single lib/<tfm> build for this version; copy whichever it ships
cp -f "$(find ./Net.IBM.Data.Db2-lnx/lib -name IBM.Data.Db2.dll | head -1)" ./IBM.Data.Db2.dll
if [ $? != 0 ]; then exit 1; fi

. "$(dirname "$0")/ci-setvar.sh"
ci_setvar PATH "$PATH:$PWD/clidriver/bin:$PWD/clidriver/lib"
ci_setvar LD_LIBRARY_PATH "$PWD/clidriver/lib/"
