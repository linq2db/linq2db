<!-- Generated from: Source/Skills/linq2db/docs/crud/crud.md -->

# LinqToDB CRUD Operations

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

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
| Query / read data - filtering, projection, ordering, pagination, associations | [`docs/crud/crud-select.md`](09-crud-and-merge.md) |
| Insert from a C# object, expression, or fluent column-by-column builder | [`docs/crud/crud-insert-values.md`](09-crud-and-merge.md) |
| `INSERT … SELECT` - copy or archive rows from a query, with JOINs or projections | [`docs/crud/crud-insert-select.md`](09-crud-and-merge.md) |
| Upsert - insert-or-update semantics (`InsertOrReplace`, `InsertOrUpdate`) | [`docs/crud/crud-upsert.md`](09-crud-and-merge.md) |
| Update rows - full entity or partial expression-based update | [`docs/crud/crud-update.md`](09-crud-and-merge.md) |
| Delete rows - by entity or by predicate | [`docs/crud/crud-delete.md`](09-crud-and-merge.md) |
| Bulk copy / batch insert - `BulkCopy` / `BulkCopyAsync` | [`docs/crud/crud-bulkcopy.md`](09-crud-and-merge.md) |
| MERGE - SQL MERGE statement via `Merge` LINQ extension | [`docs/crud/crud-merge.md`](09-crud-and-merge.md) |

---

## Out of scope for this guide

| Topic | See instead |
|---|---|
| Transactions | [`docs/configuration.md`](07-provider-configuration.md) - `BeginTransaction`, `TransactionScope` |
| Schema creation (`CreateTable`) | [`docs/agent-antipatterns.md`](06-agent-antipatterns-and-ai-tags.md) - anti-pattern #10 |
| Custom SQL functions | [`docs/custom-sql.md`](13-custom-sql.md) |
| CTE, OUTPUT / RETURNING | [`docs/provider-capabilities.md`](07-provider-configuration.md) |

<!-- Generated from: Source/Skills/linq2db/docs/crud/crud-select.md -->

# LinqToDB - Querying Data

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> You are here if you need to:
> - read rows from a table
> - filter, sort, project, or paginate query results
> - load associated entities (`LoadWith` / `ThenLoad`)
> - avoid common query translation mistakes
>
> For CTEs, named intermediate queries, or recursive traversal → [`query-cte.md`](10-query-composition.md)

---

> **Async:** Materialization and aggregation methods have `Async` counterparts (`ToListAsync`,
> `FirstOrDefaultAsync`, `SingleAsync`, `CountAsync`, `MaxAsync`, etc.).
> Examples use both forms. All async methods require:
> ```csharp
> using LinqToDB.Async;
> ```

---

## Entry point - `GetTable<T>`

All queries start with `IDataContext.GetTable<T>()`, which returns a composable `IQueryable<T>`.
Apply LINQ operators to build the query, then materialize it.

```csharp
using var db = new DataConnection(_options);

var products = await db.GetTable<Product>()
    .Where(p => p.IsActive && p.Price < 100m)
    .OrderBy(p => p.Name)
    .ToListAsync();
```

All operators applied before materialization are translated to SQL.
Materialization methods: `ToList`, `ToArray`, `ToListAsync`, `ToArrayAsync`, `FirstOrDefault`, `SingleAsync`, etc.

---

## Projection - `Select`

```csharp
var names = await db.GetTable<Product>()
    .Where(p => p.IsActive)
    .Select(p => new { p.ProductID, p.Name, p.Price })
    .ToListAsync();
```

Project before materialization to avoid fetching unused columns.

---

## Pagination - `Skip` / `Take`

```csharp
var page = await db.GetTable<Product>()
    .OrderBy(p => p.ProductID)   // ORDER BY is required for deterministic pagination
    .Skip(pageIndex * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

`Skip` without `OrderBy` produces non-deterministic results - always pair them.

---

## Loading associations - `LoadWith` / `ThenLoad`

LinqToDB does not support lazy loading. Use `LoadWith` to load associated entities eagerly.
Each `LoadWith` call issues one additional SQL query per association level.

### Basic association load

```csharp
var orders = await db.GetTable<Order>()
    .Where(o => o.CustomerID == customerId)
    .LoadWith(o => o.OrderItems)   // child collection
    .LoadWith(o => o.Customer)     // parent reference
    .ToListAsync();
```

### Nested associations - `ThenLoad`

`ThenLoad` chains directly off `LoadWith` (or a prior `ThenLoad`) and navigates one level deeper.
The selector lambda receives the *type of the previously loaded property*:

```csharp
var orders = await db.GetTable<Order>()
    .LoadWith(o => o.OrderItems)
        .ThenLoad(item => item.Product)             // load Product for each OrderItem
            .ThenLoad(product => product.Category)  // load Category for each Product
    .LoadWith(o => o.Customer)                      // independent branch, back to root
    .ToListAsync();
```

Equivalent inline dot-path notation (produces identical queries):

```csharp
db.GetTable<Order>()
    .LoadWith(o => o.OrderItems[0].Product.Category)
    .LoadWith(o => o.Customer)
    .ToListAsync();
```

Prefer `ThenLoad` when the chain is long or when a filter is needed at a specific level.

### Filtering a loaded association - `loadFunc`

The optional second parameter constrains the rows fetched for an association:

```csharp
var orders = await db.GetTable<Order>()
    .LoadWith(o => o.OrderItems,
        q => q.Where(i => !i.IsCancelled).OrderBy(i => i.SortOrder))
    .ToListAsync();
```

`ThenLoad` accepts `loadFunc` as well:

```csharp
db.GetTable<Order>()
    .LoadWith(o => o.OrderItems)
        .ThenLoad(item => item.Tags, q => q.Where(t => t.IsVisible))
    .ToListAsync();
```

To load a nested association from inside `loadFunc`, call `LoadWith` on the inner query:

```csharp
db.GetTable<Order>()
    .LoadWith(o => o.OrderItems, q => q.LoadWith(i => i.Product))
    .ToListAsync();
