## linq2db.baselines repository layout

External repository: <https://github.com/linq2db/linq2db.baselines>. Expected local path (relative to this repo's working directory): `../linq2db.baselines`.

### Two kinds of baselines

**SQL baselines** — one folder per test-provider configuration at the repo root:

```
Access.Ace.Odbc/      Access.Ace.OleDb/       Access.Jet.Odbc/    Access.Jet.OleDb/
ClickHouse.Driver/    ClickHouse.MySql/       ClickHouse.Octonica/
DB2/                  Informix.DB2/
Firebird.2.5/         Firebird.3/             Firebird.4/         Firebird.5/
MariaDB.11/           MariaDB.11.EF8/         MariaDB.11.EF9/
MySql.5.7/            MySql.8.0/              MySqlConnector.5.7[.EF8|.EF9]/     MySqlConnector.8.0[.EF8|.EF9]/
Northwind.SQLite/     Northwind.SQLite.MS/
Oracle.{11,12,18,19,21,23}.Managed/
PostgreSQL.{13,14,15,16,17,18}[.EF8|.EF9|.EF10]/
SQLite.Classic[.MPM|.MPU]/  SQLite.MS[.EF8|.EF9|.EF10|.EF31]/
SapHana.Odbc/         SqlCe/
SqlServer.{2005,2008,2012,2014,2016,2017,2019,2022,2025}[.MS][.EFn]/
SqlServer.{Contained,Northwind,SA}[.MS][.EFn]/
Sybase.Managed/
```

Inside each provider folder, the subpath mirrors the test namespace split by `.`, then a folder per test class, then files named:

```
<full.namespace.path>.<ClassName>.<MethodName>(<Provider>[,<param1>[,<param2>...]]).sql
```

Example: `SqlServer.2022.MS/Tests/Data/DataConnectionTests/Tests.Data.DataConnectionTests.TestDisposeFlagCloning962Test1(SqlServer.2022.MS,False).sql`.

First line of every SQL baseline is the comment: `-- <Provider> <ConfigurationName>`. The SQL follows.

A special `a_CreateData/a_CreateData.CreateDatabase(<Provider>).sql` per provider seeds DB state.

**Metrics baselines** — one folder per TFM at the repo root: `NET100/`, `NET90/`, `NET80/`, `NETFX/`. Files are named `<Provider>.<OS>.Metrics.txt`, e.g. `SqlServer.2022.MS.Unix.Metrics.txt`, `SqlServer.2022.MS.Win32NT.Metrics.txt`. These are plain-text performance-metric captures. Review them as their own group — the cross-provider SQL-distinctions logic does not apply.

### Branch naming

PR baselines live on `baselines/pr_<pr_number>`. Absence of the branch means the PR produced no baseline changes.

### Expected cross-provider variation (ignore these when flagging "unusual distinctions")

Minor differences that are routine and should not be called out:

- Parameter prefixes: `@p1` (SqlServer, Sybase, Access, SqlCe) vs `:p1` (Oracle, PostgreSQL, Firebird, DB2) vs `?` (ODBC/OleDb Access) vs others.
- Identifier quoting: `"x"` (ANSI) vs `` `x` `` (MySQL/MariaDB) vs `[x]` (SqlServer, Sybase, Access) vs unquoted.
- Paging: `TOP` (SqlServer, Sybase), `LIMIT/OFFSET` (PostgreSQL, MySQL, SQLite), `ROWNUM`/`FETCH FIRST N ROWS` (Oracle), etc.
- String literals: `N'...'` (SqlServer nchar) vs `'...'`.
- Boolean rendering: `1`/`0` vs `true`/`false`.
