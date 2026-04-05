# LinqToDB — Upsert (Insert-or-Update)

> **You are here if** you need to:
> - insert a row if the primary key does not exist, update it if it does
> - provide separate INSERT and UPDATE expressions for the two phases
> - control which columns participate in each phase at runtime
>
> For plain insert → [`crud-insert-values.md`](crud-insert-values.md)
> For `INSERT … SELECT` → [`crud-insert-select.md`](crud-insert-select.md)

---

> **Async:** All methods have `Async` counterparts accepting an optional `CancellationToken`.
> Examples use synchronous forms for brevity.

> **Provider support:** upsert behavior varies across databases.
> Check the `Upsert` column in [`provider-capabilities.md`](provider-capabilities.md) before using.

---

## 1. Object upsert — `db.InsertOrReplace`

Inserts the row when no row with the same primary key exists; replaces (updates) it when one does.

```csharp
db.InsertOrReplace(new Product
{
    ProductID = 42,      // caller-supplied PK — NOT an identity column
    Name      = "Widget",
    Price     = 9.99m,
});
```

### Column filter

Pass an `InsertOrUpdateColumnFilter<T>` to control which columns participate in the INSERT
and UPDATE phases independently. The delegate receives the entity, a `ColumnDescriptor`,
and a `bool` indicating whether this is the insert phase.

```csharp
db.InsertOrReplace(product, (entity, col, isInsert) =>
    isInsert
        ? col.MemberName != nameof(Product.UpdatedAt)   // exclude on INSERT
        : col.MemberName != nameof(Product.CreatedAt)); // exclude on UPDATE
```

> **Constraint:** `InsertOrReplace` requires a caller-supplied primary key.
> It will throw at query build time when the entity has an `[Identity]` PK.
> See anti-pattern #9 in [`agent-antipatterns.md`](agent-antipatterns.md).

---

## 2. Expression upsert — `InsertOrUpdate`

`InsertOrUpdate` is a LINQ extension on `ITable<T>` that accepts separate lambda expressions
for the INSERT and UPDATE phases. Use when values differ between the two phases or involve
SQL expressions.

```csharp
db.GetTable<Product>()
    .InsertOrUpdate(
        () => new Product { ProductID = 42, Name = "Widget", Price = 9.99m },  // INSERT
        p  => new Product { Name = p.Name, Price = 9.99m });                    // UPDATE
```

An optional third argument provides an explicit key predicate when the key cannot be inferred
from the mapping:

```csharp
db.GetTable<Product>()
    .InsertOrUpdate(
        () => new Product { ProductID = 42, Name = "Widget", Price = 9.99m },
        p  => new Product { Price = 9.99m },
        p  => p.ProductID == 42);  // explicit key
```

---

## See also

- [`crud-insert-values.md`](crud-insert-values.md) — plain insert from object or expressions
- [`crud-insert-select.md`](crud-insert-select.md) — `INSERT … SELECT`
- [`provider-capabilities.md`](provider-capabilities.md) — upsert support per provider
- [`agent-antipatterns.md`](agent-antipatterns.md) — anti-pattern #9 (InsertOrReplace + Identity)
