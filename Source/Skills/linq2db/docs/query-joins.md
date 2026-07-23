# LinqToDB - Joins

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](../SKILL.md).
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
`INNER JOIN`, `CanBeNull = true` produces a `LEFT JOIN`. See [`associations.md`](associations.md)
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
`APPLY / LATERAL` column in [`provider-capabilities.md`](provider-capabilities.md) - a query shape
that requires it will fail on a provider without support, with no way to opt out of the shape
short of restructuring the query.

---

## See also

- [`docs/associations.md`](associations.md) - declaring associations, `LoadWith`/`ThenLoad` eager
  loading, eager-loading strategies
- [`docs/provider-capabilities.md`](provider-capabilities.md) - `APPLY / LATERAL` provider support
- [`docs/crud/crud-select.md`](crud/crud-select.md) - everyday querying
- [`docs/agent-antipatterns.md`](agent-antipatterns.md) - common mistakes and how to avoid them
