This directory contains test configs and setup scripts for test jobs on Azure Pipelines
- `net46` folder stores test job configs for .NET 4.6 Windows tests
- `netcoreapp20` folder stores test job configs for `netcoreapp2.0` test runs for Windows, Linux and MacOS
- `scripts` folder stores test job setup scripts (`*.cmd` for windows jobs and `*.sh` for Linux and MacOS)

## Azure Pipelines
All existing pipelines we have listed below. If you need more flexible test runs, you can request more test pipelines. E.g. to run only specific database or framework/OS tests.

#### `default` pipeline

Automatically runs for:
- PR to `release` branch: runs all tests for PR commit
- commit to `master` or `release.3.0`: runs all tests and publish nugets to [Azure Artifacts feed](https://dev.azure.com/linq2db/linq2db/_packaging?_a=feed&feed=linq2db)
- commit to `release`: publish nugets to [Nuget.org](https://www.nuget.org/profiles/LinqToDB)

#### `build` pipeline

Automatically triggered for all PR commits and runs solution build

#### `test-all` pipeline

Runs manually using `/azp run test-all` command from PR comment by team member

#### `experimental` pipeline
Runs manually using `/azp run experimental` command from PR and used for development and testing of new pipelines/test providers.
Base pipeline template contains only solution build and should be reset to initial state before merge.

## Test Matrix

Following table contains information about which test jobs are awailable per:
- operating system
- target framework
- database
- database version
- database provider

Legend:
- :heavy_minus_sign: - test configuration not supported (e.g. db/provider not available for target OS/Framework)
- :heavy_check_mark: - test job implemented
- :x: - test job not implemented yet
- `net46`: .NET Framework 4.6
- `netcoreapp2.0`: .NETCoreApp 2.0
- :door: - Windows (2019 or 2016 for some docker-based tests)
- :penguin: - Linux (Ununtu 16.04)
- :green_apple: - MacOS Mojave 10.14

| Database (version): provider \ Target framework (OS) | net46 :door: | netcoreapp2.0 :door: | netcoreapp2.0 :penguin: | netcoreapp2.0 :green_apple: |
|:---|:---:|:---:|:---:|:---:|
|TestNoopProvider<sup>[1](#notes)</sup>|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|
|SQLite [3.13.0](https://www.sqlite.org/releaselog/3_13_0.html)<sup>[2](#notes)</sup><br>[Microsoft.Data.SQLite](https://www.nuget.org/packages/Microsoft.Data.SQLite/) 1.1.1<br>with NorthwindDB Tests|:heavy_check_mark:|:heavy_minus_sign:|:heavy_minus_sign:|:heavy_minus_sign:|
|SQLite [3.26.0](https://www.sqlite.org/releaselog/3_26_0.html)<br>[Microsoft.Data.SQLite](https://www.nuget.org/packages/Microsoft.Data.SQLite/) 2.2.6<br>with NorthwindDB Tests|:heavy_minus_sign:|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|
|SQLite [3.28.0](https://www.sqlite.org/releaselog/3_28_0.html)<br>[System.Data.SQLite](https://www.nuget.org/packages/System.Data.SQLite.Core/) 1.0.111<br>with NorthwindDB Tests|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|
|Access<sup>[3](#notes)</sup><br>Jet OLE DB|:heavy_check_mark:|:x:|:heavy_minus_sign:|:heavy_minus_sign:|
|Access<sup>[3](#notes)</sup><br>ACE OLE DB|:heavy_check_mark:|:x:|:heavy_minus_sign:|:heavy_minus_sign:|
|MS SQL CE<sup>[4](#notes)</sup>|:heavy_check_mark:|:x:|:heavy_minus_sign:|:heavy_minus_sign:|
|MS SQL Server 2000<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.6.1|:x:|:x:|:x:|:x:|
|MS SQL Server 2005<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.6.1|:x:|:x:|:x:|:x:|
|MS SQL Server 2008<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.6.1|:x:|:x:|:x:|:x:|
|MS SQL Server 2012<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.6.1|:x:|:x:|:x:|:x:|
|MS SQL Server 2014<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.6.1|:x:|:x:|:x:|:x:|
|MS SQL Server 2016<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.6.1|:x:|:x:|:x:|:x:|
|MS SQL Server 2017<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.6.1<br>with NorthwindDB<sup>[5](#notes)</sup> Tests|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|
|MS SQL Server 2019<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.6.1|:x:|:x:|:x:|:x:|
|Azure SQL<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.6.1|:x:|:x:|:x:|:x:|
|MySQL 5.6<br>[MySql.Data](https://www.nuget.org/packages/MySql.Data/) 8.0.17|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|MySQL 8<br>[MySql.Data](https://www.nuget.org/packages/MySql.Data/) 8.0.17|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|MySQL 8<br>[MySqlConnector](https://www.nuget.org/packages/MySqlConnector/) 0.56.0|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|MariaDB 10<br>[MySql.Data](https://www.nuget.org/packages/MySql.Data/) 8.0.17|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|
|PostgreSQL 9.2<br>[Npgsql](https://www.nuget.org/packages/Npgsql/) 4.0.9|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|PostgreSQL 9.3<br>[Npgsql](https://www.nuget.org/packages/Npgsql/) 4.0.9|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|PostgreSQL 9.5<br>[Npgsql](https://www.nuget.org/packages/Npgsql/) 4.0.9|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|PostgreSQL 10<br>[Npgsql](https://www.nuget.org/packages/Npgsql/) 4.0.9|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|
|PostgreSQL 11<br>[Npgsql](https://www.nuget.org/packages/Npgsql/) 4.0.9|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|PostgreSQL 12<br>[Npgsql](https://www.nuget.org/packages/Npgsql/) 4.0.9|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|DB2 LUW 11.5.0.0a<br>[IBM.Data.DB2](https://www.nuget.org/packages/IBM.Data.DB.Provider/) 11.1.4040.4|:x:|:heavy_minus_sign:|:heavy_minus_sign:|:heavy_minus_sign:|
|DB2 LUW 11.5.0.0a<br>[IBM.Data.DB2.Core](https://www.nuget.org/packages/IBM.Data.DB2.Core/) ([osx](https://www.nuget.org/packages/IBM.Data.DB2.Core-osx/), [lin](https://www.nuget.org/packages/IBM.Data.DB2.Core-lnx/)) 1.3.0.100|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|Informix 12.10.FC12W1DE<br>IBM.Data.Informix ?|:x:|:heavy_minus_sign:|:heavy_minus_sign:|:heavy_minus_sign:|
|Informix 14.10.FC1DE<br>IBM.Data.Informix ?|:x:|:heavy_minus_sign:|:heavy_minus_sign:|:heavy_minus_sign:|
|Informix 12.10.FC12W1DE<br>[IBM.Data.DB2.Core](https://www.nuget.org/packages/IBM.Data.DB2.Core/) ([osx](https://www.nuget.org/packages/IBM.Data.DB2.Core-osx/), [lin](https://www.nuget.org/packages/IBM.Data.DB2.Core-lnx/)) 1.3.0.100|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|Informix 14.10.FC1DE<br>[IBM.Data.DB2.Core](https://www.nuget.org/packages/IBM.Data.DB2.Core/) ([osx](https://www.nuget.org/packages/IBM.Data.DB2.Core-osx/), [lin](https://www.nuget.org/packages/IBM.Data.DB2.Core-lnx/)) 1.3.0.100|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|SAP HANA 2.0 SPS 04r40<br>Native Provider|:x:|:x:|:heavy_minus_sign:|:heavy_minus_sign:|
|SAP/Sybase ASE 16.2<br>[AdoNetCore.AseClient](https://www.nuget.org/packages/AdoNetCore.AseClient/) 0.14.0|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|SAP/Sybase ASE 16.2<br>Native Client|:x:|:x:|:x:|:x:|
|Oracle 11c XE<br>Native Client|:x:|:heavy_minus_sign:|:heavy_minus_sign:|:heavy_minus_sign:|
|Oracle 11c XE<br>[Oracle.ManagedDataAccess](https://www.nuget.org/packages/Oracle.ManagedDataAccess/) 19.3.1|:x:|:heavy_minus_sign:|:heavy_minus_sign:|:heavy_minus_sign:|
|Oracle 11g XE<br>[Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core/) 2.19.31|:heavy_minus_sign:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|Oracle 18c XE<br>Native Client|:x:|:heavy_minus_sign:|:heavy_minus_sign:|:heavy_minus_sign:|
|Oracle 18c XE<br>[Oracle.ManagedDataAccess](https://www.nuget.org/packages/Oracle.ManagedDataAccess/) 19.3.1|:x:|:heavy_minus_sign:|:heavy_minus_sign:|:heavy_minus_sign:|
|Oracle 18c XE<br>[Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core/) 2.19.31|:heavy_minus_sign:|:x:|:x:|:x:|
|Firebird 2.1<br>[FirebirdSql.Data.FirebirdClient](https://www.nuget.org/packages/FirebirdSql.Data.FirebirdClient/) 7.0.0|:x:|:x:|:x:|:x:|
|Firebird 2.5<br>[FirebirdSql.Data.FirebirdClient](https://www.nuget.org/packages/FirebirdSql.Data.FirebirdClient/) 7.0.0|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|Firebird 3.0<br>[FirebirdSql.Data.FirebirdClient](https://www.nuget.org/packages/FirebirdSql.Data.FirebirdClient/) 7.0.0|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|Firebird 4.0<br>[FirebirdSql.Data.FirebirdClient](https://www.nuget.org/packages/FirebirdSql.Data.FirebirdClient/) 7.0.0|:x:|:x:|:x:|:x:|

###### Notes:
1. `TestNoopProvider` is a fake test provider to perform tests without database dependencies
2. `1.1.1` is the last version of `Microsoft.Data.SQLite`, that supports .NET Framework, so we use it for `net46` test configuration and recent version for `netcoreapp2.0`
3. for Access right now we don't support .net core
4. for SQL CE right now we don't run .net core tests
5. Northwind SQL Server tests not enabled yet, as we need SQL Server images with full-text search included

###### Provider names in context of tests
| Name | Target Database | Extra Notes |
|:---|:---:|:---:|
|`ProviderName.Access`|Tests against Access using OLE DB or ACE (depends on connection string)||
|`ProviderName.DB2`|tests against DB2 LUW||
|`ProviderName.DB2LUW`|not used||
|`ProviderName.DB2zOS`|not used||
|`ProviderName.Firebird`|Firebird 2.5|Should be used for latest version of FB and this one replaced with `Firebird25`|
|`TestProvName.Firebird3`|Firebird 3.0||
|`ProviderName.Informix`|Informix 12| TODO: move to v14|
|`TestProvName.SqlAzure`|Azure Sql||
|`ProviderName.SqlServer`|SQL Server (2008)|TODO: use it for latest|
|`ProviderName.SqlServer2000`|SQL Server 2000||
|`ProviderName.SqlServer2005`|SQL Server 2005||
|`ProviderName.SqlServer2008`|SQL Server 2008||
|`ProviderName.SqlServer2012`|SQL Server 2012||
|`ProviderName.SqlServer2014`|SQL Server 2014||
|`ProviderName.SqlServer2017`|SQL Server 2017||
|`TestProvName.Northwind`|SQL Server FTS tests||
|`ProviderName.MySql`|Latest MySQL using MySQL.Data||
|`TestProvName.MySql55`|MySQL 5.5||
|`ProviderName.MySqlOfficial`|not used||
|`ProviderName.MySqlConnector`|Latest MySQL using MySqlConnector||
|`TestProvName.MariaDB`|Latest MariaDB using MySQL.Data||
|`ProviderName.Oracle`|not used||
|`ProviderName.OracleNative`|Oracle using native provider||
|`ProviderName.OracleManaged`|Oracle using managed provider (core version for .net core)||
|`ProviderName.PostgreSQL`|Latest PostgreSQL (12)||
|`ProviderName.PostgreSQL92`|PostgreSQL 9.2-||
|`ProviderName.PostgreSQL93`|PostgreSQL [9.3-9.5)||
|`ProviderName.PostgreSQL95`|PostgreSQL 9.5+||
|`TestProvName.PostgreSQL10`|PostgreSQL 10||
|`TestProvName.PostgreSQL11`|PostgreSQL 11||
|`ProviderName.SqlCe`|SQL CE||
|`ProviderName.SQLite`|not used||
|`ProviderName.SQLiteClassic`|System.Data.Sqlite||
|`TestProvName.NorthwindSQLite`|System.Data.Sqlite FTS||
|`ProviderName.SQLiteMS`|Microsoft.Data.Sqlite||
|`TestProvName.NorthwindSQLiteMS`|Microsoft.Data.Sqlite FTS||
|`ProviderName.Sybase`|Sybase ASE using official provider||
|`ProviderName.SybaseManaged`|Sybase ASE using DataAction provider||
|`ProviderName.SapHana`|SAP HANA 2||
|`TestProvName.NoopProvider`|fake test provider to perform tests without database dependencies|
