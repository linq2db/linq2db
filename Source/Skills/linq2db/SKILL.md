---
name: linq2db
description: Guidance for LINQ to DB (linq2db), the .NET LINQ-to-SQL data access library, matching the package version it ships in. Use when writing, reviewing or debugging C# code that uses linq2db or the LinqToDB namespaces (DataConnection, DataContext, DataOptions, mapping, ITable<T> queries, LoadWith, Insert, Update, Delete, Merge, Upsert, BulkCopy, temp tables, CTEs, window functions, hints, raw SQL), and when code written with Entity Framework habits has to be moved to linq2db.
---

# linq2db

How linq2db works, where it differs from Entity Framework, and which APIs are new in 6.4 and later.
The files in `docs/` are optional references for single topics; open one only when the task needs it.

## What linq2db is

- A translator from LINQ to SQL over ADO.NET, not a stateful ORM. There is no change tracking, no identity
  map, no `SaveChanges`, no lazy loading and no migrations. A query runs when it is enumerated; every write
  is a statement you call.
- Writes are explicit and can be set-based: `db.Insert(item)`, `db.Update(item)`, `db.Delete(item)`,
  `table.Where(...).Set(x => x.Col, value).Update()`, `table.Where(...).Delete()`,
  `query.Insert(target, x => new T { ... })` (INSERT ... SELECT), `Merge`, `Upsert`, `BulkCopy`.
- A LINQ expression that has no SQL translation is an error, not a silent client-side evaluation. The one
  exception is the final projection: parts of the last `Select` that SQL cannot compute run on the client
  after the row is read.
- The SQL follows the shape of the query: write the LINQ the way you would write the SQL. Check the result
  with `query.ToSqlQuery().Sql` instead of guessing.

## Architecture

- `DataOptions` holds the provider, connection string, mapping schema and every option. It is immutable;
  build it once (singleton or `static readonly`) and pass it to each context. Each new instance recomputes
  its configuration id, and options that take a new `MappingSchema`, interceptor or delegate per call get a
  new id and miss the query cache.
- `DataConnection` is a session over one ADO.NET connection, opened on first use and kept until
  `Dispose`. Use it for transactions (`BeginTransaction`), temp tables and anything that needs session
  state. `DataContext` opens and closes a connection per command unless `SetKeepConnectionAlive(true)` or a
  transaction keeps it open. Both implement `IDataContext`; create one per operation (request, job) and dispose it.
- `MappingSchema` describes entities: attributes (`[Table]`, `[Column]`, `[PrimaryKey]`, `[Identity]`,
  `[NotColumn]`, `[Association]`) or `FluentMappingBuilder`. Without attributes, public properties map by
  name. With `[Table]`, only members that carry `[Column]`, `[PrimaryKey]` or `[Identity]` are mapped
  (`IsColumnAttributeRequired` defaults to true): a property without one is silently ignored. Create a custom schema once at startup and attach it with `UseMappingSchema`; a new schema per
  context throws away its caches.
- A query goes: `ITable<T>` / `IQueryable<T>` expression tree, then a SQL AST, then the optimizer, then the
  provider's SQL builder, then a compiled reader that materializes rows. The translated query is cached by
  expression shape: values captured from variables become SQL parameters and reuse the cached query;
  constants written in the expression are inlined as literals and are part of the cache key.
- A provider is a `DataOptions.UseXxx(connectionString)` call plus the database's ADO.NET package, which
  linq2db does not bring:

| Database | Method | ADO.NET package |
|---|---|---|
| SQL Server | `UseSqlServer` | `Microsoft.Data.SqlClient` |
| PostgreSQL | `UsePostgreSQL` | `Npgsql` |
| SQLite | `UseSQLite` | `Microsoft.Data.Sqlite` or `System.Data.SQLite` |
| MySQL, MariaDB | `UseMySql` | `MySqlConnector` |
| Oracle | `UseOracle` | `Oracle.ManagedDataAccess.Core` |
| ClickHouse | `UseClickHouse` | `ClickHouse.Driver`, `Octonica.ClickHouseClient` or `MySqlConnector` |
| DuckDB | `UseDuckDB` | `DuckDB.NET.Data.Full` |
| Firebird | `UseFirebird` | `FirebirdSql.Data.FirebirdClient` |
| DB2 | `UseDB2` | `Net.IBM.Data.Db2` |
| SAP ASE | `UseAse` | `AdoNetCore.AseClient` |
| YDB | `UseYdb` | `Ydb.Sdk` |
| Access | `UseAccess` | `System.Data.OleDb` or `System.Data.Odbc` (Windows); `LibRed.Ado` with `AccessProvider.LibRed` (.NET 10+) |

