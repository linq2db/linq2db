---
uid: contributing
---
# Contributing guide

## Project structure

### Solution and folder structure

|Folder|Description|
|-|-|
|.\Build|Build and CI files, check readme.md in that folder|
|.\Data|Databases and database creation scripts for tests|
|.\NuGet|LINQ to DB NuGet packages build files|
|.\Redist| Binaries, unavailable officially at NuGet, used by tests and nugets|
|.\Source\LinqToDB| LINQ to DB source code|
|.\Source\LinqToDB.AspNet| LINQ to DB ASP.NET Core integration library source code|
|.\Source\LinqToDB.CLI| LINQ to DB CLI scaffold tool source code|
|.\Source\LinqToDB.Remote.Grpc| LINQ to DB Remote Context GRPC client/server source code|
|.\Source\LinqToDB.Remote.Wcf| LINQ to DB Remote Context WCF client/server source code|
|.\Source\LinqToDB.Templates| LINQ to DB t4models source code|
|.\Source\LinqToDB.Tools| LINQ to DB Tools source code|
|.\Tests| Unit test projects folder|
|.\Tests\Base|LINQ to DB testing framework|
|.\Tests\FSharp|F# models and tests|
|.\Tests\Linq|Main project for LINQ to DB unit tests|
|.\Tests\Model|Model classes for tests|
|.\Tests\Tests.T4|T4 templates test project|
|.\Tests\Tests.Benchmarks| Benchmarks|
|.\Tests\Tests.PLayground| Test project for use with linq2db.playground.sln lite test solution<br>Used for work on specific test without full solution load|
|.\Tests\VisualBasic|Visual Basic models and tests support|

#### Solutions

* `.\linq2db.sln` - full linq2db solution
* `.\linq2db.playground.slnf` - ligthweight linq2db test solution. Used to work on specific test without loading of all payload of full solution
* `.\linq2db.Benchmarks.slnf` - ligthweight linq2db benchmarks solution. Used to work on benchmarks without loading of all payload of full solution

#### Source projects

Preferred target defines:

- `NETFRAMEWORK` - `net45`, `net46`, `net462`, and `net472` target ifdef
- `NETSTANDARD2_1PLUS` - targets with `netstandard2.1` support (`netstandard2.1`, `netcoreapp3.1`, `net6.0`, `net7.0`, `net8.0`). Don't use this define in test projects!
- `NATIVE_ASYNC` - ifdef with native support for `ValueTask`, `IAsyncEnumerable<T>` and `IAsyncDisposable` types

Other allowed target defines:

- `NETSTANDARD2_1` - `netstandard2.1` target ifdef
- `NETCOREAPP3_1` - `netcoreapp3.1` target ifdef
- `NETSTANDARD2_0` - `netstandard2.0` target ifdef
- `NET6_0` - `net6.0` target ifdef
- `NET7_0` - `net7.0` target ifdef
- `NET8_0` - `net8.0` target ifdef
- `NET45` - `net45` target ifdef
- `NET46` - `net46` target ifdef
- `NET462` - `net462` target ifdef
- `NET472` - `net472` target ifdef

Allowed debugging defines:

- `TRACK_BUILD` - ???
- `DEBUG` - for debug code in debug build. To disable debug code use `DEBUG1` rename
- `OVERRIDETOSTRING` - enables `ToString()` overrides for AST model (must be enabled in LinqToDB.csproj by renaming existing `OVERRIDETOSTRING1` define)

#### Test projects

Tests targets: `net472`, `netcoreapp31`, `net6.0`, `net7.0`

Allowed target defines:

- `NETCOREAPP3_1` - `netcoreapp3.1` target ifdef
- `NET6_0` - `net6.0` target ifdef
- `NET7_0` - `net7.0` target ifdef
- `NET472` - `net472` target ifdef
- `AZURE` - for Azure Pipelines CI builds

## Build

You can use solution to build and run tests. Also you can build whole solution or library using the following batch files:

* `.\Build.cmd` - builds all the projects in the solution for Debug, Release and Azure configurations
* `.\Compile.cmd` - builds LinqToDB project for Debug and Release configurations
* `.\Clean.cmd` - cleanups solution projects for Debug, Release and Azure configurations
* `.\Test.cmd` - build `Debug` configuration and run tests for `net472`, `netcoreapp3.1`, `net6.0` and `net7.0` targets. You can set other configuration by passing it as first parameter, disable test targets by passing 0 to second (for `net472`),  third (for `netcoreapp3.1`), fourth (for `net6.0`) or fifth (for `net7.0`) parameter and format (default:html) as 7th parameter.

