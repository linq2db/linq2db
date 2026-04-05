# LinqToDB — Common Table Expressions (CTE)

> **Required:** Read [`AGENT_GUIDE.md`](../AGENT_GUIDE.md) before any implementation.

> **You are here if** you need to:
> - introduce an explicit named SQL CTE (`WITH ...`) within a larger query
> - factor out a complex subquery for readability using a named SQL construct
> - traverse a hierarchy or graph with a recursive query

> **Provider support:** CTEs are not supported on all providers.
> Check the `CTE` row in [`provider-capabilities.md`](provider-capabilities.md) before using.

---

## 1. Basic CTE — `AsCte()`

Call `.AsCte()` on any `IQueryable<T>` to declare a named intermediate result.
The returned value is a composable `IQueryable<T>` — use it as a query source.

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
in SQL — the subquery is declared once, not duplicated inline.

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

## 3. CTE parameters — captured variables

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

### Opt-in: inline captured values as literals — `InlineParameters()`

By default captured C# variables become SQL parameters — this is the correct default.
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
> The recursive branch must contain a condition that eventually stops producing new rows —
> without it the query will recurse until the database hits its maximum recursion depth.

---

## 5. CTE as a source in DML

A CTE variable is a plain `IQueryable<T>` — use it as a filter or join source in
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
> Validate against the `CTE` row in [`provider-capabilities.md`](provider-capabilities.md) and test with your specific provider.

---

## Anti-patterns

| Mistake | Consequence |
|---|---|
| Treating CTE as a materialized temp table | CTE is inlined in SQL — it is **not** a temp table; the database may re-evaluate it for each reference unless it chooses to materialize |
| Skipping provider support check | Runtime exception on providers that do not support `WITH` |
| Using recursive CTE with mismatched projections | Compile error or silent wrong SQL — anchor and recursive branch must return the same type |
| Using `AsCte()` only to introduce an intermediate query variable | A plain `IQueryable<T>` variable already composes and reuses without generating a SQL CTE; use `AsCte()` only when you intentionally need a named SQL CTE, explicit query factoring, or recursion |

---

## See also

- [`crud-select.md`](crud/crud-select.md) — everyday querying
- [`provider-capabilities.md`](provider-capabilities.md) — CTE support per provider
