# LinqToDB — Temporary Tables

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`AGENT_GUIDE.md`](../AGENT_GUIDE.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> **You are here if** you need to:
> - create a table that is automatically dropped when your code is done with it
> - stage data in a server-side table for joining, filtering, or further processing
> - work with database-native temporary table syntax (`#name`, `GLOBAL TEMPORARY TABLE`, etc.)
> - populate a temp table from a C# collection or an `IQueryable<T>` query

---

> **Agent guidance:**
> - Temporary tables require a `DataConnection`, not a `DataContext`. `DataContext` opens and closes a physical connection per command; a temp table's lifetime is tied to the session, so the table would be invisible across commands. Always use `DataConnection`.
> - Always use `await using` / `using` to ensure the backing table is dropped even on exception.
> - Do not use temp tables as a general substitute for subqueries or CTEs — they carry DDL overhead. Use them when server-side materialisation is genuinely needed or when the collection comes from C# memory.

---

> **Async:** `TempTable<T>` has `CreateAsync` static factory methods and `CopyAsync` / `InsertAsync` methods for populating after creation.
> Async methods require `using LinqToDB.Async;`.

---

## Pattern quick-reference

| Scenario | Pattern |
|---|---|
| Create empty temp table, populate later | `db.CreateTempTable<T>()` — section 1 |
| Create and populate from C# collection | `db.CreateTempTable<T>(items)` — section 2 |
| Create and populate from a query (`INSERT … SELECT`) | `new TempTable<T>(db, query)` — section 3 |
| Async creation | `TempTable<T>.CreateAsync(db, ...)` — section 4 |
| Custom table name / schema override | `tableName:` parameter or `CreateTempTableOptions` — section 5 |
| Use native temp table syntax (`#name`, `GLOBAL TEMPORARY`, etc.) | `TableOptions` flags — section 6 |
| Query the temp table in LINQ | temp table implements `ITable<T>` — section 7 |

---

## 1. Create an empty temporary table

Creates the physical table immediately. Use `Copy` or `Insert` afterwards to populate it.

```csharp
using LinqToDB;
using LinqToDB.Data; // DataConnection

// DataConnection is required — DataContext cannot be used (see Agent guidance above)
using var db    = new DataConnection(options);
using var table = db.CreateTempTable<Product>();

// table is now an ITable<Product> — query or populate it here
```

`CreateTempTable<T>` issues `CREATE TABLE` on construction and `DROP TABLE` on disposal.
The `using` / `await using` scope ensures the drop runs even if an exception is thrown.

---

## 2. Create and populate from a C# collection (BulkCopy)

Combines `CREATE TABLE` and a `BulkCopy` in one call.

```csharp
List<Product> products = GetProductsFromSomewhere();

using var db    = new DataConnection(options);
using var table = db.CreateTempTable<Product>(products);

// table now contains all products
var result = table.Where(p => p.Price > 100).ToList();
```

With explicit `BulkCopyOptions`:

```csharp
var copyOptions = new BulkCopyOptions(MaxBatchSize: 1000);
using var table = db.CreateTempTable<Product>(products, options: copyOptions);
```

---

## 3. Create and populate from a query (`INSERT … SELECT`)

Uses `INSERT INTO temp SELECT … FROM …` to populate the table server-side from an existing query.
No data crosses the wire between the database and the application.

```csharp
using var db = new DataConnection(options);

IQueryable<Product> sourceQuery = db.GetTable<Product>()
    .Where(p => p.CategoryId == 5);

using var table = new TempTable<T>(db, sourceQuery);

// join the temp table against other tables
var joined = db.GetTable<Order>()
    .Join(table, o => o.ProductId, p => p.Id, (o, p) => new { o, p })
    .ToList();
```

The `action` parameter (optional) runs after `CREATE TABLE` but before the `INSERT`:

```csharp
using var table = new TempTable<Product>(db, sourceQuery,
    action: t => t.CreateIndex("IX_Cat", x => x.CategoryId));  // if such an API is available
```

---

## 4. Async creation

For async code use the static `TempTable<T>.CreateAsync` factory instead of the constructor,
to avoid blocking on `CREATE TABLE` or the initial BulkCopy.

```csharp
using LinqToDB.Async;

await using var db    = new DataConnection(options);
await using var table = await TempTable<Product>.CreateAsync(db, products, cancellationToken: ct);
```

Empty table, then populate asynchronously:

```csharp
await using var table = await TempTable<Product>.CreateAsync(db);
await table.CopyAsync(products, cancellationToken: ct);
```

From a query asynchronously:

```csharp
await using var table = await TempTable<Product>.CreateAsync(db, sourceQuery);
```

---

## 5. Overriding the table name and schema

By default the table name comes from `[Table]` mapping attribute on `T`.
Override it for the lifetime of this instance with the named parameters:

```csharp
using var table = db.CreateTempTable<Product>(tableName: "#staging_products");
```

With `CreateTempTableOptions` for full control:

```csharp
var opts = new CreateTempTableOptions(
    TableName:   "#staging_products",
    SchemaName:  "dbo",
    TableOptions: TableOptions.IsTemporary);

using var table = db.CreateTempTable<Product>(opts);
```

---

## 6. `TableOptions` — controlling the table kind

`TableOptions` is a `[Flags]` enum. The default for `CreateTempTable` extensions is
`TableOptions.IsTemporary` — a database-native temporary table (session-scoped, invisible to other connections).

> **Important:** `TempTable<T>` constructor (called directly, not via `CreateTempTable`) defaults to
> `TableOptions.NotSet` — the kind is determined by provider/mapping defaults, not necessarily a
> native temp table. Use `CreateTempTable` extensions or pass `TableOptions.IsTemporary` explicitly
> when you need guaranteed native temp-table semantics.

Commonly used values:

| Value | Effect |
|---|---|
| `TableOptions.IsTemporary` | Database-native local (session-scoped) temporary table when supported |
| `TableOptions.None` | Regular (permanent-kind) physical table — lifecycle still managed by `TempTable<T>` |
| `TableOptions.CheckExistence` | `CREATE IF NOT EXISTS` + `DROP IF EXISTS` |
| `TableOptions.IsLocalTemporaryStructure` | Session-scoped DDL visibility |
| `TableOptions.IsGlobalTemporaryStructure` | Globally visible DDL (e.g., Oracle `GLOBAL TEMPORARY TABLE`) |
| `TableOptions.IsTransactionTemporaryData` | Transaction-scoped data (Firebird, Oracle, PostgreSQL) |

```csharp
// Force a regular physical table but still auto-drop on dispose
var opts = new CreateTempTableOptions(
    TableName:    "staging_products",
    TableOptions: TableOptions.None);

using var table = db.CreateTempTable<Product>(opts);
```

> Provider support for specific `TableOptions` values varies.
> Check `docs/provider-capabilities.md` before relying on flags beyond `IsTemporary`.

---

## 7. Querying the temporary table

`TempTable<T>` implements `ITable<T>` and can be used anywhere an `ITable<T>` or `IQueryable<T>` is accepted:

```csharp
using var table = db.CreateTempTable<Product>(products);

// Filter and project
var cheap = table.Where(p => p.Price < 10).ToList();

// Join with a permanent table
var result = db.GetTable<Order>()
    .Join(table, o => o.ProductId, p => p.Id,
          (o, p) => new { o.OrderId, p.Name, o.Quantity })
    .ToList();

// Pass to DML extension methods
await table.Where(p => p.Stock == 0).DeleteAsync();
```

---

## 8. Populating after creation — `Copy` and `Insert`

If the table was created empty (section 1 or section 4 empty-create), populate it separately:

```csharp
using var table = db.CreateTempTable<Product>();

// From C# collection — BulkCopy
long copied = table.Copy(products);

// From a query — INSERT … SELECT
long inserted = table.Insert(db.GetTable<Product>().Where(p => p.IsActive));
```

Async variants:

```csharp
long copied   = await table.CopyAsync(products, cancellationToken: ct);
long inserted = await table.InsertAsync(db.GetTable<Product>().Where(...), ct);
```

---

## 9. Column size requirements

When `TempTable<T>` creates the backing table, it runs a real `CREATE TABLE` statement.
The same rules as for any schema-created table apply — see AGENT_GUIDE.md step 3 and
anti-pattern #10 in `docs/agent-antipatterns.md`:

```csharp
// Correct: explicit Length and Precision/Scale on provider-sensitive types
[Table]
class StagingRow
{
    [PrimaryKey]                    public int     Id    { get; set; }
    [Column(Length = 200), NotNull] public string  Name  { get; set; } = ""; // TODO: confirm max length
    [Column(Precision = 18, Scale = 2)] public decimal Amount { get; set; }
}
```

Do **not** leave `string` / `decimal` columns without explicit `Length` / `Precision` / `Scale`.

---

## See also

- [`docs/agent-antipatterns.md`](agent-antipatterns.md) — anti-pattern #10: unconstrained column types in schema generation.
- [`docs/query-cte.md`](query-cte.md) — CTEs as a lightweight alternative to temp tables for query composition.
- [`docs/crud/crud-bulkcopy.md`](crud/crud-bulkcopy.md) — `BulkCopy` options used by `CreateTempTable(items, ...)`.
- `TempTable<T>` — XML documentation for full constructor / method reference.
- `TableOptions` — XML documentation for provider support details per flag.