```

---

## Aggregates

```csharp
int    count    = await db.GetTable<Product>().CountAsync(p => p.IsActive);
decimal maxPrice = await db.GetTable<Product>().MaxAsync(p => p.Price);
```

---

## Common Table Expressions - CTE

For named intermediate queries, query factoring, or recursive traversal,
see [`query-cte.md`](10-query-composition.md).
Check provider support in [`provider-capabilities.md`](07-provider-configuration.md) before using.

---

## Anti-patterns to avoid

| Mistake | Consequence | Reference |
|---|---|---|
| Applying `.Where()` after `.ToList()` | Filter runs in memory - full table fetched | Anti-pattern #5 |
| Using a non-translatable method inside `.Where()` | `LinqToDBException` at execution time | Anti-pattern #4 |
| `Skip` without `OrderBy` | Non-deterministic page results | - |
| `LoadWith` expected to behave like lazy loading or a single JOIN | One extra SQL query is issued per association level - not lazy, not a JOIN | `LoadWith` section above |

<!-- Generated from: Source/Skills/linq2db/docs/crud/crud-insert.md -->

# LinqToDB - Inserting Data

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> You are here if you need to insert data. Choose the guide that matches your scenario:

| Scenario | Guide |
|---|---|
| Insert from a C# object, setter expression, or fluent column-by-column builder | [`crud-insert-values.md`](09-crud-and-merge.md) |
| `INSERT … SELECT` - copy or archive rows from a query, with JOINs or projections | [`crud-insert-select.md`](09-crud-and-merge.md) |
| Upsert - insert-or-update semantics (`InsertOrReplace`, `InsertOrUpdate`) | [`crud-upsert.md`](09-crud-and-merge.md) |
| Bulk-insert many rows - `BulkCopy` | [`crud-bulkcopy.md`](09-crud-and-merge.md) |

<!-- Generated from: Source/Skills/linq2db/docs/crud/crud-insert-values.md -->

# LinqToDB - Insert from Values / Object

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> **You are here if** you need to:
> - insert a single row from a C# object
> - insert a row and retrieve the database-generated identity value
> - insert only specific columns at runtime (column filter)
> - insert using SQL expressions as values (`Sql.CurrentTimestamp`, etc.)
> - build the insert column-by-column in a fluent chain
> - insert a row and receive the full inserted record back (`OUTPUT / RETURNING`)
>
> For `INSERT … SELECT` (source is a query) → [`crud-insert-select.md`](09-crud-and-merge.md)
> For upsert → [`crud-upsert.md`](09-crud-and-merge.md)

---

> **Async:** All methods have `Async` counterparts accepting an optional `CancellationToken`.
> Examples use synchronous forms for brevity; add `Async` suffix and `await` in async contexts.
> Async methods require `using LinqToDB.Async;`.

> **Table targeting:** `db.Insert`, `db.InsertWithInt32Identity`, etc. accept optional parameters
> to override the target table derived from the `[Table]` mapping attribute:
> `tableName?`, `databaseName?`, `schemaName?`, `serverName?`, `tableOptions?`
> Omit them when the table name comes from the `[Table]` attribute.

---

## 1. Simple object insert - `db.Insert`

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

> Only explicitly assigned properties in `new T { ... }` are included in the `INSERT` statement - unassigned columns are omitted entirely.

---

## 4. Fluent value builder - `Into` + `Value` + `Insert`

The `Into` + `Value` pattern builds the insert query column by column via `IValueInsertable<T>`.
**Required** when the mapping class is an interface - `new IProduct { ... }` is not valid C#, so
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

## 5. OUTPUT / RETURNING - receive the inserted record

Inserts a row and returns the full database-populated record (server-generated identity,
defaults, computed columns). Maps to `OUTPUT INSERTED` (SQL Server) or `RETURNING` (PostgreSQL, etc.).

**Provider support:** SQL Server 2005+, PostgreSQL, SQLite 3.35+, Firebird 2.5+, MariaDB 10.5+.
Check the `OUTPUT / RETURNING` column in [`provider-capabilities.md`](07-provider-configuration.md) before using.

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

### Redirect OUTPUT into a separate table - `InsertWithOutputInto` (SQL Server 2005+ only)

```csharp
db.GetTable<Product>()
    .InsertWithOutputInto(
        () => new Product { Name = "Widget", Price = 9.99m },
        db.GetTable<ProductAuditLog>());
```

Available on both `ITable<T>` (single-row setter) and `IValueInsertable<T>` (fluent builder).

---

## See also

- [`crud-insert-select.md`](09-crud-and-merge.md) - `INSERT … SELECT` from a query
- [`crud-upsert.md`](09-crud-and-merge.md) - upsert (`InsertOrReplace`, `InsertOrUpdate`)
- [`provider-capabilities.md`](07-provider-configuration.md) - `OUTPUT / RETURNING` support per provider
- [`agent-antipatterns.md`](06-agent-antipatterns-and-ai-tags.md) - anti-pattern #9 (InsertOrReplace + Identity)

<!-- Generated from: Source/Skills/linq2db/docs/crud/crud-insert-select.md -->

# LinqToDB - INSERT … SELECT

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> **You are here if** you need to:
> - copy rows from one table to another without materializing them in the application
> - insert results of a JOIN, filter, or projection into a target table
> - add extra constant or computed columns alongside the source projection
> - receive the inserted rows back via `OUTPUT / RETURNING`
> - insert from one source into multiple Oracle tables in a single statement
>
> For inserting from a C# object or expression values → [`crud-insert-values.md`](09-crud-and-merge.md)
> For upsert → [`crud-upsert.md`](09-crud-and-merge.md)

---

> **Async:** All methods have `Async` counterparts accepting an optional `CancellationToken`.
> Examples use synchronous forms for brevity; add `Async` suffix and `await` in async contexts.
> Async methods require `using LinqToDB.Async;`.

---

## 1. Simple source - same or compatible types

Generates a single `INSERT INTO target SELECT … FROM source` statement.
No data is materialized in the application.

```csharp
// Copy inactive products to an archive table
db.GetTable<Product>()
    .Where(p => !p.IsActive)
    .Insert(
        db.GetTable<ProductArchive>(),
        p => new ProductArchive { ID = p.ProductID, Name = p.Name, Price = p.Price });
```

> Only explicitly assigned properties in the setter lambda are included in the INSERT - unassigned columns are omitted entirely.

---

## 2. Complex source - JOINs and projections

When the source is a JOIN or an anonymous projection the target table cannot be inferred -
pass it explicitly as the second argument.

```csharp
var source = from p in db.GetTable<Product>()
             join c in db.GetTable<Category>() on p.CategoryID equals c.ID
             select new { p, c };

source.Insert(
    db.GetTable<ProductArchive>(),
    x => new ProductArchive { ID = x.p.ProductID, CategoryName = x.c.Name });
```

---

## 3. Fluent SELECT-INSERT - `source.Into(target).Value(…).Insert()`

**Required** when the target mapping class is an interface - `new IProductArchive { ... }` is not
valid C#, so the expression setter (sections 1, 2) is not available.
Also use when you need to inject extra constant or SQL-expression columns that are not part of
the source projection, or simply as a style preference.

```csharp
db.GetTable<Product>()
    .Where(p => !p.IsActive)
    .Into(db.GetTable<ProductArchive>())
        .Value(a => a.ID,         p => p.ProductID)
        .Value(a => a.Name,       p => p.Name)
        .Value(a => a.ArchivedAt, () => Sql.CurrentTimestamp)  // extra column
    .Insert();
