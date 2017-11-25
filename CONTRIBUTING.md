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

# Building
You can use any solution for building and running tests. But you also should care about other supported platforms. To check if your changes have not affected other projects you can:
* run `.\Source\compile.cmd` - builds .Net 4, .Net 4.5, .Net WS
* run `dotnet build` - in Root folder to build .Net Core 
## Different platforms support
Because of compiling for different platforms we do use:
* Conditional compilation. Different projects and configurations define compilation symbols:
  * FW4 - .Net 4.0 compatibility level
  * SILVERLIGHT - Silverlight compatibility level
  * NETFX_CORE - Windows Store 8 compatibility level
  * NOASYNC - async is not supported 
  * NETSTANDARD - .Net Core (netstandard1.6) compatibility level
  * NOFSHARP - used by .Net Core test project to avoid compiling code dependent on F#
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
`DataContextSourceAttribute` generates tests for each configured data provider, configuration is taken: 
1. From `.\Tests\Linq\UserDataProviders.txt` (if any) (UserDataProviders.Core.txt for .Net Core)
1. From `.\Tests\Linq\DefaultDataProviders.txt` (DefaultDataProviders.Core.txt for .Net Core)
All default connection strings are stored in `.\Tests\Linq\app.config` 
`DataProviders.txt` structure is:
`[!]ConfigurationName[* ConnectionString][* DataProviderName]`
* `[]` - optional parts
* `*` - is used as field delimiter
* `--` - is used to comment line
Parts:
* `!` - means that this configuration is used as `DataConnection.DefaultConfiguration`
* `ConfigurationName` - configuration name (passed to test as `context` parameter)
* ConnectionString - used to override default connection string from `app.config`
* DataProviderName - used to define DataProvider for configuration

**UserDataProviders.txt example**
```
-- this is comment line and would be ignored
Access
!SQLiteMs       * Data Source=Database\TestData.sqlite      * SQLite
```
this does mean:
* Run tests for `Access` configuration with default settings
* Run tests for `SQLiteMs` configuration. This configuration is used as default, with connection string `Data Source=Database\TestData.sqlite` and `SQLite` data provider.

More examples are below in CI section.

So tests are done only for providers defined in `DataProviders.txt`, defaults are in `DefaultDataProviders.txt` (they are Access, SQL CE, SQLite - all file server databases). To define your own configurations **DO NOT EDIT** `DefaultDataProviders.txt` - create `.\Tests\Linq\UserDataProviders.txt` and define needed configurations. 

When all tests are executed, first `_CreateData` tests will be run - those execute SQL scripts and insert default data to database, so if you are going to run one test be sure to run `_CreateData` before it manually.

Also - if your test changes database data, be sure to revert those changes (!) to avoid side effects for other tests.

# Continuous Integration
We do run builds and tests with:
* [AppVeyor](https://ci.appveyor.com/project/igor-tkachev/linq2db) (Windows) [appveyor.yml](https://github.com/linq2db/linq2db/blob/master/appveyor.yml). Makes build and runs tests for:
  * .Net 4.5: [AppveyorDataProviders.txt](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/AppveyorDataProviders.txt)
  * .Net Core: [AppveyorDataProviders.Core.txt](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/AppveyorDataProviders.Core.txt)
* [Travis](https://travis-ci.org/linq2db/linq2db) (Linux) [.travis.yml](https://github.com/linq2db/linq2db/blob/master/.travis.yml). Makes build and runs tests for:
  * Mono: [TravisDataProviders.txt](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/TravisDataProviders.txt)
  * .Net Core: [TravisDataProviders.Core.txt](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/TravisDataProviders.Core.txt)

`xxxDataproviders` files are renamed by CI to `UserDataProviders` before running tests.

## Skip CI build
If you want to skip building commit by CI (for example you have changed *.md files only) begin commit comment with `[ci skip]`.

## Publishing packages
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

# Process
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

 
