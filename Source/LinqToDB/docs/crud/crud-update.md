# LinqToDB — Updating Data

> **You are here if** you need to:
> - update an existing row using an entity object
> - update rows matching a query or predicate (set-based, without loading entities)
> - update a related table driven by a source query (cross-table update)
> - build the SET clause column-by-column at runtime (`AsUpdatable` + `Set`)
> - receive before/after images of updated rows (`OUTPUT / RETURNING`)
> - redirect OUTPUT into another table

---

> **Async:** All methods have `Async` counterparts accepting an optional `CancellationToken`.
> Examples use synchronous forms for brevity; add `Async` suffix and `await` in async contexts.

> **Table targeting:** `db.Update` accepts optional parameters to override the target table
> derived from the `[Table]` mapping attribute:
> `tableName?`, `databaseName?`, `schemaName?`, `serverName?`, `tableOptions?`
> Omit them when the table name comes from the `[Table]` attribute.

---

## Pattern quick-reference

| Scenario | Pattern |
|---|---|
| Update a loaded entity by PK | `db.Update(entity)` — section 1 |
| Update matching rows, all SET assignments known upfront | expression setter `p => new T { ... }` — section 2 |
| Update matching rows, SET clause built at runtime | `AsUpdatable().Set(...)` — section 4 |
| Mapping class is an interface | `AsUpdatable().Set(...)` — section 4 |
| Update a related table driven by a source query | cross-table update — section 3 |
| Need before/after images of updated rows | `UpdateWithOutput` — section 5 |
| Write output into another table | `UpdateWithOutputInto` — section 6 |

> **Column generation:** `p => new T { Col = val }` (sections 2, 3) emits SQL **only for explicitly assigned properties** — unassigned columns are left untouched.

---

## 1. Update by entity — `db.Update`

Updates all mapped non-PK columns of the row identified by the entity's primary key.
The `WHERE` clause is built from `[PrimaryKey]` column(s).

```csharp
using var db = new DataConnection(_options);

product.Price    = 12.99m;
product.IsActive = false;

int affected = await db.UpdateAsync(product);
```

### Column filter

To update only a subset of columns at runtime, pass an `UpdateColumnFilter<T>` delegate.
The delegate receives the entity and a `ColumnDescriptor`; return `true` to include the column.

```csharp
// Update only Price and IsActive, skip everything else
await db.UpdateAsync(product, (entity, col) =>
    col.MemberName == nameof(Product.Price) ||
    col.MemberName == nameof(Product.IsActive));
```

---

## 2. Set-based update — expression setter

Updates matching rows directly in SQL without loading entities first.
Only the columns assigned in the setter lambda are included in `SET`; all other columns are untouched.

```csharp
int affected = await db.GetTable<Product>()
    .Where(p => !p.IsActive)
    .UpdateAsync(p => new Product { Stock = 0 });
```

### Inline predicate overload

Pass a filter predicate and a setter together instead of chaining `.Where`:

```csharp
int affected = await db.GetTable<Product>()
    .UpdateAsync(
        p => !p.IsActive,
        p => new Product { Stock = 0 });
```

---

## 3. Cross-table update — source drives target

Updates a related table using a source query as a driver.
The `target` expression navigates from the source record to the row that should be updated.

```csharp
// For each expired order, stamp the related customer's LastOrderDate
int affected = await db.GetTable<Order>()
    .Where(o => o.IsExpired)
    .UpdateAsync(
        o => o.Customer,                                          // target: navigate to Customer
        o => new Customer { LastOrderDate = Sql.CurrentTimestamp });
```

---

## 4. Fluent column-by-column — `AsUpdatable` + `Set`

**Required** when the mapping class is an interface — `new IProduct { ... }` is not valid C#, so
the expression-setter pattern (sections 2, 3) is not available.
Also use when the SET clause must be built conditionally at runtime, when column values are a mix
of application-side constants and SQL expressions, or simply as a style preference.

```csharp
var updatable = db.GetTable<Product>()
    .Where(p => p.CategoryId == categoryId)
    .AsUpdatable()
    .Set(p => p.ModifiedAt, () => Sql.CurrentTimestamp)   // SQL expression
    .Set(p => p.ModifiedBy, userId);                       // constant value

if (changeName)
    updatable = updatable.Set(p => p.Name, newName);

await updatable.UpdateAsync();
```

`Set` can also reference the current row value:

```csharp
await db.GetTable<Product>()
    .Where(p => p.IsOnSale)
    .Set(p => p.Price, p => p.Price * 0.9m)   // 10% discount
    .UpdateAsync();
```

---

## 5. OUTPUT / RETURNING — receive before/after images

Updates rows and streams back `UpdateOutput<T>` records containing `Deleted` (before) and `Inserted` (after) images.
Execution is deferred until enumeration.

**Provider support:** SQL Server 2005+, Firebird 2.5+.
For a custom projection with PostgreSQL (v18+ for `deleted` access) or SQLite 3.35+, use the projection overload below.
Check the `OUTPUT / RETURNING` column in [`provider-capabilities.md`](../provider-capabilities.md) before using.

### Return before/after images

```csharp
IAsyncEnumerable<UpdateOutput<Product>> output = db.GetTable<Product>()
    .Where(p => p.IsOnSale)
    .UpdateWithOutputAsync(p => new Product { Price = p.Price * 0.9m });

await foreach (var row in output)
    Console.WriteLine($"{row.Deleted.Price} → {row.Inserted.Price}");
```

### Return a custom projection

The `outputExpression` receives `(deleted, inserted)` and maps them to a custom type.

```csharp
IAsyncEnumerable<PriceChange> changes = db.GetTable<Product>()
    .Where(p => p.IsOnSale)
    .UpdateWithOutputAsync(
        p  => new Product { Price = p.Price * 0.9m },
        (deleted, inserted) => new PriceChange
        {
            ProductID = inserted.ProductID,
            OldPrice  = deleted.Price,
            NewPrice  = inserted.Price,
        });
```

`UpdateWithOutput` also works on an `IUpdatable<T>` built with `AsUpdatable()` + `Set()`:

```csharp
IAsyncEnumerable<UpdateOutput<Product>> output = db.GetTable<Product>()
    .Where(p => p.IsOnSale)
    .Set(p => p.Price, p => p.Price * 0.9m)
    .UpdateWithOutputAsync();
```

---

## 6. Redirect OUTPUT into a table — `UpdateWithOutputInto` (SQL Server 2005+ only)

Updates rows and writes the output into a separate table in the same statement.
Returns the number of affected rows.

```csharp
int affected = await db.GetTable<Product>()
    .Where(p => p.IsOnSale)
    .UpdateWithOutputIntoAsync(
        p => new Product { Price = p.Price * 0.9m },
        db.GetTable<PriceAuditLog>());
```

### With projection

Map before/after images to the output table with `(deleted, inserted)`:

```csharp
int affected = await db.GetTable<Product>()
    .Where(p => p.IsOnSale)
    .UpdateWithOutputIntoAsync(
        p  => new Product { Price = p.Price * 0.9m },
        db.GetTable<PriceAuditLog>(),
        (deleted, inserted) => new PriceAuditLog
        {
            ProductID = inserted.ProductID,
            OldPrice  = deleted.Price,
            NewPrice  = inserted.Price,
            ChangedAt = Sql.CurrentTimestamp,
        });
```

---

## See also

- [`crud-insert.md`](crud-insert.md) — inserting rows, identity, upsert
- [`crud-delete.md`](crud-delete.md) — deleting rows
- [`provider-capabilities.md`](../provider-capabilities.md) — `OUTPUT / RETURNING` support per provider
