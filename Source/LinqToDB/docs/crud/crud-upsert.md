# LinqToDB — Upsert (Insert-or-Update)

> **Required:** Read [`AGENT_GUIDE.md`](../../AGENT_GUIDE.md) before any implementation. This file contains global rules, required namespaces, architecture constraints, and documentation navigation.

> **You are here if** you need to:
> - insert a row if the primary key does not exist, update it if it does
> - provide separate INSERT and UPDATE expressions for the two phases
> - control which columns participate in each phase at runtime
> - insert a row only when it does not exist, without touching the existing row
>
> For plain insert → [`crud-insert-values.md`](crud-insert-values.md)
> For `INSERT … SELECT` → [`crud-insert-select.md`](crud-insert-select.md)

---

> **Async:** All methods have `Async` counterparts accepting an optional `CancellationToken`.
> Examples use synchronous forms for brevity; add `Async` suffix and `await` in async contexts.
> Async methods require `using LinqToDB.Async;`.

> **Provider support:** upsert behavior varies across databases.
> Check the `Upsert` column in [`provider-capabilities.md`](../provider-capabilities.md) before using.
> LinqToDB emits a provider-native statement where possible and falls back to a multi-statement
> emulation for providers that do not support a native upsert syntax.

---

## Pattern quick-reference

| Scenario | Pattern |
|---|---|
| Upsert a loaded entity by PK | `db.InsertOrReplace(entity)` — section 1 |
| Upsert entity, control columns at runtime | `InsertOrReplace` + column filter — section 1 |
| Expression upsert, key from mapping | `InsertOrUpdate(insertSetter, updateSetter)` — section 2 |
| Expression upsert, explicit key | `InsertOrUpdate(insertSetter, updateSetter, keySelector)` — section 2 |
| Mapping class is an interface | `InsertOrUpdate(insertSetter, updateSetter)` — section 2 |
| Insert only when row absent (no update) | `InsertOrUpdate(insertSetter, null)` — section 3 |

> **Column generation:** Only explicitly assigned properties participate in generated SQL.
> Unassigned properties are omitted from INSERT and are not modified during UPDATE (sections 2, 3).
>
> For interface-mapped entities where `new IProduct { ... }` is not valid C#, use `InsertOrUpdate`
> (section 2) or the fluent builder in [`crud-insert-values.md`](crud-insert-values.md).

---

## 1. Entity upsert — `db.InsertOrReplace`

Inserts the row when no row with the same primary key exists; otherwise performs the
provider-specific replacement/update behavior for that row.
The key columns are taken from the entity's `[PrimaryKey]` mapping.

```csharp
using var db = new DataConnection(_options);

int affected = await db.InsertOrReplaceAsync(new Product
{
    ProductID = 42,      // caller-supplied PK — NOT an identity column
    Name      = "Widget",
    Price     = 9.99m,
});
```

### Column filter

Pass an `InsertOrUpdateColumnFilter<T>` delegate to control which columns participate in the
INSERT and UPDATE phases independently.
The delegate receives the entity, a `ColumnDescriptor`, and a `bool` indicating whether this
is the insert phase (`true`) or the update phase (`false`).

```csharp
await db.InsertOrReplaceAsync(product, (entity, col, isInsert) =>
    isInsert
        ? col.MemberName != nameof(Product.UpdatedAt)   // exclude on INSERT
        : col.MemberName != nameof(Product.CreatedAt)); // exclude on UPDATE
```

> **Constraint:** `InsertOrReplace` requires a caller-supplied primary key.
> It throws `LinqToDBException` at query build time when the entity has an `[Identity]` PK,
> or when no primary key columns are present.
> See anti-pattern #9 in [`agent-antipatterns.md`](../agent-antipatterns.md).

---

## 2. Expression upsert — `InsertOrUpdate`

`InsertOrUpdate` is a LINQ extension on `ITable<T>` that accepts separate lambda expressions
for the INSERT and UPDATE phases.
Use when values differ between the two phases, involve SQL expressions, or the mapping class
is an interface (where `new IProduct { ... }` is not valid C#).

### Key from mapping (2-argument form)

The key is derived automatically from the `[PrimaryKey]` columns in the mapping:

```csharp
await db.GetTable<Product>()
    .InsertOrUpdateAsync(
        () => new Product { ProductID = 42, Name = "Widget", Price = 9.99m },  // INSERT
        p  => new Product { Name = "Widget", Price = 9.99m });                  // UPDATE
```

### Explicit key selector (3-argument form)

Pass an explicit key predicate when the key cannot be inferred from the mapping, or when you
want to match on columns other than the primary key:

```csharp
await db.GetTable<Product>()
    .InsertOrUpdateAsync(
        () => new Product { ProductID = 42, Name = "Widget", Price = 9.99m },
        p  => new Product { Price = 9.99m },
        p  => p.ProductID == 42);  // explicit key
```

---

## 3. Insert-if-not-exists — `InsertOrUpdate` with `null` update setter

Pass `null` as the update setter to perform an *insert if not exists* without updating
the existing row when a match is found.

```csharp
await db.GetTable<Product>()
    .InsertOrUpdateAsync(
        () => new Product { ProductID = 42, Name = "Widget", Price = 9.99m },
        null);  // no UPDATE — leave existing row untouched
```

With an explicit key:

```csharp
await db.GetTable<Product>()
    .InsertOrUpdateAsync(
        () => new Product { ProductID = 42, Name = "Widget", Price = 9.99m },
        null,
        p => p.ProductID == 42);
```

> **Provider note:** some providers emit `INSERT … WHERE NOT EXISTS …` while others
> use a native `MERGE` or `ON CONFLICT DO NOTHING` form.
> The generated SQL is provider-specific but the semantics are uniform.
>
> **Note:** This is not equivalent to inserting and ignoring duplicate-key exceptions.
> The existence check and insert are generated as a single provider-specific statement where possible.

---

## See also

- [`crud-insert-values.md`](crud-insert-values.md) — plain insert from object or expressions
- [`crud-insert-select.md`](crud-insert-select.md) — `INSERT … SELECT`
- [`crud-update.md`](crud-update.md) — updating rows
- [`crud-merge.md`](crud-merge.md) — full MERGE builder (multi-operation sync, OUTPUT)
- [`provider-capabilities.md`](../provider-capabilities.md) — upsert support per provider
- [`agent-antipatterns.md`](../agent-antipatterns.md) — anti-pattern #9 (`InsertOrReplace` + Identity)
