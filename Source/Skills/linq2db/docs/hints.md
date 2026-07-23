# Query, Table, and Join Hints

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](../SKILL.md).
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
> - Some providers expose provider-specific generic directive families for large or evolving vendor-defined
>   sets, for example ClickHouse `SETTINGS` or SQL Server `USE HINT`. Use those package-confirmed
>   provider helpers before plain generic raw-text fallbacks, but do not expect LinqToDB docs to list
>   every vendor setting or hint value.
> - Do not jump to a provider-specific open-ended directive family before checking concrete typed
>   helpers for the requested SQL keyword. For example, for ClickHouse `FINAL`, check `FinalHint`
>   / `FinalInScopeHint` before suggesting `SettingsHint("final = 1")`.
>   If no concrete typed helper is found, continue to the provider-specific open-ended family
>   before falling back to plain generic raw hint APIs.
> - Use general raw-text hint APIs only when the provider-specific API does not expose the required hint or directive family.
> - Provider-specific typed helpers require the provider marker first. Do not call a typed helper
>   directly on plain `ITable<T>` or `IQueryable<T>`; call `AsSqlServer()`, `AsOracle()`,
>   `AsClickHouse()`, etc. from the provider marker table, then call the typed helper.
> - One provider marker call is enough for several consecutive typed hints for that provider.
>   Call another marker only when switching to another provider's hint API.
> - Before claiming that a provider-specific hint API does not exist, inspect the provider marker table below and the generated provider `*Hints` API entries.
> - Do not choose plain `QueryHint(...)` or `TableHint(...)` only because it is documented; those are generic fallbacks after generated API/map lookup, with raw XML-doc confirmation when needed.
> - Hint syntax and meaning are provider-defined. Do not assume the same hint text is valid across providers.
> - Never build hint text from user input. Hint strings are SQL text, not query parameters.
> - Hints are deferred and composable; they become SQL AST extensions and are emitted only during SQL generation.
> - Provider-specific `AsXxx()` hint branches can be added to the same query. Only hints compatible with the active provider are emitted into SQL.
> - Do not suggest `Sql.Table(...)`, `[Sql.Expression]`, SQL text rewriting, or interceptors for a hint until provider-specific and general hint APIs have been checked.
> - For any provider-specific hint question, `docs/hints-api-map.md` and the provider `*Hints`
>   generated API surface are a required pre-answer gate, not optional follow-up material.
> - Do not skip the hint map because a database feature is a table modifier, lock clause, query
>   directive, or provider-specific SQL extension instead of a classic optimizer hint. If the user
>   asks for it as a hint, run the hint lookup algorithm first.
> - Outside knowledge can identify a candidate SQL feature, optimizer strategy, business rule, or
>   any other non-LinqToDB part of the answer, but the LinqToDB API used to express it must still be
>   confirmed through the package hint route before code is shown.
> - This document explains how to express an already chosen SQL hint or provider directive through
>   LinqToDB. It does not choose, recommend, or validate database tuning strategies.
> - Do not claim that a typed hint API is absent from the map unless you have searched the map by
>   exact provider and exact SQL/database term. If the exact map lookup is inconclusive, search
>   `docs/api.md` and XML-doc for the provider `*Hints` type before recommending a raw fallback.

---

## Pattern quick-reference

