# LinqToDB — Inserting Data

> You are here if you need to:
> - insert a single row into a table
> - insert a row and retrieve the database-generated identity value
> - insert-or-update a row (upsert)

---

## Basic insert — `Insert`

Returns the number of affected rows (typically 1).

```csharp
using var db = new DataConnection(_options);

int affected = await db.InsertAsync(new Product
{
    Name     = "Widget",
    Price    = 9.99m,
    IsActive = true,
});
```

---

## Insert with identity — `InsertWithInt32Identity` / `InsertWithInt64Identity`

Use when the table has a database-generated (`[Identity]`) primary key and you need the assigned value back.

```csharp
// Returns the generated int PK value
int newId = await db.InsertWithInt32IdentityAsync(new Product
{
    Name     = "Widget",
    Price    = 9.99m,
    IsActive = true,
});
```

Use `InsertWithInt64IdentityAsync` when the identity column is `bigint` / `long`.
Use `InsertWithIdentityAsync` when the identity type is unknown at compile time — it returns `object`.

---

## Upsert — `InsertOrReplace`

Inserts the row if no matching primary key exists; updates it if one does.

```csharp
await db.InsertOrReplaceAsync(new Product
{
    ProductID = 42,     // must be a caller-supplied PK — NOT an identity column
    Name      = "Widget",
    Price     = 9.99m,
});
```

> **Constraint:** `InsertOrReplace` / `InsertOrReplaceAsync` require a caller-supplied primary key value.
> They **do not work** with `[Identity]` columns — the method throws `LinqToDBException` at query
> build time if the entity has an identity PK. See anti-pattern #9 in `docs/agent-antipatterns.md`.

If the entity has an identity PK and you still need upsert semantics:
- Remove `[Identity]` and generate the key application-side, **or**
- Use `InsertWithInt32IdentityAsync` for new rows and `UpdateAsync` for known rows.

---

## Upsert with update expression — `InsertOrUpdate` (LINQ extension)

For more control over which columns are set on insert vs. update, use the
`InsertOrUpdate` method on `ITable<T>` (LINQ Extensions API):

```csharp
await db.GetTable<Product>()
    .InsertOrUpdateAsync(
        () => new Product { ProductID = 42, Name = "Widget", Price = 9.99m },   // insert
        p  => new Product { Name = p.Name, Price = 9.99m });                     // update
```

Check provider support before using: see `Upsert` column in `docs/provider-capabilities.md`.

---

## See also

- [`docs/crud-update.md`](crud-update.md) — updating existing rows
- [`docs/agent-antipatterns.md`](agent-antipatterns.md) — anti-pattern #9 (InsertOrReplace + Identity)
- [`docs/provider-capabilities.md`](provider-capabilities.md) — upsert support per provider
