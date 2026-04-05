# LinqToDB — Inserting Data

> You are here if you need to:
> - insert a single row from a C# object
> - insert a row and retrieve the database-generated identity value
> - insert only specific columns (column filter)
> - insert rows using a SQL expression (`INSERT INTO ... SELECT ...`)
> - insert rows from a complex query with JOINs into a specific target table
> - insert a row and receive the full inserted record back (`INSERT ... OUTPUT / RETURNING`)
> - insert-or-update a row (upsert)
> - insert from one source into multiple tables (Oracle only)
> - bulk-insert many rows → see [`docs/provider-capabilities.md`](provider-capabilities.md)

---

> **Async:** All insert methods have `Async` counterparts accepting an optional `CancellationToken`.
> Examples use synchronous forms for brevity; add `Async` suffix and `await` in async contexts.

> **Table targeting:** The entity-based methods on `IDataContext` (`db.Insert`, `db.InsertWithInt32Identity`,
> `db.InsertOrReplace`, etc.) accept optional parameters to override the target table derived from the
> `[Table]` mapping attribute:
> ```
> tableName?, databaseName?, schemaName?, serverName?, tableOptions?
> ```
> Omit all of these for the standard case where the table name comes from the `[Table]` attribute.

---

## 1. Simple object insert — `db.Insert`

Inserts a single C# object. LinqToDB infers the target table from the `[Table]` mapping on the type.
Returns the number of affected rows (typically 1).

```csharp
using var db = new DataConnection(_options);

db.Insert(new Product { Name = "Widget", Price = 9.99m, IsActive = true });
```

### Column filter

To insert only a subset of columns at runtime, pass an `InsertColumnFilter<T>` delegate.
The delegate receives the entity and a `ColumnDescriptor`; return `true` to include the column.

```csharp
// Insert all columns except audit timestamps
db.Insert(product, (entity, col) =>
    col.MemberName != nameof(Product.CreatedAt) &&
    col.MemberName != nameof(Product.UpdatedAt));
```

---

## 2. Insert with identity return

Use when the table has a database-generated (`[Identity]`) primary key and you need the assigned value back.

```csharp
int    newId = db.InsertWithInt32Identity(new Product { Name = "Widget", Price = 9.99m });
long   newId = db.InsertWithInt64Identity(entity);   // bigint identity
decimal id   = db.InsertWithDecimalIdentity(entity); // decimal identity (rare, e.g. Oracle)
object  id   = db.InsertWithIdentity(entity);        // when type is unknown at compile time
```

All variants accept an optional `InsertColumnFilter<T>` as the second parameter, followed by the
optional table targeting parameters described in the preamble.

---

## 3. Expression-based insert on `ITable<T>`

When the values to insert are SQL expressions rather than application-side values, use the
`ITable<T>.Insert` overload with a setter lambda. This generates `INSERT INTO T (...) VALUES (...)` 
where the values are translated SQL expressions.

```csharp
db.GetTable<Product>()
    .Insert(() => new Product
    {
        Name      = "Widget",
        Price     = 9.99m,
        CreatedAt = Sql.CurrentTimestamp,  // server-side SQL expression
    });
```

Identity variants are available on `ITable<T>` as well:

```csharp
int id = db.GetTable<Product>()
    .InsertWithInt32Identity(() => new Product { Name = "Widget", Price = 9.99m });
```

---

## 4. Fluent value builder — `Into` + `Value` + `Insert`

The `Into` + `Value` pattern builds the insert query column by column via `IValueInsertable<T>`.
Use it when column inclusion is conditional at runtime, or when values are a mix of
application-side constants and SQL expressions.

```csharp
// Start a value-insertable from the data context
var insert = db.Into(db.GetTable<Product>())
    .Value(p => p.Name,      "Widget")
    .Value(p => p.Price,     9.99m)
    .Value(p => p.CreatedAt, () => Sql.CurrentTimestamp);  // SQL expression

if (includeCategory)
    insert = insert.Value(p => p.CategoryID, categoryId);

insert.Insert();           // or InsertWithInt32Identity(), InsertWithOutput(), ...
```

Alternatively, start directly from the table:

```csharp
db.GetTable<Product>()
    .AsValueInsertable()
    .Value(p => p.Name,  "Widget")
    .Value(p => p.Price, 9.99m)
    .Insert();
```

---

## 5. INSERT ... SELECT

Inserts rows from a LINQ source query directly into a target table without materializing data
in the application. Generates a single `INSERT INTO target SELECT ... FROM source` statement.

### Simple source (same or compatible types)

```csharp
// Copy inactive products to an archive table
db.GetTable<Product>()
    .Where(p => !p.IsActive)
    .Insert(
        db.GetTable<ProductArchive>(),
        p => new ProductArchive { ID = p.ProductID, Name = p.Name, Price = p.Price });
```

### Complex source — JOINs, projections (explicit target)

When the source is a JOIN or anonymous projection, the target table cannot be inferred —
pass it explicitly as the second argument:

```csharp
var source = from p in db.GetTable<Product>()
             join c in db.GetTable<Category>() on p.CategoryID equals c.ID
             select new { p, c };

source.Insert(
    db.GetTable<ProductArchive>(),
    x => new ProductArchive { ID = x.p.ProductID, CategoryName = x.c.Name });
```