```

Terminal operations on `ISelectInsertable<TSource, TTarget>`:
- `.Insert()` - returns affected row count
- `.InsertWithOutput()` - returns the inserted record (see §4)

---

## 4. OUTPUT / RETURNING - receive all inserted records

Inserts rows from the source query and streams back the inserted records.
Returns `IAsyncEnumerable<TTarget>` - enumeration triggers the INSERT.

**Provider support:** SQL Server 2005+, PostgreSQL, SQLite 3.35+, Firebird 2.5+, MariaDB 10.5+.
Check the `OUTPUT / RETURNING` column in [`provider-capabilities.md`](07-provider-configuration.md) before using.

```csharp
IAsyncEnumerable<ProductArchive> inserted =
    db.GetTable<Product>()
        .Where(p => !p.IsActive)
        .InsertWithOutputAsync(
            db.GetTable<ProductArchive>(),
            p => new ProductArchive { ID = p.ProductID, Name = p.Name });

await foreach (var row in inserted)
    Console.WriteLine(row.ID);
```

With an output projection:

```csharp
IAsyncEnumerable<int> ids =
    db.GetTable<Product>()
        .Where(p => !p.IsActive)
        .InsertWithOutputAsync(
            db.GetTable<ProductArchive>(),
            p  => new ProductArchive { ID = p.ProductID, Name = p.Name },
            r  => r.ID);
```

### Redirect OUTPUT into a separate table - `InsertWithOutputInto` (SQL Server 2005+ only)

```csharp
db.GetTable<Product>()
    .Where(p => !p.IsActive)
    .InsertWithOutputInto(
        db.GetTable<ProductArchive>(),
        p  => new ProductArchive { ID = p.ProductID, Name = p.Name },
        db.GetTable<AuditLog>());
```

---

## 5. Multi-table insert - `MultiInsert` (Oracle only)

Inserts rows from one source query into multiple target tables in a single Oracle-specific statement.
Use `.Into(…)` for unconditional insert and `.When(condition, …)` for conditional insert.

```csharp
db.GetTable<SourceEvent>()
    .MultiInsert()
    .Into(db.GetTable<EventLog>(),    e => new EventLog    { ID = e.ID, Type = e.Type })
    .When(e => e.Type == "order",
        db.GetTable<OrderEvent>(),    e => new OrderEvent  { OrderID = e.RefID })
    .When(e => e.Type == "payment",
        db.GetTable<PaymentEvent>(),  e => new PaymentEvent { Amount = e.Amount })
    .Insert();
```

---

## See also

- [`crud-insert-values.md`](09-crud-and-merge.md) - insert from C# object or expression values
- [`crud-upsert.md`](09-crud-and-merge.md) - upsert (`InsertOrReplace`, `InsertOrUpdate`)
- [`provider-capabilities.md`](07-provider-configuration.md) - `OUTPUT / RETURNING` support per provider

<!-- Generated from: Source/Skills/linq2db/docs/crud/crud-upsert.md -->

# LinqToDB - Upsert (Insert-or-Update)

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> **You are here if** you need to:
> - insert a row if the primary key does not exist, update it if it does
> - provide separate INSERT and UPDATE expressions for the two phases
> - control which columns participate in each phase at runtime
> - insert a row only when it does not exist, without touching the existing row
>
> For plain insert → [`crud-insert-values.md`](09-crud-and-merge.md)
> For `INSERT … SELECT` → [`crud-insert-select.md`](09-crud-and-merge.md)

---

> **Async:** All methods have `Async` counterparts accepting an optional `CancellationToken`.
> Examples use synchronous forms for brevity; add `Async` suffix and `await` in async contexts.
> Async methods require `using LinqToDB.Async;`.

> **Provider support:** upsert behavior varies across databases.
> Check the `Upsert` column in [`provider-capabilities.md`](07-provider-configuration.md) before using.
> LinqToDB emits a provider-native statement where possible and falls back to a multi-statement
> emulation for providers that do not support a native upsert syntax.

---

## Pattern quick-reference

| Scenario | Pattern |
|---|---|
| Upsert a loaded entity by PK | `db.InsertOrReplace(entity)` - section 1 |
| Upsert entity, control columns at runtime | `InsertOrReplace` + column filter - section 1 |
| Expression upsert, key from mapping | `InsertOrUpdate(insertSetter, updateSetter)` - section 2 |
| Expression upsert, explicit key | `InsertOrUpdate(insertSetter, updateSetter, keySelector)` - section 2 |
| Mapping class is an interface | `InsertOrUpdate(insertSetter, updateSetter)` - section 2 |
| Insert only when row absent (no update) | `InsertOrUpdate(insertSetter, null)` - section 3 |

> **Column generation:** Only explicitly assigned properties participate in generated SQL.
> Unassigned properties are omitted from INSERT and are not modified during UPDATE (sections 2, 3).
>
> For interface-mapped entities where `new IProduct { ... }` is not valid C#, use `InsertOrUpdate`
> (section 2) or the fluent builder in [`crud-insert-values.md`](09-crud-and-merge.md).

---

## 1. Entity upsert - `db.InsertOrReplace`

Inserts the row when no row with the same primary key exists; otherwise performs the
provider-specific replacement/update behavior for that row.
The key columns are taken from the entity's `[PrimaryKey]` mapping.

```csharp
using var db = new DataConnection(_options);

int affected = await db.InsertOrReplaceAsync(new Product
{
    ProductID = 42,      // caller-supplied PK - NOT an identity column
    Name      = "Widget",
    Price     = 9.99m,
});
```

### Column filter

Pass an `InsertOrUpdateColumnFilter<T>` delegate to control which columns participate in the
INSERT and UPDATE phases independently.
The delegate receives the entity, a `ColumnDescriptor`, and a `bool` indicating whether this
is the insert phase (`true`) or the update phase (`false`).

```csharp
await db.InsertOrReplaceAsync(product, (entity, col, isInsert) =>
    isInsert
        ? col.MemberName != nameof(Product.UpdatedAt)   // exclude on INSERT
        : col.MemberName != nameof(Product.CreatedAt)); // exclude on UPDATE
```

> **Constraint:** `InsertOrReplace` requires a caller-supplied primary key.
> It throws `LinqToDBException` at query build time when the entity has an `[Identity]` PK,
> or when no primary key columns are present.
> See anti-pattern #9 in [`agent-antipatterns.md`](06-agent-antipatterns-and-ai-tags.md).

---

## 2. Expression upsert - `InsertOrUpdate`

`InsertOrUpdate` is a LINQ extension on `ITable<T>` that accepts separate lambda expressions
for the INSERT and UPDATE phases.
Use when values differ between the two phases, involve SQL expressions, or the mapping class
is an interface (where `new IProduct { ... }` is not valid C#).

### Key from mapping (2-argument form)

The key is derived automatically from the `[PrimaryKey]` columns in the mapping:

```csharp
await db.GetTable<Product>()
    .InsertOrUpdateAsync(
        () => new Product { ProductID = 42, Name = "Widget", Price = 9.99m },  // INSERT
        p  => new Product { Name = "Widget", Price = 9.99m });                  // UPDATE
