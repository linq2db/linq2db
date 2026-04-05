# LinqToDB — MERGE

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
> Check the `Merge` column in [`provider-capabilities.md`](../provider-capabilities.md).
> Support for base MERGE, `NOT MATCHED BY SOURCE` branches, and OUTPUT are independently
> provider-defined — a provider may support MERGE but not OUTPUT, or MERGE but not `BY SOURCE`.
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

`MergeInto(target)` is the source-first alternative to `Merge` + `Using` — section 2.

---

## Pattern quick-reference

| Scenario | Pattern |
|---|---|
| Sync target from another table (update existing, insert new) | `Merge` + `Using` + `OnTargetKey` + `UpdateWhenMatched` + `InsertWhenNotMatched` — section 4 |
| Source is a JOIN, projection, or CTE (`TSource ≠ TTarget`) | `Using(anyQuery)` + `On(...)` + setter-based operations — section 2 |
| Sync and delete rows absent from source | add `DeleteWhenNotMatchedBySource` — section 4 |
| Upsert from in-memory list | `Merge` + `Using(IEnumerable)` + `On` + `UpdateWhenMatched` + `InsertWhenNotMatched` — section 4 |
| Conditional update only | `UpdateWhenMatchedAnd(condition, setter)` — section 4 |
| Delete matched rows | `DeleteWhenMatched` / `DeleteWhenMatchedAnd` — section 4 |
| Capture affected rows (action + before/after) | `MergeWithOutput` — section 5 |
| Write output directly to a table | `MergeWithOutputInto` — section 5 |

---

## 1. Starting a merge — entry points

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

## 2. Source — `Using` / `UsingTarget`

### `Using(IQueryable<TSource>)` — any server-side query

Accepts any `IQueryable<TSource>`: a plain table, a filtered query, a JOIN, a projection, a CTE, or
an arbitrary subquery. `TSource` does not need to match `TTarget` — the operation setters in
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
// TSource is an anonymous projection from a JOIN — TSource != TTarget
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

### `Using(IEnumerable<TSource>)` — in-memory source

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

### `UsingTarget()` — target as its own source

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

## 3. Match condition — `On` / `OnTargetKey`

### `OnTargetKey()` — PK columns from mapping

Available whenever `TSource == TTarget` — either via `.UsingTarget()` or when `.Using(...)` is called
with a query that returns the same type as the target.
Uses the `[PrimaryKey]` columns from the entity mapping:

```csharp
.UsingTarget()
.OnTargetKey()
```

### `On(targetKey, sourceKey)` — key selectors

When source and target types differ, supply the key projection for each:

```csharp
.On(t => t.ProductID, s => s.ProductID)
```

### `On(matchCondition)` — arbitrary predicate

For composite or expression-based conditions:

```csharp
.On((t, s) => t.ProductID == s.ProductID && t.RegionCode == s.RegionCode)
```

---

## 4. Operations — `When*` clauses

Operations are evaluated in declaration order; the first matching operation wins for each row pair.
At least one operation must be present before calling the terminal.

### Insert when not matched (source row has no target counterpart)

```csharp
// Copy all fields — requires TSource == TTarget
.InsertWhenNotMatched()

// With condition
.InsertWhenNotMatchedAnd(s => s.IsActive)

// Custom setter — works when TSource != TTarget
.InsertWhenNotMatched(s => new Product { ProductID = s.ProductID, Name = s.Name, Price = s.Price })

// Condition + setter
.InsertWhenNotMatchedAnd(s => s.IsActive, s => new Product { ProductID = s.ProductID, Name = s.Name })
```

### Update when matched (row exists in both source and target)

```csharp
// Copy all fields — requires TSource == TTarget
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

### `.Merge()` — returns row count

```csharp
int affected = db.GetTable<Product>()
    .Merge()
    .Using(stagingData)
    .On((t, s) => t.ProductID == s.ProductID)
    .UpdateWhenMatched()
    .InsertWhenNotMatched()
    .Merge();
```

### `MergeWithOutput` — returns per-row result

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

### `MergeWithOutputInto` — writes output to a table *(SQL Server only)*

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
> Prefer the fluent builder (sections 1–5) for all new code.
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

- [`crud-upsert.md`](crud-upsert.md) — simple Insert-or-Update without a full MERGE builder
- [`crud-update.md`](crud-update.md) — plain UPDATE
- [`crud-delete.md`](crud-delete.md) — plain DELETE
- [`provider-capabilities.md`](../provider-capabilities.md) — MERGE support per provider
