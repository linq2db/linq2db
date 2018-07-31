---
uid: contributing
---
# Contributing guide

## Development rules and regulations, code style

Follow this [document](https://github.com/linq2db/linq2db/files/1056002/Development.rules.and.regulations.docx)

## Project structure description

Solution and folder structure

| Folder                     | Description                                     |
|--------------------------- |-------------------------------------------------|
|.\                          | Root folder |
|.\Build                     | Various files for AppVeyor builds and common project settings |
|.\Data                      | Contains test databases creation scripts and database files |
|.\Doc                       | DocFX documentation files |
|.\NuGet                     | LINQ to DB NuGet packages build files, readme.txt |
|.\Redist                    | Redistributable binaries for providers unavailable officially at NuGet |
|.\Source\LinqToDB           | LINQ to DB source code |
|.\Source\LinqToDB.Templates | LINQ to DB t4models source code |
|.\Tests                     | Unit tests |
|.\Tests\Base                | LINQ to DB testing framework |
|.\Tests\FSharp              | F# models and tests |
|.\Tests\IBM.Core            | Tests for IBM.Data.DB2.Core provider |
|.\Tests\Linq                | Main project for LINQ to DB unit tests |
|.\Tests\Model               | Model classes for tests |
|.\Tests\T4.Linq             | Models for test databases, generated using t4models |
|.\Tests\T4.Model            | T4Models tests |
|.\Tests\T4.Wpf              | T4Models NotifyPropertyChanged template test project |
|.\Tests\TestApp             | SQL Server spatial types test application |
|.\Tests\Tests.Benchmark     | Benchmark tests |
|.\Tests\VisualBasic         | Visual Basic models and tests |

Solutions:

* `.\linq2db.sln` - VS2017 solution

Projects:

| Project                                          |.NET 4.5 | .NET 4.5.2 | .NET 4.6 | .NET 4.6.2 | .NET Standard 1.6 | .NET Standard 2.0 | .NET Core 1.0 | .NET Core 2.0 |
|-------------------------------------------------:|:-------:|:----------:|:--------:|:----------:|:-----------------:|:-----------------:|:-------------:|:-------------:|
| `.\Source\LinqToDB\LinqToDB.csproj`              |    √    |            |          |            |         √         |         √         |               |       √       |
| `.\Tests\Linq\Tests.Base.csproj`                 |         |     √      |          |            |                   |                   |       √       |       √       |
| `.\Tests\IBM.Core\Tests.IBM.Core.csproj`         |         |            |          |     √      |                   |                   |               |       √       |
| `.\Tests\Linq\Tests.csproj`                      |         |            |    √     |            |                   |                   |       √       |       √       |
| `.\Tests\FSharp\Tests.FSharp.fsproj`             |         |     √      |          |            |                   |                   |       √       |       √       |
| `.\Tests\Model\Tests.Model.csproj`               |    √    |            |          |            |         √         |                   |               |               |
| `.\Tests\T4.Linq\Tests.T4.Linq.csproj`           |         |            |    √     |            |                   |                   |               |       √       |
| `.\Tests\T4.Model\Tests.T4.Model.csproj`         |    √    |            |          |            |                   |                   |               |               |
| `.\Tests\T4.Wpf\Tests.T4.Wpf.csproj`             |         |     √      |          |            |                   |                   |               |               |
| `.\Tests\TestApp\TestApp.csproj`                 |         |     √      |          |            |                   |                   |               |               |
| `.\Tests\Tests.Benchmark\Tests.Benchmark.csproj` |         |            |    √     |            |                   |                   |               |               |
| `.\Tests\VisualBasic\Tests.VisualBasic.vbproj`   |    √    |            |          |            |         √         |                   |               |               |


## Building

You can use the solution to build and run tests. Also you can build whole solution or library using the following batch files:

* run `.\Build.cmd` - builds all the projects in the solution for Debug, Release, and AppVeyor configurations
* run `.\Source\LinqToDB\Compile.cmd` - builds LinqToDB projects for Debug and Release configurations

### Different platforms support

Because of compiling for different platforms we do use:

* Conditional compilation. Different projects and configurations define compilation symbols:
  * NET45 - .NET 4.5 compatibility level
  * NETSTANDARD1_6 - .NET Standard 1.6 compatibility level
  * NETSTANDARD2_0 - .NET Standard 2.0 compatibility level
* Implementing missing classes and enums. There are some under `.\Source\LinqToDB\Compatibility` folder.

## Branches

* `master` - current stable branch
* `release` - branch with the latest release
* `release1` - branch for critical fixes for version 1.xx.yy
* `version1` - stable branch for version 1.xx.yy

## Run tests

NUnit3 is used as unit testing framework. Most tests are run for all supported databases, and written in same pattern:

```cs
[TestFixture]
public class Test: TestBase // TestBase - base class, provides base methods and object data sources
{
    // DataContextSourceAttribute - implements NUnit ITestBuilder and provides context values to test
    // TestAttribute - not required for nunit test runner, but needed for Resharper test runner
    [Test, DataContextSource]
    public void Test(string context)
    {
        // TestBase.GetDataContext - creates new IDataContext, supports creating WCF client and server
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

`DataContextSourceAttribute` generates tests for each configured data provider, configuration is taken
from `.\Tests\Linq\DataProviders.json` and `.\Tests\Linq\UserDataProviders.json` if it exists.
`Linq\UserDataProviders.json` is used to specify user-specific settings such as connections strings to test databases and
list of tested providers.

The `[User]DataProviders.json` is a regular JSON file:

#### UserDataProviders.json example (with description)

```js
{
    // .net framework 4.5 test configuration
    "NET45" :
    {
        // base configuration to inherit settings from
        // Inheritance rules:
        // - DefaultConfiguration, TraceLevel, Providers - use value from base configuration only if it is not defined in current configuration
        // - Connections - merge current and base connection strings
        "BasedOn"              : "LocalConnectionStrings",
								
        // default provider, used as a source of reference data
        // LINQ to DB uses SQLite for it and you hardly need to change it
        "DefaultConfiguration" : "SQLite.Classic",
								
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
            "SqlServer",
            "SqlServer.2014",
            "SqlServer.2012", "SqlServer.2012.1",
            "SqlServer.2008", "SqlServer.2008.1",
            "SqlServer.2005", "SqlServer.2005.1",
            "SqlAzure.2012",
            "DB2",
            "Firebird",
            "Informix",
            "MySql",
            "MariaDB",
            "Oracle.Native",
            "Oracle.Managed",
            "PostgreSQL",
            "Sybase",
            "Northwind",
            "TestNoopProvider"
        ],

        // list of test skip categories, disabled for current test configuration
	// to set test skip category, use SkipCategoryAttribute on test method, class or whole assembly
        "Skip"                 :
	[
	    "Access.12"
	]

    },

    // .net core 1.0 test configuration
    "CORE1" :
    {
        "BasedOn"              : "LocalConnectionStrings",
        "Providers"            :
        [
            "SQLite.MS",
            "Northwind.SQLite.MS",
            "SqlServer",
            "SqlServer.2014",
            "SqlServer.2012", "SqlServer.2012.1",
            "SqlServer.2008", "SqlServer.2008.1",
            "SqlServer.2005", "SqlServer.2005.1",
            "SqlAzure.2012",
            "Firebird",
            "MySql",
            "MariaDB",
            "PostgreSQL",
            "Northwind",
            "TestNoopProvider"
        ]
    },

    // .net core 2.0 test configuration
    "CORE2" :
    {
        "BasedOn"              : "LocalConnectionStrings",
        "Providers"            :
        [
            "SQLite.MS",
            "Northwind.SQLite.MS",
            "SqlServer",
            "SqlServer.2014",
            "SqlServer.2012", "SqlServer.2012.1",
            "SqlServer.2008", "SqlServer.2008.1",
            "SqlServer.2005", "SqlServer.2005.1",
            "SqlAzure.2012",
            "Firebird",
            "MySql",
            "MariaDB",
            "PostgreSQL",
            "Northwind",
            "TestNoopProvider"
        ]
    },

    // list of connection strings for all providers
    "LocalConnectionStrings":
    {
        "BasedOn"           : "CommonConnectionStrings",
        "Connections"       :
        {
            // override connection string for SqlAzure.2012 provider
            // all other providers will use default inherited connection strings from CommonConnectionStrings configuration
            "SqlAzure.2012" :
            {
                 "Provider"         : "System.Data.SqlClient",
                 "ConnectionString" : "Server=tcp:xxxxxxxxx.database.windows.net,1433;Database=TestData;User ID=TestUser@zzzzzzzzz;Password=TestPassword;Trusted_Connection=False;Encrypt=True;"
            }
        }
    }
}