```

### Explicit key selector (3-argument form)

Pass an explicit key predicate when the key cannot be inferred from the mapping, or when you
want to match on columns other than the primary key:

```csharp
await db.GetTable<Product>()
    .InsertOrUpdateAsync(
        () => new Product { ProductID = 42, Name = "Widget", Price = 9.99m },
        p  => new Product { Price = 9.99m },
        p  => p.ProductID == 42);  // explicit key
```

---

## 3. Insert-if-not-exists - `InsertOrUpdate` with `null` update setter

Pass `null` as the update setter to perform an *insert if not exists* without updating
the existing row when a match is found.

```csharp
await db.GetTable<Product>()
    .InsertOrUpdateAsync(
        () => new Product { ProductID = 42, Name = "Widget", Price = 9.99m },
        null);  // no UPDATE - leave existing row untouched
```

With an explicit key:

```csharp
await db.GetTable<Product>()
    .InsertOrUpdateAsync(
        () => new Product { ProductID = 42, Name = "Widget", Price = 9.99m },
        null,
        p => p.ProductID == 42);
```

> **Provider note:** some providers emit `INSERT … WHERE NOT EXISTS …` while others
> use a native `MERGE` or `ON CONFLICT DO NOTHING` form.
> The generated SQL is provider-specific but the semantics are uniform.
>
> **Note:** This is not equivalent to inserting and ignoring duplicate-key exceptions.
> The existence check and insert are generated as a single provider-specific statement where possible.

---

## See also

- [`crud-insert-values.md`](09-crud-and-merge.md) - plain insert from object or expressions
- [`crud-insert-select.md`](09-crud-and-merge.md) - `INSERT … SELECT`
- [`crud-update.md`](09-crud-and-merge.md) - updating rows
- [`crud-merge.md`](09-crud-and-merge.md) - full MERGE builder (multi-operation sync, OUTPUT)
- [`provider-capabilities.md`](07-provider-configuration.md) - upsert support per provider
- [`agent-antipatterns.md`](06-agent-antipatterns-and-ai-tags.md) - anti-pattern #9 (`InsertOrReplace` + Identity)

<!-- Generated from: Source/Skills/linq2db/docs/crud/crud-update.md -->

# LinqToDB - Updating Data

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

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
> Async methods require `using LinqToDB.Async;`.

> **Table targeting:** `db.Update` accepts optional parameters to override the target table
> derived from the `[Table]` mapping attribute:
> `tableName?`, `databaseName?`, `schemaName?`, `serverName?`, `tableOptions?`
> Omit them when the table name comes from the `[Table]` attribute.

---

## Pattern quick-reference

| Scenario | Pattern |
|---|---|
| Update a loaded entity by PK | `db.Update(entity)` - section 1 |
| Update matching rows, all SET assignments known upfront | expression setter `p => new T { ... }` - section 2 |
| Update matching rows, SET clause built at runtime | `AsUpdatable().Set(...)` - section 4 |
| Mapping class is an interface | `AsUpdatable().Set(...)` - section 4 |
| Update a related table driven by a source query | cross-table update - section 3 |
| Need before/after images of updated rows | `UpdateWithOutput` - section 5 |
| Write output into another table | `UpdateWithOutputInto` - section 6 |

> **Column generation:** `p => new T { Col = val }` (sections 2, 3) emits SQL **only for explicitly assigned properties** - unassigned columns are left untouched.

---

## 1. Update by entity - `db.Update`

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

## 2. Set-based update - expression setter

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

## 3. Cross-table update - source drives target

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

## 4. Fluent column-by-column - `AsUpdatable` + `Set`

**Required** when the mapping class is an interface - `new IProduct { ... }` is not valid C#, so
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

## 5. OUTPUT / RETURNING - receive before/after images

Updates rows and streams back `UpdateOutput<T>` records containing `Deleted` (before) and `Inserted` (after) images.
Execution is deferred until enumeration.

**Provider support:** SQL Server 2005+, Firebird 2.5+.
For a custom projection with PostgreSQL (v18+ for `deleted` access) or SQLite 3.35+, use the projection overload below.
Check the `OUTPUT / RETURNING` column in [`provider-capabilities.md`](07-provider-configuration.md) before using.

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

## 6. Redirect OUTPUT into a table - `UpdateWithOutputInto` (SQL Server 2005+ only)

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

- [`crud-insert.md`](09-crud-and-merge.md) - inserting rows, identity, upsert
- [`crud-delete.md`](09-crud-and-merge.md) - deleting rows
- [`provider-capabilities.md`](07-provider-configuration.md) - `OUTPUT / RETURNING` support per provider

<!-- Generated from: Source/Skills/linq2db/docs/crud/crud-delete.md -->

# LinqToDB - Deleting Data

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> **You are here if** you need to:
> - delete a single row by entity
> - delete rows matching a query or predicate
> - receive deleted records back (`OUTPUT / RETURNING`)
> - redirect OUTPUT into another table

---

> **Async:** All methods have `Async` counterparts accepting an optional `CancellationToken`.
> Examples use synchronous forms for brevity; add `Async` suffix and `await` in async contexts.
> Async methods require `using LinqToDB.Async;`.

> **Table targeting:** `db.Delete` accepts optional parameters to override the target table
> derived from the `[Table]` mapping attribute:
> `tableName?`, `databaseName?`, `schemaName?`, `serverName?`, `tableOptions?`
> Omit them when the table name comes from the `[Table]` attribute.

---

## 1. Delete by entity - `db.Delete`

Deletes the row identified by the entity's primary key.
The `WHERE` clause is built from `[PrimaryKey]` column(s).

```csharp
using var db = new DataConnection(_options);

int affected = await db.DeleteAsync(product);
```

---

## 2. Set-based delete - query as filter

Deletes all rows matching the query without loading entities into memory.
Generates a single `DELETE FROM … WHERE …` statement.

```csharp
int affected = await db.GetTable<Product>()
    .Where(p => p.IsDiscontinued)
    .DeleteAsync();
```

### Inline predicate overload

Pass a predicate directly instead of chaining `.Where`:

```csharp
int affected = await db.GetTable<Product>()
    .DeleteAsync(p => p.IsDiscontinued);
```

---

## 3. OUTPUT / RETURNING - receive deleted records

Deletes rows and streams back the deleted records.
Execution is deferred until enumeration.

**Provider support:** SQL Server 2005+, PostgreSQL, SQLite 3.35+, Firebird 2.5+, MariaDB 10.0+.
Check the `OUTPUT / RETURNING` column in [`provider-capabilities.md`](07-provider-configuration.md) before using.

### Return the full deleted record

```csharp
IAsyncEnumerable<Product> deleted = db.GetTable<Product>()
    .Where(p => p.IsDiscontinued)
    .DeleteWithOutputAsync();

await foreach (var row in deleted)
    Console.WriteLine($"Deleted: {row.ProductID}");
```

### Return a projection only

```csharp
IAsyncEnumerable<int> ids = db.GetTable<Product>()
    .Where(p => p.IsDiscontinued)
    .DeleteWithOutputAsync(p => p.ProductID);
