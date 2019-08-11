This directory contains test configs and setup scripts for test jobs on Azure Pipelines
- `net46` folder stores test job configs for .NET 4.6 Windows tests
- `netcoreapp20` folder stores test job configs for `netcoreapp2.0` test runs for Windows, Linux and MacOS
- `scripts` folder stores test job setup scripts (`*.cmd` for windows jobs and `*.sh` for Linux and MacOS)

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
- :question: - test job status not reviewed yet
- `(R)`: test job was running before using Travis or Appveryor CI (to track not migrated yet tests)
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
|MS SQL Server 2017<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.6.1<br>with NorthwindDB<sup>[5](#notes)</sup> Tests|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|
|MySQL 5.7<br>[MySql.Data](https://www.nuget.org/packages/MySql.Data/) 8.0.17|:question:|:question:|:question:|:question:|
|MySQL 8<br>[MySql.Data](https://www.nuget.org/packages/MySql.Data/) 8.0.17|:question:|:question:|:question:|:question:|
|MySQL 8<br>[MySqlConnector](https://www.nuget.org/packages/MySqlConnector/) 0.56.0|:question:|:question:|:question:|:question:|
|MariaDB 10<br>[MySql.Data](https://www.nuget.org/packages/MySql.Data/) 8.0.17|:question:|:question:|:question:|:question:|
|PostgreSQL 9.5<br>[Npgsql](https://www.nuget.org/packages/Npgsql/) 4.0.8|:question:|:question:|:question:|:question:|
|PostgreSQL 10<br>[Npgsql](https://www.nuget.org/packages/Npgsql/) 4.0.8|:question:|:question:|:question:|:question:|
|PostgreSQL 11<br>[Npgsql](https://www.nuget.org/packages/Npgsql/) 4.0.8|:question:|:question:|:question:|:question:|
|PostgreSQL 12<br>[Npgsql](https://www.nuget.org/packages/Npgsql/) 4.0.8|:question:|:question:|:question:|:question:|
|separator between automated and pending providers|-|-|-|-|
|Azure SQL:[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/)|?|?|?|?|
|DB2 LUW|?|?|?|?|
|Informix|?|?|?|?|
|SAP HANA 2.0|?|?|?|?|
|SAP/Sybase ASE|?|?|?|?|
|SAP/Sybase ASE:[AdoNetCore.AseClient](https://www.nuget.org/packages/AdoNetCore.AseClient/)|?|?|?|?|
|Oracle|?|?|?|?|
|Firebird:[FirebirdSql.Data.FirebirdClient](https://www.nuget.org/packages/FirebirdSql.Data.FirebirdClient/)|?|?|?|?|
|PostgreSQL 9.2<br>[Npgsql](https://www.nuget.org/packages/Npgsql/) 4.0.8|:x:|:x:|:x:|:x:|
|PostgreSQL 9.3<br>[Npgsql](https://www.nuget.org/packages/Npgsql/) 4.0.8|:x:|:x:|:x:|:x:|
|MS SQL Server 2000<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.6.1|:x:|:x:|:x:|:x:|
|MS SQL Server 2005<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.6.1|:x:|:x:|:x:|:x:|
|MS SQL Server 2008<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.6.1|:x:|:x:|:x:|:x:|
|MS SQL Server 2012<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.6.1|:x:|:x:|:x:|:x:|
|MS SQL Server 2014<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.6.1|:x:|:x:|:x:|:x:|
|MS SQL Server 2016<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.6.1|:x:|:x:|:x:|:x:|
|MS SQL Server 2019<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.6.1|:x:|:x:|:x:|:x:|

###### Notes:
1. `TestNoopProvider` is a fake test provider to perform tests without database dependencies
2. `1.1.1` is the last version of `Microsoft.Data.SQLite`, that supports .NET Framework, so we use it for `net46` test configuration and recent version for `netcoreapp2.0`
3. for Access right now we don't support .net core
4. for SQL CE right now we don't run .net core tests
5. Northwind SQL Server tests not enabled yet, as we need SQL Server images with full-text search included
