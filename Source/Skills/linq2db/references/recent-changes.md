# Recent changes

Part of the linq2db skill; the overview is in [SKILL.md](../SKILL.md). APIs added in 6.4 and later, with the
version that added them.

```csharp
// 6.4 window functions: the window is a lambda
Rank = Sql.Window.RowNumber(w => w.PartitionBy(s.RegionId).OrderByDesc(s.Amount)),
Sum  = Sql.Window.Sum(s.Amount, w => w.PartitionBy(s.RegionId).OrderBy(s.Date).RowsBetween.Unbounded.And.CurrentRow),

// 6.4 keyed query filters (fluent mapping), dropped per query by key
builder.Entity<Order>().HasQueryFilter("SoftDelete", o => !o.IsDeleted);
db.GetTable<Order>().IgnoreFilters(["SoftDelete"]);

// 6.4 insert-or-update; Match defaults to the primary key
db.GetTable<Product>().Upsert(product, u => u
    .Match((t, s) => t.Sku == s.Sku)
    .Update(v => v.Ignore(x => x.CreatedAt)));

// 6.4 NULLS FIRST / LAST
db.GetTable<Ticket>().OrderBy(t => t.DueDate, Sql.NullsPosition.Last);
```

- 6.4 eager loading per query: `.LoadWith(...).WithKeyedLoadStrategy()` (children by `WHERE key IN`) or
  `.WithUnionLoadStrategy()`; for all queries `UseDefaultEagerLoadingStrategy(...)`.
- 6.4 entity `Insert(item, i => i.Set(...))` / `Update(item, u => u.Ignore(...))`; `Sql.NewGuid7()`.
- 6.5 `[Duration(DurationUnit.Second)]` on a `TimeSpan` column stored as a number makes `TimeSpan`
  comparisons and arithmetic translate.
- 6.5 `db.UpdateOptimisticWithRefresh(entity)` (`LinqToDB.Concurrency`) also refreshes the entity's version
  column, so the instance can be updated again.
- 6.6 `UseCombinedCommands(true)` sends an eager load in one round trip.
- 6.6 a nullable comparison used as a value (`IsOne = p.Value1 == 1`) is `false`, not `NULL`, when an operand is
  `NULL`; an eager load outside a transaction reads all parent rows before returning the first one.