- Extension points, from narrow to wide: `[ExpressionMethod]` (replace a method or property with a LINQ
  expression), `[Sql.Expression]` / `[Sql.Function]` (emit SQL text for a method), `IMemberTranslator` with
  `UseMemberTranslator`, interceptors (`ICommandInterceptor`, `IConnectionInterceptor`, ... registered with
  `UseInterceptor` or `AddInterceptor`). `Sql.Property<T>(entity, "Name")` reaches a column by name.
- The package brings Roslyn analyzers (ids `L2DB*`). Follow their messages and code fixes, and the text of
  `[Obsolete]` warnings: they name the replacement API.

## Entity Framework habits that are wrong here

| EF Core | linq2db |
|---|---|
| `Add` + `SaveChanges()` | `db.Insert(item)`; `db.InsertWithInt32Identity(item)` returns the new identity |
| load, change, `SaveChanges()` | `db.Update(item)`, or set-based `table.Where(...).Set(...).Update()` without loading |
| `ExecuteUpdate` / `ExecuteDelete` | `query.Set(...).Update()`, `query.Update(x => new T { ... })`, `query.Delete()` |
| `Include` / `ThenInclude`, lazy loading | `LoadWith` / `ThenLoad`, or select the collection in the projection; nothing loads on access |
| `AsNoTracking()` | not needed, nothing is tracked |
| `FromSqlRaw` / `FromSqlInterpolated` | `db.FromSql<T>($"... {value}")`: interpolated values become parameters |
| migrations, `EnsureCreated` | none; `db.CreateTable<T>()` and `CreateTempTable` are for tests and temp data |
| options built per `DbContext` | one `DataOptions` for the application, one context per operation |
| `TransactionScope` around an open context | create the scope before the connection opens, or use `db.BeginTransaction()` |
| client evaluation in `Where` / `OrderBy` | keep those translatable; map a .NET method with `[ExpressionMethod]` or `[Sql.Expression]` |

## Areas

- **Mapping.** For any column that `CreateTable` or `CreateTempTable` creates, set `Length`, `Precision`
  and `Scale` on `string` and `decimal` columns; provider defaults differ.
- **Queries and joins.** Navigation through `[Association]` members becomes a join. Explicit joins:
  LINQ `join`, `InnerJoin` / `LeftJoin` / `RightJoin` / `FullJoin` / `CrossJoin` extension methods, or
  `from ... in table.Where(...).DefaultIfEmpty()`. Async materializers (`ToListAsync`,
  `FirstOrDefaultAsync`, `CountAsync`, ...) are in `LinqToDB.Async`.
- **Eager loading.** `LoadWith(x => x.Children).ThenLoad(c => c.Items)` loads each collection with one
  extra query joined to the parent query; there is no lazy loading.
- **Writes.** `InsertOrReplace` needs a caller-supplied primary key (not an identity). `Merge()` is a fluent
  `MERGE` (`.Using(source).On(...).InsertWhenNotMatched().UpdateWhenMatched().Merge()`); see the recent
  `Upsert` below for insert-or-update. Large inserts: `BulkCopy` / `BulkCopyAsync` (`LinqToDB.Data`).
- **Temp tables.** `db.CreateTempTable(items)` or `db.CreateTempTable(query)` creates and fills a
  `TempTable<T>` that drops the table on dispose. Session-scoped tables need a connection kept open.
- **CTEs.** `query.AsCte()`; recursive CTEs with `db.GetCte<T>(cte => ...)`.
- **Hints.** Typed provider helpers after a provider marker (`.AsSqlServer().WithNoLock()`); text hints
  such as `TableHint("NOLOCK")` are the fallback. See [docs/hints.md](docs/hints.md).