```

Synchronous forms (`DeleteWithOutput`, `DeleteWithOutput(outputExpression)`) return `IEnumerable<T>`
and are available when async enumeration is not needed.

---

## 4. Redirect OUTPUT into a table - `DeleteWithOutputInto` (SQL Server 2005+ only)

Deletes rows and writes the deleted records into a separate table in the same statement.
Returns the number of affected rows.

```csharp
int affected = await db.GetTable<Product>()
    .Where(p => p.IsDiscontinued)
    .DeleteWithOutputIntoAsync(db.GetTable<ProductAuditLog>());
```

### With projection

Map deleted columns to the output table with an expression:

```csharp
int affected = await db.GetTable<Product>()
    .Where(p => p.IsDiscontinued)
    .DeleteWithOutputIntoAsync(
        db.GetTable<DeletedProductLog>(),
        p => new DeletedProductLog { ProductID = p.ProductID, DeletedAt = Sql.CurrentTimestamp });
```

---

## See also

- [`crud-update.md`](09-crud-and-merge.md) - updating rows
- [`crud-insert.md`](09-crud-and-merge.md) - inserting rows
- [`provider-capabilities.md`](07-provider-configuration.md) - `OUTPUT / RETURNING` support per provider

<!-- Generated from: Source/Skills/linq2db/docs/crud/crud-bulkcopy.md -->

# LinqToDB - Bulk Copy

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> **You are here if** you need to:
> - insert a large number of rows as efficiently as possible
> - use the provider's native bulk-insert API (`SqlBulkCopy`, `COPY`, etc.)
> - control batch size, timeouts, identity handling, or conflict resolution during bulk insert
> - receive row-copy progress notifications during a bulk operation

---

> **Agent guidance:**
> - Use the simplest overload that satisfies the requirement. Do not introduce `BulkCopyOptions` or provider-specific flags unless the task explicitly requires them.
> - Do not force `BulkCopyType.ProviderSpecific` unless native performance is an explicit requirement and provider compatibility is known. `Default` is the safe choice for multi-provider code.
> - If provider compatibility is unclear and the task involves provider-specific options, ask rather than assume.

---

> **Async:** All `BulkCopy` methods have `BulkCopyAsync` counterparts accepting an optional `CancellationToken`.
> Both `IEnumerable<T>` and `IAsyncEnumerable<T>` sources are supported by the async overloads.
> Examples use synchronous forms for brevity; add `Async` suffix and `await` in async contexts.
> Async methods require `using LinqToDB.Async;`.

---

## Pattern quick-reference

| Scenario | Pattern |
|---|---|
| Insert a collection with default options | `db.BulkCopy(source)` - section 1 |
| Control batch size without building full options | `db.BulkCopy(maxBatchSize, source)` - section 1 |
| Full option control (type, timeout, flags) | `db.BulkCopy(new BulkCopyOptions(...), source)` - section 2 |
| Target a specific `ITable<T>` instance | `table.BulkCopy(source)` - section 1 |
| Choose insert strategy (native / multi-row / row-by-row) | `BulkCopyOptions.BulkCopyType` - section 3 |
| Preserve identity values from source data | `BulkCopyOptions.KeepIdentity = true` - section 4 |
| Track progress, cancel mid-flight | `NotifyAfter` + `RowsCopiedCallback` + `Abort` - section 5 |
| Skip / ignore conflicting rows | `BulkCopyOptions.ConflictAction = ConflictAction.Ignore` - section 6 |
| Provider-specific flags (locks, triggers, constraints) | `BulkCopyOptions` provider flags - section 7 |

---

## 1. Entry points

All overloads return `BulkCopyRowsCopied` (sync) or `Task<BulkCopyRowsCopied>` (async).
`BulkCopyRowsCopied.RowsCopied` holds the total row count; `Abort` can be set in the progress callback
to stop the operation early (see section 5).

> Prefer `db.BulkCopy(source)` when default options are sufficient.
> Only switch to `BulkCopyOptions` when there is an explicit requirement for a specific option.

**Simplest form - default options:**
```csharp
using LinqToDB;

BulkCopyRowsCopied result = db.BulkCopy(products);
Console.WriteLine(result.RowsCopied);
```

**With explicit batch size:**
```csharp
BulkCopyRowsCopied result = db.BulkCopy(maxBatchSize: 1000, source: products);
```

**With full `BulkCopyOptions`:**
```csharp
var options = new BulkCopyOptions(
    BulkCopyType: BulkCopyType.ProviderSpecific,
    MaxBatchSize: 5000);

BulkCopyRowsCopied result = db.BulkCopy(options, products);
```

**Table-targeted overloads** - use when the target table reference is already at hand
(e.g. a temporary table or a table with overridden name):
```csharp
ITable<Product> table = db.GetTable<Product>();
BulkCopyRowsCopied result = table.BulkCopy(products);
// or:
BulkCopyRowsCopied result = table.BulkCopy(options, products);
```

**Async with `IAsyncEnumerable<T>`:**
```csharp
using LinqToDB.Async;

await foreach (var batch in streamingSource)
    /* ... */

BulkCopyRowsCopied result = await db.BulkCopyAsync(asyncEnumerableSource, cancellationToken);
```

---

## 2. `BulkCopyOptions` - overview

`BulkCopyOptions` is a `sealed record`; construct it with named parameters and pass to the overload
that accepts `BulkCopyOptions`. All parameters are optional and default to `default`.

```csharp
var options = new BulkCopyOptions(
    BulkCopyType:           BulkCopyType.ProviderSpecific,
    MaxBatchSize:           5000,
    BulkCopyTimeout:        60,
    KeepIdentity:           true,
    NotifyAfter:            1000,
    RowsCopiedCallback:     r => Console.WriteLine($"{r.RowsCopied} rows copied"));

db.BulkCopy(options, source);
```

For target table overrides when the table name differs from the `[Table]` attribute:

```csharp
var options = new BulkCopyOptions(
    TableName:    "archive_products",
    SchemaName:   "dbo",
    DatabaseName: "ArchiveDB");

