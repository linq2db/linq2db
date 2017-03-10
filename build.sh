#!/usr/bin/env bash

#exit if any command fails
set -e

artifactsFolder="./artifacts"

if [ -d $artifactsFolder ]; then  
  rm -R $artifactsFolder
fi

dotnet restore

xbuild /p:Configuration=ReleaseMono linq2db.Mono.sln

# Ideally we would use the 'dotnet test' command to test netcoreapp and net451 so restrict for now 
# but this currently doesn't work due to https://github.com/dotnet/cli/issues/3073 so restrict to netcoreapp

dotnet build ./Source/project.json -c Release -f netstandard1.6

dotnet test ./Tests/Linq/project.json -c Release -f netcoreapp1.0 --where "cat != WindowsOnly"

# Instead, run directly with mono for the full .net version 
# dotnet build ./test/TEST_PROJECT_NAME -c Release -f net451

mono ./testrunner/NUnit.ConsoleRunner.3.5.0/tools/nunit3-console.exe Tests/Linq/bin/ReleaseMono/linq2db.Tests.dll --where "cat != WindowsOnly"

