# LinqToDB — Deleting Data

> You are here if you need to:
> - delete a single row by entity
> - delete multiple rows matching a predicate

---

## Delete by entity — `Delete`

Deletes the row identified by the entity's primary key.

```csharp
using var db = new DataConnection(_options);

int affected = await db.DeleteAsync(product);
```

The `WHERE` clause is built from the `[PrimaryKey]` column(s).

---

## Set-based delete — predicate on `ITable<T>`

Deletes all rows matching the predicate directly in SQL, without loading entities first.

```csharp
// Delete all discontinued products
int affected = await db.GetTable<Product>()
    .Where(p => p.IsDiscontinued)
    .DeleteAsync();
```

Generates a single `DELETE FROM ... WHERE ...` statement.
Use this form when deleting multiple rows — it avoids loading entities into memory.

---

## See also

- [`docs/crud-update.md`](crud-update.md) — updating rows
- [`docs/crud-insert.md`](crud-insert.md) — inserting rows