For a concrete provider SQL keyword or directive, do not start from the general raw-text rows in
this table. First run the [Required Hint Lookup Algorithm](#required-hint-lookup-algorithm).

| Scenario | Preferred pattern |
|---|---|
| Provider-specific known hint | `db.GetTable<T>().AsXxx().<real typed helper>()`; choose the real helper from XML-doc |
| Provider-specific query hint | `query.AsXxx().<real query helper>()`; choose the real helper from XML-doc |
| Provider-specific table hint in scope | `query.AsXxx().<real in-scope helper>()`; choose the real helper from XML-doc |
| Provider-specific generic directive family | `query.AsXxx().<real provider family helper>(...)`; use when XML-doc/map confirm a provider family API but individual vendor values are not enumerated |
| Several typed hints for one provider | `query.AsXxx().<helper1>().<helper2>()`; do not repeat `AsXxx()` between same-provider helpers |
| Typed hints for several providers | `query.AsSqlServer().<real SQL Server helper>().AsOracle().<real Oracle helper>()`; call the next provider marker before switching APIs |
| General raw table hint | `db.GetTable<T>().TableHint("...")` or `.With("...")` |
| General raw tables-in-scope hint | `query.TablesInScopeHint("...")` |
| General raw index hint | `db.GetTable<T>().IndexHint("...")` |
| General raw join hint | `query.JoinHint("...")` |
| General raw subquery hint | `query.SubQueryHint("...")` |
| MySQL/PostgreSQL subquery table hint | `query.AsXxx().SubQueryTableHint("...", Sql.TableAlias("id"))` |
| General raw query hint | `query.QueryHint("...")` |
| MERGE hint | `target.Merge("...")`; see `docs/crud/crud-merge.md` |

---

## 1. Two hint APIs

LinqToDB has two hint API layers.

The provider-specific API is the first choice. It has two required steps:

1. Call the provider marker such as `AsSqlServer()`, `AsOracle()`, or `AsClickHouse()` to switch
   the receiver to the provider-specific table/query interface.
2. Call one or more typed helpers from that provider's `*Hints` surface.

Provider-specific typed hint helpers are not extension methods on plain `ITable<T>` or
`IQueryable<T>`. If the marker call is missing, the code is incomplete even when the helper name is
correct.

Exception: some provider APIs intentionally expose an additional plain `IQueryable<T>` overload.
For example, YDB `DistinctHint(...)` and `UniqueHint(...)` have overloads on both
`IQueryable<T>` and `IYdbSpecificQueryable<T>`. When the generated API/map lists both receivers,
the plain `IQueryable<T>` overload is valid for that specific helper. Do not generalize this
exception to other providers or helpers unless the installed package API lists the same receiver.

Conceptual shape, not copy-paste code:

```text
using LinqToDB;

var query =
    db.GetTable<Product>()
        .AsSqlServer()
        .<real SqlServerHints helper>()
        .<another real SqlServerHints helper>()
        .Where(p => p.IsActive);
```

Use the provider-specific API when available. Use raw-text hints only for gaps, experiments, or
provider features that are not exposed by typed helpers in the installed package version.
For several typed hints for the same provider, call the marker once and chain the helpers. When
switching providers, call the next provider marker before calling that provider's helpers.

The general API is provider-neutral by method shape, but provider-specific by hint text.
It accepts raw SQL text and is a fallback after provider-specific lookup:

```csharp
using LinqToDB;

var query =
    db.GetTable<Product>()
        .TableHint("SOME_TABLE_HINT")
        .Where(p => p.IsActive);
```

### Required Hint Lookup Algorithm

For any hint question, including table modifiers, lock clauses, query directives, and
provider-specific SQL extensions that the user describes as hints, use this exact order:

1. Identify the provider and the SQL hint text or database term from the user request.
2. Search [`docs/hints-api-map.md`](hints-api-map.md) by provider name and exact SQL hint text or
   database term. Use likely helper-name fragments only as extra search terms, not as a substitute
   for the exact provider + SQL term lookup.
3. If the map contains a candidate provider-specific helper, use it as the first candidate and
   then verify the exact member in `lib/<TFM>/linq2db.xml`.
4. Verify the provider marker method needed to reach the helper. The marker is part of the
   required API path: `AsSqlServer()` before `SqlServerHints`, `AsOracle()` before `OracleHints`,
   `AsClickHouse()` before `ClickHouseHints`, and so on from the provider marker table.
5. If the candidate is a `Table` hint, also search the same provider and SQL hint text for a
   `TablesInScope` helper before answering. Some providers expose both table-local and scope-level
   forms, and the correct answer depends on whether the user needs one table source or all table
   references in a query scope.
6. In `docs/api.md` and, when needed, raw XML-doc, verify the helper signature, receiver type,
   namespace, overloads, XML summary, and AI metadata such as `Group=Hints; HintType=...`.
7. If the exact map lookup has no hit, search the provider `*Hints` XML-doc members directly by
   SQL hint text, provider namespace, receiver type, and likely helper-name fragments.
8. Prefer the concrete typed/provider-specific helper when it exists.
9. If the request names a concrete SQL hint/directive keyword, do not choose a provider-specific
   open-ended directive family until the exact provider + keyword lookup has failed to find a
   concrete typed helper. Example: ClickHouse `SETTINGS` can express `final = 1`, but a request
   for ClickHouse `FINAL` must first resolve `FinalHint` / `FinalInScopeHint`.
10. If no concrete typed helper exists but the map or XML-doc exposes a provider-specific
   open-ended directive family for the requested SQL feature, use that provider helper before
   plain generic raw hint APIs. The current open-ended families are ClickHouse `SettingsHint(...)`
   for `SETTINGS`, SQL Server `OptionUseHint(...)` for `USE HINT`, and Oracle
   `OptParamHint(...)` for `OPT_PARAM`.
11. Only if no concrete typed provider helper, provider-specific generic family helper, or suitable
   general hint scope exists, consider generic raw hint APIs (`QueryHint`, `TableHint`,
   `TablesInScopeHint`, etc.).
12. Only after hint APIs fail, consider custom SQL (`Sql.Table`, `[Sql.Expression]`) or
   interceptors. Treat those as last-resort fallbacks.

For questions about applying a table hint to several tables, all tables, or the current query
scope, search for `HintType=TablesInScope` and helper names containing `InScope` before suggesting
`TablesInScopeHint("...")`. A generic scope hint is still a fallback when no typed scope helper
exists. Apply a `TablesInScope` helper to the query or subquery that already contains the table
references you want to affect; do not attach it to the first table source before adding joins and
expect later tables to be included.

Use naming patterns only as search hints, never as invented API names. Common shapes are
`<Base>Hint(...)` -> `<Base>InScopeHint(...)`, and `With<Base>(...)` ->
`With<Base>InScope(...)`. Provider aliases can exist. Do not synthesize names by inserting words
such as `Table` or by string concatenation; verify the real helper in `docs/hints-api-map.md` and
XML-doc.

If the answer recommends a fallback API for a provider-specific SQL hint, it must be because the
exact map lookup, generated API lookup, and raw XML-doc confirmation failed to find a typed helper
in the installed package version.
Do not write "the map has no entry" unless that exact lookup was performed.

Answering contract: for a concrete provider-specific hint, name the required provider marker, the
found typed helper, and the helper receiver before showing code. If no typed helper exists,
explicitly say whether exact map lookup, generated API lookup, and raw XML-doc confirmation found a
provider-specific generic directive family before recommending raw `QueryHint`, `TableHint`,
`TablesInScopeHint`, custom SQL, or interceptors.

Generated API docs classify hint APIs with AI metadata and `HintType`
(`Table`, `TablesInScope`, `Index`, `Join`, `SubQuery`, `Query`, `Merge`, `TableName`).
Agents should use those tags when choosing the correct overload or scope.
For typed provider helpers, the XML-doc summary should also name the concrete SQL hint in `<c>...`,
so agents do not need to infer it only from the method name.
Generated provider-specific helpers get those tags from their T4 templates; update the `.tt`
source first and then regenerate/check in the corresponding `.generated.cs` file.
Handwritten provider-specific helpers carry the same tags directly in their XML docs.

---

## 2. Provider-specific typed hints

Provider-specific hint APIs are exposed through provider namespaces and `AsXxx()` marker methods.
The marker wraps the query or table with a provider-specific interface so provider hint extensions
can be selected by C# overload resolution. This marker call is required; typed helper methods are
not selected on the plain LinqToDB query/table receiver.
The generated provider-specific helper set is intended to cover most known hints for supported
providers in the installed package version, so inspect the provider namespace before falling back
to raw text.

Conceptual shape, not copy-paste code:

```text
using LinqToDB;
using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.DataProvider.SqlServer;

var products =
    db.GetTable<Product>()
        .AsSqlServer()
            .<real SqlServerHints helper>()
            .<another real SqlServerHints helper>()
        .AsClickHouse()
            .<real ClickHouseHints helper>(); // call the next marker before switching provider helpers
```

For one provider, call the marker once and chain all needed helpers for that provider. Calling
`AsSqlServer()` before every SQL Server hint is unnecessary. Calling an Oracle helper after
`AsSqlServer()` is invalid; call `AsOracle()` first, then chain Oracle helpers.

During SQL generation, LinqToDB emits only hint extensions that are compatible with the active
provider. Incompatible provider-specific hint branches are ignored by provider filtering.

This is intentional. It allows reusable query code to carry provider-specific refinements without
branching every query by provider.

Currently visible provider marker APIs in this package. For these providers, do not conclude
"no provider-specific hint API" until you have checked the listed namespace and XML-doc surface:

| Provider namespace | Marker methods | XML-doc surface to inspect | Notes |
|---|---|---|---|
| `LinqToDB.DataProvider.Access` | `AsAccess()` | `LinqToDB.DataProvider.Access.AccessHints` | Table and query wrappers. |
| `LinqToDB.DataProvider.ClickHouse` | `AsClickHouse()` | `LinqToDB.DataProvider.ClickHouse.ClickHouseHints` | Table and query wrappers; includes ClickHouse table, join, and query hints. |
| `LinqToDB.DataProvider.MySql` | `AsMySql()` | `LinqToDB.DataProvider.MySql.MySqlHints` | Table and query wrappers; includes MySQL optimizer hints. |
| `LinqToDB.DataProvider.Oracle` | `AsOracle()` | `LinqToDB.DataProvider.Oracle.OracleHints` | Table and query wrappers; many Oracle optimizer hints. |
| `LinqToDB.DataProvider.PostgreSQL` | `AsPostgreSQL()` | `LinqToDB.DataProvider.PostgreSQL.PostgreSQLHints` | Query wrapper; row-locking hints such as `ForUpdate...Hint()`. |
| `LinqToDB.DataProvider.SqlCe` | `AsSqlCe()` | `LinqToDB.DataProvider.SqlCe.SqlCeHints` | Table and query wrappers. |
| `LinqToDB.DataProvider.SQLite` | `AsSQLite()` | `LinqToDB.DataProvider.SQLite.SQLiteHints` | Table wrapper; SQLite table/index-style hints. |
| `LinqToDB.DataProvider.SqlServer` | `AsSqlServer()` | `LinqToDB.DataProvider.SqlServer.SqlServerHints` | Table and query wrappers; SQL Server table hints. |
| `LinqToDB.DataProvider.Ydb` | `AsYdb()` | `LinqToDB.DataProvider.Ydb.YdbHints` | Table and query wrappers; YDB query hints. |

Providers not listed in this table do not currently expose a provider-specific `AsXxx()` hint
marker API in this package.

Do not assume that the general raw-text hint methods will be emitted for an unlisted provider.
The provider SQL builder must explicitly support the relevant query extension scope.

### Provider-specific open-ended directive families

Some database directive families are too large, version-dependent, or vendor-defined to expose as
one LinqToDB method per possible value. In those cases, LinqToDB exposes one provider-specific
helper for the directive family. Treat these helpers as package-confirmed provider APIs, not as
plain provider-neutral raw fallbacks. The vendor documentation or application requirement chooses
the setting/hint value; the LinqToDB docs only confirm the method, receiver, and scope.

A `string` parameter alone does not make a helper an open-ended family. Many typed helpers use
strings for ordinary operands such as index names, query block names, SQL Server `OPTIMIZE FOR`
arguments, or table-hint values. Do not classify those as documentation gaps only because the
signature contains `string`.

| Provider | SQL/directive family | LinqToDB helper | Receiver | Value source |
|---|---|---|---|---|
| ClickHouse | `SETTINGS` clause | `SettingsHint<TSource>(...)` | `IClickHouseSpecificQueryable<TSource>` | ClickHouse setting name/value; individual settings are not enumerated by LinqToDB. |
| SQL Server | `USE HINT` query option | `OptionUseHint<TSource>(...)` | `ISqlServerSpecificQueryable<TSource>` | SQL Server `USE HINT` names; individual values are SQL Server-defined. |
| Oracle | `OPT_PARAM` optimizer hint | `OptParamHint<TSource>(...)` | `IOracleSpecificQueryable<TSource>` | Oracle optimizer parameter name/value strings. |

Do not expand this table into a vendor setting catalogue. For example, do not add one row per
ClickHouse `SETTINGS` value or one row per SQL Server `USE HINT` value. Add concrete rows only
when LinqToDB exposes a concrete helper or when a provider-specific family helper itself needs to
be discoverable by SQL/database wording.

Known provider gaps:

| Provider | Database has hint-like feature? | Current linq2db hint API | Agent guidance |
|---|---|---|---|
| DB2 | Yes: optimization profiles/guidelines, including embedded XML guidelines in SQL comments. | No regular table/query/join hint API. | Do not invent `AsDB2().XxxHint()` or expect raw `QueryHint` to emit. Treat DB2 optimization guidelines as future provider-specific work. |
| Firebird | Yes: `PLAN` clause and related optimizer controls such as `OPTIMIZE FOR`. | No regular table/query/join hint API. | Do not use raw hint APIs for Firebird plan control; `PLAN` would need explicit provider support. |
| Informix | Yes: optimizer directives, plus linq2db has separate MERGE hint output. | No regular table/query/join hint API; MERGE hint only. | Use `.Merge("...")` only for MERGE hints. Do not assume query/table directives are supported by the general hint API. |
| SAP HANA | Yes: `WITH HINT (...)` for DML statements. | No regular table/query/join hint API. | Do not invent `AsSapHana().XxxHint()`; HANA hints would need explicit provider builder support. |
| Sybase | Needs provider-specific investigation. | No regular table/query/join hint API. | Do not invent provider-specific hint helpers. Verify dialect support before proposing docs or API. |

Inspect `docs/api.md`, raw XML-doc when needed, or the provider namespace for the exact helper names. Generated helpers often use
a `Hint` suffix, but naming is provider-specific and should not be guessed from SQL text alone.
The XML-doc summary names the concrete SQL hint inside `<c>...</c>`; generated AI metadata and `HintType`
classify the scope, not the exact hint name.

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

Some provider-specific hint APIs accept `Sql.SqlID` parameters, including MySQL and
PostgreSQL `SubQueryTableHint(...)` overloads and other provider hint helpers that target
specific table references. Assign a logical id to the table source with `TableID("id")`, then
pass `Sql.TableAlias("id")`, `Sql.TableName("id")`, or `Sql.TableSpec("id")` to the
hint API. These helpers resolve the exact alias, rendered table name, or table specification
generated by LinqToDB SQL translation; do not hard-code generated aliases or mapped table names in
hint text.

### Table identifiers in hint parameters

Use this pattern for any hint API that needs to reference a generated table alias, table name, or
table specification and whose XML-doc/API signature accepts `Sql.SqlID` values or format/object
parameters documented to resolve `Sql.SqlID`.

Do not infer generated identifiers from sample SQL. LinqToDB can choose aliases during translation,
and table names can come from mapping, `TableName(...)`, temp-table naming, or provider SQL builder
rules.

Pattern:

1. Mark the table source with `.TableID("logical-id")`.
2. Pass `Sql.TableAlias("logical-id")`, `Sql.TableName("logical-id")`, or
   `Sql.TableSpec("logical-id")` to the hint API.
3. Use the same logical id on both sides.

Shape:

```csharp
using LinqToDB;

var table =
    db.GetTable<MyRow>()
        .TableID("target");

var query =
    table
        .Where(x => x.Id > 0)
        .AsXxx()
        .<real provider helper that accepts Sql.SqlID>(Sql.TableAlias("target"));
```

The `<real ...>` marker above is intentionally non-compilable. Replace it with a real provider helper found in
`docs/hints-api-map.md`, `docs/api.md`, or XML-doc. Examples of applicable API shapes include
provider `SubQueryTableHint(...)` overloads, SQL Server `OptionTableHint(...)`, provider optimizer
hints that accept `params Sql.SqlID[]`, and format-parameter hint APIs such as ClickHouse
`SettingsHint(...)`.

Concrete example of the same mechanism with a format string:

```csharp
using LinqToDB;
using LinqToDB.DataProvider.ClickHouse;

var query =
    db.GetTable<Order>()
        .TableID("orders")
        .Where(o => o.Status == OrderStatus.Open)
        .AsClickHouse()
        .SettingsHint(
            "additional_table_filters = {{'{0}': 'Status != ''Closed'''}}",
            Sql.TableName("orders"));
```

This example demonstrates identifier resolution only: `Sql.TableName("orders")` is resolved from
the generated SQL for the table source marked with `.TableID("orders")` and inserted into the
format string by the hint API. For other providers or hint scopes, choose the real helper and the
correct `Sql.TableAlias` / `Sql.TableName` / `Sql.TableSpec` resolver from XML-doc.

Raw-text examples:

```csharp
using LinqToDB;

var products =
    db.GetTable<Product>()
        .TableHint("SOME_TABLE_HINT")
        .IndexHint("INDEX(IX_Product_Category)");

var query =
    products
        .Where(p => p.IsActive)
        .QueryHint("SOME_QUERY_HINT");
```

Raw hints are useful for provider-specific features that do not have typed helpers. They are also
easy to misuse: the same text can be valid for one provider, ignored by another, or emitted in a
different SQL position.

For SQL Server, raw table hints are emitted inside `WITH (...)`, and raw query hints are emitted
inside `OPTION (...)`. Do not include those wrapper clauses
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
        from c in db.Child.TableHint("PROVIDER_TABLE_HINT")
        where c.ParentID == p.ParentID
        select p
    )
    .TablesInScopeHint("PROVIDER_TABLE_HINT");
