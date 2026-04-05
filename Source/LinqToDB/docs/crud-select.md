# LinqToDB — Querying Data

> You are here if you need to:
> - read rows from a table
> - filter, sort, project, or paginate query results
> - load associated entities (`LoadWith`)
> - avoid common query translation mistakes

---

## Entry point — `GetTable<T>`

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

## Projection — `Select`

```csharp
var names = await db.GetTable<Product>()
    .Where(p => p.IsActive)
    .Select(p => new { p.ProductID, p.Name, p.Price })
    .ToListAsync();
```

Project before materialization to avoid fetching unused columns.

---

## Pagination — `Skip` / `Take`

```csharp
var page = await db.GetTable<Product>()
    .OrderBy(p => p.ProductID)   // ORDER BY is required for deterministic pagination
    .Skip(pageIndex * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

`Skip` without `OrderBy` produces non-deterministic results — always pair them.

---

## Loading associations — `LoadWith`

LinqToDB does not support lazy loading. Use `LoadWith` to load associated entities eagerly
in a single query or a minimal number of additional queries:

```csharp
var orders = await db.GetTable<Order>()
    .Where(o => o.CustomerID == customerId)
    .LoadWith(o => o.OrderItems)          // load child collection
    .LoadWith(o => o.Customer)            // load parent reference
    .ToListAsync();
```

`LoadWith` generates an additional SQL query per association level, not a JOIN (by default).
Use `.LoadWith(o => o.Items.First().Product)` to chain deeper associations.

---

## Aggregates

```csharp
int    count    = await db.GetTable<Product>().CountAsync(p => p.IsActive);
decimal maxPrice = await db.GetTable<Product>().MaxAsync(p => p.Price);
```

---

## Anti-patterns to avoid

| Mistake | Consequence | Reference |
|---|---|---|
| Applying `.Where()` after `.ToList()` | Filter runs in memory — full table fetched | Anti-pattern #5 |
| Using a non-translatable method inside `.Where()` | `LinqToDBException` at execution time | Anti-pattern #4 |
| `Skip` without `OrderBy` | Non-deterministic page results | — |
