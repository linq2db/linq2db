<!-- Generated from: Source/LinqToDB/README.md -->

# LINQ to DB <!-- omit in toc -->

[![License](https://img.shields.io/github/license/linq2db/linq2db)](https://github.com/linq2db/linq2db/blob/master/MIT-LICENSE.txt)

LINQ to DB is the fastest LINQ database access library for .NET - a simple, light, and type-safe layer between your objects and your database.

Write SQL as type-safe C# - queries compile, refactor, and translate to transparent, predictable SQL with no magic strings.

Supports SQL Server, PostgreSQL, MySQL, Oracle, SQLite, and [many more providers](https://linq2db.github.io/articles/general/databases.html).

> **AI/LLM agents:** this package includes a package-local skill for using this linq2db version.
> In the NuGet package, read `skills/linq2db/SKILL.md` before writing code against this package.
> In the repository, the same skill lives at `Source/Skills/linq2db/SKILL.md`.
> Task-specific references are under `skills/linq2db/docs`, and exact API discovery uses
> `skills/linq2db/docs/api.md` plus `lib/<TFM>/linq2db.xml`.

## Features

- **Typed SQL** - express SQL intent directly in C#; queries are compiler-checked, refactorable, and translate to transparent predictable SQL
- **Full DML support** - `Insert`, `Update`, `Delete`, `InsertOrReplace`, set-based updates
- **Bulk copy** - high-performance batch inserts using provider-native mechanisms
- **Merge API** - set-based MERGE (INSERT / UPDATE / DELETE in one statement)
- **CTE support** - composable Common Table Expressions including recursive
- **Window / analytic functions** - `OVER`, `PARTITION BY`, `ORDER BY` via LINQ
- **Associations** - define relationships between entities once; use them as navigation properties in queries instead of writing explicit JOINs; use `LoadWith` for eager loading
- **Temp tables** - create and query temporary tables within a session
- **Explicit join syntax** - `InnerJoin`, `LeftJoin`, `CrossJoin` in addition to standard LINQ
- **Provider-specific hints** - query and table hints for SQL Server, Oracle, PostgreSQL, MySQL, and others applied directly in LINQ
- **Rich built-in SQL translation** - hundreds of standard .NET methods (`string`, `Math`, `DateTime`, numeric conversions) translated to SQL out of the box; the `Sql` class adds SQL-specific functions (`CharIndex`, `Left`/`Right`, `Stuff`, math, type conversions) with provider-aware implementations
- **Extensible SQL mapping** - map application-specific methods and properties to any SQL expression, function, operator, or fragment; reuse SQL constructs as C# methods via `[ExpressionMethod]`

## Quick start

```cs
// Configure once - reuse for all connections
static readonly DataOptions _options = new DataOptions()
    .UseSqlServer("connection string");

// Use per operation
using var db = new DataConnection(_options);

// Query
var products = await db.GetTable<Product>()
    .Where(p => p.IsActive && p.Price < 100m)
    .OrderBy(p => p.Name)
    .ToListAsync();

// Insert / Update / Delete
await db.InsertAsync(product);
await db.UpdateAsync(product);
await db.DeleteAsync(product);
```

## Mapping

```cs
[Table("Products")]
public class Product
{
    [PrimaryKey, Identity]
    public int ProductID { get; set; }

    [Column(Length = 200), NotNull]
    public string Name { get; set; } = null!;

    [Column]
    public bool IsActive { get; set; }

    [Column(Precision = 18, Scale = 2)]
    public decimal Price { get; set; }
}
```

Attributes, fluent mapping via `MappingSchema`, and convention-based mapping are all supported.
To scaffold classes from an existing database use [linq2db.cli](https://www.nuget.org/packages/linq2db.cli) (`dotnet tool`) or [T4 templates](https://linq2db.github.io/articles/T4.html).

### Default mapping conventions

When no mapping attributes are applied, LinqToDB infers names and membership automatically:

| Concept | Convention |
|---|---|
| Table name | Class name; for interfaces, a leading `I` is stripped (`IProduct` → `Product`) |
| Schema / database | None - specify via `[Table(Schema="..")]` or a runtime override |
| Column name | Property or field name (exact case) |
| Included members | All public instance properties and fields whose CLR type is scalar |
| Nullability | `Nullable<T>` / reference types → nullable column; non-nullable value types → non-nullable |

**Switching to explicit mapping**

Adding `[Table]` to a class opts the **entire class** into explicit mode: only members marked with
`[Column]`, `[PrimaryKey]`, `[Identity]`, or `[ColumnAlias]` become columns.
To keep convention-based inclusion while still using `[Table]`, set `[Table(IsColumnAttributeRequired = false)]`.

> **Note:** If you need a custom `MappingSchema`, create it **once** and share it across all connections.
> Creating a new custom `MappingSchema` per `DataConnection` or per request disables internal caches
> and severely degrades performance. See anti-pattern #1 in [`skills/linq2db/docs/agent-antipatterns.md`](skills/linq2db/docs/agent-antipatterns.md).

## Typed context

```cs
class AppDB : DataConnection
{
    static readonly DataOptions _options =
        new DataOptions().UseSqlServer("connection string");

    public AppDB() : base(_options) {}

    public ITable<Product> Products => GetTable<Product>();
}
```

Inherit from `DataConnection` (persistent connection per instance) rather than `DataContext` (per-query connections). Both support the same query APIs; see `skills/linq2db/docs/architecture.md` for a full comparison.

**DataConnection vs DataContext - when it matters**

| Scenario | Use |
|---|---|
| Temp tables (`CreateTempTable`) | `DataConnection` - requires a stable physical session |
| Explicit `BeginTransaction` / `CommitAsync` | `DataConnection` |
| Session variables, provider `SET` statements | `DataConnection` |
| Ambient `TransactionScope` (auto-enlist) | `DataContext` |
| Ordinary queries / DI scoped context | Either |

## DI / ASP.NET Core

```cs
// Register options once as singleton, inject context as scoped
builder.Services.AddSingleton(new DataOptions().UseSqlServer(connectionString));
builder.Services.AddScoped<AppDB>();
```

See [ASP.NET Core setup guide](https://linq2db.github.io/articles/get-started/asp-dotnet-core/index.html).

## Joins

```cs
// INNER JOIN
var result = from p in db.GetTable<Product>()
             join c in db.GetTable<Category>() on p.CategoryID equals c.CategoryID
             select new { p.Name, c.CategoryName };

// LEFT JOIN
var result = from p in db.GetTable<Product>()
             from c in db.GetTable<Category>()
                         .Where(c => c.CategoryID == p.CategoryID)
                         .DefaultIfEmpty()
             select new { p.Name, CategoryName = c != null ? c.CategoryName : null };
```

See [Join Operators](https://linq2db.github.io/articles/sql/Join-Operators.html) for explicit `InnerJoin` / `LeftJoin` syntax.

## Composing queries

```cs
// Queries are built lazily - add filters and projections before executing
var query = db.GetTable<Product>().AsQueryable();

if (onlyActive)
    query = query.Where(p => p.IsActive);

if (searchFor != null)
    query = query.Where(p => p.Name.Contains(searchFor));

var page = await query
    .OrderBy(p => p.Name)
    .Skip((currentPage - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

## Transactions

```cs
using var db = new AppDB();
using var tr = await db.BeginTransactionAsync();

await db.InsertAsync(order);
await db.InsertAsync(orderLine);

await tr.CommitAsync();  // or tr.RollbackAsync() on error
```

`DataContext` supports `TransactionScope` as well - open the connection inside the scope for it to enlist automatically.

> **Note:** Temp tables, session variables, and other session-scoped state require `DataConnection`.
> `DataContext` opens a new connection per command; session state created in one command does not
> survive to the next. See the decision table above.

## Bulk copy

```cs
using LinqToDB.Data;

var rows = new List<Product> { /* ... */ };

using var db = new AppDB();
await db.BulkCopyAsync(rows);  // uses provider-native bulk mechanism
```

See [Bulk Copy](https://linq2db.github.io/articles/sql/Bulk-Copy.html) for options (`BulkCopyOptions`, row count, transaction control).

## Documentation and resources

- [Full documentation](https://linq2db.github.io)
- [FAQ](https://linq2db.github.io/articles/FAQ.html)
- [Examples](https://github.com/linq2db/examples)
- [linq2db.cli](https://www.nuget.org/packages/linq2db.cli) - scaffold POCO classes from a database schema

---

## For AI/LLM agents

See `skills/linq2db/SKILL.md` for mandatory pre-coding steps, generated API/XML-doc reference rules,
provider runtime dependency checks, and a quick violation reference.

This package bundles machine-readable documentation inside the NuGet package.
The files below are bundled with this readme and are readable by any agent with filesystem access
to the NuGet package directory (e.g. via MCP filesystem tools or the NuGet global cache).

| Package-local path | Content |
|---|---|
| `skills/linq2db/SKILL.md` | Canonical AI entry point for this package version |
| `skills/linq2db/docs/architecture.md` | Architecture overview, translation pipeline, execution model, entry points |
| `skills/linq2db/docs/coverage.md` | Covered and not-yet-covered package-local AI documentation areas |
| `skills/linq2db/docs/ai-tags.md` | `<ai-tags />` metadata format - controlled vocabulary for generated API behavior annotations |
| `skills/linq2db/docs/agent-antipatterns.md` | Operational anti-patterns with `// WRONG` / `// CORRECT` code examples |
| `skills/linq2db/docs/provider-capabilities.md` | SQL feature support matrix per provider (MERGE, CTE, bulk copy, OUTPUT, etc.) |
| `skills/linq2db/docs/provider-setup.md` | Provider configuration reference (ProviderName constants, UseXxx methods, NuGet packages) |
| `skills/linq2db/docs/mapping.md` | Mapping attributes, fluent mapping, generated DDL metadata, value converters |
| `skills/linq2db/docs/crud/*.md` | SELECT, INSERT, UPDATE, DELETE, UPSERT, MERGE, and bulk-copy guides |
| `skills/linq2db/docs/query-cte.md` | CTE query composition |
| `skills/linq2db/docs/query-temp-tables.md` | Temporary table workflows |
| `skills/linq2db/docs/api.md` | API discovery rules and curated extract entries for exact package-version member lookup |
| `skills/linq2db/docs/hints.md` | Query, table, join, subquery, provider-specific, and MERGE hint guidance |
| `skills/linq2db/docs/hints-api-map.md` | Reverse lookup from concrete provider SQL hints to typed provider-specific APIs |
| `skills/linq2db/docs/translatable-methods.md` | Standard .NET methods translated to SQL (String, Math, DateTime, Nullable, casts, Sql.*) |
| `skills/linq2db/docs/configuration.md` | DataOptions patterns: connection setup, logging, retry, interceptors |
| `skills/linq2db/docs/extensions.md` | Extension mechanisms: `[Sql.Expression]`, `[Sql.Function]`, `[ExpressionMethod]`, `IMemberTranslator`, provider-specific overloads |
| `skills/linq2db/docs/interceptors.md` | Interceptor extension points and safe usage |

Online copies may exist on the repository `master` branch, but they can describe a different version.
For installed-package API decisions, agents should use the bundled files above unless package-local
files are unavailable or the user explicitly asks about latest/mainline behavior.