Example of running `Release` build tests for `netcoreapp3.1` only with trx as output:

```
test.cmd Release 0 1 0 0 0 trx
```

### Different platforms support

Because we target different TFMs, we use:

* Conditional compilation. See supported defines above
* Implementation of missing runtime functionality (usually copied from `dotnet/runtime` repository). Should be placed to `Source\Shared` folder with proper TFM `#ifdef`s (it will be picked by all projects automatically) and made explicitly internal to avoid conflicts.

## Branches

* `master` - main development branch, contains code for next release
* `release` - branch with the latest released version

## Run tests

NUnit3 is used as unit-testing framework. Most of tests are run for all supported databases and written in same pattern:

```cs
// TestBase - base class for all our tests
// provides required testing infrastructure
[TestFixture]
public class Test: TestBase
{
    // DataSourcesAttribute - custom attribute to feed test with enabled database configurations
    [Test]
    public void Test([DataSources] string context)
    {
        // TestBase.GetDataContext - creates new IDataContext
        // also supports creation of remote client and server
        // for remote contexts
        using(var db = GetDataContext(context))
        {
            // Here is the most interesting
            // this.Person - list of persons, corresponding Person table in database (derived from TestBase)
            // db.Person - database table
            // So test checks that LINQ to Objects query produces the same result as executed database query
            AreEqual(this.Person.Where(_ => _.Name == "John"), db.Person.Where(_ => _.Name == "John"));
        }
    }

}
```

### Configure data providers for tests

`DataSourcesAttribute` generates tests for each enabled data provider. Configuration is taken
from `.\Tests\DataProviders.json` and `.\Tests\UserDataProviders.json` (used first, if exists).

Repository already contains pre-configured `UserDataProviders.json.template` configuration with basic setup for SQLite-based testing and all you need is to rename it to `UserDataProviders.json`, add connection string for other databases you want to test.
`UserDataProviders.json` will be ignored by git, so you can edit it freely.

Configuration file is used to specify user-specific settings such as connection strings to test databases and
list of providers to test.

The `[User]DataProviders.json` is a regular JSON file:

#### UserDataProviders.json example (with description)

```js
{
    // .net framework 4.7.2 test configuration
    "NET472" :
    {
        // base configuration to inherit settings from
        // Inheritance rules:
        // - DefaultConfiguration, TraceLevel, Providers - use value from base configuration only if it is not defined in current configuration
        // - Connections - merge current and base connection strings
        "BasedOn"              : "LocalConnectionStrings",

        // default provider, used as a source of reference data
        // LINQ to DB uses SQLite for it and you hardly need to change it
        "DefaultConfiguration" : "SQLite.Classic",

        // (optional) contains full or relative (from test assembly location) path to test baselines directory.
        // When path is set and specified directory exists - enables baselines generation for tests.
        "BaselinesPath": "c:\\github\\linq2db.baselines",

        // logging level
        // Supported values: Off, Error, Warning, Info, Verbose
        // Default level: Info
        "TraceLevel"           : "Error",
                                
        // list of database providers, enabled for current test configuration
        "Providers"            :
        [
            "Access",
            "SqlCe",
            "SQLite.Classic",
            "SQLite.MS",
            "Northwind.SQLite",
            "Northwind.SQLite.MS",
            "SqlServer.2014",
            "SqlServer.2012",
            "SqlServer.2008",
            "SqlServer.2005",
            "SqlServer.Azure",
            "DB2",
            "Firebird",
            "Informix",
            "MySql",
            "MariaDB",
            "Oracle.Native",
            "Oracle.Managed",
            "PostgreSQL",
            "Sybase.Managed",
            "SqlServer.Northwind",
            "TestNoopProvider"
        ],

        // list of test skip categories, disabled for current test configuration
    // to set test skip category, use SkipCategoryAttribute on test method, class or whole assembly
        "Skip"                 :
    [
        "Access.12"
    ]

    },

    // .net 6.0 test configuration
    "NET60" :
    {
        "BasedOn"              : "LocalConnectionStrings",
        "Providers"            :
        [
            "SQLite.MS",
            "Northwind.SQLite.MS",
            "SqlServer.2014",
            "SqlServer.2012",
            "SqlServer.2008",
            "SqlServer.2005",
            "SqlServer.Azure",
            "Firebird",
            "MySql",
            "MariaDB",
            "PostgreSQL",
            "SqlServer.Northwind",
            "TestNoopProvider"
        ]
    },

    // .net 7.0 test configuration
    "NET70" :
    {
        "BasedOn"              : "LocalConnectionStrings",
        "Providers"            :
        [
            "SQLite.MS",
            "Northwind.SQLite.MS",
            "SqlServer.2014",
            "SqlServer.2012",
            "SqlServer.2008",
            "SqlServer.2005",
            "SqlServer.Azure",
            "Firebird",
            "MySql",
            "MariaDB",
            "PostgreSQL",
            "SqlServer.Northwind",
            "TestNoopProvider"
        ]
    },

    // list of connection strings for all providers
    "LocalConnectionStrings":
    {
        "BasedOn"           : "CommonConnectionStrings",
        "Connections"       :
        {
            // override connection string for SqlAzure provider
            // all other providers will use default inherited connection strings from CommonConnectionStrings configuration
            "SqlServer.Azure" :
            {
                 "Provider"         : "System.Data.SqlClient",
                 "ConnectionString" : "Server=tcp:xxxxxxxxx.database.windows.net,1433;Database=TestData;User ID=TestUser@zzzzzzzzz;Password=TestPassword;Trusted_Connection=False;Encrypt=True;"
            }
        }
    }
}
```

