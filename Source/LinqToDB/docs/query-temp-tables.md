# LinqToDB — Temporary Tables

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`AGENT_GUIDE.md`](../AGENT_GUIDE.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> **You are here if** you need to:
> - create a table that is automatically dropped when your code is done with it
> - stage data in a server-side table for joining, filtering, or further processing
> - work with database-native temporary table syntax (`#name`, `GLOBAL TEMPORARY TABLE`, session-scoped tables, etc.)
> - populate a staging table from a C# collection or an `IQueryable<T>` query

---

> **Agent guidance:**
> - Use the simplest `CreateTempTable` overload that satisfies the requirement. Do not introduce `setTable`, `CreateTempTableOptions`, or explicit `TableOptions` unless the task requires them.
> - Temporary tables require a `DataConnection`, not a `DataContext`. `DataContext` opens and closes a physical connection per command; a temporary table's lifetime is tied to the session, so the table would be invisible across commands.
> - Always use `await using` / `using` to ensure the backing table is dropped even on exception.
> - Do not use temporary tables as a general substitute for subqueries or CTEs — they carry DDL overhead. Use them when server-side staging is genuinely needed or when the collection originates in C# memory.
> - If provider-specific behavior is uncertain, ask instead of assuming.

---

> **Async:** All `CreateTempTable` extension methods have `CreateTempTableAsync` counterparts.
> Async methods require `using LinqToDB.Async;`.

---

## What `CreateTempTable` actually means

`CreateTempTable` is a **lifetime-managed table creation** API:

- It issues `CREATE TABLE` when called.
- The returned `TempTable<T>` instance drops the table (`DROP TABLE`) when disposed.
- The `using` / `await using` scope ensures the drop runs even if an exception is thrown.

**What kind of physical table is created** depends on the `TableOptions` parameter (default:
`TableOptions.IsTemporary`). With the default, LinqToDB requests a database-native temporary
table when the provider supports it (e.g., `#name` in SQL Server, session-scoped tables in
PostgreSQL and MySQL). Providers that do not support native temporary tables fall back to a
regular table.

The name `CreateTempTable` therefore describes the **managed lifecycle** (`CREATE` + auto-`DROP`),
not necessarily a database-native temporary table. See section 6 for `TableOptions` details.

---

## Pattern quick-reference

| Scenario | Pattern |
|---|---|
| Create empty table, populate later | `db.CreateTempTable<T>()` — section 1 |
| Create and populate from a C# collection | `db.CreateTempTable<T>(items)` — section 2 |
| Create and populate from a query (`INSERT … SELECT`) | `db.CreateTempTable(query)` — section 3 |
| Async creation | `db.CreateTempTableAsync<T>(...)` — section 4 |
| Custom table name / schema | `tableName:` parameter — section 5 |
| Control the physical table kind | `TableOptions` flags — section 6 |
| Query the table in LINQ | `TempTable<T>` implements `ITable<T>` — section 7 |
| Populate after creation | `Copy` / `Insert` — section 8 |
| Source is an anonymous-type projection | `setTable:` fluent mapping — section 9 |

---

## 1. Create an empty table

Creates the physical table immediately. Populate it later using `Copy` or `Insert` (section 8).

```csharp
using LinqToDB;
using LinqToDB.Data; // DataConnection required

using var db    = new DataConnection(options);
using var table = db.CreateTempTable<Product>();

// table is now an ITable<Product> — query or populate it here
```

---

## 2. Create and populate from a C# collection

Combines `CREATE TABLE` and a BulkCopy in one call.

```csharp
List<Product> products = LoadFromSomewhere();

using var db    = new DataConnection(options);
using var table = db.CreateTempTable<Product>(products);

var result = table.Where(p => p.Price > 100).ToList();
```

With explicit `BulkCopyOptions`:

```csharp
var copyOptions = new BulkCopyOptions(MaxBatchSize: 1000);
using var table = db.CreateTempTable<Product>(products, options: copyOptions);
```

---

## 3. Create and populate from a query (`INSERT … SELECT`)

Populates the table server-side using `INSERT INTO … SELECT`. No data crosses the wire between
the database and the application.

```csharp
using var db = new DataConnection(options);

var sourceQuery = db.GetTable<Product>()
    .Where(p => p.CategoryId == 5);

using var table = db.CreateTempTable(sourceQuery);

// Join the staging table against other tables
var joined = db.GetTable<Order>()
    .Join(table, o => o.ProductId, p => p.Id, (o, p) => new { o, p })
    .ToList();
```

The optional `action` parameter runs after `CREATE TABLE` but before the `INSERT`,
which is useful for adding indexes or other DDL that should precede data loading:

```csharp
using var table = db.CreateTempTable(sourceQuery,
    action: t => { /* DDL on t before INSERT */ });
```

---

## 4. Async creation

Use `CreateTempTableAsync` to avoid blocking on `CREATE TABLE` or the initial BulkCopy.

```csharp
using LinqToDB.Async;

await using var db    = new DataConnection(options);
await using var table = await db.CreateTempTableAsync<Product>(products, cancellationToken: ct);
```

Empty table, then populate asynchronously:

```csharp
await using var table = await db.CreateTempTableAsync<Product>(cancellationToken: ct);
await table.CopyAsync(products, cancellationToken: ct);
```

From a query asynchronously:

```csharp
await using var table = await db.CreateTempTableAsync(sourceQuery, cancellationToken: ct);
```

---

## 5. Overriding the table name and schema

By default the table name comes from the `[Table]` mapping attribute on `T`.
Override it with the named parameters:

```csharp
using var table = db.CreateTempTable<Product>(products, tableName: "#staging");
```

With `CreateTempTableOptions` for full control over name, schema, and table kind at once:

```csharp
var opts = new CreateTempTableOptions(
    TableName:    "#staging",
    SchemaName:   "dbo",
    TableOptions: TableOptions.IsTemporary);

using var table = db.CreateTempTable<Product>(opts, products);
```

---

## 6. `TableOptions` — controlling the physical table kind

`TableOptions` is a `[Flags]` enum that controls what kind of table is created.
The `CreateTempTable` extension methods default to `TableOptions.IsTemporary`.

| Value | Physical table kind |
|---|---|
| `TableOptions.IsTemporary` *(default)* | Database-native local (session-scoped) temporary table; falls back to a regular table when not supported by the provider |
| `TableOptions.None` | Regular physical table — **not** session-scoped; visible to other sessions depending on the provider; lifecycle still managed by `TempTable<T>` (auto-dropped on dispose) |
| `TableOptions.CheckExistence` | `CREATE IF NOT EXISTS` + `DROP IF EXISTS` |
| `TableOptions.IsLocalTemporaryStructure` | Session-scoped DDL visibility |
| `TableOptions.IsGlobalTemporaryStructure` | Globally visible DDL (e.g., Oracle `GLOBAL TEMPORARY TABLE`) |
| `TableOptions.IsTransactionTemporaryData` | Transaction-scoped data (Firebird, Oracle, PostgreSQL) |

> **`TableOptions.None`** creates a regular physical table, not a database-native temporary table.
> It is still automatically dropped when `TempTable<T>` is disposed, but it is not session-scoped
> and may be visible to other sessions or connections for the duration of its existence.
> Only use `TableOptions.None` when a regular (non-session-scoped) staging table is explicitly required.

```csharp
// Explicitly request a regular physical table with auto-drop lifetime
var opts = new CreateTempTableOptions(
    TableName:    "staging_products",
    TableOptions: TableOptions.None);

using var table = db.CreateTempTable<Product>(opts, products);
```

> Provider support for specific `TableOptions` values varies.
> Check `docs/provider-capabilities.md` before using flags beyond `IsTemporary`.

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

// DML on the staging table
await table.Where(p => p.Stock == 0).DeleteAsync();
```

---

## 8. Populating after creation — `Copy` and `Insert`

When the table was created empty (section 1 or section 4 empty-create), populate it afterwards:

```csharp
using var table = db.CreateTempTable<Product>();

// From a C# collection — BulkCopy
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

## 9. Anonymous-type source — fluent mapping

When the source is an anonymous-type projection (e.g. `Select(p => new { p.Id, p.Name })`),
LinqToDB cannot derive schema from `[Column]` attributes because anonymous types have none.
This is a valid and often preferable pattern — it avoids introducing an extra named class.

Use the `setTable` overload to provide column mapping inline via the fluent API:

```csharp
var query = db.GetTable<Product>()
    .Where(p => p.IsActive)
    .Select(p => new { p.Id, p.Name, p.Price });

// DataOptions must be configured with UseEnableContextSchemaEdit(true)
// for the setTable parameter to work.
using var table = db.CreateTempTable(query,
    setTable: e => e
        .Property(p => p.Id)   .IsPrimaryKey()
        .Property(p => p.Name) .HasLength(200)        // TODO: confirm max length
        .Property(p => p.Price).HasPrecision(18, 2));
```

**Prerequisites for `setTable`:**

The context's `MappingSchema` must be writable. Enable this once at startup:

```csharp
static readonly DataOptions _options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseEnableContextSchemaEdit(true);  // required for setTable to work
```

> ⚠️ Do not enable `UseEnableContextSchemaEdit` globally via `Configuration.Linq.EnableContextSchemaEdit` —
> it affects performance significantly. Use the per-options configuration shown above.

**Required rules for `setTable` columns:**

- Every `string` property must have an explicit length: `.HasLength(n)`.
  If the task does not state an exact limit, choose a bounded value and add a `TODO` comment.
- Every `decimal` property must have explicit precision and scale: `.HasPrecision(p, s)`.
- Without these, the generated `CREATE TABLE` statement will use provider defaults that differ
  across databases (see anti-pattern #10 in `docs/agent-antipatterns.md`).

---

## 10. Column size requirements for mapped classes

When a named class (not an anonymous type) is used with `CreateTempTable`, the same schema rules
apply as for any `CREATE TABLE` operation. See AGENT_GUIDE.md step 3 and anti-pattern #10:

```csharp
[Table]
class StagingRow
{
    [PrimaryKey]
    public int Id { get; set; }

    [Column(Length = 200), NotNull]
    public string Name { get; set; } = ""; // TODO: confirm max length

    [Column(Precision = 18, Scale = 2)]
    public decimal Amount { get; set; }
}
```

Do **not** leave `string` / `decimal` columns without explicit `Length` / `Precision` / `Scale`.

---

## See also

- [`docs/agent-antipatterns.md`](agent-antipatterns.md) — anti-pattern #10: unconstrained column types in schema generation.
- [`docs/query-cte.md`](query-cte.md) — CTEs as a lightweight alternative for query composition that does not require DDL.
- [`docs/crud/crud-bulkcopy.md`](crud/crud-bulkcopy.md) — `BulkCopyOptions` used by `CreateTempTable(items, ...)` and `Copy`.
- `TempTable<T>` — XML documentation for the full constructor and instance method reference.
- `TableOptions` — XML documentation for provider support details per flag.
