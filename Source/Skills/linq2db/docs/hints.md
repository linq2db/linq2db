# Hints

Part of the linq2db skill; the overview is in [SKILL.md](../SKILL.md). This file covers query, table, index,
join and subquery hints: typed provider helpers first, text hints as the fallback.

## Typed helpers

A typed helper is reached through a provider marker. The marker turns a table or query into a
provider-specific one, and the helpers are extension methods of that type only:

```csharp
using LinqToDB;
using LinqToDB.DataProvider.SqlServer;

var orders = db.GetTable<Order>()
    .AsSqlServer()
    .WithNoLock()            // WITH (NoLock) on this table
    .Where(o => o.Total > 100);
```

| Provider | Namespace `LinqToDB.DataProvider.` | Marker | Helper names (examples) |
|---|---|---|---|
| SQL Server | `SqlServer` | `AsSqlServer()` | table `WithNoLock()`, `WithUpdLock()`, `WithIndex("IX")`; query `OptionRecompile()`, `OptionMaxDop(4)`, `OptionUseHint(...)`; join `JoinHashHint()`; temporal `TemporalTableAsOf(...)` |
| Oracle | `Oracle` | `AsOracle()` | table `FullHint()`, `IndexHint("IX")`, `ParallelHint(4)`; query `LeadingHint(...)`, `FirstRowsHint(10)`, `OptParamHint(...)` |
| MySQL, MariaDB | `MySql` | `AsMySql()` | table `IndexHint("IX")`, `JoinOrderHint()`; query `MaxExecutionTimeHint(1000)`, `ForUpdateSkipLockedHint()` |
| PostgreSQL | `PostgreSQL` | `AsPostgreSQL()` | query only: `ForUpdateHint()`, `ForUpdateSkipLockedHint()`, `ForShareHint()`, `SubQueryTableHint(...)` |
| ClickHouse | `ClickHouse` | `AsClickHouse()` | table `FinalHint()`; query `SettingsHint("...")`; join `JoinAnyHint()`, `JoinGlobalHint()` |
| SQLite | `SQLite` | `AsSQLite()` | table `IndexedByHint("IX")`, `NotIndexedHint()` |
| SQL CE | `SqlCe` | `AsSqlCe()` | table only: `WithNoLock()`, `WithRowLock()`, `WithIndex("IX")` |
| Access | `Access` | `AsAccess()` | query `WithOwnerAccessOption()` |
| YDB | `Ydb` | `AsYdb()` | query `DistinctHint(...)`, `UniqueHint(...)` (also on a plain query, without the marker) |

Names follow the SQL keyword, but the shape differs by provider: SQL Server and SQL CE table hints are
`With<Hint>()`, SQL Server query hints `Option<Hint>()`; Oracle, MySQL, PostgreSQL and ClickHouse use the
keyword in PascalCase plus `Hint` (`USE_NL` is `UseNLHint()`, `INDEX_FFS` is `IndexFFSHint()`, but MySQL
`NO_ICP` is `NoIcpHint()`), so a derived name can be wrong.

The list above shows examples, not the full set. The typed helpers and the `AsXxx()` markers have no XML
documentation, so `linq2db.xml` does not list them: a search finds nothing even for `WithNoLock`. Confirm
a name by compiling it.

Other providers (DB2, Firebird, Informix, SAP HANA, Sybase ASE, DuckDB) have no typed table or query
hint helpers; do not expect text hints in their SQL either, and check the output.

- Table helpers need the marker on a table; query, join and `InScope` helpers need it on a query. On a bare
  `ITable<T>` the marker returns the table type, so `db.GetTable<T>().AsSqlServer().OptionRecompile()` does
  not compile: call the marker after a query operator (`.Where(...)`, a join) or after `.AsQueryable()`.
- One marker serves several helpers of the same provider: `.AsSqlServer().WithNoLock().WithRowLock()`.
- One query can carry hints for several providers: `.AsSqlServer().WithNoLock().AsOracle().FullHint()`.
  Only the hints of the provider the query runs on are emitted.
- Table helpers that also exist for the whole query scope have an `InScope` form: SQL Server
  `WithNoLockInScope()`, Oracle `FullInScopeHint()`, MySQL `JoinOrderInScopeHint()`, ClickHouse
  `FinalInScopeHint()`. Chained `InScope` hints stack (since 6.5).

## Text hints

When no typed helper exists, the generic methods take provider-defined SQL text:

| Method | Receiver | Emits |
|---|---|---|
| `TableHint("...")`, `With("...")` | `ITable<T>` | a hint on that table |
| `IndexHint("...")` | `ITable<T>` | an index hint on that table |
| `TablesInScopeHint("...")` | `IQueryable<T>` | the table hint on every table of the query scope |
| `JoinHint("...")` | `IQueryable<T>` | a join hint |
| `SubQueryHint("...")` | `IQueryable<T>` | a hint on a subquery |
| `QueryHint("...")` | `IQueryable<T>` | a hint on the whole statement |

Hint text is SQL, not a parameter: never build it from user input. Write the hint itself, not its
wrapper: SQL Server puts table hints in `WITH (...)` and query hints in `OPTION (...)` for you.

The analyzer L2DB2001 reports a text hint without arguments that has a typed equivalent
(`TableHint("NOLOCK")` on SQL Server), names the helper, and offers a code fix that rewrites the call; on a
generic table or query it offers one rewrite per provider. Its severity is Info: it shows in an IDE, not in
`dotnet build` output, unless `.editorconfig` raises it (`dotnet_diagnostic.L2DB2001.severity = warning`).

## Scope

- A table hint goes on one table reference: call it on that `ITable<T>` before joining.
- An `InScope` helper or `TablesInScopeHint` applies to the tables already in the query it is called on.
  Call it on the composed query, after the joins, not on the first table.
- Tables reached only from the final `Select` (an association property or a subquery in the projection)
  do not get a scope hint. Join such a table explicitly, or put a table hint on the table inside the
  subquery.
- A helper that names other tables (Oracle `LeadingHint`, MySQL `JoinOrderHint`, PostgreSQL
  `ForUpdateHint`) takes `Sql.SqlID` values: mark the table with `.TableID("t")` and pass
  `Sql.TableAlias("t")`. Do not write generated aliases into hint text.

```csharp
var q =
    (from o in db.GetTable<Order>().TableID("o")
     join c in db.GetTable<Customer>().TableID("c") on o.CustomerId equals c.Id
     select new { o, c })
    .AsOracle()
    .LeadingHint(Sql.TableAlias("c"), Sql.TableAlias("o"));
```

## MERGE

A `MERGE` statement takes its hint through `table.Merge("hint")`, not through the methods above.

Check where a hint landed with `query.ToSqlQuery().Sql`.
