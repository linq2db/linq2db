# LinqToDB — Querying Data

> You are here if you need to:
> - read rows from a table
> - filter, sort, project, or paginate query results
> - load associated entities (`LoadWith` / `ThenLoad`)
> - avoid common query translation mistakes
>
> For CTEs, named intermediate queries, or recursive traversal → [`query-cte.md`](../query-cte.md)

---

> **Async:** Materialization and aggregation methods have `Async` counterparts (`ToListAsync`,
> `FirstOrDefaultAsync`, `SingleAsync`, `CountAsync`, `MaxAsync`, etc.).
> Examples use both forms. All async methods require:
> ```csharp
> using LinqToDB.Async;
> ```

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

## Loading associations — `LoadWith` / `ThenLoad`

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

### Nested associations — `ThenLoad`

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

### Filtering a loaded association — `loadFunc`

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

## Common Table Expressions — CTE

For named intermediate queries, query factoring, or recursive traversal,
see [`query-cte.md`](../query-cte.md).
Check provider support in [`provider-capabilities.md`](../provider-capabilities.md) before using.

---

## Anti-patterns to avoid

| Mistake | Consequence | Reference |
|---|---|---|
| Applying `.Where()` after `.ToList()` | Filter runs in memory — full table fetched | Anti-pattern #5 |
| Using a non-translatable method inside `.Where()` | `LinqToDBException` at execution time | Anti-pattern #4 |
| `Skip` without `OrderBy` | Non-deterministic page results | — |
| `LoadWith` expected to behave like lazy loading or a single JOIN | One extra SQL query is issued per association level — not lazy, not a JOIN | `LoadWith` section above |
