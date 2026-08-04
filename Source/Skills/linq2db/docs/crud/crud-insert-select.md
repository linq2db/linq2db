# LinqToDB - INSERT … SELECT

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](../../SKILL.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> **You are here if** you need to:
> - copy rows from one table to another without materializing them in the application
> - insert results of a JOIN, filter, or projection into a target table
> - add extra constant or computed columns alongside the source projection
> - receive the inserted rows back via `OUTPUT / RETURNING`
> - insert from one source into multiple Oracle tables in a single statement
>
> For inserting from a C# object or expression values → [`crud-insert-values.md`](crud-insert-values.md)
> For upsert → [`crud-upsert.md`](crud-upsert.md)

---

> **Async:** All methods have `Async` counterparts accepting an optional `CancellationToken`.
> Examples use synchronous forms for brevity; add `Async` suffix and `await` in async contexts.
> Async methods require `using LinqToDB.Async;`.

---

## 1. Simple source - same or compatible types

Generates a single `INSERT INTO target SELECT … FROM source` statement.
No data is materialized in the application.

```csharp
// Copy inactive products to an archive table
db.GetTable<Product>()
    .Where(p => !p.IsActive)
    .Insert(
        db.GetTable<ProductArchive>(),
        p => new ProductArchive { ID = p.ProductID, Name = p.Name, Price = p.Price });
```

> Only explicitly assigned properties in the setter lambda are included in the INSERT - unassigned columns are omitted entirely.

---

## 2. Complex source - JOINs and projections

When the source is a JOIN or an anonymous projection the target table cannot be inferred -
pass it explicitly as the second argument.

```csharp
var source = from p in db.GetTable<Product>()
             join c in db.GetTable<Category>() on p.CategoryID equals c.ID
             select new { p, c };

source.Insert(
    db.GetTable<ProductArchive>(),
    x => new ProductArchive { ID = x.p.ProductID, CategoryName = x.c.Name });
```

---

## 3. Fluent SELECT-INSERT - `source.Into(target).Value(…).Insert()`

**Required** when the target mapping class is an interface - `new IProductArchive { ... }` is not
valid C#, so the expression setter (sections 1, 2) is not available.
Also use when you need to inject extra constant or SQL-expression columns that are not part of
the source projection, or simply as a style preference.

```csharp
db.GetTable<Product>()
    .Where(p => !p.IsActive)
    .Into(db.GetTable<ProductArchive>())
        .Value(a => a.ID,         p => p.ProductID)
        .Value(a => a.Name,       p => p.Name)
        .Value(a => a.ArchivedAt, () => Sql.CurrentTimestamp)  // extra column
    .Insert();
```

Terminal operations on `ISelectInsertable<TSource, TTarget>`:
- `.Insert()` - returns affected row count
- `.InsertWithOutput()` - returns the inserted record (see §4)

---

## 4. OUTPUT / RETURNING - receive all inserted records

Inserts rows from the source query and streams back the inserted records.
Returns `IAsyncEnumerable<TTarget>` - enumeration triggers the INSERT.

**Provider support:** SQL Server 2005+, PostgreSQL, SQLite 3.35+, Firebird 2.5+, MariaDB 10.5+,
DuckDB, YDB.
Check the `OUTPUT / RETURNING` column in [`provider-capabilities.md`](../provider-capabilities.md) before using.

```csharp
IAsyncEnumerable<ProductArchive> inserted =
    db.GetTable<Product>()
        .Where(p => !p.IsActive)
        .InsertWithOutputAsync(
            db.GetTable<ProductArchive>(),
            p => new ProductArchive { ID = p.ProductID, Name = p.Name });

await foreach (var row in inserted)
    Console.WriteLine(row.ID);
```

With an output projection:

```csharp
IAsyncEnumerable<int> ids =
    db.GetTable<Product>()
        .Where(p => !p.IsActive)
        .InsertWithOutputAsync(
            db.GetTable<ProductArchive>(),
            p  => new ProductArchive { ID = p.ProductID, Name = p.Name },
            r  => r.ID);
```

### Redirect OUTPUT into a separate table - `InsertWithOutputInto` (SQL Server 2005+ only)

```csharp
db.GetTable<Product>()
    .Where(p => !p.IsActive)
    .InsertWithOutputInto(
        db.GetTable<ProductArchive>(),
        p  => new ProductArchive { ID = p.ProductID, Name = p.Name },
        db.GetTable<AuditLog>());
```

---

## 5. Multi-table insert - `MultiInsert` (Oracle only)

Inserts rows from one source query into multiple target tables in a single Oracle-specific statement.
`.Into(…)` (unconditional) and `.When(condition, …)` (conditional) chains are **mutually
exclusive** - each starts a different fluent state and cannot be mixed. Each chain also has its
own terminal method.

### Unconditional - `.Into(…)` chain, terminal `.Insert()`

Every row from the source is inserted into every listed target table.

```csharp
db.GetTable<SourceEvent>()
    .MultiInsert()
    .Into(db.GetTable<EventLog>(),  e => new EventLog { ID = e.ID, Type = e.Type })
    .Into(db.GetTable<AuditLog>(),  e => new AuditLog  { ID = e.ID, LoggedAt = Sql.CurrentTimestamp })
    .Insert();
```

### Conditional - `.When(…)` chain, terminal `.InsertAll()` or `.InsertFirst()`

Each row is tested against each `.When(condition, target, setter)` branch in order.

`.InsertAll()` inserts into **every** branch whose condition matches (a row can be inserted into
more than one target table):

```csharp
db.GetTable<SourceEvent>()
    .MultiInsert()
    .When(e => e.Type == "order",
        db.GetTable<OrderEvent>(),    e => new OrderEvent  { OrderID = e.RefID })
    .When(e => e.Type == "payment",
        db.GetTable<PaymentEvent>(),  e => new PaymentEvent { Amount = e.Amount })
    .InsertAll();
```

`.InsertFirst()` inserts into only the **first** matching branch, optionally falling back to
`.Else(target, setter)` when no branch matches:

```csharp
db.GetTable<SourceEvent>()
    .MultiInsert()
    .When(e => e.Type == "order",
        db.GetTable<OrderEvent>(),    e => new OrderEvent  { OrderID = e.RefID })
    .When(e => e.Type == "payment",
        db.GetTable<PaymentEvent>(),  e => new PaymentEvent { Amount = e.Amount })
    .Else(db.GetTable<UnknownEvent>(), e => new UnknownEvent { ID = e.ID })
    .InsertFirst();
```

All three terminals (`Insert`, `InsertAll`, `InsertFirst`) have `Async` counterparts.

---

## See also

- [`crud-insert-values.md`](crud-insert-values.md) - insert from C# object or expression values
- [`crud-upsert.md`](crud-upsert.md) - upsert (`InsertOrReplace`, `InsertOrUpdate`)
- [`provider-capabilities.md`](../provider-capabilities.md) - `OUTPUT / RETURNING` support per provider
