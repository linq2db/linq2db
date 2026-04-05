# LinqToDB — Bulk Copy

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`AGENT_GUIDE.md`](../../AGENT_GUIDE.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> **You are here if** you need to:
> - insert a large number of rows as efficiently as possible
> - use the provider's native bulk-insert API (`SqlBulkCopy`, `COPY`, etc.)
> - control batch size, timeouts, identity handling, or conflict resolution during bulk insert
> - receive row-copy progress notifications during a bulk operation

---

> **Async:** All `BulkCopy` methods have `BulkCopyAsync` counterparts accepting an optional `CancellationToken`.
> Both `IEnumerable<T>` and `IAsyncEnumerable<T>` sources are supported by the async overloads.
> Examples use synchronous forms for brevity; add `Async` suffix and `await` in async contexts.
> Async methods require `using LinqToDB.Async;`.

---

## Pattern quick-reference

| Scenario | Pattern |
|---|---|
| Insert a collection with default options | `db.BulkCopy(source)` — section 1 |
| Control batch size without building full options | `db.BulkCopy(maxBatchSize, source)` — section 1 |
| Full option control (type, timeout, flags) | `db.BulkCopy(new BulkCopyOptions(...), source)` — section 2 |
| Target a specific `ITable<T>` instance | `table.BulkCopy(source)` — section 1 |
| Choose insert strategy (native / multi-row / row-by-row) | `BulkCopyOptions.BulkCopyType` — section 3 |
| Preserve identity values from source data | `BulkCopyOptions.KeepIdentity = true` — section 4 |
| Track progress, cancel mid-flight | `NotifyAfter` + `RowsCopiedCallback` + `Abort` — section 5 |
| Skip / ignore conflicting rows | `BulkCopyOptions.ConflictAction = ConflictAction.Ignore` — section 6 |
| Provider-specific flags (locks, triggers, constraints) | `BulkCopyOptions` provider flags — section 7 |

---

## 1. Entry points

All overloads return `BulkCopyRowsCopied` (sync) or `Task<BulkCopyRowsCopied>` (async).
`BulkCopyRowsCopied.RowsCopied` holds the total row count; `Abort` can be set in the progress callback
to stop the operation early (see section 5).

**Simplest form — default options:**
```csharp
using LinqToDB;

BulkCopyRowsCopied result = db.BulkCopy(products);
Console.WriteLine(result.RowsCopied);
```

**With explicit batch size:**
```csharp
BulkCopyRowsCopied result = db.BulkCopy(maxBatchSize: 1000, source: products);
```

**With full `BulkCopyOptions`:**
```csharp
var options = new BulkCopyOptions(
    BulkCopyType: BulkCopyType.ProviderSpecific,
    MaxBatchSize: 5000);

BulkCopyRowsCopied result = db.BulkCopy(options, products);
```

**Table-targeted overloads** — use when the target table reference is already at hand
(e.g. a temporary table or a table with overridden name):
```csharp
ITable<Product> table = db.GetTable<Product>();
BulkCopyRowsCopied result = table.BulkCopy(products);
// or:
BulkCopyRowsCopied result = table.BulkCopy(options, products);
```

**Async with `IAsyncEnumerable<T>`:**
```csharp
using LinqToDB.Async;

await foreach (var batch in streamingSource)
    /* ... */

BulkCopyRowsCopied result = await db.BulkCopyAsync(asyncEnumerableSource, cancellationToken);
```

---

## 2. `BulkCopyOptions` — overview

`BulkCopyOptions` is a `sealed record`; construct it with named parameters and pass to the overload
that accepts `BulkCopyOptions`. All parameters are optional and default to `default`.

```csharp
var options = new BulkCopyOptions(
    BulkCopyType:           BulkCopyType.ProviderSpecific,
    MaxBatchSize:           5000,
    BulkCopyTimeout:        60,
    KeepIdentity:           true,
    NotifyAfter:            1000,
    RowsCopiedCallback:     r => Console.WriteLine($"{r.RowsCopied} rows copied"));

db.BulkCopy(options, source);
```

For target table overrides when the table name differs from the `[Table]` attribute:

```csharp
var options = new BulkCopyOptions(
    TableName:    "archive_products",
    SchemaName:   "dbo",
    DatabaseName: "ArchiveDB");

db.BulkCopy(options, source);
```

Available targeting overrides: `TableName`, `SchemaName`, `DatabaseName`, `ServerName`, `TableOptions`.

---

## 3. `BulkCopyType` — insert strategy

`BulkCopyOptions.BulkCopyType` (or the `BulkCopyType` enum directly) controls which insert path is used.

| Value | Behaviour |
|---|---|
| `Default` | Provider selects the most efficient available strategy (recommended for most cases) |
| `ProviderSpecific` | Uses the provider's native bulk API (`SqlBulkCopy`, PostgreSQL `COPY`, etc.); degrades to `RowByRow` if not supported |
| `MultipleRows` | Emits multi-row `INSERT … VALUES (…), (…)` statements; degrades to `RowByRow` if not supported |
| `RowByRow` | One `INSERT` per row — slowest, but maximally compatible |

```csharp
// Force multi-row INSERT for providers that support it
var options = new BulkCopyOptions(BulkCopyType: BulkCopyType.MultipleRows, MaxBatchSize: 500);
db.BulkCopy(options, source);
```

> **Provider support:** check the `Bulk Copy` column in [`docs/provider-capabilities.md`](../provider-capabilities.md)
> before relying on `ProviderSpecific` or `MultipleRows` for a specific database.

---

## 4. `KeepIdentity` — preserve source identity values

When `KeepIdentity` is `true`, columns marked with `[Identity]` are **included** in the insert
using the values from the source objects. The `SkipOnInsert` flag is ignored for those columns.

When `false` (the default), identity columns are excluded and values are generated by the database.

> ⚠️ `KeepIdentity = true` is **not compatible** with `BulkCopyType.RowByRow`.

```csharp
[Table]
class Product
{
    [PrimaryKey, Identity] public int    Id   { get; set; }
    [Column]               public string Name { get; set; } = "";
}

// Re-import rows keeping their original IDs
var options = new BulkCopyOptions(
    KeepIdentity: true,
    BulkCopyType: BulkCopyType.ProviderSpecific);

db.BulkCopy(options, archivedProducts);
```

---

## 5. Progress callback and cancellation

Use `NotifyAfter` + `RowsCopiedCallback` to track progress.
Set `BulkCopyRowsCopied.Abort = true` inside the callback to stop the operation early.

```csharp
var options = new BulkCopyOptions(
    NotifyAfter:        500,
    RowsCopiedCallback: r =>
    {
        Console.WriteLine($"{r.RowsCopied} rows inserted");

        if (CancellationRequested())
            r.Abort = true;   // stops the bulk copy operation
    });

BulkCopyRowsCopied result = db.BulkCopy(options, source);
```

`NotifyAfter = 0` (default) disables the callback entirely.

---

## 6. `ConflictAction` — handling duplicate-key conflicts

`BulkCopyOptions.ConflictAction` controls what happens when an inserted row conflicts with an
existing row (e.g. a duplicate primary key).

| Value | Behaviour |
|---|---|
| `Default` | Database default — typically raises an error |
| `Ignore` | Silently skip conflicting rows |

Provider support for `Ignore` (requires `BulkCopyType.MultipleRows`):

| Provider | SQL emitted |
|---|---|
| MySQL / MariaDB | `INSERT IGNORE INTO …` |
| PostgreSQL | `INSERT INTO … ON CONFLICT DO NOTHING` |
| SQLite | `INSERT OR IGNORE INTO …` |

```csharp
var options = new BulkCopyOptions(
    BulkCopyType:   BulkCopyType.MultipleRows,
    ConflictAction: ConflictAction.Ignore);

db.BulkCopy(options, source);
```

---

## 7. Provider-specific flags

These flags are honoured only for `BulkCopyType.ProviderSpecific` mode on the listed providers.
Passing them with other modes or unsupported providers has no effect.

| Option | Effect | Supported providers |
|---|---|---|
| `CheckConstraints` | Enforce database constraints during bulk insert | Oracle, SQL Server, SAP/Sybase ASE |
| `TableLock` | Acquire table-level lock during bulk insert | DB2, Informix (via DB2), SQL Server, SAP/Sybase ASE |
| `KeepNulls` | Insert `NULL` instead of column default constraint values | SQL Server, SAP/Sybase ASE |
| `FireTriggers` | Fire insert triggers during bulk insert | Oracle, SQL Server, SAP/Sybase ASE |
| `UseInternalTransaction` | Wrap bulk insert in an automatic transaction | Oracle, SQL Server, SAP/Sybase ASE |
| `BulkCopyTimeout` | Operation timeout in seconds | DB2, Informix, MySqlConnector, Oracle, PostgreSQL, SAP HANA, SQL Server, SAP/Sybase ASE |
| `MaxDegreeOfParallelism` | Number of parallel connections for insert | ClickHouse (ClickHouse.Driver) only |
| `WithoutSession` | Use a session-less connection for insert | ClickHouse (ClickHouse.Driver) only |

```csharp
// SQL Server: table lock + fire triggers, 30-second timeout
var options = new BulkCopyOptions(
    BulkCopyType:   BulkCopyType.ProviderSpecific,
    TableLock:      true,
    FireTriggers:   true,
    BulkCopyTimeout: 30);

db.BulkCopy(options, source);
```

---

## 8. `UseParameters` and `MaxParametersForBatch` — `MultipleRows` tuning

Relevant only for `BulkCopyType.MultipleRows`.

- `UseParameters = true` — always use parameterised `INSERT … VALUES` statements.
  The provider's per-statement parameter limit is used to cap the rows per batch automatically.
- `MaxParametersForBatch` — override the maximum parameter count per batch when `UseParameters` is true.

```csharp
var options = new BulkCopyOptions(
    BulkCopyType:          BulkCopyType.MultipleRows,
    UseParameters:         true,
    MaxParametersForBatch: 2000);

db.BulkCopy(options, source);
```

---

## See also

- [`docs/provider-capabilities.md`](../provider-capabilities.md) — `Bulk Copy` column: provider support matrix.
- [`docs/crud/crud-insert-values.md`](crud-insert-values.md) — single-row insert from C# objects.
- [`docs/crud/crud-upsert.md`](crud-upsert.md) — insert-or-update semantics.
- `BulkCopyOptions` — XML documentation on the record type for full parameter details.
- `BulkCopyType` — XML documentation on the enum for provider degradation rules.
