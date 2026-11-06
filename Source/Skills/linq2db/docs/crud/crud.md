# LinqToDB CRUD Operations

> Part of the linq2db skill. General rules and the guide index are in [`SKILL.md`](../../SKILL.md).

> You are here if you need to:
> - read data from a table (`SELECT`)
> - insert rows into a table
> - update existing rows
> - delete rows
> - perform upsert (insert-or-update)
> - bulk-copy / batch insert multiple rows
> - MERGE (provider-side insert-or-update via SQL MERGE statement)

---

## Choose by operation

| What you need to do | Go to |
|---|---|
| Query / read data - filtering, projection, ordering, pagination, associations | [`docs/crud/select.md`](select.md) |
| Insert from a C# object, expression, or fluent column-by-column builder | [`docs/crud/insert-values.md`](insert-values.md) |
| `INSERT … SELECT` - copy or archive rows from a query, with JOINs or projections | [`docs/crud/insert-select.md`](insert-select.md) |
| Upsert - insert-or-update semantics (`InsertOrReplace`, `InsertOrUpdate`) | [`docs/crud/upsert.md`](upsert.md) |
| Update rows - full entity or partial expression-based update | [`docs/crud/update.md`](update.md) |
| Delete rows - by entity or by predicate | [`docs/crud/delete.md`](delete.md) |
| Bulk copy / batch insert - `BulkCopy` / `BulkCopyAsync` | [`docs/crud/bulk-copy.md`](bulk-copy.md) |
| MERGE - SQL MERGE statement via `Merge` LINQ extension | [`docs/crud/merge.md`](merge.md) |

---

## Out of scope for this guide

| Topic | See instead |
|---|---|
| Transactions | [`docs/configuration.md`](../configuration.md) - `BeginTransaction`, `TransactionScope` |
| Schema creation (`CreateTable`) | [`docs/agent-antipatterns.md`](../agent-antipatterns.md) - anti-pattern #10 |
| Custom SQL functions | [`docs/extensions.md`](../extensions.md) |
| CTE, OUTPUT / RETURNING | [`docs/provider-capabilities.md`](../provider-capabilities.md) |
