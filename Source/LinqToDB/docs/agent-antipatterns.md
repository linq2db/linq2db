# LinqToDB Anti-Patterns

> You are here if you need to:
> - verify that the code you are generating follows correct LinqToDB patterns
> - look up a specific mistake by symptom or exception
> - understand why a pattern is wrong and what the correct alternative is
>
> Review this document before generating non-trivial LinqToDB code, or when diagnosing unexpected behavior.

Common incorrect usage patterns, their consequences, and correct alternatives.
Intended for developers and AI agents generating code against LinqToDB.

---

## Quick symptom index

| Symptom / trigger | Anti-pattern |
|---|---|
| `MappingSchema` created per connection or per request | #1 — MappingSchema per connection |
| Performance degrades under load despite correct queries | #1 — MappingSchema per connection |
| Second `DataConnection` does not see another connection's transaction | #2 — Transaction isolation |
| `Rollback()` does not undo all expected changes | #2 — Transaction isolation |
| Unexpected exceptions or corrupted state under concurrent access | #3 — Thread safety |
| Exception thrown on `Where(p => MyMethod(p.Column))` | #4 — Non-translatable methods |
| Full table loaded even though a `Where` was written | #5 — Post-materialization filtering |
| Navigation properties or lazy loading not working | #6 — EF Core assumptions |
| `SaveChanges()` not found or not needed | #6 — EF Core assumptions |
| Data committed outside `TransactionScope` | #7 — TransactionScope ordering |
| Code written before reading XML-doc for lifetime-sensitive types | #8 — Skipping XML-doc |
| `InsertOrReplace` / `InsertOrReplaceAsync` throws `LinqToDBException` | #9 — InsertOrReplace + Identity PK |
| Column schema differs across providers or is unexpectedly wide | #10 — Unconstrained column types |

---

## 1. Creating `MappingSchema` per connection or per request

**Anti-pattern:**
```cs
// Wrong: new MappingSchema per connection destroys caching
using var db = new DataConnection(
    new DataOptions()
        .UseSqlServer(connectionString)
        .UseMappingSchema(new MappingSchema())); // recreated every time
```

**Consequence:**
`MappingSchema` maintains internal expression caches (type conversions, column mappings, query metadata).
Creating a new instance per connection or per request bypasses these caches entirely,
causing cumulative and significant performance degradation under load.

**Correct pattern:**
Configure once at application startup and reuse:
```cs
// Correct: configure once
static readonly MappingSchema _schema = BuildMappingSchema();

static readonly DataOptions _options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseMappingSchema(_schema);

// Reuse per operation
using var db = new DataConnection(_options);
```

---

## 2. Expecting a new `DataConnection` to participate in another connection's transaction

**Anti-pattern:**
```cs
using var db = new DataConnection(options);
using var tx = db.BeginTransaction();

// Wrong: db2 is a separate physical connection — not part of tx
using var db2 = new DataConnection(options);
db2.Insert(new Order { ProductID = 1, Quantity = 10 }); // committed immediately

tx.Rollback(); // only operations on db are rolled back; db2's insert is permanent
```

**Consequence:**
Each `DataConnection` instance holds its own physical database connection.
`BeginTransaction()` starts a transaction on that specific connection only.
A separately created `DataConnection` instance opens a different connection
with no knowledge of the first connection's transaction.
Data modifications on `db2` are committed immediately and are not affected by `tx.Rollback()`.

**Correct pattern:**
Pass the existing connection (or transaction) explicitly, or consolidate the work into one `DataConnection`:
```cs
using var db = new DataConnection(options);
using var tx = db.BeginTransaction();

// Correct: all work goes through the same connection
db.Insert(new Order { ProductID = 1, Quantity = 10 });
db.Update(product);

tx.Commit(); // or tx.Rollback() — both operations are covered
```

---

## 3. Sharing `DataConnection` or `DataContext` across threads

**Anti-pattern:**
```cs
// Wrong: shared instance used concurrently from multiple threads
static readonly DataConnection _sharedDb = new DataConnection(options);

_ = Task.Run(() => _sharedDb.GetTable<Product>().ToList());
_ = Task.Run(() => _sharedDb.Insert(new Product()));
```

**Consequence:**
`DataConnection` and `DataContext` are not thread-safe.
Concurrent use from multiple threads causes undefined behavior:
corrupted query state, unexpected exceptions, or silent data errors.

**Correct pattern:**
Create a new instance per operation, per request, or per scope.
`DataOptions` is thread-safe and is designed to be shared:
```cs
// Correct: DataOptions shared, DataConnection per-scope
static readonly DataOptions _options = new DataOptions().UseSqlServer(connectionString);

// Each scope gets its own instance
using var db = new DataConnection(_options);
var result = await db.GetTable<Product>().ToListAsync();
```

