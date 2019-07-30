This directory contains MSBUILD props files, used by solution projects and CI build scrips

## Props files

### All projects
- linq2db.Default.props - default props, used by all projects. Contains generic properties, that are used by all projects.

### Product projects
- linq2db.Source.props - props for `LinqToDB` and `LinqToDB.Tools` projects. Imports linq2db.Default.props

### Test Projects
- linq2db.Tests.props - props, used by test projects. Imports linq2db.Default.props
- linq2db.Tests.Providers.props - references to database providers, used by tests, and some other shared properties. Imports linq2db.Tests.props.

## CI Scripts

### SetVersion.ps1
This script updates assembly `Version` property in `linq2db.Default.props` file using provided version number.

Usage:
```
SetVersion.ps1 -path <PATH_TO_PROPS_FILE> -version <VERSION>
```

### BuildNuspecs.ps1
This script update nuspecs with missing properties like:
- versions
- copyright strings
- repository details
- etc

Usage:
```
BuildNuspecs.ps1 -path <NUSPEC_SEARCH_MASK> -version <NUGET_VERSION>
```
