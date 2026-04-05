# LinqToDB CRUD Operations

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
| Query / read data — filtering, projection, ordering, pagination, associations | [`docs/crud-select.md`](crud-select.md) |
| Insert rows — including identity columns and upsert | [`docs/crud-insert.md`](crud-insert.md) |
| Update rows — full entity or partial expression-based update | [`docs/crud-update.md`](crud-update.md) |
| Delete rows — by entity or by predicate | [`docs/crud-delete.md`](crud-delete.md) |
| Bulk copy / batch insert — `DataConnection.BulkCopy` | [`docs/provider-capabilities.md`](provider-capabilities.md) — check `Bulk Copy` column per provider before using |
| MERGE — SQL MERGE statement via `Merge` LINQ extension | [`docs/provider-capabilities.md`](provider-capabilities.md) — check `MERGE` column per provider before using |

---

## Out of scope for this guide

| Topic | See instead |
|---|---|
| Transactions | [`docs/configuration.md`](configuration.md) — `BeginTransaction`, `TransactionScope` |
| Schema creation (`CreateTable`) | [`docs/agent-antipatterns.md`](agent-antipatterns.md) — anti-pattern #10 |
| Custom SQL functions | [`docs/custom-sql.md`](custom-sql.md) |
| CTE, OUTPUT / RETURNING | [`docs/provider-capabilities.md`](provider-capabilities.md) |
