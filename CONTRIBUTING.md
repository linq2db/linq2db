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
|.\Source\CodeGenerators| LINQ to DB internal source generators source code|
|.\Source\LinqToDB| LINQ to DB source code|
|.\Source\LinqToDB.CLI| LINQ to DB CLI scaffold tool source code|
|.\Source\LinqToDB.Compat| LINQ to DB Compat library source code|
|.\Source\LinqToDB.EntityFrameworkCore| LINQ to DB integration with Entity Framework Core source code|
|.\Source\LinqToDB.Extensions| LINQ to DB Dependency Injection and Logging extensions library source code|
|.\Source\LinqToDB.FSharp | F# support extension for Linq To DB|
|.\Source\LinqToDB.LINQPad | LINQPad driver for Linq To DB|
|.\Source\LinqToDB.Remote.Grpc| LINQ to DB Remote Context GRPC client/server source code|
|.\Source\LinqToDB.Remote.HttpClient.Client| LINQ to DB Remote Context HttpClient client source code|
|.\Source\LinqToDB.Remote.HttpClient.Server| LINQ to DB Remote Context HttpClient server source code|
|.\Source\LinqToDB.Remote.SignalR.Client| LINQ to DB Remote Context Signal/R client source code|
|.\Source\LinqToDB.Remote.SignalR.Server| LINQ to DB Remote Context Signal/R server source code|
|.\Source\LinqToDB.Remote.Wcf| LINQ to DB Remote Context WCF client/server source code|
|.\Source\LinqToDB.Scaffold| LINQ to DB scaffold framework for cli and T4|
|.\Source\LinqToDB.Templates| LINQ to DB t4models source code|
|.\Source\LinqToDB.Tools| LINQ to DB Tools source code|
|.\Tests| Unit test projects folder|
|.\Tests\Base|LINQ to DB testing framework|
|.\Tests\EntityFrameworkCore|LINQ to DB EF.Core integration tests|
|.\Tests\EntityFrameworkCore.FSharp|LINQ to DB EF.Core F# integration tests|
|.\Tests\FSharp|F# models and tests|
|.\Tests\Linq|Main project for LINQ to DB unit tests|
|.\Tests\Model|Model classes for tests|
|.\Tests\Tests.Benchmarks| Benchmarks|
|.\Tests\Tests.PLayground| Test project for use with linq2db.playground.slnf lite test solution. Used for work on specific test without full solution load|
|.\Tests\Tests.T4|T4 templates test project|
|.\Tests\Tests.T4.Nugets|T4 nugets test project|
|.\Tests\VisualBasic|Visual Basic models and tests support|

#### Solutions

* `.\linq2db.slnx` - full linq2db solution
* `.\linq2db.playground.slnf` - ligthweight linq2db test solution. Used to work on specific test without loading of all payload of full solution
* `.\linq2db.Benchmarks.slnf` - ligthweight linq2db benchmarks solution. Used to work on benchmarks without loading of all payload of full solution
* `.\Tests\Tests.T4.Nugets\Tests.T4.Nugets.slnx` - separate solution for T4 nugets testing

#### Source projects

Custom feature symbols:

* `SUPPORTS_COMPOSITE_FORMAT`: for code that use `System.Text.CompositeFormat` class

Custom debugging symbols:

* `BUGCHECK` - enables extra bugchecks in debug and ci test builds

#### Test projects

Tests targets: `net462`, `net8.0`, `net9.0`, `net10.0`. In general we test 3 configurations: lowest supported .NET Framework, lowest supported .NET version, highest supported .NET version.

Custom symbols:

* `AZURE` - for Azure Pipelines CI builds

## Build

You can use solution to build and run tests. Also you can build whole solution or library using the following batch files:

* `.\Build.cmd` - builds all the projects in the solution for Debug, Release and Azure configurations
* `.\Compile.cmd` - builds LinqToDB project for Debug and Release configurations
* `.\Clean.cmd` - cleanups solution projects for Debug, Release and Azure configurations
* `.\Test.cmd` - builds and runs tests with `Debug` configuration for all supported TFMs and produce HTML report. Parameters supported to change build configuration, logger type and executed TFMs

Example of running `Release` build tests for `net9.0` only with `trx` as output:

```cmd
test.cmd Release 0 0 1 0 trx
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
        using var db = GetDataContext(context);

        // AssertQuery method will execute query against both DB and memory-based collections
        // and check that both return same results
        AssertQuery(db.Person.Where(_ => _.Name == "John"));
    }

}
```

### Configure data providers for tests

`DataSourcesAttribute` generates tests for each enabled data provider. Configuration is taken
from `.\DataProviders.json` and `.\UserDataProviders.json` (used first, if exists).

Repository already contains pre-configured `UserDataProviders.json.template` configuration with basic setup for SQLite-based testing and all you need is to rename it to `UserDataProviders.json`, add connection string for other databases you want to test.
`UserDataProviders.json` will be ignored by git, so you can edit it freely.

Configuration file is used to specify user-specific settings such as connection strings to test databases and
list of providers to test.

The `[User]DataProviders.json` is a regular JSON file:

#### UserDataProviders.json example (with description)

```js
{
    // .net framework 4.6.2 test configuration
    "NETFX" :
    {
        // base configuration to inherit settings from
        // Inheritance rules:
        // - DefaultConfiguration, TraceLevel, Providers - use value from base configuration only if it is not defined in current configuration
        // - Connections - merge current and base connection strings
        "BasedOn"              : "LocalConnectionStrings",

        // default provider, used as a source of reference data
        // LINQ to DB uses SQLite for it and you hardly need to change it
        "DefaultConfiguration" : "SQLite.MS",

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
            "Access.Ace.OleDb",
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
            "Firebird.5",
            "Informix",
            "MySql.8.0",
            "MariaDB.11",
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

    // .net 8.0 test configuration
    "NET80" :
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
            "Firebird.5",
            "MySql.8.0",
            "MariaDB.11",
            "PostgreSQL",
            "SqlServer.Northwind",
            "TestNoopProvider"
        ]
    },

    // .net 9.0 test configuration
    "NET90" :
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
            "Firebird.5",
            "MySql.8.0",
            "MariaDB.11",
            "PostgreSQL",
            "SqlServer.Northwind",
            "TestNoopProvider"
        ]
    },

    // .net 10.0 test configuration
    "NET100" :
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
            "Firebird.5",
            "MySql.8.0",
            "MariaDB.11",
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

To define your own configurations **DO NOT EDIT** `DataProviders.json` - create `.\UserDataProviders.json` and define needed configurations.

Tests execution depends on `_CreateData.*` tests executed first. Those tests recreate test databases and populate them with test data, so if you are going to run one test be sure to run `_CreateData` before it manually.

Also - if your test changes database data, be sure to revert those changes (!) to avoid side effects for other tests.

## Continuous Integration

We do run builds and tests with:

* [Azure Pipelines](https://dev.azure.com/linq2db/linq2db/_build?definitionId=3) [pipelines/default.yml](https://github.com/linq2db/linq2db/blob/master/Build/Azure/pipelines/default.yml).
It builds solution, generate and publish nugets and runs tests for:
  * .NET Framework 4.6.2
  * .NET 8 (Windows, Linux and MacOS)
  * .NET 9 (Windows, Linux and MacOS)
  * .NET 10 (Windows, Linux and MacOS)
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
   * in [\Directory.Build.props](https://github.com/linq2db/linq2db/blob/master/Directory.Build.props) set `Version` and `EFxVersion` properties to next version. Always use next minor version and change it to major only before release, if it should be new major version release

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

## Other Topics

### LINQPad Testing

To test LINQPad nuget you need to build it and add test nuget source to LINQPad, but because LINQPad expects driver to have tags, it cannot be file-based source.

1. Install local nuget server using [this guide](https://learn.microsoft.com/en-us/nuget/hosting-packages/nuget-server)
2. Build nugets using command `dotnet pack /p:Version=<version>` where version should be release version (e.g. `1.2.3`) as LINQPad ignores non-release versions
3. Push built nugets to your custom nuget server using this command `dotnet nuget push .build/nugets/*.nupkg -s https://<your local nuget server addres>/nuget --skip-duplicate`. If will produce 406 error for some unsupported nugets - you need to remove them and rerun command
4. add your feed (`https://<your local nuget server addres>/nuget`) to LINQPad