```

Apply provider-specific `TablesInScope` helpers to the composed query scope, not to only the first
table before joins are added:

Conceptual shape, not copy-paste code:

```text
var query =
    (
        from a in db.GetTable<A>()
        join b in db.GetTable<B>() on a.Id equals b.AId
        join c in db.GetTable<C>() on b.Id equals c.BId
        select new { a, b, c }
    )
    .AsXxx()
    .<real provider-specific in-scope helper>();
```

For providers that emit per-table hint clauses, this produces table hints for all table references
in that scope. A table-local hint is preserved and combined with the scope hint, so the `Child`
table can keep its explicit index hint and also receive the scope-level table hint.

Scope boundaries matter:

- A scope hint applies to tables already inside that query scope.
- If a scope hint is called on only one `ITable<T>` before a later `join`, it applies to that table
  source, not to the later joined tables.
- Tables introduced later by composing another outer query are not automatically affected.
- A nested table/query expression with its own `TablesInScopeHint(...)` has its own scope.
- Table-local hints are more specific than a scope-level hint: they are not removed by the scope
  hint and can coexist with it.

Provider SQL output differs. SQL Server emits per-table `WITH (...)` clauses, while Oracle and
MySQL often collect table-scoped optimizer hints into provider-specific hint blocks. Use generated
SQL inspection when exact placement matters.

---

## 5. Combining provider-specific branches

Provider-specific wrappers are still query/table expressions. Shared query code may carry several
provider-specific branches, but the concrete helper names must come from the installed package
XML-doc, not from memory or examples.

Shape of a multi-provider query:

Conceptual shape, not copy-paste code:

```text
using LinqToDB;
using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.DataProvider.SqlServer;

