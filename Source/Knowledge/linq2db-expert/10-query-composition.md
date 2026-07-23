<!-- Generated from: Source/Skills/linq2db/docs/query-cte.md -->

# LinqToDB - Common Table Expressions (CTE)

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> **You are here if** you need to:
> - introduce an explicit named SQL CTE (`WITH ...`) within a larger query
> - factor out a complex subquery for readability using a named SQL construct
> - traverse a hierarchy or graph with a recursive query

> **Provider support:** CTEs are not supported on all providers.
> Check the `CTE` row in [`provider-capabilities.md`](07-provider-configuration.md) before using.

---

## 1. Basic CTE - `AsCte()`

Call `.AsCte()` on any `IQueryable<T>` to declare a named intermediate result.
The returned value is a composable `IQueryable<T>` - use it as a query source.

```csharp
var activeCte = db.GetTable<Product>()
    .Where(p => p.IsActive)
    .AsCte();   // LinqToDB assigns an auto-generated name

var result = from p in activeCte
             join c in db.GetTable<Category>() on p.CategoryID equals c.ID
             select new { p.Name, c.CategoryName };
```

Pass an explicit name to control what appears in the SQL `WITH` clause:

```csharp
var activeCte = db.GetTable<Product>()
    .Where(p => p.IsActive)
    .AsCte("ActiveProducts");
```

---

## 2. Reusing a CTE in one query

Referencing the same CTE variable multiple times in a query produces a single `WITH` clause entry
in SQL - the subquery is declared once, not duplicated inline.

```csharp
var expensiveCte = db.GetTable<Product>()
    .Where(p => p.Price > 100m)
    .AsCte("ExpensiveProducts");

var result =
    from p in expensiveCte
    join r in db.GetTable<Review>() on p.ProductID equals r.ProductID
    where expensiveCte.Any(ep => ep.CategoryID == p.CategoryID && ep.ProductID != p.ProductID)
    select new { p.Name, r.Rating };
```

`ExpensiveProducts` appears once in `WITH` and is referenced twice in the query body.
Use this pattern to avoid repeating complex filter/join logic.

---

## 3. CTE parameters - captured variables

There is no special syntax for parameterized CTEs. Any C# variable captured inside a CTE
definition becomes a SQL parameter automatically.

```csharp
var menuId = 5;

var cte = db.GetCte<MenuItem>(prev =>
    db.GetTable<MenuItem>()
        .Where(m => m.MenuId == menuId)         // menuId → @p1
        .Select(m => new MenuItem { Id = m.Id, ParentId = m.ParentId })
        .UnionAll(db.GetTable<MenuItem>().InnerJoin(
            prev,
            (m, r) => r.Id == m.ParentId,
            (m, r) => new MenuItem { Id = m.Id, ParentId = m.ParentId })));
```

The same applies to `AsCte()`:

```csharp
var minPrice = 100m;

var cte = db.GetTable<Product>()
    .Where(p => p.Price > minPrice)   // minPrice → @p1
    .AsCte("ExpensiveProducts");
```

### Opt-in: inline captured values as literals - `InlineParameters()`

By default captured C# variables become SQL parameters - this is the correct default.
Use `.InlineParameters()` only when you explicitly need SQL literals instead of `@p` parameters
(provider-specific plan behavior, diagnostics, or a known query-shaping scenario):

```csharp
var dateFrom = DateTime.Today;
var dateTo   = dateFrom.AddDays(30);

var cte = db.GetCte<DateRange>(x =>
    db.SelectQuery(() => new DateRange { Date = dateFrom, Counter = 1 })
      .Concat(x
          .Where(r => r.Date < dateTo)
          .Select(r => new DateRange { Date = r.Date.AddDays(1), Counter = r.Counter + 1 })));

cte = cte.InlineParameters();   // WHERE Date < '2024-01-30'  instead of  WHERE Date < @p1

var result = cte.ToList();
```

---

## 4. Recursive CTE

Use `db.GetCte<T>()` when the CTE body must reference itself (hierarchy traversal, graph walk).
The factory lambda receives the CTE as a parameter; use it in the recursive branch.
The anchor and recursive branches are joined with `.Concat` (translates to `UNION ALL`).