db.BulkCopy(options, source);
```

Available targeting overrides: `TableName`, `SchemaName`, `DatabaseName`, `ServerName`, `TableOptions`.

---

## 3. `BulkCopyType` - insert strategy

`BulkCopyOptions.BulkCopyType` (or the `BulkCopyType` enum directly) controls which insert path is used.

| Value | Behaviour |
|---|---|
| `Default` | Provider selects the most efficient available strategy (recommended for most cases) |
| `ProviderSpecific` | Uses the provider's native bulk API (`SqlBulkCopy`, PostgreSQL `COPY`, etc.); degrades to `RowByRow` if not supported |
| `MultipleRows` | Emits multi-row `INSERT … VALUES (…), (…)` statements; degrades to `RowByRow` if not supported |
| `RowByRow` | One `INSERT` per row - slowest, but maximally compatible |

```csharp
// Force multi-row INSERT for providers that support it
var options = new BulkCopyOptions(BulkCopyType: BulkCopyType.MultipleRows, MaxBatchSize: 500);
db.BulkCopy(options, source);
```

> **Provider support:** check the `Bulk Copy` column in [`docs/provider-capabilities.md`](07-provider-configuration.md)
> before relying on `ProviderSpecific` or `MultipleRows` for a specific database.
>
> **Cross-provider code:** do not force `ProviderSpecific` when the code must run on more than one database.
> `Default` lets each provider choose the best available path without hard-coding a strategy.

---

## 4. `KeepIdentity` - preserve source identity values

When `KeepIdentity` is `true`, columns marked with `[Identity]` are **included** in the insert
using the values from the source objects. The `SkipOnInsert` flag is ignored for those columns.

When `false` (the default), identity columns are excluded and values are generated by the database.

> Do not enable `KeepIdentity` by default or as a precaution. Use it only when the task explicitly requires
> preserving identity values from the source data (e.g. data migration, re-import from a backup).
>
> ⚠️ `KeepIdentity = true` is **not compatible** with `BulkCopyType.RowByRow`.

```csharp
[Table]
class Product
{
    [PrimaryKey, Identity] public int    Id   { get; set; }
    [Column]               public string Name { get; set; } = "";
}

// Re-import rows keeping their original IDs
var options = new BulkCopyOptions(
    KeepIdentity: true,
    BulkCopyType: BulkCopyType.ProviderSpecific);

db.BulkCopy(options, archivedProducts);
```

---

## 5. Progress callback and cancellation

Use `NotifyAfter` + `RowsCopiedCallback` to track progress.
Set `BulkCopyRowsCopied.Abort = true` inside the callback to stop the operation early.

```csharp
var options = new BulkCopyOptions(
    NotifyAfter:        500,
    RowsCopiedCallback: r =>
    {
        Console.WriteLine($"{r.RowsCopied} rows inserted");

        if (CancellationRequested())
            r.Abort = true;   // stops the bulk copy operation
    });

BulkCopyRowsCopied result = db.BulkCopy(options, source);
```

`NotifyAfter = 0` (default) disables the callback entirely.

---

## 6. `ConflictAction` - handling duplicate-key conflicts

`BulkCopyOptions.ConflictAction` controls what happens when an inserted row conflicts with an
existing row (e.g. a duplicate primary key).

| Value | Behaviour |
|---|---|
| `Default` | Database default - typically raises an error |
| `Ignore` | Silently skip conflicting rows - **see warning below** |

> ⚠️ `ConflictAction.Ignore` silently discards rows without raising an error.
> Use it only when the task explicitly calls for skipping duplicates and the caller is aware that some rows will not be inserted.
> Do not use it as a general "make bulk copy not fail" workaround - silent data loss is the consequence.

Provider support for `Ignore` (requires `BulkCopyType.MultipleRows`):

| Provider | SQL emitted |
|---|---|
| MySQL / MariaDB | `INSERT IGNORE INTO …` |
| PostgreSQL | `INSERT INTO … ON CONFLICT DO NOTHING` |
| SQLite | `INSERT OR IGNORE INTO …` |

```csharp
var options = new BulkCopyOptions(
    BulkCopyType:   BulkCopyType.MultipleRows,
    ConflictAction: ConflictAction.Ignore);

db.BulkCopy(options, source);
```

---

## 7. Provider-specific flags

These flags are honoured only for `BulkCopyType.ProviderSpecific` mode on the listed providers.
Passing them with other modes or unsupported providers has no effect.

| Option | Effect | Supported providers |
|---|---|---|
| `CheckConstraints` | Enforce database constraints during bulk insert | Oracle, SQL Server, SAP/Sybase ASE |
| `TableLock` | Acquire table-level lock during bulk insert | DB2, Informix (via DB2), SQL Server, SAP/Sybase ASE |
| `KeepNulls` | Insert `NULL` instead of column default constraint values | SQL Server, SAP/Sybase ASE |
| `FireTriggers` | Fire insert triggers during bulk insert | Oracle, SQL Server, SAP/Sybase ASE |
| `UseInternalTransaction` | Wrap bulk insert in an automatic transaction | Oracle, SQL Server, SAP/Sybase ASE |
| `BulkCopyTimeout` | Operation timeout in seconds | DB2, Informix, MySqlConnector, Oracle, PostgreSQL, SAP HANA, SQL Server, SAP/Sybase ASE |
| `MaxDegreeOfParallelism` | Number of parallel connections for insert | ClickHouse (ClickHouse.Driver) only |
| `WithoutSession` | Use a session-less connection for insert | ClickHouse (ClickHouse.Driver) only |

```csharp
// SQL Server: table lock + fire triggers, 30-second timeout
var options = new BulkCopyOptions(
    BulkCopyType:   BulkCopyType.ProviderSpecific,
    TableLock:      true,
    FireTriggers:   true,
    BulkCopyTimeout: 30);

db.BulkCopy(options, source);
```

---

## 8. `UseParameters` and `MaxParametersForBatch` - `MultipleRows` tuning

Relevant only for `BulkCopyType.MultipleRows`.

- `UseParameters = true` - always use parameterised `INSERT … VALUES` statements.
  The provider's per-statement parameter limit is used to cap the rows per batch automatically.
- `MaxParametersForBatch` - override the maximum parameter count per batch when `UseParameters` is true.

```csharp
var options = new BulkCopyOptions(
    BulkCopyType:          BulkCopyType.MultipleRows,
    UseParameters:         true,
    MaxParametersForBatch: 2000);

db.BulkCopy(options, source);
```

---

## See also

- [`docs/provider-capabilities.md`](07-provider-configuration.md) - `Bulk Copy` column: provider support matrix.
- [`docs/crud/crud-insert-values.md`](09-crud-and-merge.md) - single-row insert from C# objects.
- [`docs/crud/crud-upsert.md`](09-crud-and-merge.md) - insert-or-update semantics.
- `BulkCopyOptions` - XML documentation on the record type for full parameter details.
- `BulkCopyType` - XML documentation on the enum for provider degradation rules.

<!-- Generated from: Source/Skills/linq2db/docs/crud/crud-merge.md -->

# LinqToDB - MERGE

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> **You are here if** you need to:
> - synchronize a target table from a source query or in-memory collection in one statement
> - apply INSERT, UPDATE, and DELETE operations per match case in a single pass
> - use SQL Server's `WHEN NOT MATCHED BY SOURCE` (rows present in target but absent from source)
> - capture changed rows via OUTPUT

> **Async:** `.Merge()` has a `.MergeAsync(CancellationToken)` counterpart.
> `MergeWithOutput` has `MergeWithOutputAsync` returning `IAsyncEnumerable<TOutput>`.
> Examples use synchronous forms for brevity.
> All async methods require `using LinqToDB.Async;`.

> **Provider support:** MERGE is not universally available.
> Check the `Merge` column in [`provider-capabilities.md`](07-provider-configuration.md).
> Support for base MERGE, `NOT MATCHED BY SOURCE` branches, and OUTPUT are independently
> provider-defined - a provider may support MERGE but not OUTPUT, or MERGE but not `BY SOURCE`.
> Provider-specific operations (`WhenNotMatchedBySource*`, `UpdateWhenMatchedThenDelete`) are noted per section.

---

## Builder call graph

```
Merge(target)          ← section 1
 └─ Using(source)      ← section 2
 │  UsingTarget()
 └─ On(...) / OnTargetKey()   ← section 3
     └─ WhenNotMatched*(...)  ← section 4
     │  WhenMatched*(...)
     │  WhenNotMatchedBySource*(...)  [SQL Server]
     └─ .Merge()              ← section 5 (terminal)
        .MergeWithOutput*(...)