```

To define your own configurations **DO NOT EDIT** `DataProviders.json` - create `.\Tests\Linq\UserDataProviders.json` and define needed configurations.

Right now tests execution depends on `_CreateData.*` tests executed first. Those tests recreate test databases and populate them with test data, so if you are going to run one test be sure to run `_CreateData` before it manually.

Also - if your test changes database data, be sure to revert those changes (!) to avoid side effects for other tests.

## Continuous Integration

We do run builds and tests with:

* [AppVeyor](https://ci.appveyor.com/project/igor-tkachev/linq2db) (Windows) [appveyor.yml](https://github.com/linq2db/linq2db/blob/master/appveyor.yml). Makes build and runs tests for:
  * .Net 4.5.2: [NET45.AppVeyor configuration](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/DataProviders.json). Full set of tests are done.
  * .Net Core 1.0: [CORE1.AppVeyor configuration](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/DataProviders.json). Only `_Create` tests are done (smoke testing).
  * .Net Core 2.0: [CORE2.AppVeyor configuration](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/DataProviders.json). Only `_Create` tests are done (smoke testing).
  * DocFx - to build [documentation](https://linq2db.github.io). Deploy is done only for `release` branch.
* [Travis](https://travis-ci.org/linq2db/linq2db) (Linux) [.travis.yml](https://github.com/linq2db/linq2db/blob/master/.travis.yml). Makes build and runs tests for:
  * .Net Core 2.0: [CORE2.Travis configuration](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/DataProviders.json). Full set of tests are done.

CI builds are done only for next branches:

* `master`
* `/version.*/` (regex)
* `/release.*/` (regex)
* `/dev.*/` (regex)

### Skip CI build

If you want to skip building commit by CI (for example you have changed *.md files only) begin commit comment with `[ci skip]`.

### Publishing packages

* **Release candidate** packages are published by AppVeyor to [MyGet.org](https://github.com/linq2db/linq2db#feeds) for each successful build of **master** branch.
* **Release** packages are published by AppVeyor to [NuGet.org](https://github.com/linq2db/linq2db#feeds) for each successful build of **release** and **release1** branch.

## Building releases

1. Update [Release Notes](https://github.com/linq2db/linq2db/wiki/Releases-and-Roadmap) and create empty entry for vNext release
1. Create PR from `master` to `release` branch, in comments add [@testers](https://github.com/linq2db/linq2db/wiki/How-can-i-help#testing-how-to) to notify all testers that we are ready to release
1. Wait few days for feedback from testers and approval from contributors
1. Merge PR
1. [Tag release](https://github.com/linq2db/linq2db/releases)
1. Update versions in `master` branch (this will lead to publish all next `master` builds as new version RC):
   * in [.\appveyor.yml](https://github.com/linq2db/linq2db/blob/master/appveyor.yml) set `assemblyVersion` parameter
   * in *.nuspec files update linq2db dependency version
   * in issue template update default linq2db version

## Process

In general you should follow simple rules:

* Development rules and regulations, code style
* Do not add new features without tests
* Avoid direct pushes to `master` and `release` branches
* To fix some issue or implement new feature create new branch and make pull request after you are ready to merge. Merge your PR only after contributor's review.
* If you are going to implement any big feature you may want other contributors to participate (coding, code review, feature discuss and so on), so to do it:
  * Create new PR with **[WIP]** prefix (Work In Process)
  * After you are ready to merge remove the prefix & assign contributors as reviewers
* If you do have write access, it is recommended to use central repository (not forks). Why - simple, it would allow other teammates to help you in developing (if needed). Certainly you are free to use fork if it is more convenient to you
* Please avoid adding new public classes, properties, methods without XML doc
* Read issues and help users
* Do not EF :)