```csharp
sealed class OrgNode
{
    public int  EmployeeID { get; set; }
    public int? ReportsTo  { get; set; }
    public int  Level      { get; set; }
}

var hierarchy = db.GetCte<OrgNode>(cte =>
    // Anchor: top-level employees (no manager)
    (
        from e in db.GetTable<Employee>()
        where e.ReportsTo == null
        select new OrgNode { EmployeeID = e.EmployeeID, ReportsTo = null, Level = 1 }
    )
    .Concat(
        // Recursive branch: employees whose manager is already in the CTE
        from e  in db.GetTable<Employee>()
        from ct in cte.InnerJoin(ct => ct.EmployeeID == e.ReportsTo)
        select new OrgNode { EmployeeID = e.EmployeeID, ReportsTo = e.ReportsTo, Level = ct.Level + 1 }
    ),
    "OrgHierarchy"   // optional SQL name
);

var result = hierarchy.OrderBy(n => n.Level).ToList();
```

> The anchor and recursive branch must project to the same type.
> All fields used in the recursive branch must be present in the anchor projection.
> The recursive branch must contain a condition that eventually stops producing new rows -
> without it the query will recurse until the database hits its maximum recursion depth.

---

## 5. CTE as a source in DML

A CTE variable is a plain `IQueryable<T>` - use it as a filter or join source in
`Update`, `Delete`, or `Insert … SELECT`:

```csharp
// Delete records identified through a CTE
var stale = db.GetTable<AuditLog>()
    .Where(a => a.CreatedAt < cutoff)
    .AsCte();

var toDelete =
    from r in db.GetTable<Record>()
    from a in stale.InnerJoin(a => a.RecordID == r.ID)
    select r;

toDelete.Delete();
```

> Provider support for CTE in DML (UPDATE / DELETE with `WITH`) may be narrower than for SELECT.
> Validate against the `CTE` row in [`provider-capabilities.md`](07-provider-configuration.md) and test with your specific provider.

---

## Anti-patterns

| Mistake | Consequence |
|---|---|
| Treating CTE as a materialized temp table | CTE is inlined in SQL - it is **not** a temp table; the database may re-evaluate it for each reference unless it chooses to materialize |
| Skipping provider support check | Runtime exception on providers that do not support `WITH` |
| Using recursive CTE with mismatched projections | Compile error or silent wrong SQL - anchor and recursive branch must return the same type |
| Using `AsCte()` only to introduce an intermediate query variable | A plain `IQueryable<T>` variable already composes and reuses without generating a SQL CTE; use `AsCte()` only when you intentionally need a named SQL CTE, explicit query factoring, or recursion |

---

## See also

- [`crud-select.md`](09-crud-and-merge.md) - everyday querying
- [`provider-capabilities.md`](07-provider-configuration.md) - CTE support per provider

<!-- Generated from: Source/Skills/linq2db/docs/query-joins.md -->

# LinqToDB - Joins

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> **You are here if** you need to:
> - use LinqToDB's fluent `InnerJoin`/`LeftJoin`/`RightJoin`/`FullJoin`/`CrossJoin` extensions
> - understand how association (navigation-like) access translates into a SQL join
> - understand why a join expression fails to translate
> - check provider limitations for outer joins and `APPLY`/`LATERAL`
>
> This is **not** a tutorial on the standard LINQ `join` / `GroupJoin` keywords - that syntax
> works in LinqToDB the same way it works everywhere else. This document covers what is
> LinqToDB-specific: the fluent join extensions, association-driven joins, translation failure
> cases, and provider limitations.

---

## 1. Fluent join extensions

`InnerJoin`, `LeftJoin`, `RightJoin`, `FullJoin` each have two forms. The single-argument form is
used inside a query expression the same way a correlated subquery join is written; the four-argument
form composes two queries directly.

### Single-argument form - `from x in outer.XxxJoin(predicate)`

```csharp
var result =
    from o in db.GetTable<Order>()
    from c in db.GetTable<Customer>().LeftJoin(c => c.CustomerID == o.CustomerID)
    select new { o.OrderID, CustomerName = c.Name };
```

### Four-argument form - `outer.XxxJoin(inner, predicate, resultSelector)`

```csharp
var result = db.GetTable<Order>()
    .LeftJoin(
        db.GetTable<Customer>(),
        (o, c) => o.CustomerID == c.CustomerID,
        (o, c) => new { o.OrderID, CustomerName = c.Name });
```

`CrossJoin` only has the two-source form (no predicate - every row pairs with every row):

