This directory contains files, used by local and CI builds.

#### `SetVersion.ps1`

This script updates assembly `Version` property in `linq2db.Default.props` file using provided version number and path to `props` file

Usage:

```ps
SetVersion.ps1 -path <PATH_TO_PROPS_FILE> -version <VERSION>
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