---

## 4. Using non-translatable methods that reference query data

**Anti-pattern:**
```cs
// Wrong: CustomFilter is not mapped to SQL and references a column value
var results = db.GetTable<Product>()
    .Where(p => CustomFilter(p.Name))  // throws at execution time
    .ToList();
```

**Consequence:**
LinqToDB does not fall back to client-side evaluation for expressions
that reference query data (column values) and cannot be translated to SQL.
An exception is thrown at query execution time.
Note: independent subexpressions that do not reference query data may be evaluated client-side
and passed to SQL as parameters — this is not an error, it is intentional behavior.

**Correct patterns:**

Option A — materialize first, then apply in-memory logic:
```cs
var results = db.GetTable<Product>()
    .ToList()
    .Where(p => CustomFilter(p.Name));
```

Option B — map the method to a SQL function:
```cs
[Sql.Function("MY_SQL_FUNC", ServerSideOnly = true)]
static bool CustomFilter(string name) => throw new InvalidOperationException();
```

Option C — rewrite as a SQL-translatable LINQ expression.

---

## 5. Filtering or projecting after materialization and expecting SQL

**Anti-pattern:**
```cs
// Wrong: all rows fetched, then filtered in memory
var all = db.GetTable<Product>().ToList();     // full table scan
var active = all.Where(p => p.IsActive);       // in-memory, no SQL WHERE
```

**Consequence:**
Once `.ToList()`, `.ToArray()`, or `.AsEnumerable()` is called, all subsequent LINQ operators
are evaluated in-memory by LINQ-to-Objects, not translated to SQL.
The entire result set is fetched from the database before any filtering occurs.

**Correct pattern:**
Apply all filters and projections before materialization:
```cs
var active = await db.GetTable<Product>()
    .Where(p => p.IsActive)
    .Select(p => new { p.ProductID, p.Name })
    .ToListAsync();
```

---

## 6. Assuming EF Core / full ORM behavior

LinqToDB intentionally differs from Entity Framework Core:

| Behavior | EF Core | LinqToDB |
|---|---|---|
| Change tracking | Automatic | Not provided |
| Saving changes | `SaveChanges()` | Explicit `Insert` / `Update` / `Delete` |
| Lazy loading | Supported | Not supported — use `LoadWith` |
| Client-side evaluation | Partial fallback (EF 6), throws (EF Core 3+) | Throws for non-translatable expressions that depend on query data; independent subexpressions may be evaluated client-side and passed as parameters |
| Identity map | Yes | Not provided |
| Navigation loading | Implicit + lazy | Explicit via `LoadWith` only |
| Deferred persistence | Unit of work | Not provided |

LinqToDB is a translation engine, not an object state management framework.

---

## 7. Opening a `DataConnection` before creating a `TransactionScope`

**Anti-pattern:**
```cs
// Wrong: connection opens (and enlists) before the scope exists
using var db = new DataConnection(options);
var _ = db.GetTable<Product>().ToList(); // connection physically opens here

using var scope = new TransactionScope();
db.Insert(new Order { ProductID = 1, Quantity = 10 }); // NOT inside the scope
scope.Complete(); // order is already committed outside the scope
```

**Consequence:**
`System.Transactions.TransactionScope` works by enlisting the underlying `DbConnection`
in the ambient transaction at the moment the connection is physically opened.
`DataConnection` opens its connection lazily on first command execution.
If the connection was already opened before the scope was created,
it will not re-enlist — the subsequent operations run outside the transaction
and cannot be rolled back by abandoning the scope.

**Correct pattern:**
Create the `TransactionScope` before executing any query or command:
```cs
// Correct: scope is active when the connection first opens
using var scope = new TransactionScope(
    TransactionScopeOption.Required,
    TransactionScopeAsyncFlowOption.Enabled);

using var db = new DataConnection(options);
db.Insert(new Order { ProductID = 1, Quantity = 10 }); // connection opens here → enlists
scope.Complete();
```

For explicit, scope-independent transaction control, use `BeginTransaction` instead:
```cs
using var db = new DataConnection(options);
using var tx = db.BeginTransaction();
db.Insert(new Order { ProductID = 1, Quantity = 10 });
tx.Commit();
```

---

## 8. Generating code without inspecting symbol XML-doc

**Anti-pattern:**
Reading markdown docs and then generating code without inspecting the XML documentation
of the specific LinqToDB types being used.