```csharp
var result = db.GetTable<Size>()
    .CrossJoin(db.GetTable<Color>(), (s, c) => new { s.Name, c.Name });
```

### Versus standard LINQ `join` / `GroupJoin`

The standard LINQ `join ... equals ...` (inner join) and `join ... into g from x in
g.DefaultIfEmpty()` (left join emulation) work in LinqToDB exactly as in any other LINQ provider -
no LinqToDB-specific behavior there. The fluent extensions above are LinqToDB-specific sugar over
the same translation; prefer them when the join is a left/right/full outer join and the
`GroupJoin` + `DefaultIfEmpty` pattern would be less readable.

---

## 2. Association-driven (navigation-like) joins

Accessing a reference association inside a filter or projection generates an implicit join - no
`.Join()`/`.LeftJoin()` call needed:

```csharp
var result =
    from o in db.GetTable<Order>()
    select new { o.OrderID, CustomerName = o.Customer.Name };  // implicit join via [Association]
```

The join type follows the association's `CanBeNull` setting: `CanBeNull = false` produces an
`INNER JOIN`, `CanBeNull = true` produces a `LEFT JOIN`. See [`associations.md`](17-associations.md)
for how to declare associations, `LoadWith`/`ThenLoad` eager loading, and eager-loading strategy
tuning - this document only covers the join shape that association access produces.

---

## 3. Cases that fail to translate

### Composite-key join with mismatched projections

Joining on `new { ... } equals new { ... }` requires the two anonymous/object-initializer
projections to describe the same key shape. A structurally mismatched pair - for example, the left
side carrying an extra unrelated field that isn't part of the actual join key - throws
`LinqToDBException` at query execution:

```csharp
// Throws LinqToDBException - FirstName on the left side is not part of a real key match
var q =
    from p1 in db.Person
    join p2 in db.Person on new Person { FirstName = "", ID = p1.ID } equals new Person { ID = p2.ID }
    select new { p1.ID, p2.FirstName };
```

Keep both sides of a composite-key `join ... equals ...` structurally symmetric - same property
set, each pair describing one comparison.

---

## 4. Redundant joins are optimized away

LinqToDB's query optimizer collapses joins that are provably redundant - same source table, same
join key as an already-present join in the query. Writing several joins to the same table with an
equivalent key does not necessarily produce that many `JOIN` clauses in the generated SQL:

```csharp
var q = from od in db.GetTable<OrderDetail>()
        join o1 in db.GetTable<Order>() on od.OrderID equals o1.OrderID
        join o2 in db.GetTable<Order>() on o1.OrderID equals o2.OrderID   // same key as o1 - collapsed
        select new { od.OrderID, o1.OrderDate, o2.OrderDate };
// Generated SQL has a single JOIN to Order, not two.
```

Do not avoid intermediate joins for fear of extra `JOIN` clauses in generated SQL - write the
query in whatever shape is clearest and let the optimizer collapse the redundancy.

---

## 5. Provider limitations

### `RightJoin` / `FullJoin` are not validated against provider support

Unlike `MERGE`, upsert, or bulk copy, `RightJoin`/`FullJoin` support is **not gated by a
`SqlProviderFlags` check** - LinqToDB always emits `RIGHT JOIN` / `FULL JOIN` text regardless of
provider. On a provider without native support for one of these join types, the failure surfaces
as a **database-level SQL syntax error**, not a `LinqToDBException` at query-build time. Check
provider SQL dialect documentation before relying on `RightJoin`/`FullJoin` for a specific
database, and prefer rewriting as a `LeftJoin` with swapped operands when portability matters.

### `APPLY` / `LATERAL` is chosen automatically, not requestable directly

`CROSS APPLY` / `OUTER APPLY` (or `LATERAL JOIN` on providers that use that syntax) is an internal
SQL-generation strategy, not a public LINQ API - there is no `.CrossApply()`/`.OuterApply()`
extension to call. LinqToDB emits it automatically when a correlated subquery cannot be flattened
into a regular `JOIN` (for example, a subquery with `Take`/`First` that depends on the outer row,
or certain `LoadWith` eager-loading shapes). Provider support for this capability is listed as the
`APPLY / LATERAL` column in [`provider-capabilities.md`](07-provider-configuration.md) - a query shape
that requires it will fail on a provider without support, with no way to opt out of the shape
short of restructuring the query.

---

## See also

