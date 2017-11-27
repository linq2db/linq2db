---
uid: contributing
---
# Development rules and regulations, code style
Follow this [document](https://github.com/linq2db/linq2db/files/1056002/Development.rules.and.regulations.docx)

# Project structure description

## linq2db

Solution and folder structure

```
.\ //Root folder
.\Data // Contains fileserver databases and scripts for creating and initializing test databases
.\NuGet // Stuff for nuget package, readme.txt
.\Redist // Redistributable binaries unavailable in nugget
.\Source // Source
.\Tests // Unit tests stuff
.\Tests\FSharp // F# models and tests
.\Tests\Linq // All unit tests
.\Tests\Model // Models for tests
.\Tests\TestApp // Test application
.\Tests\Utils // Test helper and utilities application
.\Tests\VisualBasic //Visual Basic models and tests
```

Solutions:

* `.\linq2db.sln` - VS2017 solution

Projects:

| Project                                        | .NET 4.0 | 4.5 | 4.51 | 4.52 | .NET Standard 1.6 | 2.0 | .NET Core 1.0 | 2.0 |
|----------------------------------------------- |:--------:|:---:|:----:|:----:|:-----------------:|:---:|:-------------:|:---:|
| `.\Source\LinqToDB\LinqToDB.csproj`            |    √     |  √  |  √   |      |         √         |  √  |               |  √  |
| `.\Tests\Linq\Tests.csproj`                    |          |     |      |  √   |                   |     |       √       |  √  |
| `.\Tests\FSharp\Tests.FSharp.fsproj`           |          |     |      |  √   |                   |     |               |     |
| `.\Tests\Model\Tests.Model.csproj`             |          |  √  |      |      |         √         |     |               |     |
| `.\Tests\Utils\Tests.Utils.csproj`             |          |     |      |  √   |                   |     |       √       |  √  |
| `.\Tests\VisualBasic\Tests.VisualBasic.vbproj` |          |  √  |      |      |         √         |     |               |     |

## Building

You can use the solution for building and running tests. Also you can build te whole solution or library itself
using the following .cmd files:
* run `.\Build.cmd` - builds all the projects in the solution for Debug, Release, and AppVeyor configurations
* run `.\Source\LinqToDB\Compile.cmd` - builds LinqToDB projects for Debug and Release configurations

### Different platforms support

Because of compiling for different platforms we do use:

* Conditional compilation. Different projects and configurations define compilation symbols:
  * NET40 - .Net 4.0 compatibility level
  * NET45 - .Net 4.5 compatibility level
  * NETSTANDARD1_6 - .NET Standard 1.6 compatibility level
  * NETSTANDARD2_0 - .NET Standard 2.0 compatibility level
* Exclude files from build - some files are excluded from build in projects, corresponding to target framework
* Implementing missing classes and enums. There are some under `.\Source\Compatibility` folder.

## Run tests

NUnit3 is used as unit testing framework. Most tests are run for all supported databases, and written in same pattern:

```cs
[TestFixture]
public class Test: TestBase // TestBase - base class, provides base methods and object data sources
{
    // DataContextSourceAttribute - implements Nunit ITestBuilder and provides context values to test
    [DataContextSource]
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
from `.\Tests\Linq\DataProviders.json` and `.\Tests\Linq\UserDataProviders.json` if exists.
`Linq\UserDataProviders.json` is used to override local user settings such as connections strings, 
list of tested providers, base configuration, etc.

The `[User]DataProviders.json` is a regular JSON file:




**UserDataProviders.json example**
```
{
    "NET45" :
    {
        "Providers" :
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
        ]
    },

    "CORE1" :
    {
        "Providers" :
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

    "CORE2" :
    {
        "Providers" :
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

    "LocalConnectionStrings":
    {
        "BasedOn"     : "CommonConnectionStrings",
        "Connections" :
        {
            "SqlAzure.2012" :
            {
                 "Provider"         : "System.Data.SqlClient",
                 "ConnectionString" : "Server=tcp:xxxxxxxxx.database.windows.net,1433;Database=TestData;User ID=TestUser@zzzzzzzzz;Password=TestPassword;Trusted_Connection=False;Encrypt=True;"
            }
        }
    }
}

```

this does mean:

* Run tests for `Access` configuration with default settings
* Run tests for `SQLiteMs` configuration. This configuration is used as default, with connection string `Data Source=Database\TestData.sqlite` and `SQLite` data provider.

More examples are below in CI section.

So tests are done only for providers defined in `DataProviders.txt`, defaults are in `DefaultDataProviders.txt` (they are Access, SQL CE, SQLite - all file server databases). To define your own configurations **DO NOT EDIT** `DefaultDataProviders.txt` - create `.\Tests\Linq\UserDataProviders.txt` and define needed configurations. 

When all tests are executed, first `_CreateData` tests will be run - those execute SQL scripts and insert default data to database, so if you are going to run one test be sure to run `_CreateData` before it manually.

Also - if your test changes database data, be sure to revert those changes (!) to avoid side effects for other tests.

## Continuous Integration

We do run builds and tests with:

* [AppVeyor](https://ci.appveyor.com/project/igor-tkachev/linq2db) (Windows) [appveyor.yml](https://github.com/linq2db/linq2db/blob/master/appveyor.yml). Makes build and runs tests for:
  * .Net 4.5: [AppveyorDataProviders.txt](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/AppveyorDataProviders.txt)
  * .Net Core: [AppveyorDataProviders.Core.txt](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/AppveyorDataProviders.Core.txt)
* [Travis](https://travis-ci.org/linq2db/linq2db) (Linux) [.travis.yml](https://github.com/linq2db/linq2db/blob/master/.travis.yml). Makes build and runs tests for:
  * Mono: [TravisDataProviders.txt](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/TravisDataProviders.txt)
  * .Net Core: [TravisDataProviders.Core.txt](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/TravisDataProviders.Core.txt)

`xxxDataproviders` files are renamed by CI to `UserDataProviders` before running tests.

### Skip CI build

If you want to skip building commit by CI (for example you have changed *.md files only) begin commit comment with `[ci skip]`.

### Publishing packages

* **Release candidate** packages are published by AppVeyor to [MyGet.org](https://github.com/linq2db/linq2db#feeds) for each successful build of **master** branch. 
* **Release** packages are published by AppVeyor to [NuGet.org](https://github.com/linq2db/linq2db#feeds) for each successful build of **release** branch. 

## Building releases

1. Update `.\NuGet\Readme.txt` file (append release notes)
1. Create PR from `master` to `release` branch, in comments add [@testers](https://github.com/linq2db/linq2db/wiki/How-can-i-help#testing-how-to) to notify all testers that we are ready to release
1. Wait few days for feedback from testers and approval from contributors
1. Merge PR
1. [Tag release](https://github.com/linq2db/linq2db/releases)
1. Update versions in `master` branch (this will lead to publish all next `master` builds as new version RC):
   * in [.\appveyor.yml](https://github.com/linq2db/linq2db/blob/master/appveyor.yml) set `packageVersion` parameter
   * in [.\Source\project.json](https://github.com/linq2db/linq2db/blob/master/Source/project.json) set new `version` parameter
   * in [.\Source\Properties\LinqToDBConstants.cs](https://github.com/linq2db/linq2db/blob/master/Source/Properties/LinqToDBConstants.cs) set `FullVersionString` constant.

## Process

In general you should follow simple rules:

* Development rules and regulations, code style
* Do not add new features without tests
* Avoid direct pushes to `master` and `release` branches
* To fix some issue or implement new feature create new branch and make pull request after you are ready to merge. Merge your PR only after contributor's review.
* If you are going to implement any big feature you may want other contributors to participate (coding, code review, feature discuss and so on), so to do it:
  * Create new PR with **[WIP]** prefix (Work In Process)
  * After you are ready to merge remove the prefix & assign contributors as reviewers
* If you wo have wright access, it is recommended to use central repository (not forks). Why - simple, it would allow other teammates to help you in developing (if needed). Certainly you are free to use fork if it is more convenient to you
* Please avoid adding new public classes, properties, methods without XML doc
* Read issues and help users
* Do not EF :)