**Consequence:**
XML documentation for key LinqToDB types (`DataOptions`, `DataConnection`, `MappingSchema`,
provider `UseXxx` methods) contains explicit lifetime rules, usage constraints, and
performance-critical requirements that markdown docs summarise but do not fully enumerate.
Skipping XML-doc inspection produces code that compiles and runs but violates these rules —
for example, recreating `DataOptions` per operation instead of sharing a single instance.

**Correct pattern:**
Markdown documentation is sufficient for most code generation scenarios.
For lifetime-sensitive types, inspect XML-doc when available.
Start with `LinqToDBArchitecture` (namespace `LinqToDB`) for cross-references,
then inspect `DataOptions`, `DataConnection`, `DataContext`, `MappingSchema`,
and provider `UseXxx` methods — these contain lifetime and caching constraints
not fully enumerated in markdown.

---

## 9. Using `InsertOrReplace` / `InsertOrReplaceAsync` with an identity PK column

**Anti-pattern:**
```cs
[Table]
class Person
{
    [PrimaryKey, Identity] public int    ID   { get; set; }
    [Column]               public string Name { get; set; } = "";
}

await db.InsertOrReplaceAsync(person); // throws at query build time
```

**Consequence:**
`InsertOrReplace` / `InsertOrReplaceAsync` require a caller-supplied primary key value so they
can decide whether to insert or update. Identity columns are database-generated and have no
caller-supplied value. The method detects this in `QueryRunner.InsertOrReplace<T>.CreateQuery`
and throws `LinqToDBException` before any SQL is executed. This is provider-agnostic — it
affects SQL Server, PostgreSQL, SQLite, MySQL, Oracle, and all other providers equally.

**Correct pattern:**
Option A — remove `[Identity]` and generate the key application-side:
```cs
[PrimaryKey] public int ID { get; set; } // no [Identity]

person.ID = await db.GetTable<Person>().MaxAsync(p => (int?)p.ID) ?? 0 + 1;
await db.InsertOrReplaceAsync(person);
```

Option B — use `InsertWithInt32IdentityAsync` for new rows and `UpdateAsync` for updates:
```cs
if (isNew)
    person.ID = (int)await db.InsertWithInt32IdentityAsync(person);
else
    await db.UpdateAsync(person);
```

---

## 10. Leaving provider-sensitive column types unconstrained in mapped entities

**Anti-pattern:**
```cs
[Table]
class Person
{
    [PrimaryKey]      public int     ID        { get; set; }
    [Column, NotNull] public string  FirstName { get; set; } = ""; // no Length
    [Column, NotNull] public string  Email     { get; set; } = ""; // no Length
    [Column]          public decimal Amount    { get; set; }       // no Precision/Scale
}
```

**Consequence:**
When schema is generated from a mapped entity — whether via `CreateTable`,
`TableOptions.CreateIfNotExists`, `TableOptions.CheckExistence`, temporary tables,
or migrations — **provider defaults must not be relied upon** because they differ
across databases:
- `string` without `Length` may become `nvarchar(MAX)`, `text`, `clob`, or a similar
  large-text type — far wider than intended.
- `decimal` without `Precision` / `Scale` may produce a different numeric type or
  default precision than the schema requires.

The resulting schema is formally workable but non-portable: column definitions can
differ across providers, produce unexpectedly wide storage, and behave differently
when switching databases.

**Correct pattern:**
For every provider-sensitive type, specify schema-relevant attributes explicitly.

When exact limits are stated in the task — use them directly:
```cs
[Column(Length = 254), NotNull]      public string  Email  { get; set; } = "";
[Column(Precision = 18, Scale = 2)]  public decimal Amount { get; set; }
```

When the task does not state exact limits, choose a bounded technical value guided
by the domain meaning of the field, and add a TODO comment to flag it for review:
```cs
[Column(Length = 200), NotNull]      public string  Name  { get; set; } = ""; // TODO: confirm max length with domain owner
[Column(Precision = 18, Scale = 2)]  public decimal Price { get; set; }
```

> **Rule:** add a TODO comment when the bound is a heuristic placeholder chosen by the agent
> for an application-specific field — this signals to the developer that the value needs review.
>
> A TODO is not required when the value comes directly from the task or follows a widely
> established technical convention (for example `Length = 254` for an email address, or
> `Precision = 18, Scale = 2` for a monetary amount).
>
> Do NOT write `[Column] // TODO: add length later` — the column must carry an explicit value
> even if the bound is provisional.

---

## See also

- `LinqToDB.LinqToDBArchitecture` — architecture overview (XML documentation class, namespace `LinqToDB`).
- [`docs/architecture.md`](architecture.md) — extended architectural model.
- [`docs/ai-tags.md`](ai-tags.md) — machine-readable metadata specification.
- [`docs/provider-capabilities.md`](provider-capabilities.md) — SQL feature support matrix per provider.