- **Raw SQL.** `db.FromSql<T>(...)` as a query root you can compose; `db.Query<T>(sql)`,
  `db.Execute(sql)` on a context.
- **Null comparisons.** By default `a == b` on nullable columns compares like C# (`NULL == NULL` is true),
  which adds `IS NULL` terms. `UseCompareNulls(CompareNulls.LikeSql)` gives plain SQL semantics.
- **Parameters.** Captured variables are parameters; `Sql.Parameter(x)` and `Sql.Constant(x)` force one or
  the other; `InlineParameters = true` on a context inlines all values.
- **Configuration.** Change behaviour through `DataOptions` (`UseCommandTimeout`, `UseTracing`, ...);
  static `Configuration.*` settings do not reach contexts that already exist. Temporary changes on one
  context: `using (db.UseOptions(o => ...))`.

## Recent changes (6.4 to 6.7)

These APIs postdate most model training data. Examples are in [docs/recent-changes.md](docs/recent-changes.md).

- 6.4 full `Sql.Window.*` API (`Sql.Window.RowNumber(w => w.PartitionBy(...).OrderBy(...))`, offset,
  value and statistical functions, frames, `Filter`); `Sql.Ext.*().Over()` chains still work, analyzer
  L2DB1001 migrates them.
- 6.4 eager-loading strategies `WithKeyedLoadStrategy()` / `WithUnionLoadStrategy()` per query, or
  `UseDefaultEagerLoadingStrategy(EagerLoadingStrategy.KeyedQuery)` globally.
- 6.4 keyed query filters: `HasQueryFilter("key", ...)` in fluent mapping, `IgnoreFilters(["key"])` per query.
- 6.4 fluent `table.Upsert(item, u => u.Match(...).Insert(...).Update(...))`; entity `Insert(item, i => ...)`
  / `Update(item, u => ...)` with `Set` and `Ignore`.
- 6.4 `OrderBy(x => x.Col, Sql.NullsPosition.Last)` (also `ThenBy*`); default via `UseDefaultNullsPosition`.
- 6.4 `Sql.NewGuid7()` and `Guid.CreateVersion7()` for UUIDv7 values.
- 6.4 `UsePreferClientCalculation(true)`: the final projection computes on the client what does not need SQL.
- 6.4 query cache is bounded; `DistinctBy` emits `DISTINCT ON` on PostgreSQL and DuckDB.
- 6.5 `[Duration(DurationUnit.Second)]` on a `TimeSpan` column stored as a number; `TimeSpan` comparisons
  and arithmetic on it then translate. Separately, a date difference now measures elapsed time.
- 6.5 `db.UpdateOptimisticWithRefresh(entity)` (`LinqToDB.Concurrency`) updates and refreshes the
  concurrency token on the entity.
- 6.5 chained `*InScope` table hints stack instead of keeping only the innermost.
- 6.6 `UseCombinedCommands(true)` sends an eager load (and on SQLite/MySQL an identity insert) in one round trip.
- 6.6 .NET 11 tuple-returning `Join` / `LeftJoin` / `RightJoin` / `FullJoin` overloads translate.
- 6.6 behaviour: a nullable comparison used as a value is `false` when an operand is `NULL` (on databases
  with a boolean type); an eager load
  outside a transaction reads all main rows before returning the first one.
- 6.6 packages target `net462`, `netstandard2.0`, `net10.0`, `net11.0`.
- 6.7 analyzer L2DB2001: a text hint with a typed helper gets a code fix to that helper.

## Checking an API

`linq2db.xml`, the XML documentation of the package, has the exact signatures and remarks for this
version. It lies next to the assembly in the restored package:
`<global-packages>/linq2db/<version>/lib/<tfm>/linq2db.xml` (`dotnet nuget locals global-packages --list`
prints the folder; from a skill read inside the package it is `../../lib/<tfm>/linq2db.xml`). Search it by
member name; do not read it whole. Member ids look like `M:LinqToDB.LinqExtensions.LoadWith``2(...)`.
Provider hint helpers and `AsXxx()` markers are not in it; see [docs/hints.md](docs/hints.md).
Do not use `LinqToDB.Internal.*` types in application code.