- [`docs/associations.md`](17-associations.md) - declaring associations, `LoadWith`/`ThenLoad` eager
  loading, eager-loading strategies
- [`docs/provider-capabilities.md`](07-provider-configuration.md) - `APPLY / LATERAL` provider support
- [`docs/crud/crud-select.md`](09-crud-and-merge.md) - everyday querying
- [`docs/agent-antipatterns.md`](06-agent-antipatterns-and-ai-tags.md) - common mistakes and how to avoid them

<!-- Generated from: Source/Skills/linq2db/docs/query-temp-tables.md -->

# LinqToDB - Temporary Tables

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> You are here if you need to:
> - create a table that is automatically dropped when your code is done with it
> - stage data in a server-side table for joining, filtering, or further processing
> - work with database-native temporary table syntax (`#name`, `GLOBAL TEMPORARY TABLE`, session-scoped tables, etc.)
> - populate a staging table from a C# collection or an `IQueryable<T>` query

---

> **Agent guidance:**
> - Use the simplest `CreateTempTable` overload that satisfies the requirement.
> - If rows are already available in C# memory, prefer `CreateTempTable(items)` / `CreateTempTableAsync(items)`. Do not create an empty table and call `BulkCopy` separately unless rows are loaded later or copy behavior must be controlled separately.
> - If rows come from an `IQueryable<T>` query, prefer `CreateTempTable(query)` / `CreateTempTableAsync(query)`. LinqToDB populates the temp table server-side with `INSERT ... SELECT`.
> - Do not introduce `setTable`, `CreateTempTableOptions`, or explicit `TableOptions` unless the task requires them.
> - Temporary tables require a `DataConnection`, not a `DataContext`. `DataContext` opens and closes a physical connection per command; a temporary table's lifetime is tied to the session, so the table would be invisible across commands.
> - Always use `await using` / `using` to ensure the backing table is dropped even on exception.
> - Do not use temporary tables as a general substitute for subqueries or CTEs - they carry DDL overhead. Use them when server-side staging is genuinely needed or when the collection originates in C# memory.
> - `CreateTempTable(items)` loads in-memory data through provider default `BulkCopy`; use the overload with `BulkCopyOptions` when copy behavior must be controlled.
> - `CreateTempTable(query)` loads server-side query data with `INSERT ... SELECT`; it is not a client-side BulkCopy path.
> - For anonymous-type projections, specify a table name. Use the `setTable` fluent mapping parameter when anonymous `string` or `decimal` columns need length/precision metadata.
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
PostgreSQL and MySQL). Behavior on providers that do not support native temporary tables
depends on the provider implementation and may vary.

The name `CreateTempTable` therefore describes the **managed lifecycle** (`CREATE` + auto-`DROP`),
not necessarily a database-native temporary table. See section 6 for `TableOptions` details.

---

## Pattern quick-reference

| Scenario | Pattern |
|---|---|
| Create empty table, populate later | `db.CreateTempTable<T>()` - section 1 |
| Create and populate from a C# collection | Prefer `db.CreateTempTable<T>(items)` - section 2 |
| Create and populate from a query (`INSERT ... SELECT`) | Prefer `db.CreateTempTable(query)` - section 3 |
| Async creation | `db.CreateTempTableAsync<T>(...)` - section 4 |
| Custom table name / schema | `tableName:` parameter - section 5 |
| Control the physical table kind | `TableOptions` flags - section 6 |
| Query the table in LINQ | `TempTable<T>` implements `ITable<T>` - section 7 |
| Populate after empty creation | `Copy` / `Insert` only when rows are not available at create time - section 8 |
| Source is an anonymous-type projection | `CreateTempTable("#name", query, e => e...)` - section 9. `setTable` provides inline schema metadata (`HasLength`, `HasPrecision`) for anonymous-type columns. |

---

## 1. Create an empty table

Creates the physical table immediately. Populate it later using `Copy` or `Insert` (section 8).

```csharp
using LinqToDB;
using LinqToDB.Data; // DataConnection required

using var db    = new DataConnection(options);
using var table = db.CreateTempTable<Product>();

// table is a disposable TempTable<Product>; use it as an ITable<Product> to query or populate it here
```

---

## 2. Create and populate from a C# collection

Combines `CREATE TABLE` and provider default `BulkCopy` in one call.
This is the recommended path when the rows are already in application memory.
Do not split it into `CreateTempTable<T>()` followed by `table.BulkCopy(...)`
unless the table must be created before rows are available or the task explicitly needs
separate copy control.

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

