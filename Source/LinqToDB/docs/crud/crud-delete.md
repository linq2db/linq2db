# LinqToDB — Deleting Data

> **Required:** Read [`AGENT_GUIDE.md`](../../AGENT_GUIDE.md) before any implementation.

> **You are here if** you need to:
> - delete a single row by entity
> - delete rows matching a query or predicate
> - receive deleted records back (`OUTPUT / RETURNING`)
> - redirect OUTPUT into another table

---

> **Async:** All methods have `Async` counterparts accepting an optional `CancellationToken`.
> Examples use synchronous forms for brevity; add `Async` suffix and `await` in async contexts.
> Async methods require `using LinqToDB.Async;`.

> **Table targeting:** `db.Delete` accepts optional parameters to override the target table
> derived from the `[Table]` mapping attribute:
> `tableName?`, `databaseName?`, `schemaName?`, `serverName?`, `tableOptions?`
> Omit them when the table name comes from the `[Table]` attribute.

---

## 1. Delete by entity — `db.Delete`

Deletes the row identified by the entity's primary key.
The `WHERE` clause is built from `[PrimaryKey]` column(s).

```csharp
using var db = new DataConnection(_options);

int affected = await db.DeleteAsync(product);
```

---

## 2. Set-based delete — query as filter

Deletes all rows matching the query without loading entities into memory.
Generates a single `DELETE FROM … WHERE …` statement.

```csharp
int affected = await db.GetTable<Product>()
    .Where(p => p.IsDiscontinued)
    .DeleteAsync();
```

### Inline predicate overload

Pass a predicate directly instead of chaining `.Where`:

```csharp
int affected = await db.GetTable<Product>()
    .DeleteAsync(p => p.IsDiscontinued);
```

---

## 3. OUTPUT / RETURNING — receive deleted records

Deletes rows and streams back the deleted records.
Execution is deferred until enumeration.

**Provider support:** SQL Server 2005+, PostgreSQL, SQLite 3.35+, Firebird 2.5+, MariaDB 10.0+.
Check the `OUTPUT / RETURNING` column in [`provider-capabilities.md`](../provider-capabilities.md) before using.

### Return the full deleted record

```csharp
IAsyncEnumerable<Product> deleted = db.GetTable<Product>()
    .Where(p => p.IsDiscontinued)
    .DeleteWithOutputAsync();

await foreach (var row in deleted)
    Console.WriteLine($"Deleted: {row.ProductID}");
```

### Return a projection only

```csharp
IAsyncEnumerable<int> ids = db.GetTable<Product>()
    .Where(p => p.IsDiscontinued)
    .DeleteWithOutputAsync(p => p.ProductID);
```

Synchronous forms (`DeleteWithOutput`, `DeleteWithOutput(outputExpression)`) return `IEnumerable<T>`
and are available when async enumeration is not needed.

---

## 4. Redirect OUTPUT into a table — `DeleteWithOutputInto` (SQL Server 2005+ only)

Deletes rows and writes the deleted records into a separate table in the same statement.
Returns the number of affected rows.

```csharp
int affected = await db.GetTable<Product>()
    .Where(p => p.IsDiscontinued)
    .DeleteWithOutputIntoAsync(db.GetTable<ProductAuditLog>());
```

### With projection

Map deleted columns to the output table with an expression:

```csharp
int affected = await db.GetTable<Product>()
    .Where(p => p.IsDiscontinued)
    .DeleteWithOutputIntoAsync(
        db.GetTable<DeletedProductLog>(),
        p => new DeletedProductLog { ProductID = p.ProductID, DeletedAt = Sql.CurrentTimestamp });
```

---

## See also

- [`crud-update.md`](crud-update.md) — updating rows
- [`crud-insert.md`](crud-insert.md) — inserting rows
- [`provider-capabilities.md`](../provider-capabilities.md) — `OUTPUT / RETURNING` support per provider
