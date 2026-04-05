# LinqToDB — Inserting Data

> You are here if you need to:
> - insert a single row from a C# object
> - insert a row and retrieve the database-generated identity value
> - insert rows from a LINQ query (`INSERT ... SELECT`)
> - insert rows from a complex query with JOINs into a specific target table
> - insert a row and receive the full inserted record back (`INSERT ... OUTPUT`)
> - insert-or-update a row (upsert)
> - bulk-insert many rows → see [`docs/provider-capabilities.md`](provider-capabilities.md)

---

## 1. Simple object insert — `Insert` / `InsertAsync`

Inserts a single C# object. LinqToDB infers the target table from the `[Table]` mapping on the type.
Returns the number of affected rows (typically 1).

```csharp
using var db = new DataConnection(_options);

await db.InsertAsync(new Product
{
    Name     = "Widget",
    Price    = 9.99m,
    IsActive = true,
});
```

---

## 2. Insert with identity — `InsertWithInt32Identity` / `InsertWithInt64Identity`

Use when the table has a database-generated (`[Identity]`) primary key and you need the
assigned value back.

```csharp
// Returns the generated int PK value
int newId = await db.InsertWithInt32IdentityAsync(new Product
{
    Name     = "Widget",
    Price    = 9.99m,
    IsActive = true,
});
```

| Method | Return type | Use when |
|---|---|---|
| `InsertWithInt32IdentityAsync` | `int` | Identity column is `int` |
| `InsertWithInt64IdentityAsync` | `long` | Identity column is `bigint` / `long` |
| `InsertWithIdentityAsync` | `object` | Identity type is unknown at compile time |

---

## 3. INSERT ... SELECT — insert from a LINQ query

Inserts rows from a source query directly into a target table without materializing
the data in the application. Generates a single `INSERT INTO ... SELECT ... FROM ...` statement.

```csharp
// Copy all inactive products into an archive table
await db.GetTable<Product>()
    .Where(p => !p.IsActive)
    .InsertAsync(
        db.GetTable<ProductArchive>(),
        p => new ProductArchive
        {
            ID    = p.ProductID,
            Name  = p.Name,
            Price = p.Price,
        });
```

The second argument is the target table (`ITable<TTarget>`), the third is the column-mapping
expression from source row to target row.

---

## 4. INSERT ... SELECT with explicit target — complex source (JOINs)

When the source query involves JOINs or projections the result type differs from the target
entity type. The target table must be supplied explicitly as the second argument.

```csharp
var source = from p in db.GetTable<Product>()
             join c in db.GetTable<Category>() on p.CategoryID equals c.ID
             select new { p, c };

await source.InsertAsync(
    db.GetTable<ProductArchive>(),
    x => new ProductArchive
    {
        ID           = x.p.ProductID,
        Name         = x.p.Name,
        CategoryName = x.c.Name,
    });
```

This is the same `Insert<TSource, TTarget>` overload as section 3 — the key difference
is that `TSource` (anonymous join result) and `TTarget` (`ProductArchive`) are different types,
so the target table cannot be inferred and must be passed explicitly.

---

## 5. INSERT with OUTPUT — `InsertWithOutput`

Inserts a single row and returns the full inserted record as populated by the database
(including server-generated values such as identity, default columns, and computed columns).

```csharp
// Returns the inserted record with all server-filled values
Product inserted = await db.GetTable<Product>()
    .InsertWithOutputAsync(() => new Product
    {
        Name     = "Widget",
        Price    = 9.99m,
        IsActive = true,
    });

Console.WriteLine(inserted.ProductID); // server-generated identity value
```

To return only specific columns, pass an output projection:

```csharp
var result = await db.GetTable<Product>()
    .InsertWithOutputAsync(
        () => new Product { Name = "Widget", Price = 9.99m, IsActive = true },
        r  => new { r.ProductID, r.Name });   // output projection
```

**Provider support:** SQL Server 2005+, PostgreSQL, SQLite 3.35+, Firebird 2.5+, MariaDB 10.5+.
Check the `OUTPUT / RETURNING` column in [`docs/provider-capabilities.md`](provider-capabilities.md) before using.

---

## 6. Upsert — `InsertOrReplace`

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
> build time if the entity has an identity PK. See anti-pattern #9 in [`docs/agent-antipatterns.md`](agent-antipatterns.md).

If the entity has an identity PK and you still need upsert semantics:
- Remove `[Identity]` and generate the key application-side, **or**
- Use `InsertWithInt32IdentityAsync` for new rows and `UpdateAsync` for known rows.

---

## 7. Upsert with separate insert / update expressions — `InsertOrUpdate`

For more control over which columns are set on insert vs. update, use the
`InsertOrUpdate` LINQ extension on `ITable<T>`:

```csharp
await db.GetTable<Product>()
    .InsertOrUpdateAsync(
        () => new Product { ProductID = 42, Name = "Widget", Price = 9.99m },   // INSERT values
        p  => new Product { Name = p.Name, Price = 9.99m });                     // UPDATE values
```

Check provider support before using: see `Upsert` column in [`docs/provider-capabilities.md`](provider-capabilities.md).

---

## 8. Bulk copy

For inserting large numbers of rows use `DataConnection.BulkCopy`.
Provider support and behavior vary significantly — check the `Bulk Copy` column in
[`docs/provider-capabilities.md`](provider-capabilities.md) before using.

---

## See also

- [`docs/crud-update.md`](crud-update.md) — updating existing rows
- [`docs/agent-antipatterns.md`](agent-antipatterns.md) — anti-pattern #9 (InsertOrReplace + Identity)
- [`docs/provider-capabilities.md`](provider-capabilities.md) — OUTPUT/RETURNING, upsert, and bulk copy support per provider