Use this path when the source data is already in application memory. LinqToDB sends the rows to
the provider through its default bulk-copy implementation unless explicit `BulkCopyOptions` are
passed.

---

## 3. Create and populate from a query (`INSERT … SELECT`)

Populates the table server-side using `INSERT INTO … SELECT`. No data crosses the wire between
the database and the application.
This path is selected when the source is an `IQueryable<T>` query.
This is the recommended path for query sources; do not materialize the query into memory
and BulkCopy it back into the database.

```csharp
using var db = new DataConnection(options);
using var table = db.CreateTempTable(
    db.GetTable<Product>().Where(p => p.CategoryId == 5));

// Join the staging table against other tables
var joined = db.GetTable<Order>()
    .Join(table, o => o.ProductId, p => p.Id, (o, p) => new { o, p })
    .ToList();
```

The optional `action` parameter runs after `CREATE TABLE` but before the `INSERT`,
which can be used to execute DDL on the table (e.g., creating an index) before data is loaded.
Pass it via `CreateTempTableOptions` or the named `action:` parameter if this is required.

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
using var table = db.CreateTempTable<Product>("#staging", products);
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

## 6. `TableOptions` - controlling the physical table kind

`TableOptions` is a `[Flags]` enum that controls what kind of table is created.
The `CreateTempTable` extension methods default to `TableOptions.IsTemporary`.

| Value | Physical table kind |
|---|---|
| `TableOptions.NotSet` | Does not override mapped table options. It does not ask the provider to choose a temporary-table kind. |
| `TableOptions.IsTemporary` *(default)* | Requests a database-native local (session-scoped) temporary table when supported by the provider; exact SQL and behavior are provider-defined |
| `TableOptions.None` | Regular physical table - **not** session-scoped; visible to other sessions depending on the provider; lifecycle still managed by `TempTable<T>` (auto-dropped on dispose) |
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
> Check the `TableOptions` XML-doc entries in `docs/api.md` or `linq2db.xml`
> before using flags beyond `IsTemporary`.

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

## 8. Populating after creation - `Copy` and `Insert`

When the table was created empty (section 1 or section 4 empty-create), populate it afterwards:
Use this only when rows are not available at creation time, when load timing must be separated
from table creation, or when the task needs explicit post-create work before loading rows.
If the collection or query is already available, prefer section 2 or section 3 instead.

```csharp
using var table = db.CreateTempTable<Product>();

// From a C# collection - BulkCopy
long copied = table.Copy(products);

// From a query - INSERT … SELECT
long inserted = table.Insert(db.GetTable<Product>().Where(p => p.IsActive));
```

Async variants:

```csharp
long copied   = await table.CopyAsync(products, cancellationToken: ct);
long inserted = await table.InsertAsync(db.GetTable<Product>().Where(...), ct);
```

---

## 9. Anonymous-type source and `setTable` fluent mapping

Search anchors: anonymous type, anonymous-type projection, `CreateTempTable("#name", query, e => e...)`, `setTable`, `MappingSchema`, `HasLength`, `HasPrecision`, `Length`, `Precision`, `Scale`, string SQL type, decimal SQL type, `DataType`, provider defaults, `CREATE TABLE`.

`CreateTempTable` supports anonymous-type projections directly.
When the projected columns are only needed locally for staging or query composition,
prefer an anonymous type over introducing a separate DTO or named class whose sole purpose
is to serve as a temp-table row type.

> **Always specify the table name** when using an anonymous-type source.
> LinqToDB cannot derive a meaningful name from an anonymous type and will generate an
> internal placeholder that may vary across .NET versions or compilations.

When the projection includes `string` or `decimal` columns, use the `setTable` parameter
to provide per-column schema metadata inline via the fluent API:

For temp-table creation, `setTable` is applied to a fresh writable temporary `MappingSchema`
created by the temp-table API from the context mapping schema. Do not call
`UseEnableContextSchemaEdit(...)` and do not enable global `Linq.EnableContextSchemaEdit`
for this case.

```csharp
using var table = db.CreateTempTable(
    "#active_products",
    db.GetTable<Product>().Where(p => p.IsActive).Select(p => new { p.Id, p.Name, p.Price }),
    e => e
        .Property(p => p.Id)
            .IsPrimaryKey()
        .Property(p => p.Name)
            .HasLength(200)             // TODO: confirm max length
        .Property(p => p.Price)
            .HasPrecision(18, 2));
```

