This directory contains test configs and setup scripts for test jobs on Azure Pipelines
- `net472` folder stores test job configs for .NET 4.7.2 Windows tests
- `netcoreapp31` folder stores test job configs for `netcoreapp3.1` test runs for Windows, Linux and MacOS
- `net60` folder stores test job configs for `net6.0` test runs for Windows, Linux and MacOS
- `scripts` folder stores test job setup scripts (`*.cmd` for Windows jobs and `*.sh` for Linux and MacOS)

## Azure Pipelines
All existing pipelines we have listed below. If you need more flexible test runs, you can request more test pipelines. E.g. to run only specific database or framework/OS tests.

#### `default` pipeline

Automatically runs for:
- PR to `release` branch: runs all tests for PR commit
- commit to `master`: runs all tests and publish nugets to [Azure Artifacts feed](https://dev.azure.com/linq2db/linq2db/_packaging?_a=feed&feed=linq2db)
- commit to `release`: publish nugets to [Nuget.org](https://www.nuget.org/profiles/LinqToDB)

#### `build` pipeline

Automatically triggered for all PR commits and runs solution build

#### `test-all` pipeline

Runs manually using `/azp run test-all` command from PR comment by team member. Currently this pipeline will skip testing targeting macos (you need to use db-specific pipeline for it) due to incredible slowness of docker for macos.

#### db-specific test pipelines

Those pipelines used to run tests only for specific databases manually by team member:
- `/azp run test-access` - MS Access tests
- `/azp run test-db2` - IBM DB2 tests
- `/azp run test-firebird` - Firebird tests
- `/azp run test-informix` - IBM Informix tests
- `/azp run test-mysql` - MySQL and MariaDB tests
- `/azp run test-oracle` - Oracle tests
- `/azp run test-postgresql` - PostgreSQL tests
- `/azp run test-saphana` - SAP HANA 2 tests
- `/azp run test-sqlce` - SQL CE tests
- `/azp run test-sqlite` - SQLite tests
- `/azp run test-sqlserver` - SQL Server tests (all versions)
- `/azp run test-sqlserver-2019` - SQL Server 2019 tests
- `/azp run test-sybase` - SAP/SYBASE ASE tests

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
- `netfx`: .NET Framework (4.7.2)
- `netcore`: .NET Core 3.1 OR .NET 6.0
- :door: - Windows 2022 or 2019 (for docker-images with win2019 dependency)
- :penguin: - Linux (Ununtu 20.04)
- :green_apple: - MacOS Catalina 10.15

| Database (version): provider \ Target framework (OS) | netfx :door: | netcore :door: | netcore :penguin: | netcore :green_apple: |
|:---|:---:|:---:|:---:|:---:|
|TestNoopProvider<sup>[1](#notes)</sup>|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|
|SQLite [3.35.5](https://www.sqlite.org/releaselog/3_35_5.html)<br>[Microsoft.Data.SQLite](https://www.nuget.org/packages/Microsoft.Data.SQLite/) 6.0.4<br>with NorthwindDB Tests|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|
|SQLite [3.37.0](https://www.sqlite.org/draft/releaselog/current.html)<br>[System.Data.SQLite](https://www.nuget.org/packages/System.Data.SQLite.Core/) 1.0.115.5<br>with NorthwindDB Tests|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|
|SQLite [3.37.0](https://www.sqlite.org/draft/releaselog/current.html)<br>[System.Data.SQLite](https://www.nuget.org/packages/System.Data.SQLite.Core/) 1.0.115.5<br>with [MiniProfiler](https://www.nuget.org/packages/MiniProfiler.Shared/) 4.2.22<br>without mappings to underlying provider|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|
|SQLite [3.37.0](https://www.sqlite.org/draft/releaselog/current.html)<br>[System.Data.SQLite](https://www.nuget.org/packages/System.Data.SQLite.Core/) 1.0.115.5<br>with [MiniProfiler](https://www.nuget.org/packages/MiniProfiler.Shared/) 4.2.22<br>with mappings to underlying provider|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|
|MySQL 5.6<br>[MySql.Data](https://www.nuget.org/packages/MySql.Data/) 8.0.28|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|MySQL 5.6<br>[MySqlConnector](https://www.nuget.org/packages/MySqlConnector/) 0.69.10/1.3.14/2.1.8|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|MySQL (latest)<br>[MySql.Data](https://www.nuget.org/packages/MySql.Data/) 8.0.28|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|MySQL (latest)<br>[MySqlConnector](https://www.nuget.org/packages/MySqlConnector/) 0.69.10/1.3.14/2.1.8|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|MariaDB (latest)<br>[MySql.Data](https://www.nuget.org/packages/MySql.Data/) 8.0.28|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|MariaDB (latest)<br>[MySqlConnector](https://www.nuget.org/packages/MySqlConnector/) 0.69.10/1.3.14/2.1.8|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|PostgreSQL 10<br>[Npgsql](https://www.nuget.org/packages/Npgsql/) 6.0.4 |:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|PostgreSQL 11<br>[Npgsql](https://www.nuget.org/packages/Npgsql/) 6.0.4 |:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|PostgreSQL 12<br>[Npgsql](https://www.nuget.org/packages/Npgsql/) 6.0.4 |:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|PostgreSQL 13<br>[Npgsql](https://www.nuget.org/packages/Npgsql/) 6.0.4 |:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|PostgreSQL 14<br>[Npgsql](https://www.nuget.org/packages/Npgsql/) 6.0.4 |:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|Firebird 2.5<br>[FirebirdSql.Data.FirebirdClient](https://www.nuget.org/packages/FirebirdSql.Data.FirebirdClient/) 9.0.0|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|Firebird 3.0<br>[FirebirdSql.Data.FirebirdClient](https://www.nuget.org/packages/FirebirdSql.Data.FirebirdClient/) 9.0.0|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|Firebird 4.0<br>[FirebirdSql.Data.FirebirdClient](https://www.nuget.org/packages/FirebirdSql.Data.FirebirdClient/) 9.0.0|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|SAP/Sybase ASE 16.2<br>[AdoNetCore.AseClient](https://www.nuget.org/packages/AdoNetCore.AseClient/) 0.19.2|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|SAP/Sybase ASE 16.2<br>Native Client|:x:|:x:|:x:|:x:|
|MS SQL CE|:heavy_check_mark:|:heavy_check_mark:|:heavy_minus_sign:|:heavy_minus_sign:|
|MS SQL Server 2005<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.8.3<br>[Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/) 4.1.0|:heavy_check_mark:|:heavy_check_mark:|:heavy_minus_sign:|:heavy_minus_sign:|
|MS SQL Server 2008<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.8.3<br>[Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/) 4.1.0|:heavy_check_mark:|:heavy_check_mark:|:heavy_minus_sign:|:heavy_minus_sign:|
|MS SQL Server 2012<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.8.3<br>[Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/) 4.1.0|:heavy_check_mark:|:heavy_check_mark:|:heavy_minus_sign:|:heavy_minus_sign:|
|MS SQL Server 2014<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.8.3<br>[Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/) 4.1.0|:heavy_check_mark:|:heavy_check_mark:|:heavy_minus_sign:|:heavy_minus_sign:|
|MS SQL Server 2016<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.8.3<br>[Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/) 4.1.0|:heavy_check_mark:|:heavy_check_mark:|:heavy_minus_sign:|:heavy_minus_sign:|
|MS SQL Server 2017<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.8.3<br>[Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/) 4.1.0<br>with FTS Tests|:heavy_check_mark:<sup>[2](#notes)</sup>|:heavy_check_mark:<sup>[2](#notes)</sup>|:heavy_check_mark:|:heavy_check_mark:|
|MS SQL Server 2019<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.8.3<br>[Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/) 4.1.0<br>with FTS Tests|:heavy_check_mark:<sup>[2](#notes)</sup>|:heavy_check_mark:<sup>[2](#notes)</sup>|:heavy_check_mark:|:heavy_check_mark:|
|Azure SQL<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.8.3<br>[Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/) 4.1.0|:x:|:x:|:x:|:x:|
|Access<br>Jet 4.0 OLE DB|:heavy_check_mark:|:x:|:heavy_minus_sign:|:heavy_minus_sign:|
|Access><br>ACE 12 OLE DB|:heavy_check_mark:|:heavy_check_mark:|:heavy_minus_sign:|:heavy_minus_sign:|
|Access<br>MDB ODBC|:heavy_check_mark:|:x:|:heavy_minus_sign:|:heavy_minus_sign:|
|Access<br>MDB+ACCDB ODBC|:heavy_check_mark:|:heavy_check_mark:|:heavy_minus_sign:|:heavy_minus_sign:|
|DB2 LUW 11.5<br>[IBM.Data.DB2](https://www.nuget.org/packages/IBM.Data.DB.Provider/) 11.5.5010.4 (netfx)<br>[IBM.Data.DB2.Core](https://www.nuget.org/packages/IBM.Data.DB2.Core/) 3.1.0.500 ([osx](https://www.nuget.org/packages/IBM.Data.DB2.Core-osx/) 2.0.0.100, [lin](https://www.nuget.org/packages/IBM.Data.DB2.Core-lnx/) 3.1.0.500) (core)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|Informix 14.10<br>[IBM.Data.DB2](https://www.nuget.org/packages/IBM.Data.DB.Provider/) IDS 11.5.5010.4 (netfx)<br>[IBM.Data.DB2.Core](https://www.nuget.org/packages/IBM.Data.DB2.Core/) 3.1.0.500 ([osx](https://www.nuget.org/packages/IBM.Data.DB2.Core-osx/) 2.0.0.100, [lin](https://www.nuget.org/packages/IBM.Data.DB2.Core-lnx/) 3.1.0.500) (core)|:x:|:x:|:x:|:x:|
|Oracle 11.2g XE<br>[Oracle.ManagedDataAccess](https://www.nuget.org/packages/Oracle.ManagedDataAccess/) 21.5.0 (netfx)<br>[Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core/) 3.21.50 (core)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|Oracle 12.2c<br>[Oracle.ManagedDataAccess](https://www.nuget.org/packages/Oracle.ManagedDataAccess/) 21.5.0 (netfx)<br>[Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core/) 3.21.50 (core)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|SAP HANA 2.0 SPS 05r57<br>ODBC Provider|:x:|:x:|:heavy_check_mark:|:x:|

###### Notes:
1. `TestNoopProvider` is a fake test provider to perform tests without database dependencies
5. Northwind FTS SQL Server tests not enabled yet, as we need SQL Server images with full-text search included

###### Test providers
| Name | Target Database | Extra Notes |
|:---|:---:|:---:|
|`TestProvName.NoopProvider`|fake test provider to perform tests without database dependencies|
|`ProviderName.SQLiteClassic`|System.Data.Sqlite||
|`ProviderName.SQLiteMS`|Microsoft.Data.Sqlite||
|`TestProvName.SQLiteClassicMiniProfilerUnmapped`|System.Data.Sqlite + MiniProfiler|Tests compatibility with connection wrappers (without mappings to provider types)|
|`TestProvName.SQLiteClassicMiniProfilerMapped`|System.Data.Sqlite + MiniProfiler|Tests compatibility with connection wrappers (with mappings to provider types)|
|`TestProvName.NorthwindSQLite`|System.Data.Sqlite FTS||
|`TestProvName.NorthwindSQLiteMS`|Microsoft.Data.Sqlite FTS||
|`ProviderName.MySql`|Latest MySQL using MySQL.Data||
|`ProviderName.MySqlConnector`|Latest MySQL using MySqlConnector||
|`TestProvName.MySql55`|MySQL 5.5 using MySQL.Data||
|`TestProvName.MySql55Connector`|MySQL 5.5 using MySqlConnector||
|`TestProvName.MariaDB`|Latest MariaDB using MySQL.Data||
|`TestProvName.MariaDBConnector`|Latest MariaDB using MySqlConnector||
|`ProviderName.PostgreSQL92`|PostgreSQL 9.2-|PGSQL 9 not tested by CI|
|`ProviderName.PostgreSQL93`|PostgreSQL [9.3-9.5)|PGSQL 9 not tested by CI|
|`ProviderName.PostgreSQL95`|PostgreSQL 9.5+|PGSQL 9 not tested by CI|
|`TestProvName.PostgreSQL10`|PostgreSQL 10||
|`TestProvName.PostgreSQL11`|PostgreSQL 11||
|`TestProvName.PostgreSQL12`|PostgreSQL 12||
|`TestProvName.PostgreSQL13`|PostgreSQL 13||
|`TestProvName.PostgreSQL14`|PostgreSQL 14||
|`ProviderName.Firebird`|Firebird 2.5||
|`TestProvName.Firebird3`|Firebird 3.0||
|`TestProvName.Firebird4`|Firebird 4.0||
|`ProviderName.Sybase`|Sybase ASE using official unmanaged provider||
|`ProviderName.SybaseManaged`|Sybase ASE using DataAction managed provider||
|`ProviderName.SqlCe`|SQL CE| SQL CE 4.0 used for testing|
|`ProviderName.SqlServer2005`|SQL Server 2005 using System.Data.SqlClient||
|`TestProvName.SqlServer2005MS`|SQL Server 2005 using Microsoft.Data.SqlClient||
|`ProviderName.SqlServer2008`|SQL Server 2008 using System.Data.SqlClient||
|`TestProvName.SqlServer2008MS`|SQL Server 2008 using Microsoft.Data.SqlClient||
|`ProviderName.SqlServer2012`|SQL Server 2012 using System.Data.SqlClient||
|`TestProvName.SqlServer2012MS`|SQL Server 2012 using Microsoft.Data.SqlClient||
|`ProviderName.SqlServer2014`|SQL Server 2014 using System.Data.SqlClient||
|`TestProvName.SqlServer2014MS`|SQL Server 2014 using Microsoft.Data.SqlClient||
|`ProviderName.SqlServer2017`|SQL Server 2017 using System.Data.SqlClient||
|`TestProvName.SqlServer2017MS`|SQL Server 2017 using Microsoft.Data.SqlClient||
|`ProviderName.SqlServer2019`|SQL Server 2019 using System.Data.SqlClient||
|`TestProvName.SqlServer2019MS`|SQL Server 2019 using Microsoft.Data.SqlClient||
|`TestProvName.SqlServerSA`|SQL Server latest (2019) in SequentialAccess mode using System.Data.SqlClient||
|`TestProvName.SqlServerSAMS`|SQL Server latest (2019) in SequentialAccess mode using Microsoft.Data.SqlClient||
|`TestProvName.SqlServerContained`|SQL Server latest (2019) in contained database mode using System.Data.SqlClient||
|`TestProvName.SqlServerContainedMS`|SQL Server latest (2019) in contained database mode using Microsoft.Data.SqlClient||
|`TestProvName.SqlServerAzure`|SQL Server Azure (latest) using System.Data.SqlClient||
|`TestProvName.SqlServerAzureMS`|SQL Server Azure (latest) using Microsoft.Data.SqlClient||
|`TestProvName.SqlServerNorthwind`|SQL Server latest (2019) Northwind database with FTS using System.Data.SqlClient||
|`TestProvName.SqlServerNorthwindMS`|SQL Server latest (2019) Northwind database with FTS using Microsoft.Data.SqlClient||
|`ProviderName.Access`|Tests against Access using OLE DB JET or ACE provider||
|`ProviderName.AccessOdbc`|Tests against Access using ODBC MDB or MDB+ACCDB provider||
|`ProviderName.DB2`|DB2 LUW 11.5||
|`ProviderName.InformixDB2`|Informix 14.10 (IDS using IBM.Data.DB2)||
|`ProviderName.OracleNative`|Oracle 12c using native provider||
|`ProviderName.OracleManaged`|Oracle 12c using managed provider (core version for .net core)||
|`TestProvName.Oracle11Native`|Oracle 11g using native provider||
|`TestProvName.Oracle11Managed`|Oracle 11g using managed provider (core version for .net core)||
|`ProviderName.SapHanaNative`|SAP HANA 2 using native provider||
|`ProviderName.SapHanaOdbc`|SAP HANA 2 using ODBC provider||
