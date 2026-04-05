# LinqToDB — Updating Data

> You are here if you need to:
> - update an existing row using an entity object
> - update rows matching a predicate (set-based update without loading entities)

---

## Full entity update — `Update`

Updates all mapped columns of the row identified by the entity's primary key.

```csharp
using var db = new DataConnection(_options);

product.Price    = 12.99m;
product.IsActive = false;

int affected = await db.UpdateAsync(product);
```

The `WHERE` clause is built from the `[PrimaryKey]` column(s).
All non-PK columns are included in the `SET` clause by default.

---

## Set-based update — expression on `ITable<T>`

Updates matching rows directly in SQL without loading entities first.
Use when you need to update multiple rows or only specific columns.

```csharp
// Set all inactive products' stock to 0
int affected = await db.GetTable<Product>()
    .Where(p => !p.IsActive)
    .UpdateAsync(p => new Product { Stock = 0 });
```

Only the columns assigned in the expression lambda are included in `SET`.
Unassigned columns are not touched.

---

## For upsert (insert-or-update)

See [`docs/crud-insert.md`](crud-insert.md) — `InsertOrReplace` and `InsertOrUpdate`.

---

## See also

- [`docs/crud-insert.md`](crud-insert.md) — inserting rows, identity, upsert
- [`docs/crud-delete.md`](crud-delete.md) — deleting rows