### Fluent SELECT-INSERT — `source.Into(target).Value(...).Insert()`

Use when you need to add extra constant columns that are not present in the source projection:

```csharp
db.GetTable<Product>()
    .Where(p => !p.IsActive)
    .Into(db.GetTable<ProductArchive>())
    .Value(a => a.ID,          p => p.ProductID)
    .Value(a => a.Name,        p => p.Name)
    .Value(a => a.ArchivedAt,  () => Sql.CurrentTimestamp)  // extra column
    .Insert();
```

---

## 6. INSERT with OUTPUT / RETURNING

Inserts a row and returns the full database-populated record (server-generated identity,
default values, computed columns). Maps to `OUTPUT INSERTED` (SQL Server) or `RETURNING` (PostgreSQL, etc.).

**Provider support:** SQL Server 2005+, PostgreSQL, SQLite 3.35+, Firebird 2.5+, MariaDB 10.5+.
Check the `OUTPUT / RETURNING` column in [`docs/provider-capabilities.md`](provider-capabilities.md) before using.

### Single row — returns inserted record

```csharp
// Returns the full inserted record with all server-filled values
Product inserted = db.GetTable<Product>()
    .InsertWithOutput(() => new Product { Name = "Widget", Price = 9.99m });

Console.WriteLine(inserted.ProductID); // server-generated identity
```

### Single row with output projection

```csharp
var result = db.GetTable<Product>()
    .InsertWithOutput(
        () => new Product { Name = "Widget", Price = 9.99m },
        r  => new { r.ProductID, r.Name });   // project only what you need
```

### SELECT INSERT + OUTPUT — returns all inserted records

```csharp
// Returns IAsyncEnumerable<ProductArchive> of all inserted rows
IAsyncEnumerable<ProductArchive> inserted =
    db.GetTable<Product>()
        .Where(p => !p.IsActive)
        .InsertWithOutputAsync(
            db.GetTable<ProductArchive>(),
            p => new ProductArchive { ID = p.ProductID, Name = p.Name });

await foreach (var row in inserted)
    Console.WriteLine(row.ID);
```

### Output into a separate table — `InsertWithOutputInto` (SQL Server 2005+ only)

```csharp
// Redirect OUTPUT rows into a dedicated audit table instead of returning them
db.GetTable<Product>()
    .InsertWithOutputInto(
        () => new Product { Name = "Widget", Price = 9.99m },
        db.GetTable<ProductAuditLog>());
```

---

## 7. Upsert — `db.InsertOrReplace`

Inserts the row if no matching primary key exists; updates it if one does.

```csharp
db.InsertOrReplace(new Product
{
    ProductID = 42,     // must be a caller-supplied PK — NOT an identity column
    Name      = "Widget",
    Price     = 9.99m,
});
```

An `InsertOrUpdateColumnFilter<T>` overload is available to control which columns
participate in the insert and update phases independently.

> **Constraint:** `InsertOrReplace` requires a caller-supplied primary key.
> It throws `LinqToDBException` at query build time if the entity has an `[Identity]` PK.
> See anti-pattern #9 in [`docs/agent-antipatterns.md`](agent-antipatterns.md).

---

## 8. Upsert with expressions — `InsertOrUpdate`

`InsertOrUpdate` is a LINQ extension on `ITable<T>` that lets you provide separate expressions
for the INSERT and UPDATE phases. Check provider support before using:
see `Upsert` column in [`docs/provider-capabilities.md`](provider-capabilities.md).

```csharp
db.GetTable<Product>()
    .InsertOrUpdate(
        () => new Product { ProductID = 42, Name = "Widget", Price = 9.99m },  // INSERT
        p  => new Product { Name = p.Name, Price = 9.99m });                    // UPDATE
```

---

## 9. Multi-table insert — `MultiInsert` (Oracle only)

Inserts rows from one source query into multiple target tables in a single statement.
Only supported by the Oracle provider.

```csharp
db.GetTable<SourceEvent>()
    .MultiInsert()
    .Into(db.GetTable<EventLog>(),     e => new EventLog     { ID = e.ID, Type = e.Type })
    .When(e => e.Type == "order",
        db.GetTable<OrderEvent>(),     e => new OrderEvent   { OrderID = e.RefID })
    .When(e => e.Type == "payment",
        db.GetTable<PaymentEvent>(),   e => new PaymentEvent { Amount = e.Amount })
    .Insert();
```

Use `.Into(...)` for unconditional insert and `.When(condition, ...)` for conditional insert.

---

## 10. Bulk copy

For inserting large numbers of rows use `DataConnection.BulkCopy`.
Provider support and behavior vary significantly — check the `Bulk Copy` column in
[`docs/provider-capabilities.md`](provider-capabilities.md) before using.

---

## See also

- [`docs/crud-update.md`](crud-update.md) — updating existing rows
- [`docs/agent-antipatterns.md`](agent-antipatterns.md) — anti-pattern #9 (InsertOrReplace + Identity)
- [`docs/provider-capabilities.md`](provider-capabilities.md) — OUTPUT/RETURNING, upsert, bulk copy support per provider


