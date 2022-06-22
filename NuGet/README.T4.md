# LINQ to DB

[![License](https://img.shields.io/github/license/linq2db/linq2db)](MIT-LICENSE.txt)
[![Follow @linq2db](https://img.shields.io/twitter/follow/linq2db.svg)](https://twitter.com/linq2db)

> Starting from version 4 we obsolete T4 nugets in favor of new command-line scaffolding tool: [linq2db.cli](https://www.nuget.org/packages/linq2db.cli). Consider migration as we don't plan to introduce new features and fixes to T4 functionality.

This nuget package contains database scaffolding T4 templates to generate POCO classes from your database.

You can read about T4 templates options [here](https://linq2db.github.io/articles/T4.html). Demonstration video could be found [here](https://linq2db.github.io/articles/general/Video.html).

## Important

Don't use this package if you don't need database scaffolding functionality. Instead of this package, use:
1. main [linq2db](https://www.nuget.org/packages/linq2db) package
2. database provider nuget (see list of supported providers below)

### Supported database providers

- MS Access (.NET Core-only)
  - [System.Data.Odbc](https://www.nuget.org/packages/System.Data.Odbc) for ODBC Access provider
  - [System.Data.OleDb](https://www.nuget.org/packages/System.Data.OleDb) for OLE DB Access provider
- DB2 LUW and Informix
  - [IBM.Data.DB.Provider](https://www.nuget.org/packages/IBM.Data.DB.Provider): .NET Framework provider
  - [IBM.Data.DB2.Core](https://www.nuget.org/packages/IBM.Data.DB2.Core): .NET Core Windows provider
  - [IBM.Data.DB2.Core-lnx](https://www.nuget.org/packages/IBM.Data.DB2.Core-lnx): .NET Core Linux provider
  - [IBM.Data.DB2.Core-osx](https://www.nuget.org/packages/IBM.Data.DB2.Core-osx): .NET Core MacOS provider
  - [Net5.IBM.Data.Db2](https://www.nuget.org/packages/Net5.IBM.Data.Db2): .NET 5+ Windows provider
  - [Net5.IBM.Data.Db2-lnx](https://www.nuget.org/packages/Net5.IBM.Data.Db2-lnx): .NET 5+ Linux provider
  - [Net5.IBM.Data.Db2-osx](https://www.nuget.org/packages/Net5.IBM.Data.Db2-osx): .NET 5+ MacOS provider
  - [Net.IBM.Data.Db2](https://www.nuget.org/packages/Net.IBM.Data.Db2): .NET 6+ Windows provider
  - [Net.IBM.Data.Db2-lnx](https://www.nuget.org/packages/Net.IBM.Data.Db2-lnx): .NET 6+ Linux provider
  - [Net.IBM.Data.Db2-osx](https://www.nuget.org/packages/Net.IBM.Data.Db2-osx): .NET 6+ MacOS provider
  - for `Informix` you can also use legacy ODS or SQLi `IBM.Data.Informix` providers, but we don't recommend it
- DB2 iSeries
  - [linq2db4iSeries](https://www.nuget.org/packages/linq2db4iSeries): Don't reference linq2db package explicitly. This package already references supported linq2db version
- Firebird
  - [FirebirdSql.Data.FirebirdClient](https://www.nuget.org/packages/FirebirdSql.Data.FirebirdClient)
- MySQL and MariaDB
  - [MySqlConnector](https://www.nuget.org/packages/MySqlConnector) (recommended)
  - [MySql.Data](https://www.nuget.org/packages/MySql.Data) (highly discouraged, low quality provider)
- Oracle
  - [Oracle.ManagedDataAccess](https://www.nuget.org/packages/Oracle.ManagedDataAccess) (.NET Framework)
  - [Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core) (.NET Core)
  - also native legacy ODP.NET provider supported, but not recommended
- PostgreSQL
  - [Npgsql](https://www.nuget.org/packages/Npgsql)
- SAP HANA 2
  - [System.Data.Odbc](https://www.nuget.org/packages/System.Data.Odbc) for ODBC provider
  - also native ADO.NET provider could be used (Windows-only)
- SQL Server Compact Edition
  - `System.Data.SqlServerCe` provider
- SQLite
  - [System.Data.SQLite.Core](https://www.nuget.org/packages/System.Data.SQLite.Core) (recommended as it use most recent SQLite engine and provides access to SQLite functions)
  - [Microsoft.Data.Sqlite](https://www.nuget.org/packages/Microsoft.Data.Sqlite)
- SQL Server
  - [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient) (recommended)
  - [System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient) (legacy)
- SAP/Sybase ASE
  - [AdoNetCore.AseClient](https://www.nuget.org/packages/AdoNetCore.AseClient) (recommended)
  - `Sybase.AdoNet45.AseClient` native provider (not recommended, low quality provider)

## Obsoletion Note

T4 templates will be obsoleted in `Linq To DB` 4.0.0 and replaced with new scaffolding utility.

There are multiple reasons for it:

- T4 templates doesn't work well outside of Visual Studio. MacOS/Linux users or users of alternate IDEs could face serious issues trying to use T4 templates.
- T4 templates executed using .NET Framework by Visual Studio, which also creates issues for non-Windows users.
- T4 host use x86 process for Visual Studio 2019- and x64 process for Visual Studio 2022+. This creates issues with native providers as they are platform-dependent (ODBC and OleDb providers, various native ADO.NET providers).
- It's very hard to maintain big code generation framework, written in T4 templates.

Those are just several most major issues current T4 templates have.
