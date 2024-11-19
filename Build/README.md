This directory contains files, used by local and CI builds.

#### `SetVersion.ps1`

This script updates assembly `Version` property in `linq2db.Default.props` file using provided version number and path to `props` file

VERSION_PROP should be one of:
- Version - version for majority of projects
- EF3Version - version for EF.Core 3.1 integration
- EF6Version - version for EF.Core 6 integration
- EF8Version - version for EF.Core 8 integration
- EF9Version - version for EF.Core 9 integration

Usage:

```ps
SetVersion.ps1 -path <PATH_TO_PROPS_FILE> -version <VERSION> -prop <VERSION_PROP>
```

#### `BuildNuspecs.ps1`

This script updates nuget generation `*.nuspec` files with common information:

- nuget/linq2db version
- copyright strings
- repository details
- etc

Usage:

```ps
BuildNuspecs.ps1 -path <NUSPEC_SEARCH_MASK> -version <NUGET_VERSION>[ -branch <BRANCH_NAME>]
```

#### `BannedSymbols.txt`

Contains information on banned from use in `LinqToDB` symbols (types and type members). Used by [this](https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md) analyzer during build in `Release` configuration (including CI builds)

#### `linq2db.snk`

`LinqToDB` assembly sign key for strong name generation.