**Required rules for `setTable` columns:**

- Every `string` property must have an explicit length: `.HasLength(n)`.
  If the task does not state an exact limit, choose a bounded value and add a `TODO` comment.
- Every `decimal` property must have explicit precision and scale: `.HasPrecision(p, s)`.
- Without these, the generated `CREATE TABLE` statement will use provider defaults that differ
  across databases (see anti-pattern #10 in `docs/agent-antipatterns.md`).

For projections where all columns are provider-neutral types (`int`, `bool`, etc.),
`setTable` is not needed:

```csharp
using var table = db.CreateTempTable(
    "#active_products",
    db.GetTable<Product>().Where(p => p.IsActive).Select(p => new { p.Id, p.Stock }));
```

---

## 10. Column size requirements for mapped classes

When a named class (not an anonymous type) is used with `CreateTempTable`, the same schema rules
apply as for any `CREATE TABLE` operation. See SKILL.md step 3 and anti-pattern #10:

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

- [`docs/agent-antipatterns.md`](06-agent-antipatterns-and-ai-tags.md) - anti-pattern #10: unconstrained column types in schema generation.
- [`docs/query-cte.md`](10-query-composition.md) - CTEs as a lightweight alternative for query composition that does not require DDL.
- [`docs/crud/crud-bulkcopy.md`](09-crud-and-merge.md) - `BulkCopyOptions` used by `CreateTempTable(items, ...)` and `Copy`.
- `TempTable<T>` - XML documentation for the full constructor and instance method reference.
- `TableOptions` - XML documentation for provider support details per flag.

<!-- Generated from: Source/Skills/linq2db/docs/null-semantics.md -->

# Null Semantics

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

Use this guide when the SQL generated for a null comparison looks more complex than the LINQ
expression that produced it, or when deciding how to compare nullable values.

## Baseline fact

A literal `null` in a query always compiles to `IS NULL` / `IS NOT NULL`, regardless of any
setting below.

```csharp
db.Person.Where(p => p.MiddleName == null)
```

```sql
SELECT ... FROM Person WHERE MiddleName IS NULL
```

## The controlling option: `CompareNulls`

`LinqOptions.CompareNulls` (configure via `DataOptions.UseCompareNulls(CompareNulls)`, or
`LinqOptions.WithCompareNulls(CompareNulls)` if building a `LinqOptions` value directly) decides how
`==`/`!=`/`Contains` behave when a *non-literal* nullable value is involved. Do not use the
obsolete `DataOptions.UseCompareNullsAsValues(bool)` - `true` maps to `LikeClr`, `false` to
`LikeSqlExceptParameters`; it is scheduled for removal in v7.

| Value | Behavior |
|---|---|
| `CompareNulls.LikeClr` (**default**) | C# equality semantics: two nulls compare equal. Can add `OR (... IS NULL AND ... IS NULL)` to preserve this - see below. May prevent index usage on the affected predicate. |
| `CompareNulls.LikeSql` | Straight translation to SQL operators; nulls compare as `UNKNOWN` (three-valued logic), matching raw SQL semantics. Parameter values are **not** sniffed for null. |
| `CompareNulls.LikeSqlExceptParameters` | Same as `LikeSql`, except a null-valued parameter still compiles to `IS NULL`. Kept for pre-6.0 backward compatibility; prefer `LikeSql` for new code. |

## Why a null-valued parameter becomes `IS NULL`

Under the default `LikeClr` (and under `LikeSqlExceptParameters`), linq2db inspects a captured
variable's runtime value for `Equal`/`NotEqual` comparisons. If it is `null` at query-build time,
the comparison compiles to `IS NULL` instead of `= @p`:

```csharp
string? name = null;
db.Person.Where(p => p.MiddleName == name)
```

```sql
SELECT ... FROM Person WHERE MiddleName IS NULL
```

This is intentional: `@p = NULL` in raw SQL evaluates to `UNKNOWN`, never `TRUE` - without this
sniffing, the query would silently match zero rows whenever the parameter happened to be null.
Under plain `LikeSql`, this sniffing does not happen; the comparison stays `= @p` and inherits
standard SQL `UNKNOWN` behavior for a null parameter.

## Why two nullable columns can generate `OR (... IS NULL AND ... IS NULL)`

C# `a == b` treats `null == null` as `true`. Plain SQL `a = b` treats `NULL = NULL` as `UNKNOWN`
(never true). Under the default `LikeClr`, linq2db closes this gap - but only when **both** sides
of the comparison are independently classified as nullable. If only one side can be null (or
neither), the simpler form is used instead.

```csharp
from e1 in db.Parent
from e2 in db.Parent
where e1.Value1 == e2.Value1   // both Value1 columns are nullable
select e1
```

```sql
... WHERE e1.Value1 = e2.Value1 OR (e1.Value1 IS NULL AND e2.Value1 IS NULL)
```

## Manual control

Use these when the default `CompareNulls.LikeClr` expansion is unwanted for a specific comparison,
without changing the setting globally.

| API | Effect |
|---|---|
| `Sql.AsNotNull(value)` / `Sql.AsNotNullable(value)` | Marks one side of a comparison as non-nullable for nullability analysis. Since the `OR (... IS NULL AND ...)` expansion only fires when **both** sides are classified nullable, marking either side non-nullable makes the comparison use the simpler form. |
| `.IsDistinctFrom(other)` / `.IsNotDistinctFrom(other)` | Extension methods mapping to SQL `IS [NOT] DISTINCT FROM` (or its provider-specific equivalent) - a null-safe comparison for one specific expression, without touching the `CompareNulls` setting at all. |
| `Sql.ToNullable(value)` (value types only) | Widens `T` to `T?` **as a real C# type change**, so the expression can be compared to `null` or assigned to a nullable-typed slot. Use this when the column's C# type will not otherwise let you write `== null`. |
| `Sql.ToNotNull(value)` / `Sql.ToNotNullable(value)` (value types only) | The reverse narrowing, `T?` to `T`. |
| `Sql.AsNullable(value)` | Annotates SQL-level nullability **without changing the C# type** (`T` in, `T` out - unlike `ToNullable`, which returns `T?`). Rarely needed directly; prefer `ToNullable` when you need the C# type itself to become nullable. |

### `AsNotNull` example

```csharp
// Wrong - relies on the default LikeClr expansion, unclear intent:
from p1 in db.Parent
from p2 in db.Parent
where p1.Value1 == p2.Value1
select p1;

// Correct - explicit that a null on either side should not match, simpler generated SQL:
from p1 in db.Parent
from p2 in db.Parent
where Sql.AsNotNull(p1.Value1) == Sql.AsNotNull(p2.Value1)
select p1;
```

The second form is equivalent to filtering with `p1.Value1 != null && p1.Value1 == p2.Value1` on
the client - either operand being null excludes the row, matching plain SQL equality.

## Common Mistakes

### Assuming the extra `OR (... IS NULL AND ...)` is a bug or an inefficiency to "fix" by rewriting the query

Wrong:

```csharp
// "Simplifying" a nullable comparison because the generated SQL looks redundant
where e1.Value1 == e2.Value1 && e1.Value1 != null
```

Correct: recognize this is `CompareNulls.LikeClr` deliberately preserving C# null-equality
semantics. If SQL `UNKNOWN`-based semantics are actually wanted, use `Sql.AsNotNull` on the
specific comparison, `IsDistinctFrom`/`IsNotDistinctFrom`, or set `CompareNulls.LikeSql` for the
whole query context - do not assume the current SQL is wrong.

### Switching to `CompareNulls.LikeSql` without checking parameter-null handling

Wrong: changing `CompareNulls` to `LikeSql` globally to get simpler SQL, without checking whether
any existing `== someNullableVariable` comparison depended on the default sniffing to match null
values via `IS NULL`.

Correct: under `LikeSql`, a null-valued parameter compiles to `= @p` with standard SQL `UNKNOWN`
semantics (never matches). Audit comparisons against nullable captured variables before switching
away from `LikeClr`/`LikeSqlExceptParameters`, or use `Sql.AsNotNull`/`IsDistinctFrom` per
comparison instead of a global setting change.

## API Lookup Anchors

Search `docs/api.md` for:

- `CompareNulls`
- `LikeClr`
- `LikeSql`
- `LikeSqlExceptParameters`
- `WithCompareNulls`
- `AsNotNull`
- `AsNotNullable`
- `AsNullable`
- `ToNullable`
- `ToNotNull`
- `ToNotNullable`
- `IsDistinctFrom`
- `IsNotDistinctFrom`