```

`MergeInto(target)` is the source-first alternative to `Merge` + `Using` - section 2.

---

## Pattern quick-reference

| Scenario | Pattern |
|---|---|
| Sync target from another table (update existing, insert new) | `Merge` + `Using` + `OnTargetKey` + `UpdateWhenMatched` + `InsertWhenNotMatched` - section 4 |
| Source is a JOIN, projection, or CTE (`TSource ≠ TTarget`) | `Using(anyQuery)` + `On(...)` + setter-based operations - section 2 |
| Sync and delete rows absent from source | add `DeleteWhenNotMatchedBySource` - section 4 |
| Upsert from in-memory list | `Merge` + `Using(IEnumerable)` + `On` + `UpdateWhenMatched` + `InsertWhenNotMatched` - section 4 |
| Conditional update only | `UpdateWhenMatchedAnd(condition, setter)` - section 4 |
| Delete matched rows | `DeleteWhenMatched` / `DeleteWhenMatchedAnd` - section 4 |
| Capture affected rows (action + before/after) | `MergeWithOutput` - section 5 |
| Write output directly to a table | `MergeWithOutputInto` - section 5 |

---

## 1. Starting a merge - entry points

### Target-first: `table.Merge()`

The standard entry point. Starts from the target table and requires a subsequent `.Using(...)` call
to supply the source:

```csharp
int affected = db.GetTable<Product>()
    .Merge()
    .Using(db.GetTable<ProductStaging>())
    .OnTargetKey()
    .UpdateWhenMatched()
    .InsertWhenNotMatched()
    .Merge();
```

An optional database-specific hint can be passed:

```csharp
db.GetTable<Product>()
    .Merge("WITH (HOLDLOCK)")
    ...
```

### Source-first: `source.MergeInto(target)`

Use when the source query is already in scope. Combines the `.Merge()` + `.Using()` steps:

```csharp
int affected = db.GetTable<ProductStaging>()
    .MergeInto(db.GetTable<Product>())
    .OnTargetKey()
    .UpdateWhenMatched()
    .InsertWhenNotMatched()
    .Merge();
```

---

## 2. Source - `Using` / `UsingTarget`

### `Using(IQueryable<TSource>)` - any server-side query

Accepts any `IQueryable<TSource>`: a plain table, a filtered query, a JOIN, a projection, a CTE, or
an arbitrary subquery. `TSource` does not need to match `TTarget` - the operation setters in
section 4 map between the two types.

```csharp
// TSource == TTarget: filter a staging table before merging
db.GetTable<Product>()
    .Merge()
    .Using(db.GetTable<ProductStaging>().Where(s => s.IsActive))
    .On((t, s) => t.ProductID == s.ProductID)
    .UpdateWhenMatched((t, s) => new Product { Name = s.Name, Price = s.Price })
    .InsertWhenNotMatched(s => new Product { ProductID = s.ProductID, Name = s.Name, Price = s.Price })
    .Merge();
```

```csharp
// TSource is an anonymous projection from a JOIN - TSource != TTarget
var source =
    from s in db.GetTable<ProductStaging>()
    join v in db.GetTable<Vendor>() on s.VendorID equals v.VendorID
    where v.IsApproved
    select new { s.ProductID, s.Name, s.Price };

db.GetTable<Product>()
    .Merge()
    .Using(source)
    .On((t, s) => t.ProductID == s.ProductID)
    .UpdateWhenMatched((t, s) => new Product { Name = s.Name, Price = s.Price })
    .InsertWhenNotMatched(s => new Product { ProductID = s.ProductID, Name = s.Name, Price = s.Price })
    .Merge();
```

### `Using(IEnumerable<TSource>)` - in-memory source

The source is a local in-memory collection. LinqToDB uses it as the merge source;
the exact SQL representation is provider-specific.

```csharp
var stagingData = new[]
{
    new ProductStaging { ProductID = 1, Name = "Widget", Price = 9.99m },
    new ProductStaging { ProductID = 2, Name = "Gadget", Price = 19.99m },
};

db.GetTable<Product>()
    .Merge()
    .Using(stagingData)
    .On((t, s) => t.ProductID == s.ProductID)
    .UpdateWhenMatched((t, s) => new Product { Name = s.Name, Price = s.Price })
    .InsertWhenNotMatched(s => new Product { ProductID = s.ProductID, Name = s.Name, Price = s.Price })
    .Merge();
```

### `UsingTarget()` - target as its own source

Merges the table against itself. Used with `OnTargetKey()` for conditional self-updates:

```csharp
db.GetTable<Product>()
    .Merge()
    .UsingTarget()
    .OnTargetKey()
    .UpdateWhenMatchedAnd(
        (t, s) => t.Stock == 0,
        (t, s) => new Product { IsDiscontinued = true })
    .Merge();
```

---

## 3. Match condition - `On` / `OnTargetKey`

### `OnTargetKey()` - PK columns from mapping

Available whenever `TSource == TTarget` - either via `.UsingTarget()` or when `.Using(...)` is called
with a query that returns the same type as the target.
Uses the `[PrimaryKey]` columns from the entity mapping:

```csharp
.UsingTarget()
.OnTargetKey()
```

### `On(targetKey, sourceKey)` - key selectors

When source and target types differ, supply the key projection for each:

```csharp
.On(t => t.ProductID, s => s.ProductID)
```

### `On(matchCondition)` - arbitrary predicate

For composite or expression-based conditions:

```csharp
.On((t, s) => t.ProductID == s.ProductID && t.RegionCode == s.RegionCode)
```

---

## 4. Operations - `When*` clauses

Operations are evaluated in declaration order; the first matching operation wins for each row pair.
At least one operation must be present before calling the terminal.

### Insert when not matched (source row has no target counterpart)

```csharp
// Copy all fields - requires TSource == TTarget
.InsertWhenNotMatched()

// With condition
.InsertWhenNotMatchedAnd(s => s.IsActive)

// Custom setter - works when TSource != TTarget
.InsertWhenNotMatched(s => new Product { ProductID = s.ProductID, Name = s.Name, Price = s.Price })

