---
name: linq2db
license: MIT
description: Guidance for LINQ to DB (linq2db), the .NET LINQ-to-SQL library, matching the package version it ships in. Use when writing, reviewing or debugging C# code that uses linq2db or the LinqToDB namespaces (DataConnection, DataContext, DataOptions, mapping, ITable<T> queries, inserts, updates, hints), or when moving code from Entity Framework to linq2db.
---

# linq2db

## What linq2db is

- A translator from LINQ to SQL over ADO.NET. It is not a change-tracking ORM: no `SaveChanges`, identity map,
  lazy loading or migrations. A query runs when enumerated; every write is a statement you call:
  `db.Insert(item)`, `db.Update(item)`, `table.Where(...).Set(x => x.Col, v).Update()`, `query.Delete()`,
  `Merge`, `BulkCopy`. Related rows load with `LoadWith(x => x.Children)`, never on access.
- The SQL follows the shape of the query. See it with `query.ToSqlQuery().Sql`.

## Architecture

- `DataOptions` (provider, connection string, `MappingSchema`, options) is immutable; one per application.
- `DataConnection` holds one ADO.NET connection from first use to `Dispose`; `DataContext` opens one per
  command. Both are `IDataContext`; create one per operation (request, job) and dispose it.
- A provider is `UseSqlServer(...)`, `UsePostgreSQL(...)`, ... plus the database's ADO.NET package
  (`Microsoft.Data.SqlClient`, `Npgsql`, ...), which linq2db does not bring.
- A query is an expression tree, translated to a SQL AST, optimized, rendered by the provider, and cached by
  expression shape. Async materializers (`ToListAsync`, ...) are in `LinqToDB.Async`.

## Traps

1. **`[Table]` maps only attributed members.** With `[Table]`, a property without `[Column]`,
   `[PrimaryKey]` or `[Identity]` is silently not mapped (`IsColumnAttributeRequired` defaults to true).
   Without `[Table]`, every public property or field of a scalar type (not an entity or a collection) maps by
   name.
2. **Lifetime.** Build `DataOptions` and a custom `MappingSchema` once: options that get a new schema,
   interceptor or delegate per call miss the query cache. Temp tables and session state need one open
   connection: use `DataConnection` (a `DataContext` only with `SetKeepConnectionAlive(true)`). A
   `TransactionScope` must exist before the connection opens; otherwise use `db.BeginTransaction()`.
3. **No silent client evaluation.** A call with no SQL translation in `Where`, `OrderBy`, `GroupBy` or a join
   throws. Only the final `Select` computes untranslatable parts on the client. Map a .NET method to SQL with
   `[ExpressionMethod]` or `[Sql.Expression]`.
4. **Parameters and literals.** Captured variables become SQL parameters; constants written in the
   expression become literals and part of the cache key. Do not build queries from varying constants;
   force either form with `Sql.Parameter(x)` / `Sql.Constant(x)`.
5. **Writes are explicit.** Loading entities, changing them and expecting them saved does nothing. Update in
   place with `table.Where(...).Set(...).Update()`; `InsertOrReplace` needs a caller-supplied key, not an
   identity.

## Recent changes

APIs added in 6.4 and later postdate most model training data: window functions, eager-loading strategies,
keyed query filters, `Upsert`, `NULLS FIRST/LAST`, durations, combined commands. See
[references/recent-changes.md](references/recent-changes.md).

## References

| Area | File |
|---|---|
| Hints (typed provider helpers, text hints) | [references/hints.md](references/hints.md) |
| APIs added in 6.4 and later | [references/recent-changes.md](references/recent-changes.md) |

Other areas have no reference file yet; use `linq2db.xml`.

## Checking an API

`linq2db.xml` in the restored package (`<global-packages>/linq2db/<version>/lib/<tfm>/linq2db.xml`; from a
skill read inside the package, `../../lib/<tfm>/linq2db.xml`) has the signatures and remarks for this
version. Search it by member name. Most provider hint helpers and `AsXxx()` markers are not in it. Do not use
`LinqToDB.Internal.*` types in application code.
