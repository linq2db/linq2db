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
- `?`: test job status not reviewed yet
- `(R)`: test job was running before using Travis or Appveryor CI (to track not migrated yet tests)
- `net46`: .NET Framework 4.6
- `netcoreapp2.0`: .NETCoreApp 2.0
- :door: - Windows (2019)
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
|MS SQL Server 2017<br>[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) 4.6.1<br>with NorthwindDB Tests<sup>[5](#notes)</sup>|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|:heavy_check_mark:|
|separator between automated and pending providers|-|-|-|-|
|MySQL (5.7):[MySql.Data](https://www.nuget.org/packages/MySql.Data/)|v|v|v|v|
|MySQL:[MySqlConnector](https://www.nuget.org/packages/MySqlConnector/)|(R)|(R)|(R)|?|
|PostgreSQL:[Npgsql](https://www.nuget.org/packages/Npgsql/)|(R)|(R)|?|?|
|Azure SQL:[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/)|?|?|?|?|
|DB2 LUW|?|?|?|?|
|Informix|?|?|?|?|
|SAP HANA 2.0|?|?|?|?|
|MariaDB|?|?|?|?|
|SAP/Sybase ASE|?|?|?|?|
|SAP/Sybase ASE:[AdoNetCore.AseClient](https://www.nuget.org/packages/AdoNetCore.AseClient/)|?|?|?|?|
|Oracle|?|?|?|?|
|Firebird:[FirebirdSql.Data.FirebirdClient](https://www.nuget.org/packages/FirebirdSql.Data.FirebirdClient/)|?|?|?|?|

###### Notes:
1. `TestNoopProvider` is a fake test provider to perform tests without database dependencies
2. `1.1.1` is the last version of `Microsoft.Data.SQLite`, that supports .NET Framework, so we use it for `net46` test configuration and recent version for `netcoreapp2.0`
3. for Access right now we don't support .net core
4. for SQL CE right now we don't run .net core tests
5. Northwind SQL Server tests not enabled yet
