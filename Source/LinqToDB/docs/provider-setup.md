# LinqToDB Provider Setup

> **Required:** Read [`AGENT_GUIDE.md`](../AGENT_GUIDE.md) before any implementation. This file contains global rules, required namespaces, architecture constraints, and documentation navigation.

> You are here if you need to:
> - select the correct `UseXxx` method and `ProviderName` constant for a specific database
> - identify which NuGet driver package to install
> - configure connection string, existing `DbConnection`, or `DbDataSource`

AI-facing reference: how to configure each supported database provider.
Use this to select the correct `ProviderName` constant, call the right `DataOptions.UseXxx()` method,
and install the required NuGet packages.

All `UseXxx` methods are extension methods on `DataOptions` (namespace `LinqToDB`,
source: `DataOptionsExtensions`). They return a new `DataOptions` instance — `DataOptions` is immutable.
The primary constant (e.g. `ProviderName.SqlServer`) triggers auto-detection of dialect and driver;
versioned or driver-specific constants (e.g. `ProviderName.SqlServer2022`) pin the exact variant
and are set automatically after detection — use the primary constant for initial configuration.

Every `UseXxx` method also accepts an optional last parameter: a delegate of the form
`Func<TOptions, TOptions>` for provider-specific advanced settings. Omit it for standard setups.

---

## Configuration patterns

The patterns below apply to every provider. Examples use SQL Server — substitute `UseSqlServer`
with the appropriate `UseXxx` method for your provider.

### Connection string (standard)
```csharp
var options = new DataOptions()
    .UseSqlServer("Server=.;Database=MyDb;Trusted_Connection=True;");
```

### Existing `DbConnection`
```csharp
// disposeConnection: false — DataConnection will not close or dispose the supplied connection
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
| Firebird | `ProviderName.Firebird` | `UseFirebird(...)` | `FirebirdSql.Data.FirebirdClient` |
| Informix | `ProviderName.Informix` | `UseInformix(...)` | ⚠️ see notes |
| SAP HANA | `ProviderName.SapHana` | `UseSapHana(...)` | ⚠️ see notes |
| SQL Server CE | `ProviderName.SqlCe` | `UseSqlCe(...)` | ⚠️ .NET Framework only |
| Sybase / SAP ASE | `ProviderName.Sybase` | `UseAse(...)` | `AdoNetCore.AseClient` |
| YDB | `ProviderName.Ydb` | `YdbTools.CreateDataConnection(...)` ⚠️ no `UseYdb()` — see notes | `Ydb.Sdk` |

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
| `Ace` | ACE engine — Access 2007+ (.accdb) |

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
| `AutoDetect` | Use first available driver (default) | — |
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
# IBM.Data.Informix — .NET Framework only; no public NuGet for .NET Core / .NET 5+
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
| `AutoDetect` | Use first available driver (default) | — |
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
| `AutoDetect` | Try Managed → Devart → Native (default) | — |
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
| `AutoDetect` | Detect provider automatically (default) | — |
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
| `AutoDetect` | Use first available driver (default) | — |
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
| `AutoDetect` | Detect provider automatically (default) | — |
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
// Option 1 — factory method (returns DataConnection directly)
using var db = YdbTools.CreateDataConnection("Host=grpcs://localhost:2135;Database=/local");

// Option 2 — DataOptions (use with DI, DataContext, or pooling)
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

- [`docs/provider-capabilities.md`](provider-capabilities.md) — SQL feature support matrix per provider
  (MERGE, CTE, window functions, bulk copy, OUTPUT/RETURNING, upsert).
- [`docs/architecture.md`](architecture.md) — architecture overview, translation pipeline, execution model.
- [`docs/agent-antipatterns.md`](agent-antipatterns.md) — common mistakes and how to avoid them.
