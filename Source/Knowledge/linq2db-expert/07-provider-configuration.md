<!-- Generated from: Source/Skills/linq2db/docs/provider-capabilities.md -->

# LinqToDB Provider Capability Matrix

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> You are here if you need to:
> - verify whether a specific SQL feature (MERGE, CTE, bulk copy, OUTPUT/RETURNING, upsert) is supported by the target provider
> - avoid generating SQL patterns that will fail or behave incorrectly on a given database

AI-facing reference: lists SQL feature support per provider.
Use this table to avoid generating SQL patterns that are unsupported by the target provider.

Version-conditional capabilities are noted with a version qualifier (e.g. `v8.0+`).
Entries marked ❌ are not supported by the documented LinqToDB provider capability surface.
Generated SQL may fail, be unavailable, or require provider-specific alternatives.

**Note on data source:** This is a curated matrix sourced from `SqlProviderFlags` and provider
builders. Verify exact behavior in XML-doc/provider flags when correctness is critical.
The **Upsert** column reflects `SqlProviderFlags.IsInsertOrUpdateSupported`.
See [Notes](#notes) section below for exceptions where the flag is true but the feature is not practically supported (ClickHouse, YDB).

---

## Capability Flags by Provider

| Provider | `ProviderName` constant | MERGE | CTE | Window Functions | APPLY / LATERAL | Upsert | OUTPUT / RETURNING | Bulk Copy |
|---|---|:---:|:---:|:---:|:---:|:---:|:---|:---|
| Access | `ProviderName.Access` | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| ClickHouse | `ProviderName.ClickHouse` | ❌ | ✅ | ✅ | ❌ | ❌ | ⚠️ limited | ✅ native |
| DB2 | `ProviderName.DB2` | ✅ | ✅ | ✅ | ❌ | ✅ | ❌ | ⚠️ opt-in |
| DuckDB | `ProviderName.DuckDB` | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ RETURNING | ✅ native |
| Firebird | `ProviderName.Firebird` | ✅ | ✅ | ✅ v3+ | ✅ v4+ | ✅ | ✅ RETURNING | ❌ |
| Informix | `ProviderName.Informix` | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ native |
| MySQL / MariaDB | `ProviderName.MySql` | ❌ | ✅ v8.0+ | ✅ v8.0+ | ✅ v8.0+ | ✅ | ❌ MySQL; ⚠️ MariaDB only | ⚠️ opt-in |
| Oracle | `ProviderName.Oracle` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ RETURNING INTO | ⚠️ opt-in |
| PostgreSQL | `ProviderName.PostgreSQL` | ✅ v15+ | ✅ | ✅ | ✅ v9.3+ | ✅ v9.5+ | ✅ RETURNING | ⚠️ opt-in |
| SAP HANA | `ProviderName.SapHana` | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ⚠️ opt-in |
| SQL Server CE | `ProviderName.SqlCe` | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| SQLite | `ProviderName.SQLite` | ❌ | ✅ | ✅ | ❌ | ✅ | ✅ RETURNING v3.35+ | ❌ |
| SQL Server | `ProviderName.SqlServer` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ OUTPUT | ✅ native |
| Sybase | `ProviderName.Sybase` | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ | ⚠️ opt-in |
| YDB | `ProviderName.Ydb` | ❌ | ✅ | ✅ | ❌ | ❌ | ✅ RETURNING | ✅ native |

---

## Column Definitions

**MERGE**
SQL MERGE statement (combined INSERT/UPDATE/DELETE in a single DML).
Exposed as `LinqExtensions.Merge()` → fluent builder → `Merge()` terminal.
Providers that do not support it throw `LinqToDBException` at query generation time.

**CTE**
`WITH` clause (Common Table Expressions), including non-recursive CTEs.
Exposed as `LinqExtensions.AsCte()`. Recursive CTEs are supported on a subset of providers;
on providers that support recursive CTEs some may have additional restrictions on using
JOIN conditions inside the recursive part - check the matrix row for your provider.

**Window Functions**
`OVER (PARTITION BY ... ORDER BY ...)` analytical functions.
Exposed via `Sql.Ext` window function helpers.

**APPLY / LATERAL**
`CROSS APPLY` / `OUTER APPLY` (SQL Server, Oracle, SAP HANA) or
`LATERAL JOIN` (PostgreSQL v9.3+, MySQL v8.0+, Firebird v4+).
Used internally by LinqToDB for correlated subqueries and `LoadWith`.

**Upsert**
`INSERT OR UPDATE` semantics (single statement that inserts or updates).
Each provider translates the operation to its native syntax (e.g., `ON DUPLICATE KEY UPDATE` for MySQL,
`ON CONFLICT DO UPDATE` for PostgreSQL/SQLite, `MERGE` for SQL Server/DB2/Oracle/Firebird).
Exposed as `DataExtensions.InsertOrUpdate()` / `InsertOrReplace()`.

**OUTPUT / RETURNING**
Ability to return column values from INSERT / UPDATE / DELETE statements.
Exposed as `LinqExtensions.Output()` / `OutputInto()`.
SQL Server emits `OUTPUT` into a table variable; other providers use `RETURNING`.
The exact syntax and supported DML operations vary by provider.
For combined rows such as MySQL / MariaDB, treat the cell as a family-level warning:
MySQL does not expose a general `RETURNING` feature, while MariaDB supports `RETURNING`
only for specific statement/version combinations. Check the CRUD guide for the operation
you are using.

**Bulk Copy**
Native provider-level bulk insert, bypassing row-by-row INSERT overhead.
Exposed as `DataContextExtensions.BulkCopy()` / `BulkCopyAsync()` with `BulkCopyOptions.BulkCopyType`.
`✅ native` - provider uses a native driver API by default (`BulkCopyType.ProviderSpecific` is the default).
`⚠️ opt-in` - native bulk copy is available but requires explicitly setting `BulkCopyType.ProviderSpecific`;
  the default is `BulkCopyType.MultipleRows` (multi-row INSERT batches).
`❌` - no native bulk copy; only `MultipleRows` (multi-row INSERT) or `RowByRow` modes are available.

---

## Notes

**Upsert Limitations**

The `SqlProviderFlags.IsInsertOrUpdateSupported` flag may be `true` for providers where the actual implementation is not supported at runtime:

- **ClickHouse Upsert**: `InsertOrUpdate()` / `InsertOrReplace()` are not supported and will throw
  `LinqToDBException` at query build time - ClickHouse cannot provide the row-count feedback
  required for correct upsert emulation. Use provider-specific alternatives instead.

- **YDB Upsert**: `InsertOrUpdate()` / `InsertOrReplace()` are not implemented for YDB and will
  throw `LinqToDBException` at runtime. Do not call these methods against YDB.

---

Other Notes

- **MariaDB**: shares the `MySql` version flags; MariaDB has added some features
  earlier than MySQL (e.g. window functions since MariaDB 10.2, CTEs since 10.2).
  LinqToDB uses the same flags for both - check your actual server version.
  `RETURNING` support is statement-specific: `DELETE ... RETURNING` is available on
  MariaDB 10.0+, while `INSERT ... RETURNING` requires MariaDB 10.5+. Do not infer
  general MySQL-family `OUTPUT / RETURNING` support from a MariaDB-only case.

- **PostgreSQL MERGE**: requires PostgreSQL 15 or later. The `MERGE` statement was
  standardised and added to PostgreSQL in version 15. Earlier versions will fail at
  the database level even though LinqToDB does not block generation.

- **ClickHouse**: does not support SQL transactions in the ACID sense;
  `TransactionScope` and `BeginTransaction` may silently succeed or have no effect
  depending on the adapter and ClickHouse engine.

- **Sybase Bulk Copy**: `BulkCopyType.ProviderSpecific` is available but the default is `MultipleRows`
  because the native Sybase bulk copy API has known issues with `bit` columns and identity fields.
  Set `BulkCopyType.ProviderSpecific` explicitly only if your table does not use those column types.

- **Oracle Bulk Copy**: `BulkCopyType.ProviderSpecific` (ODP.NET `OracleBulkCopy`) falls back to
  `MultipleRows` when column names require SQL identifier escaping - a known ODP.NET limitation.

- **Informix Bulk Copy**: `BulkCopyType.ProviderSpecific` is the default and uses the IDS native
  bulk copy API or the DB2 bulk copy API depending on the adapter in use.
  Falls back to `MultipleRows` if neither adapter is available at runtime.

- **Version-conditional capabilities**: LinqToDB detects the server version at provider
  initialisation time and adjusts feature support accordingly.
  The version you pass to `DataOptions.UseXxx(...)` determines which features are active.

---

## See also

- [`docs/architecture.md`](05-architecture.md) - extended architectural model.
- [`docs/ai-tags.md`](06-agent-antipatterns-and-ai-tags.md) - machine-readable metadata specification.
- [`docs/agent-antipatterns.md`](06-agent-antipatterns-and-ai-tags.md) - common mistakes and how to avoid them.
- [`docs/provider-setup.md`](07-provider-configuration.md) - provider configuration reference (ProviderName constants, UseXxx methods, NuGet packages).

<!-- Generated from: Source/Skills/linq2db/docs/provider-setup.md -->

# LinqToDB Provider Setup

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> You are here if you need to:
> - select the correct `UseXxx` method and `ProviderName` constant for a specific database
> - identify which NuGet driver package to install
> - configure connection string, existing `DbConnection`, or `DbDataSource`

AI-facing reference: how to configure each supported database provider.
Use this to select the correct `ProviderName` constant, call the right `DataOptions.UseXxx()` method,
and install the required NuGet packages.

All `UseXxx` methods are extension methods on `DataOptions` (namespace `LinqToDB`,
source: `DataOptionsExtensions`). They return a new `DataOptions` instance - `DataOptions` is immutable.
The primary constant (e.g. `ProviderName.SqlServer`) triggers auto-detection of dialect and driver;
versioned or driver-specific constants (e.g. `ProviderName.SqlServer2022`) pin the exact variant
and are set automatically after detection - use the primary constant for initial configuration.

Every `UseXxx` method also accepts an optional last parameter: a delegate of the form
`Func<TOptions, TOptions>` for provider-specific advanced settings. Omit it for standard setups.

---

## Configuration patterns

The patterns below apply to every provider. Examples use SQL Server - substitute `UseSqlServer`
with the appropriate `UseXxx` method for your provider.

### Connection string (standard)
```csharp
var options = new DataOptions()
    .UseSqlServer("Server=.;Database=MyDb;Trusted_Connection=True;");
```

### Existing `DbConnection`
```csharp
// disposeConnection: false - DataConnection will not close or dispose the supplied connection
var options = new DataOptions()
    .UseConnection(SqlServerTools.GetDataProvider(), existingConnection, disposeConnection: false);
```

### Existing `DbTransaction`
```csharp
var options = new DataOptions()
    .UseTransaction(SqlServerTools.GetDataProvider(), existingConnection, existingTransaction);
```

### Connection factory (DI / pooling)
```csharp
var options = new DataOptions()
    .UseConnectionFactory(SqlServerTools.GetDataProvider(), _ =>
        new SqlConnection(connectionString));
```

### Provider-specific options callback
```csharp
var options = new DataOptions()
    .UseSqlServer(connectionString, o => o
        .WithBulkCopyType(SqlServerBulkCopyType.ProviderSpecific));
```
The `optionSetter` overload is available on every `UseXxx` method and exposes
provider-specific settings (dialect, driver variant, bulk-copy type, etc.).

For tracing, retry policies, interceptors, and member translators see `docs/configuration.md`.

---

## Quick Reference

| Provider | Primary `ProviderName` | `DataOptions` method | ADO.NET driver NuGet |
|---|---|---|---|
| SQL Server | `ProviderName.SqlServer` | `UseSqlServer(...)` | `Microsoft.Data.SqlClient` |
| Oracle | `ProviderName.Oracle` | `UseOracle(...)` | `Oracle.ManagedDataAccess.Core` |
| PostgreSQL | `ProviderName.PostgreSQL` | `UsePostgreSQL(...)` | `Npgsql` |
| MySQL / MariaDB | `ProviderName.MySql` | `UseMySql(...)` | `MySqlConnector` |
| SQLite | `ProviderName.SQLite` | `UseSQLite(...)` | `Microsoft.Data.Sqlite` |
| Access | `ProviderName.Access` | `UseAccess(...)` | `System.Data.OleDb` |
| ClickHouse | `ProviderName.ClickHouse` | `UseClickHouse(...)` | `ClickHouse.Driver` |
| DB2 | `ProviderName.DB2` | `UseDB2(...)` | `Net.IBM.Data.Db2` |
| DuckDB | `ProviderName.DuckDB` | `UseDuckDB(...)` | `DuckDB.NET.Data.Full` |
| Firebird | `ProviderName.Firebird` | `UseFirebird(...)` | `FirebirdSql.Data.FirebirdClient` |
| Informix | `ProviderName.Informix` | `UseInformix(...)` | ⚠️ see notes |
| SAP HANA | `ProviderName.SapHana` | `UseSapHana(...)` | ⚠️ see notes |
| SQL Server CE | `ProviderName.SqlCe` | `UseSqlCe(...)` | ⚠️ .NET Framework only |
| Sybase / SAP ASE | `ProviderName.Sybase` | `UseAse(...)` | `AdoNetCore.AseClient` |
| YDB | `ProviderName.Ydb` | `YdbTools.CreateDataConnection(...)` ⚠️ no `UseYdb()` - see notes | `Ydb.Sdk` |

All providers require the `linq2db` NuGet package. The ADO.NET driver is installed separately.

---

## Access

**`ProviderName` constants**

| Constant | String value |
|---|---|
| `ProviderName.Access` | `"Access"` |
| `ProviderName.AccessOdbc` | `"Access.Odbc"` |
| `ProviderName.AccessJetOleDb` | `"Access.Jet.OleDb"` |
| `ProviderName.AccessJetOdbc` | `"Access.Jet.Odbc"` |
| `ProviderName.AccessAceOleDb` | `"Access.Ace.OleDb"` |
| `ProviderName.AccessAceOdbc` | `"Access.Ace.Odbc"` |

**Configuration**

```csharp
var options = new DataOptions()
    .UseAccess("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\\mydb.accdb");
```

**Method signature**

```csharp
UseAccess(string connectionString,
          AccessVersion  version  = AccessVersion.AutoDetect,
          AccessProvider provider = AccessProvider.AutoDetect)
```

**`AccessVersion` enum**

| Value | Description |
|---|---|
| `AutoDetect` | Detect engine automatically (default) |
| `Jet` | Legacy JET engine (.mdb) |
| `Ace` | ACE engine - Access 2007+ (.accdb) |

**`AccessProvider` enum**

| Value | Description |
|---|---|
| `AutoDetect` | Detect provider automatically (default) |
| `OleDb` | OLE DB provider |
| `ODBC` | ODBC provider |

**NuGet packages**

```
linq2db
System.Data.OleDb          # Windows only
```

> ⚠️ Access is Windows-only. OLE DB is not available on Linux or macOS.

---

## ClickHouse

**`ProviderName` constants**

| Constant | String value |
|---|---|
| `ProviderName.ClickHouse` | `"ClickHouse"` |
| `ProviderName.ClickHouseDriver` | `"ClickHouse.Driver"` |
| `ProviderName.ClickHouseOctonica` | `"ClickHouse.Octonica"` |
| `ProviderName.ClickHouseMySql` | `"ClickHouse.MySql"` |

**Configuration**

```csharp
var options = new DataOptions()
    .UseClickHouse("Host=localhost;Port=8123;Database=default");
```

**Method signature**

```csharp
UseClickHouse(string connectionString,
              ClickHouseProvider provider = ClickHouseProvider.AutoDetect)
```

**`ClickHouseProvider` enum**

| Value | Description | NuGet |
|---|---|---|
| `AutoDetect` | Use first available driver (default) | - |
| `ClickHouseDriver` | Official ClickHouse C# driver (**recommended**) | `ClickHouse.Driver` |
| `Octonica` | Octonica.ClickHouseClient | `Octonica.ClickHouseClient` |
| `MySqlConnector` | MySQL protocol via MySqlConnector | `MySqlConnector` |

**NuGet packages**

```
linq2db
ClickHouse.Driver               # recommended (official driver)
# OR
Octonica.ClickHouseClient
# OR
MySqlConnector                  # MySQL protocol; limited feature set
```

> ⚠️ ClickHouse does not support ACID transactions. `TransactionScope` / `BeginTransaction` may
> succeed silently with no effect depending on the engine and driver used.

---

## DB2

**`ProviderName` constants**

| Constant | String value |
|---|---|
| `ProviderName.DB2` | `"DB2"` |
| `ProviderName.DB2LUW` | `"DB2.LUW"` |
| `ProviderName.DB2zOS` | `"DB2.z/OS"` |

**Configuration**

```csharp
var options = new DataOptions()
    .UseDB2("Server=localhost:50000;Database=mydb;UID=user;PWD=pass");
```

**Method signature**

```csharp
UseDB2(string connectionString,
       DB2Version version = DB2Version.AutoDetect)
```

**`DB2Version` enum**

| Value | Description |
|---|---|
| `AutoDetect` | Detect server type automatically (default) |
| `LUW` | DB2 LUW (Linux, Unix, Windows) |
| `zOS` | DB2 for z/OS |

**NuGet packages**

```
linq2db
Net.IBM.Data.Db2              # .NET on Windows
Net.IBM.Data.Db2-lnx          # .NET on Linux
Net.IBM.Data.Db2-osx          # .NET on macOS
IBM.Data.DB.Provider          # .NET Framework
```

> DB2 for IBM i (iSeries / AS/400) is not supported. Use the community package
> [`linq2db4iSeries`](https://www.nuget.org/packages/linq2db4iSeries) instead.

---

## DuckDB

**`ProviderName` constants**

| Constant | String value |
|---|---|
| `ProviderName.DuckDB` | `"DuckDB"` |

**Configuration**

```csharp
var options = new DataOptions()
    .UseDuckDB("Data Source=mydb.duckdb");
```

**Method signature**

```csharp
UseDuckDB(string connectionString)
```

**Provider-specific options**

```csharp
UseDuckDB(string connectionString,
          Func<DuckDBOptions, DuckDBOptions>? optionSetter = null)
```

`DuckDBOptions` currently exposes the default bulk-copy mode. The default is
`BulkCopyType.ProviderSpecific`, which uses the native DuckDB Appender when possible.

**NuGet packages**

```
linq2db
DuckDB.NET.Data.Full
```

---

## Firebird

**`ProviderName` constants**

| Constant | String value |
|---|---|
| `ProviderName.Firebird` | `"Firebird"` |
| `ProviderName.Firebird25` | `"Firebird.2.5"` |
| `ProviderName.Firebird3` | `"Firebird.3"` |
| `ProviderName.Firebird4` | `"Firebird.4"` |
| `ProviderName.Firebird5` | `"Firebird.5"` |

**Configuration**

```csharp
var options = new DataOptions()
    .UseFirebird("DataSource=localhost;Database=/var/db/mydb.fdb;User=SYSDBA;Password=masterkey");
```

**Method signature**

```csharp
UseFirebird(string connectionString,
            FirebirdVersion version = FirebirdVersion.AutoDetect)
```

**`FirebirdVersion` enum**

| Value | Description |
|---|---|
| `AutoDetect` | Detect version automatically (default) |
| `v25` | Firebird 2.5+ |
| `v3` | Firebird 3+ |
| `v4` | Firebird 4+ |
| `v5` | Firebird 5+ |

**NuGet packages**

```
linq2db
FirebirdSql.Data.FirebirdClient
```

---

## Informix

**`ProviderName` constants**

| Constant | String value |
|---|---|
| `ProviderName.Informix` | `"Informix"` |
| `ProviderName.InformixDB2` | `"Informix.DB2"` |

**Configuration**

```csharp
var options = new DataOptions()
    .UseInformix("Server=myserver:9088;Database=mydb;UID=user;PWD=pass");
```

**Method signature**

```csharp
UseInformix(string connectionString,
            InformixProvider provider = InformixProvider.AutoDetect)
```

**`InformixProvider` enum**

| Value | Description |
|---|---|
| `AutoDetect` | Detect provider automatically (default) |
| `Informix` | `IBM.Data.Informix` driver |
| `DB2` | `IBM.Data.DB2` IDS driver |

**NuGet packages**

```
linq2db
# IBM.Data.Informix - .NET Framework only; no public NuGet for .NET Core / .NET 5+
# Obtain the IBM Data Server driver from the IBM Support portal and reference the DLL locally.
# For .NET Core: use InformixProvider.DB2 with Net.IBM.Data.Db2 (see DB2 section above).
```

> ⚠️ No public NuGet package for `IBM.Data.Informix` exists on .NET Core / .NET 5+.
> On those targets configure `InformixProvider.DB2` and install `Net.IBM.Data.Db2` instead.

---

## MySQL / MariaDB

**`ProviderName` constants**

| Constant | String value |
|---|---|
| `ProviderName.MySql` | `"MySql"` |
| `ProviderName.MySql57` | `"MySql.5.7"` |
| `ProviderName.MySql80` | `"MySql.8.0"` |
| `ProviderName.MariaDB10` | `"MariaDB.10"` |

Combined dialect + driver constants (set automatically after detection, not needed for initial config):
`MySql57MySqlData`, `MySql57MySqlConnector`, `MySql80MySqlData`, `MySql80MySqlConnector`,
`MariaDB10MySqlData`, `MariaDB10MySqlConnector`.

**Configuration**

```csharp
var options = new DataOptions()
    .UseMySql("Server=localhost;Database=mydb;User ID=user;Password=pass");
```

**Method signature**

```csharp
UseMySql(string connectionString,
         MySqlVersion  dialect  = MySqlVersion.AutoDetect,
         MySqlProvider provider = MySqlProvider.AutoDetect)
```

**`MySqlVersion` enum**

| Value | Description |
|---|---|
| `AutoDetect` | Detect version automatically (default) |
| `MySql57` | MySQL 5.7+ dialect |
| `MySql80` | MySQL 8.0+ dialect |
| `MariaDB10` | MariaDB 10+ dialect |

**`MySqlProvider` enum**

| Value | Description | NuGet |
|---|---|---|
| `AutoDetect` | Use first available driver (default) | - |
| `MySqlConnector` | MySqlConnector (**recommended**) | `MySqlConnector` |
| `MySqlData` | MySql.Data (Oracle connector) | `MySql.Data` |

**NuGet packages**

```
linq2db
MySqlConnector          # recommended
# OR
MySql.Data
```

---

## Oracle

**`ProviderName` constants**

| Constant | String value |
|---|---|
| `ProviderName.Oracle` | `"Oracle"` |
| `ProviderName.OracleManaged` | `"Oracle.Managed"` |
| `ProviderName.OracleNative` | `"Oracle.Native"` |
| `ProviderName.OracleDevart` | `"Oracle.Devart"` |
| `ProviderName.Oracle11Managed` | `"Oracle.11.Managed"` |
| `ProviderName.Oracle11Native` | `"Oracle.11.Native"` |
| `ProviderName.Oracle11Devart` | `"Oracle.11.Devart"` |

**Configuration**

```csharp
var options = new DataOptions()
    .UseOracle("Data Source=localhost:1521/MYDB;User Id=user;Password=pass");
```

**Method signature**

```csharp
UseOracle(string connectionString,
          OracleVersion  version  = OracleVersion.AutoDetect,
          OracleProvider provider = OracleProvider.AutoDetect)
```

**`OracleVersion` enum**

| Value | Description |
|---|---|
| `AutoDetect` | Detect server version automatically (default) |
| `v11` | Oracle 11g dialect |
| `v12` | Oracle 12c+ dialect |

**`OracleProvider` enum**

| Value | Description | NuGet |
|---|---|---|
| `AutoDetect` | Try Managed → Devart → Native (default) | - |
| `Managed` | ODP.NET managed driver (**recommended**) | `Oracle.ManagedDataAccess.Core` (.NET) / `Oracle.ManagedDataAccess` (.NET Fx) |
| `Native` | ODP.NET native driver (.NET Framework only) | `Oracle.DataAccess` (legacy) |
| `Devart` | Devart dotConnect for Oracle | `Devart.Data.Oracle` |

**NuGet packages**

```
linq2db
Oracle.ManagedDataAccess.Core   # .NET (recommended)
# OR
Oracle.ManagedDataAccess        # .NET Framework
```

---

## PostgreSQL

**`ProviderName` constants**

| Constant | String value |
|---|---|
| `ProviderName.PostgreSQL` | `"PostgreSQL"` |
| `ProviderName.PostgreSQL92` | `"PostgreSQL.9.2"` |
| `ProviderName.PostgreSQL93` | `"PostgreSQL.9.3"` |
| `ProviderName.PostgreSQL95` | `"PostgreSQL.9.5"` |
| `ProviderName.PostgreSQL13` | `"PostgreSQL.13"` |
| `ProviderName.PostgreSQL15` | `"PostgreSQL.15"` |
| `ProviderName.PostgreSQL18` | `"PostgreSQL.18"` |

**Configuration**

```csharp
var options = new DataOptions()
    .UsePostgreSQL("Host=localhost;Database=mydb;Username=user;Password=pass");
```

**Method signature**

```csharp
UsePostgreSQL(string connectionString,
              PostgreSQLVersion dialect = PostgreSQLVersion.AutoDetect)
```

**`PostgreSQLVersion` enum**

| Value | Description |
|---|---|
| `AutoDetect` | Detect version automatically (default) |
| `v92` | PostgreSQL 9.2+ dialect |
| `v93` | PostgreSQL 9.3+ dialect |
| `v95` | PostgreSQL 9.5+ dialect |
| `v13` | PostgreSQL 13+ dialect |
| `v15` | PostgreSQL 15+ dialect |
| `v18` | PostgreSQL 18+ dialect |

**NuGet packages**

```
linq2db
Npgsql
```

---

## SAP HANA

**`ProviderName` constants**

| Constant | String value |
|---|---|
| `ProviderName.SapHana` | `"SapHana"` |
| `ProviderName.SapHanaNative` | `"SapHana.Native"` |
| `ProviderName.SapHanaOdbc` | `"SapHana.Odbc"` |

**Configuration**

```csharp
var options = new DataOptions()
    .UseSapHana("Server=localhost:30015;UserID=user;Password=pass");
```

**Method signature**

```csharp
UseSapHana(string connectionString,
           SapHanaProvider provider = SapHanaProvider.AutoDetect)
```

**`SapHanaProvider` enum**

| Value | Description |
|---|---|
| `AutoDetect` | Detect provider automatically (default) |
| `Unmanaged` | Native `Sap.Data.Hana` / `Sap.Data.Hana.Core` assembly |
| `ODBC` | HDBODBC / HDBODBC32 ODBC driver |

**NuGet packages**

```
linq2db
# No public NuGet package exists for the SAP HANA ADO.NET driver.
# Obtain Sap.Data.Hana.v4.5.dll or Sap.Data.Hana.Net.v8.0.dll from the SAP HANA Client
# installation and reference the DLL directly.
# ODBC alternative: System.Data.Odbc (built-in) + HDBODBC driver from SAP.
```

> ⚠️ The SAP HANA ADO.NET assembly (`Sap.Data.Hana`) is not available on NuGet.
> It must be obtained from the SAP HANA Client distribution (SAP Software Downloads or the client
> installer) and referenced as a local assembly. The ODBC variant requires only `System.Data.Odbc`
> (built into .NET) plus the HDBODBC driver from SAP.

---

## SQLite

**`ProviderName` constants**

| Constant | String value |
|---|---|
| `ProviderName.SQLite` | `"SQLite"` |
| `ProviderName.SQLiteMS` | `"SQLite.MS"` |
| `ProviderName.SQLiteClassic` | `"SQLite.Classic"` |

**Configuration**

```csharp
var options = new DataOptions()
    .UseSQLite("Data Source=mydb.sqlite");
```

**Method signature**

```csharp
UseSQLite(string connectionString,
          SQLiteProvider provider = SQLiteProvider.AutoDetect)
```

**`SQLiteProvider` enum**

| Value | Description | NuGet |
|---|---|---|
| `AutoDetect` | Detect provider automatically (default) | - |
| `Microsoft` | `Microsoft.Data.Sqlite` (**recommended**) | `Microsoft.Data.Sqlite` |
| `System` | `System.Data.SQLite` (classic) | `System.Data.SQLite` |

**NuGet packages**

```
linq2db
Microsoft.Data.Sqlite           # recommended
# OR
System.Data.SQLite
```

---

## SQL Server

**`ProviderName` constants**

| Constant | String value |
|---|---|
| `ProviderName.SqlServer` | `"SqlServer"` |
| `ProviderName.SqlServer2005` | `"SqlServer.2005"` |
| `ProviderName.SqlServer2008` | `"SqlServer.2008"` |
| `ProviderName.SqlServer2012` | `"SqlServer.2012"` |
| `ProviderName.SqlServer2014` | `"SqlServer.2014"` |
| `ProviderName.SqlServer2016` | `"SqlServer.2016"` |
| `ProviderName.SqlServer2017` | `"SqlServer.2017"` |
| `ProviderName.SqlServer2019` | `"SqlServer.2019"` |
| `ProviderName.SqlServer2022` | `"SqlServer.2022"` |
| `ProviderName.SqlServer2025` | `"SqlServer.2025"` |

**Configuration**

```csharp
var options = new DataOptions()
    .UseSqlServer("Server=.;Database=MyDb;Integrated Security=true;TrustServerCertificate=true");
```

**Method signature**

```csharp
UseSqlServer(string connectionString,
             SqlServerVersion  dialect  = SqlServerVersion.AutoDetect,
             SqlServerProvider provider = SqlServerProvider.AutoDetect)
```

**`SqlServerVersion` enum**

| Value | Description |
|---|---|
| `AutoDetect` | Detect version automatically (default) |
| `v2005` | SQL Server 2005 |
| `v2008` | SQL Server 2008 |
| `v2012` | SQL Server 2012 |
| `v2014` | SQL Server 2014 |
| `v2016` | SQL Server 2016 |
| `v2017` | SQL Server 2017 |
| `v2019` | SQL Server 2019 |
| `v2022` | SQL Server 2022 |
| `v2025` | SQL Server 2025 |

**`SqlServerProvider` enum**

| Value | Description | NuGet |
|---|---|---|
| `AutoDetect` | Use first available driver (default) | - |
| `MicrosoftDataSqlClient` | `Microsoft.Data.SqlClient` (**recommended**) | `Microsoft.Data.SqlClient` |
| `SystemDataSqlClient` | `System.Data.SqlClient` (legacy) | `System.Data.SqlClient` |

**NuGet packages**

```
linq2db
Microsoft.Data.SqlClient        # recommended
# OR
System.Data.SqlClient           # legacy
```

---

## SQL Server CE

**`ProviderName` constants**

| Constant | String value |
|---|---|
| `ProviderName.SqlCe` | `"SqlCe"` |

**Configuration**

```csharp
var options = new DataOptions()
    .UseSqlCe("Data Source=mydb.sdf");
```

**Method signature**

```csharp
UseSqlCe(string connectionString)
```

**NuGet packages**

```
linq2db
# No NuGet package. System.Data.SqlServerCe.dll is part of the SQL Server CE 3.5/4.0 runtime.
```

> ⚠️ SQL Server CE is **.NET Framework only** and is end-of-life. It is not supported on
> .NET Core / .NET 5+.

---

## Sybase / SAP ASE

**`ProviderName` constants**

| Constant | String value |
|---|---|
| `ProviderName.Sybase` | `"Sybase"` |
| `ProviderName.SybaseManaged` | `"Sybase.Managed"` |

**Configuration**

```csharp
var options = new DataOptions()
    .UseAse("Data Source=myserver;Port=5000;Database=mydb;Uid=user;Pwd=pass");
```

**Method signature**

```csharp
UseAse(string connectionString,
       SybaseProvider provider = SybaseProvider.AutoDetect)
```

**`SybaseProvider` enum**

| Value | Description | NuGet |
|---|---|---|
| `AutoDetect` | Detect provider automatically (default) | - |
| `DataAction` | DataAction managed provider (**recommended**) | `AdoNetCore.AseClient` |
| `Unmanaged` | SAP native unmanaged driver | local `Sybase.AdoNet45.AseClient.dll` |

**NuGet packages**

```
linq2db
AdoNetCore.AseClient            # recommended managed provider
```

> ⚠️ Bulk copy with `BulkCopyType.ProviderSpecific` has known issues with `bit` columns and
> identity fields. The default bulk copy mode is `MultipleRows`.

---

## YDB

**`ProviderName` constants**

| Constant | String value |
|---|---|
| `ProviderName.Ydb` | `"YDB"` |

**Configuration**

YDB does not have a `UseYdb()` extension method. Use the `YdbTools` factory class instead:

```csharp
// Option 1 - factory method (returns DataConnection directly)
using var db = YdbTools.CreateDataConnection("Host=grpcs://localhost:2135;Database=/local");

// Option 2 - DataOptions (use with DI, DataContext, or pooling)
var options = new DataOptions()
    .UseConnectionString(YdbTools.GetDataProvider(), connectionString);
```

**NuGet packages**

```
linq2db
Ydb.Sdk
```

> ⚠️ `InsertOrUpdate()` / `InsertOrReplace()` are not supported on YDB and will throw at runtime.

---

## See Also

- [`docs/provider-capabilities.md`](07-provider-configuration.md) - SQL feature support matrix per provider
  (MERGE, CTE, window functions, bulk copy, OUTPUT/RETURNING, upsert).
- [`docs/architecture.md`](05-architecture.md) - architecture overview, translation pipeline, execution model.
- [`docs/agent-antipatterns.md`](06-agent-antipatterns-and-ai-tags.md) - common mistakes and how to avoid them.

<!-- Generated from: Source/Skills/linq2db/docs/configuration.md -->

# LinqToDB Configuration and Extensibility

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> You are here if you need to:
> - configure `DataOptions` for a provider
> - add SQL tracing, retry policy, or interceptors
> - register custom member translators
> - understand `DataOptions` lifetime and immutability

This document describes the supported `DataOptions` configuration patterns and extensibility
points available to package consumers.

`DataOptions` is immutable - every `UseXxx` method returns a new instance. Configure once,
store as a singleton, and pass to every `DataConnection` / `DataContext` constructor.

---

## Quick pattern

```csharp
static readonly DataOptions _options = new DataOptions()
    .UseSqlServer("connection string")   // provider + connection string
    .UseTracing(TraceLevel.Info, t =>    // optional: SQL logging
        Console.WriteLine(t.SqlText))
    .UseRetryPolicy(new TransientRetryPolicy()); // optional: auto-retry

using var db = new DataConnection(_options);
```

---

## Connection configuration

### Standard setup - connection string

```csharp
// Use a provider-specific UseXxx method (recommended)
var options = new DataOptions()
    .UseSqlServer("Server=.;Database=MyDb;Trusted_Connection=True;");
```

See `docs/provider-setup.md` for the full list of `UseXxx` methods per provider.

### Reuse an existing DbConnection

```csharp
// DataConnection does NOT dispose the connection when disposeConnection is false
var options = new DataOptions()
    .UseConnection(dataProvider, existingConnection, disposeConnection: false);
```

Use this pattern with connection pooling wrappers (e.g. MiniProfiler) or when you need
to share a connection across multiple `DataConnection` instances.

### Connection factory

```csharp
// Called once per DataConnection instance
var options = new DataOptions()
    .UseConnectionFactory(dataProvider, opts =>
        new SqlConnection(opts.ConnectionOptions.ConnectionString));
```

Prefer `UseConnectionFactory` over `UseConnection` when you need to create a new physical
connection for each `DataConnection` instance but still need to customize the `DbConnection`
object before use (e.g. to set access tokens).

### Before / after connection open hooks

```csharp
var options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseBeforeConnectionOpened(cn =>
    {
        ((SqlConnection)cn).AccessToken = GetToken();
    })
    .UseAfterConnectionOpened(cn =>
    {
        // executed after Open() completes
    });
```

Async overloads are available: `.UseBeforeConnectionOpened(sync, async)`.

---

## Tracing and logging

`UseTracing` receives a `TraceInfo` object that exposes the generated SQL text, parameters,
execution time, and exception (if any).

```csharp
var options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseTracing(TraceLevel.Info, traceInfo =>
    {
        if (traceInfo.TraceInfoStep == TraceInfoStep.BeforeExecute)
            logger.LogDebug(traceInfo.SqlText);
        else if (traceInfo.Exception != null)
            logger.LogError(traceInfo.Exception, "Query failed");
    });
```

`TraceLevel` is `System.Diagnostics.TraceLevel` (not a LinqToDB-defined type). LinqToDB only
distinguishes `Off` from every other level: `Off` disables the `onTrace`/`WriteTrace` callback
entirely, while `Error`, `Warning`, `Info`, and `Verbose` all receive the same events - LinqToDB
itself only ever tags an event `Info` (normal execution steps) or `Error` (an exception occurred);
it never produces `Warning`- or `Verbose`-tagged events, and `TraceInfo.SqlText` always includes
the command text and parameter values regardless of the level passed to `UseTracing`. Pick `Info`
(or any non-`Off` level) to receive tracing; pick `Off` to disable it.

| Level | What is traced |
|---|---|
| `Off` | nothing |
| any other value (`Error`, `Warning`, `Info`, `Verbose`) | all SQL statements + parameter values, and exceptions - LinqToDB does not vary behavior by these four levels |

String-callback overload (for legacy `TraceSwitch`-based setups):

```csharp
options.UseTraceWith((message, category, level) =>
    Trace.WriteLine(message, category));
```

---

## Retry policies

LinqToDB does not retry by default. Use `UseRetryPolicy` to enable automatic retries for
transient failures (e.g. network blips, connection pool exhaustion).

```csharp
// Built-in exponential back-off with defaults (5 retries, max 30 s delay)
var options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseDefaultRetryPolicyFactory();

// Built-in policy with custom parameters
var options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseMaxRetryCount(3)
    .UseMaxDelay(TimeSpan.FromSeconds(10));

// Custom IRetryPolicy implementation
var options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseRetryPolicy(new MyRetryPolicy());
```

`IRetryPolicy` has four methods to implement: `Execute<TResult>(Func<TResult> operation)`,
`Execute(Action operation)`, `ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)`,
and `ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)`.

---

## Configuration via external files

### JSON (`appsettings.json`)

LinqToDB has no built-in JSON-specific configuration API. For a single connection, the standard
.NET hosting pattern is enough: read a connection string via `IConfiguration`, then pass it to
`UseConnectionString`.

```csharp
var connectionString = configuration.GetConnectionString("Default");

var options = new DataOptions()
    .UseSqlServer(connectionString);
```

For named-configuration semantics equivalent to `app.config` (multiple connections,
`DataConnection.DefaultSettings`, `new DataConnection("Name")` / `UseConfiguration("Name")`),
implement `ILinqToDBSettings` yourself against `IConfiguration` - this is the same
provider-agnostic seam `LinqToDBSection` implements for XML, and it does not require
`linq2db.Compat` (that package is specific to reading `System.Configuration.ConfigurationManager`
XML sections):

```csharp
public class JsonLinqToDBSettings(IConfiguration configuration) : ILinqToDBSettings
{
    public IEnumerable<IDataProviderSettings> DataProviders => [];
    public string? DefaultConfiguration => "Default";
    public string? DefaultDataProvider  => null;

    public IEnumerable<IConnectionStringSettings> ConnectionStrings =>
        configuration.GetSection("ConnectionStrings").GetChildren()
            .Select(s => new ConnectionStringSettings(s.Key, s.Value!, providerName: "SqlServer"));
}

DataConnection.DefaultSettings = new JsonLinqToDBSettings(configuration);

using var db = new DataConnection("Default");
```

Provider-name resolution per connection is application-specific - `appsettings.json`'s
`ConnectionStrings` section has no standard provider-name field, so map it however the
application's JSON shape represents it.

### Legacy XML (`app.config` / `web.config`)

`ILinqToDBSettings`, `IConnectionStringSettings`, and the named-configuration API on
`DataConnection` (`AddConfiguration`, `AddOrSetConfiguration`, `GetConnectionString`,
`DefaultConfiguration`, `DefaultDataProvider`) are core, provider-agnostic, and work on every TFM -
without any config file, entries can be registered programmatically:

```csharp
DataConnection.AddConfiguration("MyDb", "Server=...;...", SqlServerTools.GetDataProvider());
using var db = new DataConnection("MyDb");
```

Reading the classic `<linq2db>` / `<connectionStrings>` XML sections from `app.config`/`web.config`
is a separate concern, backed by `System.Configuration.ConfigurationManager`:

- On **`net462`**, this is already compiled into `linq2db.dll` - `DataConnection.DefaultSettings`
  lazily resolves to `LinqToDBSection.Instance` automatically, so `app.config` is read with no
  extra package or startup code.
- On **`netstandard2.0`/`net8.0`+**, add the **`linq2db.Compat`** NuGet package and opt in
  explicitly at startup:

```csharp
using LinqToDB.Configuration;
DataConnection.DefaultSettings = LinqToDBSection.Instance;
```

Select a named configuration into a `DataOptions` with `UseConfiguration(string?)`
(the older `UseConfigurationString` is obsolete, scheduled for removal in v7).

---

## Interceptors

Interceptors allow viewing, modifying, or suppressing operations performed by LinqToDB.
Register with `UseInterceptor(IInterceptor)` or `UseInterceptors(IEnumerable<IInterceptor>)`;
remove with `RemoveInterceptor(instance)`. Multiple calls accumulate; existing interceptors are
not replaced.

```csharp
var options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseInterceptor(new LoggingInterceptor());
```

For the full list of interceptor interfaces, which events each one covers, and worked examples,
see [`docs/interceptors.md`](15-interceptors.md).

---

## Custom SQL translation (member translators)

`UseMemberTranslator(IMemberTranslator)` registers a `DataOptions`-level translator that extends
how LinqToDB translates .NET member expressions to SQL; remove one with
`RemoveTranslator(instance)`.

```csharp
var options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseMemberTranslator(new MyTranslator());
```

For `MyTranslator` implementation patterns, `[Sql.Expression]` / `[Sql.Function]` /
`[ExpressionMethod]` alternatives, and a caveat about the `IMemberTranslator` implementation
surface, see [`docs/extensions.md`](13-extensions.md).

---

## Mapping schema

```csharp
// Replace active schema
var options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseMappingSchema(mySchema);

// Combine with existing schema (additive)
var options = baseOptions.UseAdditionalMappingSchema(extraSchema);
```

> **Note:** Create `MappingSchema` instances once and reuse them. See anti-pattern #1 in
> `docs/agent-antipatterns.md`.

---

## Temporary context options

Use `IDataContext.UseOptions(...)` when an already-created context needs a scoped, temporary
option override.

The returned `IDisposable` restores the previous options and related context state when disposed.
Always use a `using` scope.

```csharp
using var db = new DataConnection(options);

using (db.UseOptions<DataContextOptions>(o => o.WithCommandTimeout(30)))
{
    // Command timeout override is active only inside this block.
    db.GetTable<Product>().ToList();
}

// Previous options are restored here.
```

Typed helpers are available when only one option group should change:

```csharp
using var db = new DataConnection(options);

using (db.UseLinqOptions(o => o with { DisableQueryCache = true }))
{
    db.GetTable<Product>().Where(p => p.IsActive).ToList();
}
```

Use `UseOptions` for short-lived per-context overrides, not for normal application configuration.
For regular configuration, build a reusable `DataOptions` instance once and pass it to each
`DataConnection` / `DataContext` constructor.

Connection-related overrides are limited: `UseOptions`/`UseConnectionOptions` reapply only mapping
schema and the connection interceptor. Passing a different `ConnectionString`, `ProviderName`,
`DataProvider`, `DbConnection`, `DbTransaction`, `DisposeConnection`, `ConnectionFactory`,
`DataProviderFactory`, or `OnEntityDescriptorCreated` value than the context was created with
**throws `LinqToDBException`** - these are creation-time identity settings, not silently ignored
overrides.

`UseMappingSchema(mappingSchema)` is a convenience override for temporarily replacing the context
mapping schema. It follows the same disposable-scope rule.

---

## DataProviderFactory

For advanced scenarios where the provider itself must be chosen at runtime:

```csharp
var options = new DataOptions()
    .UseConnectionString("connection string")
    .UseDataProviderFactory(connOptions =>
        DetectProvider(connOptions.ConnectionString!));
```

---

## Chaining summary

All `UseXxx` methods return `DataOptions` and can be chained. The result is a new immutable
instance at each step:

```csharp
static readonly DataOptions _options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseMappingSchema(myMappingSchema)
    .UseTracing(TraceLevel.Info, t => logger.LogDebug(t.SqlText))
    .UseDefaultRetryPolicyFactory()
    .UseInterceptor(new LoggingInterceptor());
```

---

## See also

- `docs/provider-setup.md` - provider-specific `UseXxx` methods and connection string formats
- `docs/agent-antipatterns.md` - common mistakes including `MappingSchema` reuse
- `docs/interceptors.md` - full interceptor interface list and worked examples
- `docs/extensions.md` - `[Sql.Expression]` / `[Sql.Function]` / `[ExpressionMethod]` and `IMemberTranslator`
- `docs/translatable-methods.md` - standard .NET methods translated to SQL
- [`DataOptions` API reference](16-xml-doc.md)
