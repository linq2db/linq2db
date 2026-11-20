# Hints

Part of the linq2db skill; the overview is in [SKILL.md](../SKILL.md).

## Typed helpers first

Typed hint helpers are extension methods of a provider-specific table or query, reached through a marker from
`LinqToDB.DataProvider.<Provider>`:

```csharp
db.GetTable<Order>().AsSqlServer().WithNoLock()                 // table hint
db.GetTable<Order>().Where(o => o.Total > 100).AsSqlServer().OptionRecompile()   // query hint
```

| Marker | Name pattern, examples |
|---|---|
| `AsSqlServer()` | table `With<Hint>()`: `WithNoLock()`, `WithIndex("IX")`; query `Option<Hint>()`: `OptionRecompile()` |
| `AsOracle()`, `AsMySql()`, `AsPostgreSQL()`, `AsClickHouse()` | keyword in PascalCase + `Hint`: `FullHint()`, `JoinOrderHint()`, `ForUpdateSkipLockedHint()`, `FinalHint()` |
| `AsSQLite()` | `IndexedByHint("IX")`, `NotIndexedHint()` |
| `AsSqlCe()`, `AsAccess()`, `AsYdb()` | `WithNoLock()`; `WithOwnerAccessOption()`; `DistinctHint()` |

- Most helpers and markers have no entry in `linq2db.xml` (ClickHouse's helpers do). Derived names can be wrong
  (MySQL `NO_ICP` is `NoIcpHint()`): confirm a name by compiling it.
- On a bare `ITable<T>` the marker returns the table type, which has only table helpers. For query, join and
  scope helpers call the marker on a query (after `.Where(...)` or `.AsQueryable()`).
- Scope forms apply a table hint to every table of the query they are called on: `WithNoLockInScope()`,
  `FullInScopeHint()`. Call them after the joins. Known limitation (issue #4321): tables joined only through an
  association in the final `Select` do not get the hint.
- Hints for several providers can be chained; only the active provider's hints are emitted.
- DB2, Firebird, Informix, SAP HANA, Sybase ASE and DuckDB have no hint helpers.

## Text hints

When no typed helper exists: `TableHint("...")` / `With("...")` and `IndexHint("...")` on a table,
`TablesInScopeHint`, `JoinHint`, `SubQueryHint`, `QueryHint` on a query, `table.Merge("...")` for `MERGE`. The
text is SQL: never build it from user input, and write the hint without its `WITH (...)` / `OPTION (...)`
wrapper.

Analyzer L2DB2001 reports a text hint without arguments that has a typed equivalent and offers a code fix.
Its severity is Info: it shows in an IDE, not in `dotnet build` output, unless `.editorconfig` sets
`dotnet_diagnostic.L2DB2001.severity = warning`.