var query =
    db.GetTable<Product>()
        .Where(p => p.IsActive)
        .AsSqlServer()
            .<real SqlServerHints helper>()
        .AsClickHouse()
            .<real ClickHouseHints helper>()
        .AsPostgreSQL()
            .<real PostgreSQLHints helper>();
```

The `<real ...>` markers above show branch placement only. Replace each marker with a real
provider helper from the installed package XML-doc.

Use this workflow:

1. Build the provider-neutral query shape first.
2. For each target provider, call the provider marker (`AsSqlServer()`, `AsClickHouse()`,
   `AsPostgreSQL()`, etc.).
3. After the marker, chain all typed helpers needed for that provider.
4. If the next hint is for a different provider, call that provider's marker before using its
   helpers.
5. Inspect the matching provider `*Hints` XML-doc surface and choose the helper whose XML summary
   names the required SQL hint and whose `HintType` matches the required scope.
6. Add only helpers that exist in the installed package version.

During SQL generation, only hint extensions compatible with the active provider are emitted.
Incompatible provider-specific hint extensions are ignored by provider filtering.

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
| Using `TablesInScopeHint("...")` when a typed `*InScope*` provider helper exists | Prefer the typed provider-specific scope helper found in `docs/hints-api-map.md` and XML-doc. |
| Expecting a provider-specific hint to affect every provider | Provider-specific hints are emitted only for compatible providers. |
| Inventing provider-specific helpers for unsupported providers | Check the provider table and XML docs; if no `AsXxx()` hint API or builder support exists, document the gap instead. |
| Choosing a SQL hint or database tuning strategy from this document | Do not treat this document as database tuning guidance. Use it only to map an already chosen SQL hint/provider directive to the correct LinqToDB API. |
| Using `.Merge("...")` as if it were a query/table hint | MERGE hints belong to the merge builder only. |

---

## Related documentation

- [`docs/crud/crud-merge.md`](crud/crud-merge.md) - MERGE builder and merge-specific hints.
- [`docs/hints-api-map.md`](hints-api-map.md) - reverse lookup from concrete provider SQL hint
  text to typed provider-specific helper APIs.
- [`docs/provider-capabilities.md`](provider-capabilities.md) - provider feature support.
- [`docs/provider-setup.md`](provider-setup.md) - provider selection, dialects, and driver packages.
- [`docs/extensions.md`](extensions.md) - custom SQL expressions when hints are not the right tool.
- `LinqExtensions.Hints` - XML documentation for general raw hint APIs.
- Provider namespaces such as `LinqToDB.DataProvider.SqlServer` and `LinqToDB.DataProvider.Oracle` - XML documentation for typed provider-specific hint helpers.
