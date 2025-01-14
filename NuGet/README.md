### T4 nugets structure

- `tools` folder contains `linq2db.dll`, `linq2db.Tools.dll`, `linq2db.Scaffold.dll` and, optionally, provider assemblies. Assemblies should target .net framework, as they run under .net framework x86 T4 host in Visual Studio (what about Rider?)
- `build` folder contains `.props` file, included into user's project. It defines MSBuild properties, needed for T4 templates preprocessor. See list of properties below
- `contentFiles` folder contains set of T4 templates, used for scaffolding
- `content` folder contains copy of T4 templates, included into legacy projects (note that legacy project use templates from `contentFiles` for scaffolding). The only reason we have this folder with copy of templates is to make templates visible to user from legacy project. Unfortunately we cannot show templates from `contentFiles` in legacy project using `ItemGroup` in imported `.props` file, as Visual Studio (not MS Build) doesn't support display of imported `ItemGroup`s

#### MS Build Properties

Properties implemented in a way to support redefinition if multiple T4 templates installed.
All T4 properties use `LinqToDBT4` name prefix.

- `LinqToDBT4SharedTools`: defines path to `tools` folder and must be used only for assemblies, included into all T4 nugets: `linq2db.dll`, `linq2db.Tools.dll`, `linq2db.Scaffold.dll`, `Humanizer.dll`. Defined by every T4 nuget, so last one will be used.
- `LinqToDBT4<PROVIDER>ClientPath`: defines path to `tools` folder in nuget cache for T4 template, that use this provider to read database schema. Could be defined by multiple T4 nugets and we will use last one imported (we don't care which one). List of such properties:
  - `LinqToDBT4AccessClientPath`: path to `tools` folder with Access client
  - `LinqToDBT4ClickHouseClientPath`: path to `tools` folder with ClickHouse client
  - `LinqToDBT4DB2ClientPath`: path to `tools` folder with DB2 client
  - `LinqToDBT4FirebirdClientPath`: path to `tools` folder with Firebird client
  - `LinqToDBT4InformixClientPath`: path to `tools` folder with Informix client
  - `LinqToDBT4MySqlClientPath`: path to `tools` folder with `MySqlConnector` MySql client
  - `LinqToDBT4OracleClientPath`: path to `tools` folder with `Oracle.ManagedDataAccess` Oracle client
  - `LinqToDBT4PostgreSQLClientPath`: path to `tools` folder with `Npgsql` PostgreSQL client
  - `LinqToDBT4SapHanaClientPath`: path to `tools` folder with SapHana client
  - `LinqToDBT4SqlCeClientPath`: path to `tools` folder with SqlCe client
  - `LinqToDBT4SQLiteClientPath`: path to `tools` folder with `System.Data.SQLite` SQLite client
  - `LinqToDBT4SqlServerClientPath`: path to `tools` folder with `Microsoft.SqlServer.Types` assembly for SQL Server spatial types
  - `LinqToDBT4SybaseClientPath`: path to `tools` folder with `AdoNetCore.AseClient` Sybase ASE client
  - all other databases require native client installed or be a part of framework
- `LinqToDBT4<DB>TemplatesPath`: defines path to folder with T4 templates for specific database. Also could be refedined if multiple T4 nugets with same database support installed, e.g. `linq2db.<DB>` nuget and `linq2db.t4models` nuget which contains support for all databases
  - `LinqToDBT4AccessTemplatesPath`: MS Access T4 templates
  - `LinqToDBT4ClickHouseTemplatesPath`: ClickHouse T4 templates
  - `LinqToDBT4DB2TemplatesPath`: DB2 T4 templates
  - `LinqToDBT4FirebirdTemplatesPath`: Firebird T4 templates
  - `LinqToDBT4InformixTemplatesPath`: Informix T4 templates
  - `LinqToDBT4MySqlTemplatesPath`: MySql/MariaDB T4 templates
  - `LinqToDBT4OracleTemplatesPath`: Oracle T4 templates
  - `LinqToDBT4PostgreSQLTemplatesPath`: PostgreSQL T4 templates
  - `LinqToDBT4SapHanaTemplatesPath`: SAP HANA T4 templates
  - `LinqToDBT4SqlCeTemplatesPath`: SQL CE T4 templates
  - `LinqToDBT4SQLiteTemplatesPath`: SQLite templates
  - `LinqToDBT4SqlServerTemplatesPath`: SQL Server templates
  - `LinqToDBT4SybaseTemplatesPath`: Sybase ASE templates
