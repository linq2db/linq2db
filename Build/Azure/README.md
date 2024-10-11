#

This directory contains test configs and setup scripts for test jobs on Azure Pipelines.

- `netfx` folder stores test job configs for .NET 4.6.2 Windows tests
- `net60` folder stores test job configs for `net6.0` test runs for Windows, Linux and MacOS
- `net80` folder stores test job configs for `net8.0` test runs for Windows, Linux and MacOS
- `scripts` folder stores test job setup scripts (`*.cmd` for Windows jobs, `*.sh` for Linux and MacOS, `*.ps1` for PowerShell scripts)

## Azure Pipelines

### `default` pipeline

Performs default maintenance actions. Automatically runs for:

- PR to `release` branch: runs all tests for each commit to `master` branch
- commit to `master`: publish preview nugets to [Azure Artifacts feed](https://dev.azure.com/linq2db/linq2db/_packaging?_a=feed&feed=linq2db)
- commit to `release`: publish release nugets to [Nuget.org](https://www.nuget.org/profiles/LinqToDB)

### `build` pipeline

Automatically triggered for all PR commits. Performs build to verify code could be built on CI.

### `testing` pipeline

Runs manually using `/azp run test-all` command from PR comment by team member.

### db-specific test pipelines

Those pipelines used to run tests only for specific databases manually by team member. Currently doesn't support execution from external PRs.

- `/azp run test-access` - MS Access tests
- `/azp run test-clickhouse` - ClickHouse tests
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
- `/azp run test-sqlserver-2022` - SQL Server 2022 tests
- `/azp run test-sybase` - SAP/SYBASE ASE tests
- `/azp run test-metrics` - SQL Server 2022 tests with metrics

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
- `netfx`: .NET Framework (4.6.2)
- `netcore`: .NET 6 OR .NET 8
- :door: - Windows 2022
- :penguin: - Linux (Ununtu 24.04)
- :green_apple: - MacOS 13 (MacOS testing currently disabled)

| Database (version): provider \ Target framework (OS) | netfx :door: | netcore :door: | netcore :penguin: | netcore :green_apple: |
|:---|:---:|:---:|:---:|:---:|
|TestNoopProvider<sup>[1](#notes)</sup>|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|
|SQLite [3.41.2](https://www.sqlite.org/releaselog/3_41_2.html)<br>[Microsoft.Data.SQLite](https://www.nuget.org/packages/Microsoft.Data.SQLite/)<br>with NorthwindDB Tests|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|
|SQLite [3.46.1](https://www.sqlite.org/releaselog/3_46_1.html)<br>[System.Data.SQLite](https://www.nuget.org/packages/System.Data.SQLite.Core/)<br>with NorthwindDB Tests|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|
|SQLite [3.46.1](https://www.sqlite.org/releaselog/3_46_1.html)<br>[System.Data.SQLite](https://www.nuget.org/packages/System.Data.SQLite.Core/)<br>with [MiniProfiler](https://www.nuget.org/packages/MiniProfiler.Shared/)<br>without mappings to underlying provider|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|
|SQLite [3.46.1](https://www.sqlite.org/releaselog/3_46_1.html)<br>[System.Data.SQLite](https://www.nuget.org/packages/System.Data.SQLite.Core/)<br>with [MiniProfiler](https://www.nuget.org/packages/MiniProfiler.Shared/)<br>with mappings to underlying provider|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|
|MySQL 5.7<br>[MySql.Data](https://www.nuget.org/packages/MySql.Data/)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|MySQL 5.7<br>[MySqlConnector](https://www.nuget.org/packages/MySqlConnector/)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|MySQL 8.0<br>[MySql.Data](https://www.nuget.org/packages/MySql.Data/)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|MySQL 8.0<br>[MySqlConnector](https://www.nuget.org/packages/MySqlConnector/)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|MariaDB 11<br>[MySqlConnector](https://www.nuget.org/packages/MySqlConnector/)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|PostgreSQL 11<br>[Npgsql](https://www.nuget.org/packages/Npgsql/)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|PostgreSQL 12<br>[Npgsql](https://www.nuget.org/packages/Npgsql/)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|PostgreSQL 13<br>[Npgsql](https://www.nuget.org/packages/Npgsql/)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|PostgreSQL 14<br>[Npgsql](https://www.nuget.org/packages/Npgsql/)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|PostgreSQL 15<br>[Npgsql](https://www.nuget.org/packages/Npgsql/)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|PostgreSQL 16<br>[Npgsql](https://www.nuget.org/packages/Npgsql/)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|Firebird 2.5<br>[FirebirdSql.Data.FirebirdClient](https://www.nuget.org/packages/FirebirdSql.Data.FirebirdClient/)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|Firebird 3.0<br>[FirebirdSql.Data.FirebirdClient](https://www.nuget.org/packages/FirebirdSql.Data.FirebirdClient/)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|Firebird 4.0<br>[FirebirdSql.Data.FirebirdClient](https://www.nuget.org/packages/FirebirdSql.Data.FirebirdClient/)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|Firebird 5.0<br>[FirebirdSql.Data.FirebirdClient](https://www.nuget.org/packages/FirebirdSql.Data.FirebirdClient/)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|SAP/Sybase ASE 16.2<br>[AdoNetCore.AseClient](https://www.nuget.org/packages/AdoNetCore.AseClient/)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|SAP/Sybase ASE 16.2<br>Native Client|:x:|:x:|:x:|:x:|
|MS SQL CE|:heavy_check_mark:|:heavy_check_mark:|:heavy_minus_sign:|:heavy_minus_sign:|
|MS SQL Server 2005<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/)<br>[Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/)|:heavy_check_mark:|:heavy_check_mark:|:heavy_minus_sign:|:heavy_minus_sign:|
|MS SQL Server 2008<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/)<br>[Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/)|:heavy_check_mark:|:heavy_check_mark:|:heavy_minus_sign:|:heavy_minus_sign:|
|MS SQL Server 2012<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/)<br>[Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/)|:heavy_check_mark:|:heavy_check_mark:|:heavy_minus_sign:|:heavy_minus_sign:|
|MS SQL Server 2014<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/)<br>[Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/)|:heavy_check_mark:|:heavy_check_mark:|:heavy_minus_sign:|:heavy_minus_sign:|
|MS SQL Server 2016<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/)<br>[Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/)|:heavy_check_mark:|:heavy_check_mark:|:heavy_minus_sign:|:heavy_minus_sign:|
|MS SQL Server 2017<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/)<br>[Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/)<br>with FTS Tests|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|
|MS SQL Server 2019<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/)<br>[Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/)<br>with FTS Tests|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|
|MS SQL Server 2022<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/)<br>[Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/)<br>with FTS Tests|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|
|Azure SQL<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/)<br>[Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/)|:x:|:x:|:x:|:x:|
|Access<br>Jet 4.0 OLE DB|:heavy_check_mark:|:x:|:heavy_minus_sign:|:heavy_minus_sign:|
|Access><br>ACE 12 OLE DB|:heavy_check_mark:|:heavy_check_mark:|:heavy_minus_sign:|:heavy_minus_sign:|
|Access<br>MDB ODBC|:heavy_check_mark:|:x:|:heavy_minus_sign:|:heavy_minus_sign:|
|Access<br>MDB+ACCDB ODBC|:heavy_check_mark:|:heavy_check_mark:|:heavy_minus_sign:|:heavy_minus_sign:|
|DB2 LUW 11.5<br>[IBM.Data.DB2](https://www.nuget.org/packages/IBM.Data.DB.Provider/) (netfx)<br>[IBM.Data.DB2.Core](https://www.nuget.org/packages/IBM.Data.DB2.Core/) ([osx](https://www.nuget.org/packages/IBM.Data.DB2.Core-osx/), [lin](https://www.nuget.org/packages/IBM.Data.DB2.Core-lnx/)) (core)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|Informix 14.10<br>[IBM.Data.DB2](https://www.nuget.org/packages/IBM.Data.DB.Provider/) IDS (netfx)<br>[IBM.Data.DB2.Core](https://www.nuget.org/packages/IBM.Data.DB2.Core/) ([osx](https://www.nuget.org/packages/IBM.Data.DB2.Core-osx/), [lin](https://www.nuget.org/packages/IBM.Data.DB2.Core-lnx/)) (core)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|Oracle 11.2g XE<br>[Oracle.ManagedDataAccess](https://www.nuget.org/packages/Oracle.ManagedDataAccess/) (netfx)<br>[Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core/) (core)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|Oracle 12.2c<br>[Oracle.ManagedDataAccess](https://www.nuget.org/packages/Oracle.ManagedDataAccess/) (netfx)<br>[Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core/) (core)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|Oracle 18c<br>[Oracle.ManagedDataAccess](https://www.nuget.org/packages/Oracle.ManagedDataAccess/) (netfx)<br>[Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core/) (core)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|Oracle 19c<br>[Oracle.ManagedDataAccess](https://www.nuget.org/packages/Oracle.ManagedDataAccess/) (netfx)<br>[Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core/) (core)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|Oracle 21c<br>[Oracle.ManagedDataAccess](https://www.nuget.org/packages/Oracle.ManagedDataAccess/) (netfx)<br>[Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core/) (core)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|Oracle 23c<br>[Oracle.ManagedDataAccess](https://www.nuget.org/packages/Oracle.ManagedDataAccess/) (netfx)<br>[Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core/) (core)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|SAP HANA 2.0 SPS 05r57<br>ODBC Provider|:x:|:x:|:heavy_check_mark:|:x:|
|ClickHouse (latest)<br>[Octonica.ClickHouseClient](https://www.nuget.org/packages/Octonica.ClickHouseClient/)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|ClickHouse (latest)<br>[ClickHouse.Client](https://www.nuget.org/packages/ClickHouse.Client/)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|
|ClickHouse (latest)<br>[MySqlConnector](https://www.nuget.org/packages/MySqlConnector/)|:x:|:x:|:heavy_check_mark:|:heavy_check_mark:|

### Notes

1. `TestNoopProvider` is a fake test provider to perform tests without database dependencies

### Test providers

| Name | Target Database | Extra Notes |
|:---|:---:|:---:|
|`TestProvName.NoopProvider`|fake test provider to perform tests without database dependencies|
|`ProviderName.SQLiteClassic`|System.Data.Sqlite||
|`ProviderName.SQLiteMS`|Microsoft.Data.Sqlite||
|`TestProvName.SQLiteClassicMiniProfilerUnmapped`|System.Data.Sqlite + MiniProfiler|Tests compatibility with connection wrappers (without mappings to provider types)|
|`TestProvName.SQLiteClassicMiniProfilerMapped`|System.Data.Sqlite + MiniProfiler|Tests compatibility with connection wrappers (with mappings to provider types)|
|`TestProvName.NorthwindSQLite`|System.Data.Sqlite FTS||
|`TestProvName.NorthwindSQLiteMS`|Microsoft.Data.Sqlite FTS||
|`ProviderName.MySql57`|MySQL 5.7 using MySQL.Data||
|`TestProvName.MySql57Connector`|MySQL 5.7 using MySqlConnector||
|`ProviderName.MySql80`|Latest MySQL using MySQL.Data||
|`TestProvName.MySql80Connector`|Latest MySQL using MySqlConnector||
|`TestProvName.MariaDB11Connector`|Latest MariaDB using MySqlConnector||
|`ProviderName.PostgreSQL92`|PostgreSQL 9.2-|PGSQL 9 not tested by CI|
|`ProviderName.PostgreSQL93`|PostgreSQL [9.3-9.5)|PGSQL 9 not tested by CI|
|`ProviderName.PostgreSQL95`|PostgreSQL 9.5+|PGSQL 9 not tested by CI|
|`TestProvName.PostgreSQL10`|PostgreSQL 10|Not tested by CI|
|`TestProvName.PostgreSQL11`|PostgreSQL 11||
|`TestProvName.PostgreSQL12`|PostgreSQL 12||
|`TestProvName.PostgreSQL13`|PostgreSQL 13||
|`TestProvName.PostgreSQL14`|PostgreSQL 14||
|`ProviderName.PostgreSQL15`|PostgreSQL 15||
|`TestProvName.PostgreSQL16`|PostgreSQL 16||
|`ProviderName.Firebird25`|Firebird 2.5||
|`TestProvName.Firebird3`|Firebird 3.0||
|`TestProvName.Firebird4`|Firebird 4.0||
|`TestProvName.Firebird5`|Firebird 5.0||
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
|`ProviderName.SqlServer2022`|SQL Server 2022 using System.Data.SqlClient||
|`TestProvName.SqlServer2022MS`|SQL Server 2022 using Microsoft.Data.SqlClient||
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
|`TestProvName.Oracle11Native`|Oracle 11g using native provider||
|`TestProvName.Oracle11Managed`|Oracle 11g using managed provider (core version for .net core)||
|`ProviderName.Oracle11DevartDirect`|Oracle 11g using Devart.Data.Oracle provider (Direct connect)|Not tested on CI|
|`ProviderName.Oracle11DevartOCI`|Oracle 11g using Devart.Data.Oracle provider (Oracle client)|Not tested on CI|
|`TestProvName.Oracle12Native`|Oracle 12c using native provider||
|`TestProvName.Oracle12Managed`|Oracle 12c using managed provider (core version for .net core)||
|`ProviderName.Oracle12DevartDirect`|Oracle 12c using Devart.Data.Oracle provider (Direct connect)|Not tested on CI|
|`ProviderName.Oracle12DevartOCI`|Oracle 12c using Devart.Data.Oracle provider (Oracle client)|Not tested on CI|
|`TestProvName.Oracle18Native`|Oracle 18c using native provider||
|`TestProvName.Oracle18Managed`|Oracle 18c using managed provider (core version for .net core)||
|`ProviderName.Oracle18DevartDirect`|Oracle 18c using Devart.Data.Oracle provider (Direct connect)|Not tested on CI|
|`ProviderName.Oracle18DevartOCI`|Oracle 18c using Devart.Data.Oracle provider (Oracle client)|Not tested on CI|
|`TestProvName.Oracle19Native`|Oracle 19c using native provider||
|`TestProvName.Oracle19Managed`|Oracle 19c using managed provider (core version for .net core)||
|`ProviderName.Oracle19DevartDirect`|Oracle 19c using Devart.Data.Oracle provider (Direct connect)|Not tested on CI|
|`ProviderName.Oracle19DevartOCI`|Oracle 19c using Devart.Data.Oracle provider (Oracle client)|Not tested on CI|
|`TestProvName.Oracle21Native`|Oracle 21c using native provider||
|`TestProvName.Oracle21Managed`|Oracle 21c using managed provider (core version for .net core)||
|`ProviderName.Oracle21DevartDirect`|Oracle 21c using Devart.Data.Oracle provider (Direct connect)|Not tested on CI|
|`ProviderName.Oracle21DevartOCI`|Oracle 21c using Devart.Data.Oracle provider (Oracle client)|Not tested on CI|
|`TestProvName.Oracle23Native`|Oracle 23c using native provider||
|`TestProvName.Oracle23Managed`|Oracle 23c using managed provider (core version for .net core)||
|`ProviderName.Oracle23DevartDirect`|Oracle 23c using Devart.Data.Oracle provider (Direct connect)|Not tested on CI|
|`ProviderName.Oracle23DevartOCI`|Oracle 23c using Devart.Data.Oracle provider (Oracle client)|Not tested on CI|
|`ProviderName.SapHanaNative`|SAP HANA 2 using native provider||
|`ProviderName.SapHanaOdbc`|SAP HANA 2 using ODBC provider||
|`ProviderName.ClickHouseOctonica`|ClickHouse using `Octonica.ClickHouseClient` provider||
|`ProviderName.ClickHouseClient`|ClickHouse using `ClickHouse.Client` provider||
|`ProviderName.ClickHouseMySql`|ClickHouse using `MySqlConnector` provider||