To define your own configurations **DO NOT EDIT** `DataProviders.json` - create `.\Tests\Linq\UserDataProviders.json` and define needed configurations.

Tests execution depends on `_CreateData.*` tests executed first. Those tests recreate test databases and populate them with test data, so if you are going to run one test be sure to run `_CreateData` before it manually.

Also - if your test changes database data, be sure to revert those changes (!) to avoid side effects for other tests.

## Continuous Integration

We do run builds and tests with:

* [Azure Pipelines](https://dev.azure.com/linq2db/linq2db/_build?definitionId=3) [pipelines/default.yml](https://github.com/linq2db/linq2db/blob/master/Build/Azure/pipelines/default.yml).
It builds solution, generate and publish nugets and runs tests for:
  * .Net 4.7.2
  * .Net Core 3.1 (Windows, Linux and MacOS)
  * .Net 6.0 (Windows, Linux and MacOS)
  * .Net 7.0 (Windows, Linux and MacOS)
For more details check [readme](https://github.com/linq2db/linq2db/blob/master/Build/Azure/README.md)

CI builds are done for all branches and PRs.

* Tests run for all branches and PRs except `release` branch
* Nugets publishing to [Azure feeds](https://dev.azure.com/linq2db/linq2db/_packaging?_a=feed&feed=linq2db) enabled only for `branch`
* Nugets publishing to [Nuget.org](https://www.nuget.org/profiles/LinqToDB) enabled only for `release` branch

### Publishing packages

* **"Nightly" builds** packages are published to [Azure feeds](https://dev.azure.com/linq2db/linq2db/_packaging?_a=feed&feed=linq2db) for each successful build of **master** branch.
* **Release** packages are published to [Nuget.org](https://www.nuget.org/profiles/LinqToDB) for each successful build of **release** branch.

## Building releases

1. Update [Release Notes](https://github.com/linq2db/linq2db/wiki/Releases-and-Roadmap) and create empty entry for vNext release
1. Create PR from `master` to `release` branch, in comments add [@testers](https://github.com/linq2db/linq2db/wiki/How-can-i-help#testing-how-to) to notify all testers that we are ready to release
1. Wait few days for feedback from testers and approval from contributors
1. Merge PR
1. [Tag release](https://github.com/linq2db/linq2db/releases) on `master` branch
1. Update versions in `master` branch (this will lead to publish all next `master` builds as new version RC):
   * in [\Build\Azure\pipelines\templates\build-vars.yml](https://github.com/linq2db/linq2db/blob/master/Build/Azure/pipelines/templates/build-vars.yml) set `assemblyVersion` and `packageVersion` (for release and development) parameters to next version. Always use next minor version and change it to major only before release, if it should be new major version release

## Process

In general you should follow simple rules:

* Code style should match exising code
* There should be no compilation warnings (will fail CI builds)
* Do not add new features without tests
* Avoid direct pushes to `master` and `release` branches
* To fix some issue or implement new feature create new branch and make pull request after you are ready to merge or create pull request as `work-in-progress` pull request. Merge your PR only after contributors' review.
* bugfix branches must use `issue/<issue_id>` naming format
* feature branches must use `feature/<issue_id_or_feature_name>` naming format
* If you do have repository write access, it is recommended to use central repository instead of fork
* Do not add new public classes, properties, methods without XML documentation on them
* Read issues and help users
* Do not EF :)
