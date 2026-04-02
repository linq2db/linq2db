# LINQ to DB

[![License](https://img.shields.io/github/license/linq2db/linq2db)](https://github.com/linq2db/linq2db/blob/master/MIT-LICENSE.txt)

LINQ to DB is the fastest LINQ database access library for .NET — a simple, light, and type-safe layer between your objects and your database.

Write SQL as type-safe C# — queries compile, refactor, and translate to transparent, predictable SQL with no magic strings.

Supports SQL Server, PostgreSQL, MySQL, Oracle, SQLite, and [many more providers](https://linq2db.github.io/articles/general/databases.html).

## Features

- **Typed SQL** — express SQL intent directly in C#; queries are compiler-checked, refactorable, and translate to transparent predictable SQL
- **Full DML support** — `Insert`, `Update`, `Delete`, `InsertOrReplace`, set-based updates
- **Bulk copy** — high-performance batch inserts using provider-native mechanisms
- **Merge API** — set-based MERGE (INSERT / UPDATE / DELETE in one statement)
- **CTE support** — composable Common Table Expressions including recursive
- **Window / analytic functions** — `OVER`, `PARTITION BY`, `ORDER BY` via LINQ
- **Associations** — define relationships between entities once; use them as navigation properties in queries instead of writing explicit JOINs; use `LoadWith` for eager loading
- **Temp tables** — create and query temporary tables within a session
- **Explicit join syntax** — `InnerJoin`, `LeftJoin`, `CrossJoin` in addition to standard LINQ
- **Provider-specific hints** — query and table hints for SQL Server, Oracle, PostgreSQL, MySQL, and others applied directly in LINQ
- **Rich built-in SQL translation** — hundreds of standard .NET methods (`string`, `Math`, `DateTime`, numeric conversions) translated to SQL out of the box; the `Sql` class adds SQL-specific functions (`CharIndex`, `Left`/`Right`, `Stuff`, math, type conversions) with provider-aware implementations
- **Extensible SQL mapping** — map application-specific methods and properties to any SQL expression, function, operator, or fragment; reuse SQL constructs as C# methods via `[ExpressionMethod]`

## Quick start

```cs
// Configure once — reuse for all connections
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
    [PrimaryKey, Identity] public int     ProductID { get; set; }
    [Column, NotNull]      public string  Name      { get; set; } = null!;
    [Column]               public bool    IsActive  { get; set; }
    [Column]               public decimal Price     { get; set; }
}
```

Attributes, fluent mapping via `MappingSchema`, and convention-based mapping are all supported.
To scaffold classes from an existing database use [linq2db.cli](https://www.nuget.org/packages/linq2db.cli) (`dotnet tool`) or [T4 templates](https://linq2db.github.io/articles/T4.html).

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

## DI / ASP.NET Core

```cs
// Register options once as singleton, inject context as scoped
builder.Services.AddSingleton(new DataOptions().UseSqlServer(connectionString));
builder.Services.AddScoped<AppDB>();
```

See [ASP.NET Core setup guide](https://linq2db.github.io/articles/get-started/asp-dotnet-core/index.html).

## Documentation and resources

- [Full documentation](https://linq2db.github.io)
- [FAQ](https://linq2db.github.io/articles/FAQ.html)
- [Examples](https://github.com/linq2db/examples)
- [linq2db.cli](https://www.nuget.org/packages/linq2db.cli) — scaffold POCO classes from a database schema

---

## For AI / LLM agents

Package includes machine-readable documentation:

- XML docs class `LinqToDB.LinqToDBArchitecture` — architecture overview, translation pipeline, anti-patterns.
- [`docs/architecture.md`](docs/architecture.md) — extended architecture reference.
- [`docs/ai-tags.md`](docs/ai-tags.md) — AI-Tags metadata format specification.
- [`docs/agent-antipatterns.md`](docs/agent-antipatterns.md) — operational anti-patterns with code examples.