// Condition + setter
.InsertWhenNotMatchedAnd(s => s.IsActive, s => new Product { ProductID = s.ProductID, Name = s.Name })
```

### Update when matched (row exists in both source and target)

```csharp
// Copy all fields - requires TSource == TTarget
.UpdateWhenMatched()

// With condition on target and source
.UpdateWhenMatchedAnd((t, s) => t.Version < s.Version)

// Custom setter
.UpdateWhenMatched((t, s) => new Product { Name = s.Name, Price = s.Price, UpdatedAt = Sql.CurrentTimestamp })

// Condition + setter
.UpdateWhenMatchedAnd(
    (t, s) => t.Version < s.Version,
    (t, s) => new Product { Name = s.Name, Price = s.Price })
```

### Delete when matched

```csharp
// Delete every matched target row
.DeleteWhenMatched()

// Conditional delete
.DeleteWhenMatchedAnd((t, s) => t.IsDiscontinued && s.Stock == 0)
```

### Update / Delete when not matched by source *(SQL Server only)*

These operations fire for rows that exist in the target but have no match in the source:

```csharp
// Mark target rows that disappeared from source as inactive
.UpdateWhenNotMatchedBySource(t => new Product { IsActive = false })

// Conditional
.UpdateWhenNotMatchedBySourceAnd(
    t => t.IsActive,
    t => new Product { IsActive = false, DeactivatedAt = Sql.CurrentTimestamp })

// Hard-delete rows absent from source
.DeleteWhenNotMatchedBySource()

// Conditional delete
.DeleteWhenNotMatchedBySourceAnd(t => t.CreatedAt < deleteCutoff)
```

### Update-then-delete *(Oracle only)*

Updates matched rows and then immediately deletes the ones that satisfy an additional condition:

```csharp
// Update all, then delete those with zero stock
.UpdateWhenMatchedThenDelete(
    (t, s) => new Product { Name = s.Name, Stock = s.Stock },
    (t, s) => t.Stock == 0)

// Conditional update + conditional delete
.UpdateWhenMatchedAndThenDelete(
    (t, s) => t.Version < s.Version,            // update condition
    (t, s) => new Product { Name = s.Name },     // setter
    (t, s) => t.Stock == 0)                      // delete condition
```

---

## 5. Executing the merge

### `.Merge()` - returns row count

```csharp
int affected = db.GetTable<Product>()
    .Merge()
    .Using(stagingData)
    .On((t, s) => t.ProductID == s.ProductID)
    .UpdateWhenMatched()
    .InsertWhenNotMatched()
    .Merge();
```

### `MergeWithOutput` - returns per-row result

`outputExpression` receives the merge action string (`"INSERT"`, `"UPDATE"`, `"DELETE"`),
the old target row, and the new target row:

```csharp
var log = db.GetTable<Product>()
    .Merge()
    .Using(stagingData)
    .On((t, s) => t.ProductID == s.ProductID)
    .UpdateWhenMatched()
    .InsertWhenNotMatched()
    .MergeWithOutput((action, old, inserted) => new
    {
        Action     = action,
        OldPrice   = old.Price,
        NewPrice   = inserted.Price,
    })
    .ToList();
```

A 4-argument overload includes the source row as the last parameter:

```csharp
.MergeWithOutput((action, old, inserted, source) => new { action, source.ProductID })
```

> **Provider support:** SQL Server 2008+, Firebird 3+ (no `action`, Firebird < 5 limited to one row),
> PostgreSQL 17+ (no old data).

### `MergeWithOutputInto` - writes output to a table *(SQL Server only)*

```csharp
int affected = db.GetTable<Product>()
    .Merge()
    .Using(stagingData)
    .On((t, s) => t.ProductID == s.ProductID)
    .UpdateWhenMatched()
    .InsertWhenNotMatched()
    .MergeWithOutputInto(
        db.GetTable<MergeLog>(),
        (action, old, inserted) => new MergeLog
        {
            Action    = action,
            ProductID = inserted.ProductID,
            LoggedAt  = Sql.CurrentTimestamp,
        });
```

### Async terminal

```csharp
int affected = await db.GetTable<Product>()
    .Merge()
    .Using(stagingData)
    .On((t, s) => t.ProductID == s.ProductID)
    .UpdateWhenMatched()
    .InsertWhenNotMatched()
    .MergeAsync(cancellationToken);
```

```csharp
await foreach (var row in db.GetTable<Product>()
    .Merge()
    .Using(stagingData)
    .On((t, s) => t.ProductID == s.ProductID)
    .UpdateWhenMatched()
    .InsertWhenNotMatched()
    .MergeWithOutputAsync((action, old, inserted) => new { action, inserted.ProductID }))
{
    Console.WriteLine(row);
}
```

---

## 6. Legacy API *(obsolete)*

> **These methods are marked `[Obsolete]`.**
> Prefer the fluent builder (sections 1-5) for all new code.
> The legacy methods are thin wrappers over the fluent builder and are preserved only for
> backward compatibility.

`LegacyMergeExtensions` exposes `Merge` / `MergeAsync` overloads on both `DataConnection` and
`ITable<T>`. All variants:

- match on `[PrimaryKey]` columns (`OnTargetKey`)
- always execute **Update** + **Insert** (Update is skipped automatically if all non-PK columns are
  identity or marked skip-on-update)
- optionally execute **DeleteWhenNotMatchedBySource** (SQL Server only)

| Overload | Operations | Notes |
|---|---|---|
| `Merge(source)` | Update + Insert | Any provider |
| `Merge(delete, source)` | Update + Insert [+ Delete] | `delete: true` requires SQL Server |
| `Merge(predicate, source)` | Update + Insert + DeleteAnd(predicate) | SQL Server; predicate on delete only |
| `Merge(source, predicate)` | Update + Insert + DeleteAnd(predicate) | SQL Server; predicate filters source too |

```csharp
// Update + Insert from an in-memory list (any provider)
dataConnection.Merge(stagingData);

// Update + Insert + unconditional DeleteBySource (SQL Server)
dataConnection.Merge(delete: true, stagingData);

// Update + Insert + conditional DeleteBySource (SQL Server)
dataConnection.Merge(
    predicate: t => t.RegionCode == "EU",
    source: stagingData);
```

**Equivalent fluent builder translation for `Merge(source)`:**

```csharp
db.GetTable<Product>()
    .Merge()
    .Using(source)
    .OnTargetKey()
    .UpdateWhenMatched()
    .InsertWhenNotMatched()
    .Merge();
```

Async counterparts (`MergeAsync`) accept the same parameters plus an optional `CancellationToken`.

---

## See also

- [`crud-upsert.md`](09-crud-and-merge.md) - simple Insert-or-Update without a full MERGE builder
- [`crud-update.md`](09-crud-and-merge.md) - plain UPDATE
- [`crud-delete.md`](09-crud-and-merge.md) - plain DELETE
- [`provider-capabilities.md`](07-provider-configuration.md) - MERGE support per provider
