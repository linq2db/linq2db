# LinqToDB Anti-Patterns

Common incorrect usage patterns, their consequences, and correct alternatives.
Intended for developers and AI agents generating code against LinqToDB.

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

## See also

- `LinqToDB.LinqToDBArchitecture` — architecture overview (XML documentation class, namespace `LinqToDB`).
- [`docs/architecture.md`](architecture.md) — extended architectural model.
- [`docs/ai-tags.md`](ai-tags.md) — machine-readable metadata specification.
