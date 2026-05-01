# Query, Table, and Join Hints

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`AGENT_GUIDE.md`](../AGENT_GUIDE.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> You are here if you need to:
> - add optimizer, lock, table, index, join, subquery, or query hints
> - choose between raw text hints and provider-specific typed hint APIs
> - use provider-specific `AsXxx()` hint APIs
> - understand why several provider-specific hint branches can be added to one query
> - recognize that MERGE has a separate hint API

---

> **Agent guidance:**
> - Prefer provider-specific typed hint APIs when they exist. They encode provider syntax and are safer than raw SQL text.
> - Use general raw-text hint APIs only when the provider-specific API does not expose the required hint.
> - Hint syntax and meaning are provider-defined. Do not assume the same hint text is valid across providers.
> - Never build hint text from user input. Hint strings are SQL text, not query parameters.
> - Hints are deferred and composable; they become SQL AST extensions and are emitted only during SQL generation.
> - Provider-specific `AsXxx()` hint branches can be added to the same query. Only hints compatible with the active provider are emitted into SQL.
> - Do not use hints to hide a broken query shape. Prefer correct LINQ, indexes, mapping, and provider configuration first.

---

## Pattern quick-reference

| Scenario | Preferred pattern |
|---|---|
| Provider-specific known hint | `db.GetTable<T>().AsSqlServer().WithNoLock()` |
| Provider-specific query hint | `query.AsOracle().AllRowsHint()` or provider XML-doc equivalent |
| Provider-specific table hint in scope | `query.AsSqlServer().WithNoLockInScope()` |
| General raw table hint | `db.GetTable<T>().TableHint("...")` or `.With("...")` |
| General raw tables-in-scope hint | `query.TablesInScopeHint("...")` |
| General raw index hint | `db.GetTable<T>().IndexHint("...")` |
| General raw join hint | `query.JoinHint("...")` |
| General raw subquery hint | `query.SubQueryHint("...")` |
| General raw query hint | `query.QueryHint("...")` |
| MERGE hint | `target.Merge("...")`; see `docs/crud/crud-merge.md` |

---

## 1. Two hint APIs

LinqToDB has two hint API layers.

The general API is provider-neutral by method shape, but provider-specific by hint text.
It accepts raw SQL text:

```csharp
using LinqToDB;

var query =
    db.GetTable<Product>()
        .TableHint("NOLOCK")
        .Where(p => p.IsActive);
```

The provider-specific API starts with a provider marker such as `AsSqlServer()`, `AsOracle()`,
or `AsClickHouse()`. After that marker, provider-specific extension methods become available:

```csharp
using LinqToDB;
using LinqToDB.DataProvider.SqlServer;

var query =
    db.GetTable<Product>()
        .AsSqlServer()
        .WithNoLock()
        .Where(p => p.IsActive);
```

Use the provider-specific API when available. Use raw-text hints only for gaps, experiments, or
provider features that are not exposed by typed helpers in the installed package version.

Machine-readable XML docs classify hint APIs with `AI-Tags` and `HintType`
(`Table`, `TablesInScope`, `Index`, `Join`, `SubQuery`, `Query`, `Merge`, `TableName`).
Agents should use those tags when choosing the correct overload or scope.
Generated provider-specific helpers get those tags from their T4 templates; update the `.tt`
source first and then regenerate/check in the corresponding `.generated.cs` file.
Handwritten provider-specific helpers carry the same tags directly in their XML docs.

---

## 2. Provider-specific typed hints

Provider-specific hint APIs are exposed through provider namespaces and `AsXxx()` marker methods.
The marker wraps the query or table with a provider-specific interface so provider hint extensions
can be selected by C# overload resolution.
The generated provider-specific helper set is intended to cover most known hints for supported
providers in the installed package version, so inspect the provider namespace before falling back
to raw text.

```csharp
using LinqToDB;
using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.DataProvider.SqlServer;

var products =
    db.GetTable<Product>()
        .AsSqlServer()
            .WithNoLock()
        .AsClickHouse()
            .FinalHint();
```

The example can be shared by SQL Server and ClickHouse code paths. During SQL generation, LinqToDB
emits only the hint extensions that are compatible with the active provider. Incompatible
provider-specific hint branches are ignored by provider filtering.

This is intentional. It allows reusable query code to carry provider-specific refinements without
branching every query by provider.

Currently visible provider marker APIs in this package:

| Provider namespace | Marker methods | Notes |
|---|---|---|
| `LinqToDB.DataProvider.Access` | `AsAccess()` | Table and query wrappers. |
| `LinqToDB.DataProvider.ClickHouse` | `AsClickHouse()` | Table and query wrappers; includes `FinalHint()` and many join/query hints. |
| `LinqToDB.DataProvider.MySql` | `AsMySql()` | Table and query wrappers; includes MySQL optimizer hints. |
| `LinqToDB.DataProvider.Oracle` | `AsOracle()` | Table and query wrappers; many Oracle optimizer hints. |
| `LinqToDB.DataProvider.PostgreSQL` | `AsPostgreSQL()` | Query wrapper; row-locking hints such as `ForUpdate...Hint()`. |
| `LinqToDB.DataProvider.SqlCe` | `AsSqlCe()` | Table and query wrappers. |
| `LinqToDB.DataProvider.SQLite` | `AsSQLite()` | Table wrapper; SQLite table/index-style hints. |
| `LinqToDB.DataProvider.SqlServer` | `AsSqlServer()` | Table and query wrappers; SQL Server table hints. |
| `LinqToDB.DataProvider.Ydb` | `AsYdb()` | Table and query wrappers; YDB query hints. |

Providers not listed in this table do not currently expose a provider-specific `AsXxx()` hint
marker API in this package.

Do not assume that the general raw-text hint methods will be emitted for an unlisted provider.
The provider SQL builder must explicitly support the relevant query extension scope.

Known provider gaps:

| Provider | Database has hint-like feature? | Current linq2db hint API | Agent guidance |
|---|---|---|---|
| DB2 | Yes: optimization profiles/guidelines, including embedded XML guidelines in SQL comments. | No regular table/query/join hint API. | Do not invent `AsDB2().XxxHint()` or expect raw `QueryHint` to emit. Treat DB2 optimization guidelines as future provider-specific work. |
| Firebird | Yes: `PLAN` clause and related optimizer controls such as `OPTIMIZE FOR`. | No regular table/query/join hint API. | Do not use raw hint APIs for Firebird plan control; `PLAN` would need explicit provider support. |
| Informix | Yes: optimizer directives, plus linq2db has separate MERGE hint output. | No regular table/query/join hint API; MERGE hint only. | Use `.Merge("...")` only for MERGE hints. Do not assume query/table directives are supported by the general hint API. |
| SAP HANA | Yes: `WITH HINT (...)` for DML statements. | No regular table/query/join hint API. | Do not invent `AsSapHana().XxxHint()`; HANA hints would need explicit provider builder support. |
| Sybase | Needs provider-specific investigation. | No regular table/query/join hint API. | Do not invent provider-specific hint helpers. Verify dialect support before proposing docs or API. |

Inspect XML-doc or the provider namespace for the exact helper names. Many generated helpers use a
`Hint` suffix, for example `FinalHint()`, `WithNoLock()`, `ForUpdateSkipLockedHint()`, or
`AllRowsHint()`.

---

## 3. General raw-text hint API

The general API lives in `LinqToDB` extension methods and accepts provider-defined SQL text.

| Method | Applies to | Meaning |
|---|---|---|
| `.With("...")` | `ITable<T>` | Alias for table hint style. |
| `.TableHint("...")` | `ITable<T>` | Adds a hint to one table. |
| `.TablesInScopeHint("...")` | `IQueryable<T>` | Adds a table hint to tables in the method scope. |
| `.IndexHint("...")` | `ITable<T>` | Adds an index hint to one table. |
| `.JoinHint("...")` | `IQueryable<T>` | Adds a join hint to the generated join shape. |
| `.SubQueryHint("...")` | `IQueryable<T>` | Adds a hint to the subquery scope. |
| `.QueryHint("...")` | `IQueryable<T>` | Adds a top-level query hint. |

Some overloads accept a hint plus one or more hint parameters. The text and parameters still form
SQL hint syntax; they are not general query parameters and should not be derived from user input.

Raw-text examples:

```csharp
using LinqToDB;

var products =
    db.GetTable<Product>()
        .TableHint("NOLOCK")
        .IndexHint("INDEX(IX_Product_Category)");

var query =
    products
        .Where(p => p.IsActive)
        .QueryHint("RECOMPILE");
```

Raw hints are useful for provider-specific features that do not have typed helpers. They are also
easy to misuse: the same text can be valid for one provider, ignored by another, or emitted in a
different SQL position.

For SQL Server, raw `.TableHint("NOLOCK")` is emitted inside `WITH (...)`, and raw
`.QueryHint("RECOMPILE")` is emitted inside `OPTION (...)`. Do not include those wrapper clauses
unless the specific API XML-doc or provider guide says the method expects them.

---

## 4. Query, table, index, join, and subquery scope

Choose the narrowest hint scope that matches the SQL feature:

| Scope | Use when |
|---|---|
| Table hint | The hint belongs to a single table reference. |
| Tables-in-scope hint | The same table hint should apply to all table references inside a query scope. |
| Index hint | The provider has index-selection syntax attached to a table. |
| Join hint | The provider has join-shape syntax attached to joins. |
| Subquery hint | The provider places the hint on a subquery or SELECT scope. |
| Query hint | The provider places the hint on the top-level query or optimizer hint block. |

If a provider-specific helper exists, it usually chooses the right SQL extension scope internally.
Do not translate a typed provider helper into a raw hint unless you have checked the provider SQL
syntax.

### Tables-in-scope hints

`TablesInScopeHint(...)` applies a table hint to table references that are part of the query scope
where the method is applied:

```csharp
var query =
    (
        from p in db.Parent
        from c in db.Child.AsSqlServer().WithIndex("IX_ChildIndex")
        where c.ParentID == p.ParentID
        select p
    )
    .AsSqlServer()
    .WithNoLockInScope();
```

For SQL Server this produces table hints for both table references in that scope. A table-local
hint is preserved and combined with the scope hint, so the `Child` table can get both
`Index(IX_ChildIndex)` and `NoLock`.

Scope boundaries matter:

- A scope hint applies to tables already inside that query scope.
- Tables introduced later by composing another outer query are not automatically affected.
- A nested table/query expression with its own `TablesInScopeHint(...)` has its own scope.
- Table-local hints are more specific than a scope-level hint: they are not removed by the scope
  hint and can coexist with it.

Provider SQL output differs. SQL Server emits per-table `WITH (...)` clauses, while Oracle and
MySQL often collect table-scoped optimizer hints into provider-specific hint blocks. Use generated
SQL inspection when exact placement matters.

---

## 5. Combining provider-specific branches

Provider-specific wrappers are still query/table expressions. You can layer provider-specific hint
branches onto the same query when shared code targets multiple providers:

```csharp
using LinqToDB;
using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.DataProvider.SqlServer;

var query =
    db.GetTable<Product>()
        .Where(p => p.IsActive)
        .AsSqlServer()
            .WithNoLockInScope()
        .AsClickHouse()
            .FinalInScopeHint()
        .AsPostgreSQL()
            .ForUpdateSkipLockedHint();
```

For SQL Server, only SQL Server-compatible hint extensions are emitted. For ClickHouse, only the
ClickHouse-compatible hint extensions are emitted. For PostgreSQL, only PostgreSQL-compatible hint
extensions are emitted.

This provider filtering is a key reason to prefer the provider-specific API in reusable query code.

---

## 6. MERGE hints are separate

MERGE has its own hint entry point in the merge builder API:

```csharp
using LinqToDB;

db.GetTable<Product>()
    .Merge("WITH (HOLDLOCK)")
    .Using(db.GetTable<ProductStaging>())
    .OnTargetKey()
    .UpdateWhenMatched()
    .InsertWhenNotMatched()
    .Merge();
```

This is not the same API as `.TableHint(...)` or provider-specific `AsXxx()` query hints. Use it
only for hints that belong to the generated MERGE statement. See `docs/crud/crud-merge.md`.

---

## 7. Common mistakes

| Mistake | Correct action |
|---|---|
| Using raw text when a provider-specific helper exists | Prefer `AsXxx().SpecificHint()` helpers. |
| Assuming raw hint text is portable | Treat raw hint strings as provider-specific SQL. |
| Building hint text from user input | Do not do this; hint text is SQL text. |
| Applying a query hint where the provider expects a table hint | Use the provider-specific helper or the narrowest correct raw scope. |
| Expecting a provider-specific hint to affect every provider | Provider-specific hints are emitted only for compatible providers. |
| Inventing provider-specific helpers for unsupported providers | Check the provider table and XML docs; if no `AsXxx()` hint API or builder support exists, document the gap instead. |
| Using hints to compensate for wrong LINQ or mapping | Fix query shape, indexes, mapping, or provider setup first. |
| Using `.Merge("...")` as if it were a query/table hint | MERGE hints belong to the merge builder only. |

---

## Related documentation

- [`docs/crud/crud-merge.md`](crud/crud-merge.md) - MERGE builder and merge-specific hints.
- [`docs/provider-capabilities.md`](provider-capabilities.md) - provider feature support.
- [`docs/provider-setup.md`](provider-setup.md) - provider selection, dialects, and driver packages.
- [`docs/custom-sql.md`](custom-sql.md) - custom SQL expressions when hints are not the right tool.
- `LinqExtensions.Hints` - XML documentation for general raw hint APIs.
- Provider namespaces such as `LinqToDB.DataProvider.SqlServer` and `LinqToDB.DataProvider.Oracle` - XML documentation for typed provider-specific hint helpers.
