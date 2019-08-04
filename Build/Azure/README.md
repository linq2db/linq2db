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
- `-`: test configuration not supported (e.g. db/provider not available for target OS/Framework)
- `v`: test job implemented
- `x`: test job not implemented yet
- `?`: test job status not reviewed yet
- `(R)`: test job was running before using Travis or Appveryor CI (to track not migrated yet tests)
- `net46`: .NET Framework 4.6
- `netcoreapp2.0`: .NETCoreApp 2.0
- `(W)`: Windows (2019)
- `(L)`: Linux (Ununtu 16.04)
- `(X)`: MacOS Mojave 10.14

| Database (version): provider \ Target framework (OS) | net46 (W) | netcoreapp2.0 (W) | netcoreapp2.0 (L) | netcoreapp2.0 (M) |
|-|-|-|-|-|
|TestNoopProvider|v|v|v|v|
|Access:OLEDB|(R)|?|?|?|
|Access:ACE|?|?|?|?|
|MS SQL CE|(R)|?|?|?|
|SQLite:[System.Data.SQLite](https://www.nuget.org/packages/System.Data.SQLite.Core/) + NorthwindDB|v|?|?|?|
|SQLite:[Microsoft.Data.SQLite](https://www.nuget.org/packages/Microsoft.Data.SQLite/) + NorthwindDB|(R)|v|v|v|
|MySQL (5.7):[MySql.Data](https://www.nuget.org/packages/MySql.Data/)|v|v|v|v|
|MySQL:[MySqlConnector](https://www.nuget.org/packages/MySqlConnector/)|(R)|(R)|(R)|?|
|PostgreSQL:[Npgsql](https://www.nuget.org/packages/Npgsql/)|(R)|(R)|?|?|
|MS SQL:[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) + NorthwindDB|(R)|(R)|?|?|
|Azure SQL:[System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/)|?|?|?|?|
|DB2 LUW|?|?|?|?|
|Informix|?|?|?|?|
|SAP HANA 2.0|?|?|?|?|
|MariaDB|?|?|?|?|
|SAP/Sybase ASE|?|?|?|?|
|SAP/Sybase ASE:[AdoNetCore.AseClient](https://www.nuget.org/packages/AdoNetCore.AseClient/)|?|?|?|?|
|Oracle|?|?|?|?|
|Firebird:[FirebirdSql.Data.FirebirdClient](https://www.nuget.org/packages/FirebirdSql.Data.FirebirdClient/)|?|?|?|?|

Providers:
- `TestNoopProvider` fake test provider to perform testing without database dependencies
