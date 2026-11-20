# Recent changes, with examples

Part of the linq2db skill; the overview is in [SKILL.md](../SKILL.md). Each entry names the version that
added the API. `linq2db.xml` has the full signatures and the remaining overloads.

## Window functions (6.4)

`Sql.Window.*` takes the window as a lambda. Frames, `Filter`, named windows and `NULLS FIRST/LAST` are
on the same builder.

```csharp
var q =
    from s in db.GetTable<Sale>()
    select new
    {
        s.Id,
        Rank       = Sql.Window.RowNumber(w => w.PartitionBy(s.RegionId).OrderByDesc(s.Amount)),
        RunningSum = Sql.Window.Sum(s.Amount, w => w.PartitionBy(s.RegionId).OrderBy(s.Date)
            .RowsBetween.Unbounded.And.CurrentRow),
        Previous   = Sql.Window.Lag(s.Amount, 1, 0m, w => w.OrderBy(s.Date)),
    };
```

The older `Sql.Ext.RowNumber().Over()...ToValue()` chains still run; analyzer L2DB1001 offers a fix to
the new form.

## Eager-loading strategies (6.4)

```csharp
var customers = db.GetTable<Customer>()
    .LoadWith(c => c.Orders)
    .WithKeyedLoadStrategy()   // children by WHERE key IN (...) over the buffered parents
    .ToList();
```

`WithUnionLoadStrategy()` loads collections of one level in one `UNION ALL` query;
`WithSeparateLoadStrategy()` is the default. Set the default for all queries with
`options.UseDefaultEagerLoadingStrategy(EagerLoadingStrategy.KeyedQuery)`.
Since 6.6, `UseCombinedCommands(true)` sends the parent and child queries in one round trip.

## Keyed query filters (6.4)

```csharp
var builder = new FluentMappingBuilder(mappingSchema);

builder.Entity<Order>()
    .HasQueryFilter("SoftDelete", o => !o.IsDeleted)
    .HasQueryFilter<AppDb>("Tenant", (o, dc) => o.TenantId == dc.TenantId);

builder.Build();

var all = db.GetTable<Order>().IgnoreFilters(["SoftDelete"]).ToList();   // tenant filter stays
```

`IgnoreFilters()` without keys drops every filter; a second argument limits it to entity types.

## Upsert and entity DML builders (6.4)

```csharp
db.GetTable<Product>().Upsert(product, u => u
    .Match((t, s) => t.Sku == s.Sku)                      // default: the primary key
    .Insert(i => i.Set(x => x.CreatedAt, () => Sql.CurrentTimestamp))
    .Update(v => v.Ignore(x => x.CreatedAt)));

db.GetTable<Product>().Update(product, u => u.Ignore(x => x.CreatedAt));   // by primary key
db.GetTable<Product>().Insert(product, i => i.Set(x => x.CreatedAt, () => Sql.CurrentTimestamp));
```

`Upsert` also takes `IEnumerable<T>` or `IQueryable<T>` as the source. It emits a native upsert or
`MERGE` where the database has one, otherwise a SELECT/UPDATE/INSERT sequence that is atomic only inside a
transaction (`LinqOptions.UpsertEmulationPolicy` can forbid that). `SkipInsert()` / `SkipUpdate()` turn it
into update-only or insert-only.

## NULLS FIRST / LAST in ORDER BY (6.4)

```csharp
var q = db.GetTable<Ticket>()
    .OrderBy(t => t.DueDate, Sql.NullsPosition.Last)
    .ThenByDescending(t => t.Priority, Sql.NullsPosition.First);
```

Rendered natively where the database supports it, emulated elsewhere. Default for all queries:
`options.UseDefaultNullsPosition(Sql.NullsPosition.Last)`.

## Smaller 6.4 additions

- `Sql.NewGuid7()` and `Guid.CreateVersion7()` in a query produce UUIDv7 values.
- `options.UsePreferClientCalculation(true)`: parts of the final projection that do not need the
  database are computed on the client, as in 5.x.
- `DistinctBy` emits `DISTINCT ON` on PostgreSQL and DuckDB.

## Durations (6.5)

```csharp
public class Job
{
    [Column(DataType = DataType.Int64), Duration(DurationUnit.Second)]
    public TimeSpan Elapsed { get; set; }
}

var slow = db.GetTable<Job>().Where(j => j.Elapsed > TimeSpan.FromMinutes(5));
```

Without `[Duration]` a `TimeSpan` column keeps its provider meaning (often a time of day). With it,
comparisons and arithmetic translate; analyzer L2DB1002 reports a comparison with a value the unit cannot
represent. Also since 6.5, a date difference (`end - start`) measures elapsed time instead of counting
calendar boundaries.

## Optimistic update with refresh (6.5)

```csharp
public class Doc
{
    [PrimaryKey] public int Id { get; set; }
    [OptimisticLockProperty(VersionBehavior.AutoIncrement)] public int Version { get; set; }
}

// using LinqToDB.Concurrency;
if (db.UpdateOptimisticWithRefresh(doc) == 0)
    throw new DBConcurrencyException();   // someone else changed the row
// doc.Version now holds the new value, so doc can be updated again
```

`UpdateOptimistic` leaves the entity's token as it was, so a second update of the same instance affects no
rows.

## 6.6

- `options.UseCombinedCommands(true)` sends an eager load, and on SQLite and MySQL an identity insert, as
  one command.
- The .NET 11 `Queryable.Join` / `LeftJoin` / `RightJoin` / `FullJoin` overloads that return tuples
  translate.
- A nullable comparison used as a value (`Select(x => x.A == x.B)`) reads `false` when an operand is
  `NULL`, as in C#, on databases with a boolean type.
- An eager load outside a transaction reads all parent rows and commits before returning the first row.
- Packages target `net462`, `netstandard2.0`, `net10.0` and `net11.0`; .NET 8 and 9 use `netstandard2.0`.
- `UseAccess(connectionString, AccessProvider.LibRed)` opens `.mdb` / `.accdb` files without the Access
  Database Engine (.NET 10+, `LibRed.Ado` package).
