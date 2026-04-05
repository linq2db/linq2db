# LinqToDB — Insert from Values / Object

> **Required:** Read [`AGENT_GUIDE.md`](../../AGENT_GUIDE.md) before any implementation.

> **You are here if** you need to:
> - insert a single row from a C# object
> - insert a row and retrieve the database-generated identity value
> - insert only specific columns at runtime (column filter)
> - insert using SQL expressions as values (`Sql.CurrentTimestamp`, etc.)
> - build the insert column-by-column in a fluent chain
> - insert a row and receive the full inserted record back (`OUTPUT / RETURNING`)
>
> For `INSERT … SELECT` (source is a query) → [`crud-insert-select.md`](crud-insert-select.md)
> For upsert → [`crud-upsert.md`](crud-upsert.md)

---

> **Async:** All methods have `Async` counterparts accepting an optional `CancellationToken`.
> Examples use synchronous forms for brevity; add `Async` suffix and `await` in async contexts.
> Async methods require `using LinqToDB.Async;`.

> **Table targeting:** `db.Insert`, `db.InsertWithInt32Identity`, etc. accept optional parameters
> to override the target table derived from the `[Table]` mapping attribute:
> `tableName?`, `databaseName?`, `schemaName?`, `serverName?`, `tableOptions?`
> Omit them when the table name comes from the `[Table]` attribute.

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
int     newId = db.InsertWithInt32Identity(new Product { Name = "Widget", Price = 9.99m });
long    newId = db.InsertWithInt64Identity(entity);   // bigint identity
decimal id    = db.InsertWithDecimalIdentity(entity); // decimal identity (rare, e.g. Oracle)
object  id    = db.InsertWithIdentity(entity);        // when type is unknown at compile time
```

All variants accept an optional `InsertColumnFilter<T>` as the second parameter, followed by the
optional table targeting parameters described in the preamble.

---

## 3. Expression-based insert on `ITable<T>`

When values to insert are SQL expressions rather than application-side values, use the
`ITable<T>.Insert` overload with a setter lambda. Generates `INSERT INTO T (...) VALUES (...)`
where the values are translated to SQL expressions.

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

> Only explicitly assigned properties in `new T { ... }` are included in the `INSERT` statement — unassigned columns are omitted entirely.

---

## 4. Fluent value builder — `Into` + `Value` + `Insert`

The `Into` + `Value` pattern builds the insert query column by column via `IValueInsertable<T>`.
**Required** when the mapping class is an interface — `new IProduct { ... }` is not valid C#, so
the expression-based pattern (section 3) is not available.
Also use when column inclusion is conditional at runtime, when values are a mix of
application-side constants and SQL expressions, or simply as a style preference.

```csharp
var insert = db.Into(db.GetTable<Product>())
    .Value(p => p.Name,      "Widget")
    .Value(p => p.Price,     9.99m)
    .Value(p => p.CreatedAt, () => Sql.CurrentTimestamp);  // SQL expression

if (includeCategory)
    insert = insert.Value(p => p.CategoryID, categoryId);

insert.Insert();  // or InsertWithInt32Identity(), InsertWithOutput(), ...
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

## 5. OUTPUT / RETURNING — receive the inserted record

Inserts a row and returns the full database-populated record (server-generated identity,
defaults, computed columns). Maps to `OUTPUT INSERTED` (SQL Server) or `RETURNING` (PostgreSQL, etc.).

**Provider support:** SQL Server 2005+, PostgreSQL, SQLite 3.35+, Firebird 2.5+, MariaDB 10.5+.
Check the `OUTPUT / RETURNING` column in [`provider-capabilities.md`](../provider-capabilities.md) before using.

### Return the full inserted record

```csharp
Product inserted = db.GetTable<Product>()
    .InsertWithOutput(() => new Product { Name = "Widget", Price = 9.99m });

Console.WriteLine(inserted.ProductID); // server-generated identity
```

### Return a projection only

```csharp
var result = db.GetTable<Product>()
    .InsertWithOutput(
        () => new Product { Name = "Widget", Price = 9.99m },
        r  => new { r.ProductID, r.Name });
```

### Redirect OUTPUT into a separate table — `InsertWithOutputInto` (SQL Server 2005+ only)

```csharp
db.GetTable<Product>()
    .InsertWithOutputInto(
        () => new Product { Name = "Widget", Price = 9.99m },
        db.GetTable<ProductAuditLog>());
```

Available on both `ITable<T>` (single-row setter) and `IValueInsertable<T>` (fluent builder).

---

## See also

- [`crud-insert-select.md`](crud-insert-select.md) — `INSERT … SELECT` from a query
- [`crud-upsert.md`](crud-upsert.md) — upsert (`InsertOrReplace`, `InsertOrUpdate`)
- [`provider-capabilities.md`](../provider-capabilities.md) — `OUTPUT / RETURNING` support per provider
- [`agent-antipatterns.md`](../agent-antipatterns.md) — anti-pattern #9 (InsertOrReplace + Identity)
