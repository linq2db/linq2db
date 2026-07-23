<!-- Generated from: Source/Skills/linq2db/docs/extensions.md -->

# Extension Mechanisms

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> You are here if you need to:
> - map a C# method or property to a SQL function or expression
> - use `[Sql.Expression]`, `[Sql.Function]`, or `[ExpressionMethod]`
> - define provider-specific SQL overloads for the same method
> - create reusable LINQ query fragments or calculated entity columns
> - register a `DataOptions`-level custom member translator (`IMemberTranslator`)

This document describes the mechanisms for extending how LinqToDB translates application-defined
C# code to SQL: attribute-based mapping on individual methods/properties (`[Sql.Expression]`,
`[Sql.Function]`, `[ExpressionMethod]`) and the `DataOptions`-level `IMemberTranslator` extension
point.

If the task is to use application-provided SQL text as a query root, execute command text
directly, or inspect generated SQL text, use [`raw-sql.md`](13-extensions.md) instead.

---

## Extension points overview

| Mechanism | Use case |
|---|---|
| `[Sql.Expression("...")]` | Map a static method/property to an arbitrary SQL template |
| `[Sql.Function("name")]` | Map a static method/property to a standard SQL function call |
| `[ExpressionMethod("helper")]` | Replace a method/property with a LINQ expression tree |
| `IMemberTranslator` / `UseMemberTranslator` | Register a `DataOptions`-level translator for member/method expressions that attributes cannot annotate (e.g. members of a type you don't own) |

The three attributes are in the `LinqToDB` namespace and can be stacked with a `Configuration`
argument to provide provider-specific overloads. `IMemberTranslator` is registered separately
through `DataOptions` - see [Member translators](#member-translators) below.

---

## `[Sql.Expression]`

Maps a static C# method or property to an arbitrary SQL template.
Parameters are substituted positionally using `{0}`, `{1}`, etc.

Do not use `[Sql.Expression]` as the first answer for SQL hints, provider table modifiers,
lock clauses, query directives, index directives, or join modifiers. First check
[`docs/hints.md`](11-hints.md), [`docs/hints-api-map.md`](12-hints-api-map.md), and the provider
`*Hints` XML-doc members. `[Sql.Expression]` is a fallback when the hints API does not cover the
requested provider feature.

```csharp
// Maps to: SOUNDEX({0})
[Sql.Expression("SOUNDEX({0})", ServerSideOnly = true)]
public static string Soundex(string value) => throw new InvalidOperationException();
```

Usage in a query:
```csharp
var result = db.Customers
    .Where(c => Soundex(c.Name) == Soundex("Smith"))
    .ToList();
```

### Key properties

| Property | Default | Description |
|---|---|---|
| `ServerSideOnly` | `false` | When `true`, throws `LinqToDBException` if the expression cannot be translated to SQL. When `false` (default), LinqToDB falls back to client-side (in-memory, post-materialization) evaluation if SQL translation is not possible. |
| `InlineParameters` | `false` | Emit literal values instead of parameters |
| `IsPredicate` | `false` | Expression returns a boolean predicate - omits `= 1` wrapping |
| `IsAggregate` | `false` | Expression is an aggregate function (e.g. `SUM`, `COUNT`) |
| `IsWindowFunction` | `false` | Expression is a window / analytic function |
| `IsPure` | `true` | Same inputs always produce the same output (enables optimizer hints) |

### Argument reordering

Pass explicit `argIndices` to reorder parameters:

```csharp
// Maps to: CONVERT({1}, {0}) - type first, value second
[Sql.Expression("CONVERT({1}, {0})", 0, 1)]
public static TTo SqlConvert<TTo>(object value) => throw new InvalidOperationException();
```

### Provider-specific overloads

Stack multiple `[Sql.Expression]` attributes with the `Configuration` argument to emit
different SQL per provider:

```csharp
[Sql.Expression(ProviderName.SqlServer,  "ISNULL({0}, {1})")]
[Sql.Expression(ProviderName.Oracle,     "NVL({0}, {1})")]
[Sql.Expression(                         "COALESCE({0}, {1})")]   // default
public static T IfNull<T>(T value, T replacement) => throw new InvalidOperationException();
```

---

## `[Sql.Function]`

A specialization of `[Sql.Expression]` for standard SQL function call syntax
(`name(arg0, arg1, ...)`). Equivalent to `[Sql.Expression("name({0}, {1}, ...)")]`
but more concise.

```csharp
// Maps to: DIFFERENCE('a', 'b')
[Sql.Function("DIFFERENCE", ServerSideOnly = true)]
public static int Difference(string s1, string s2) => throw new InvalidOperationException();
```

Provider-specific overload:
```csharp
[Sql.Function(ProviderName.SqlServer, "CHECKSUM")]
[Sql.Function(                        "HASH")]          // default / fallback
public static int Checksum(object value) => throw new InvalidOperationException();
```

---

## `[ExpressionMethod]`

Replaces a method or property call with a LINQ expression tree returned by a companion
method in the same class. Use this when:
- the SQL translation is itself a LINQ/LinqToDB expression (not a raw SQL string),
- you want to reuse a complex query fragment across multiple queries,
- you need a **calculated column** that LinqToDB materializes during entity load.

### Reusable query fragment

```csharp
public static class QueryExtensions
{
    [ExpressionMethod(nameof(IsActiveExpr))]
    public static bool IsActive(this Customer c) => throw new InvalidOperationException();

    static Expression<Func<Customer, bool>> IsActiveExpr()
        => c => c.ValidFrom <= Sql.CurrentTimestamp && c.ValidTo > Sql.CurrentTimestamp;
}
```

Usage:
```csharp
var active = db.Customers.Where(c => c.IsActive()).ToList();
// Translated to: WHERE ValidFrom <= CURRENT_TIMESTAMP AND ValidTo > CURRENT_TIMESTAMP
```

### Calculated column (entity property)

Setting `IsColumn = true` causes LinqToDB to populate the property during entity
materialization using the expression, without a separate round-trip:

```csharp
public class Product
{
    public decimal  Price    { get; set; }
    public decimal  TaxRate  { get; set; }

    [ExpressionMethod(nameof(PriceWithTaxExpr), IsColumn = true)]
    public decimal PriceWithTax { get; set; }

    static Expression<Func<Product, decimal>> PriceWithTaxExpr()
        => p => p.Price * (1 + p.TaxRate);
}
```

---

## Supported extension points

- `[Sql.Expression]` and `[Sql.Function]` on `static` methods and static properties.
- `[ExpressionMethod]` on instance or static methods and properties; the companion
  expression method may be `private`.
- Multiple stacked attributes on the same member for provider-specific overloads.
- `[ExpressionMethod]` companion methods may accept the instance (`this`), query
  parameters, and optionally an `IDataContext` as the last parameter.

### Custom extension builders

When implementing `Sql.IExtensionCallBuilder` / `Sql.ISqlExtensionBuilder` logic, build string
concatenation with `builder.Concat(...)`. Do not build string concatenation with
`builder.Add(...)`: that API is for numeric/temporal arithmetic and throws for string-typed
operands. Use the `builder.Concat(bool preserveNull, ...)` overload when the custom builder must
choose between strict any-null-to-null semantics and null-as-empty semantics.

### Query-dependent parameters

`[SqlQueryDependentParams]` is deprecated and scheduled for removal in v7. Do not use it for new
custom SQL functions. For query-cache-sensitive parameters, first check whether the default
structural comparison is sufficient; if a parameter must be evaluated before SQL generation, use
the exact package XML-doc/API guidance for `[SqlQueryDependent]` instead.

## Not supported

- Instance methods on non-entity types annotated with `[Sql.Expression]` or
  `[Sql.Function]` - the target must be `static`.
- `void`-returning methods with `[ExpressionMethod]`.
- Dynamic (runtime-constructed) SQL templates - templates must be compile-time constants.
- `[Sql.Expression]` on types, constructors, or operators directly - use a static wrapper
  method instead.

---

## Member translators

`IMemberTranslator` (`LinqToDB.Linq.Translation`) is a `DataOptions`-level extension point for
translating .NET member/method expressions to SQL. Prefer the `[Sql.Expression]` / `[Sql.Function]`
/ `[ExpressionMethod]` attributes above when the target method or property is one you can annotate
directly - they are simpler and scoped to the member itself. Reach for `IMemberTranslator` only when
you need to translate members you cannot annotate (e.g. members of a BCL or third-party type) or
need context-aware translation logic.

Register translators with `UseMemberTranslator(IMemberTranslator)` or
`UseMemberTranslator(IEnumerable<IMemberTranslator>)`; remove one with
`RemoveTranslator(IMemberTranslator)`. `DataOptions` is immutable, so each call returns a new
instance.

```csharp
public class MyTranslator : MemberTranslatorBase
{
    public MyTranslator()
    {
        Registration.RegisterMethod(
            (string s) => s.MyCustomMethod(),
            TranslateMyCustomMethod);
    }

    Expression? TranslateMyCustomMethod(
        ITranslationContext ctx, MethodCallExpression call, TranslationFlags flags)
    {
        if (!ctx.TranslateToSqlExpression(call.Object!, out var arg))
            return null;
        return ctx.CreatePlaceholder(
            ctx.ExpressionFactory.Function(
                ctx.ExpressionFactory.GetDbDataType(call.Type),
                "MY_FUNCTION", arg),
            call);
    }
}

var options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseMemberTranslator(new MyTranslator());
```

> **Caveat:** the convenient base class shown above (`MemberTranslatorBase`, with its
> `Registration.RegisterMethod` dispatch) and the SQL node types the example builds
> (`ISqlExpression`, `SqlPlaceholderExpression`) live in `LinqToDB.Internal.*` namespaces, which are
> implementation detail, not a supported consumer API (see `SKILL.md`). `IMemberTranslator` itself
> is public, but as of this writing there is no documented way to implement it usefully without
> reaching into `LinqToDB.Internal.*`. Track this at
> [linq2db/linq2db#5716](https://github.com/linq2db/linq2db/issues/5716). Until resolved, treat this
> example as "how the shipped providers do it internally", not as a supported public pattern - do
> not present it to a consumer as a stable public API.

---

## Further reading

- [`Sql` API reference](16-xml-doc.md)
- [Extensible SQL mapping article](https://linq2db.github.io/articles/sql/Sql-Function.html)
- [Window / analytic functions](https://linq2db.github.io/articles/sql/Window-Functions-(Analytic-Functions).html)

<!-- Generated from: Source/Skills/linq2db/docs/raw-sql.md -->

# Raw SQL

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

Use this guide when a task needs SQL text supplied by the application instead of SQL fully
generated from a LINQ expression.

Raw SQL APIs have different execution and composition rules. Do not mix them:

| Need | Use | Execution | Composition |
|---|---|---|---|
| Raw SQL as a LINQ query root | `FromSql<T>()` | Deferred | Composable when the provider can compose over the supplied SQL |
| Raw SQL returning one scalar column as a LINQ query root | `FromSqlScalar<T>()` | Deferred | Composable when the provider can compose over the supplied SQL |
| Execute command text directly | `SetCommand(...).Execute*()` / `Query*()` / `ExecuteReader*()` | Immediate at terminal call | Not a LINQ query |
| Inspect generated SQL for a LINQ/DML query | `ToSqlQuery(...)` | Immediate SQL generation only | Terminal inspection API |
| Map a reusable C# method/property to SQL | `[Sql.Expression]`, `[Sql.Function]`, `[ExpressionMethod]` | Depends on query execution | See [`extensions.md`](13-extensions.md) |
| Add provider hints/table modifiers | Provider-specific hint helpers first | Depends on hint API | See [`hints.md`](11-hints.md) |

## Raw SQL Query Roots

`FromSql<T>()` creates an `IQueryable<T>` from a raw SQL query. LinqToDB can then compose LINQ
operators on top when the configured provider supports composition over the supplied SQL.

Prefer the interpolated overload for user values:

```csharp
using LinqToDB;
using LinqToDB.Async;

var minTotal = 100m;

var orders = await db.FromSql<Order>(
        $"SELECT * FROM Sales.Orders WHERE Total >= {minTotal}")
    .Where(o => o.IsActive)
    .OrderBy(o => o.Id)
    .ToListAsync();
```

The interpolated values are converted to parameters by the `FromSql<T>(IDataContext,
FormattableString)` overload. Do not build SQL by concatenating user values into the SQL text.

If the SQL text is not interpolated, use placeholders and pass values separately:

```csharp
using LinqToDB;

var query = db.FromSql<Order>(
    "SELECT * FROM Sales.Orders WHERE CustomerId = {0}",
    customerId);
```

When exact database parameter metadata matters, pass `DataParameter` values:

```csharp
using LinqToDB;
using LinqToDB.Data;

var query = db.FromSql<Order>(
    "SELECT * FROM Sales.Orders WHERE CustomerId = {0}",
    new DataParameter("@customerId", customerId, DataType.Int32));
```

## Scalar Raw SQL Query Roots

Use `FromSqlScalar<T>()` when the raw SQL returns a single scalar column and you still need an
`IQueryable<T>` that can be composed:

```csharp
using LinqToDB;

var ids =
    from id in db.FromSqlScalar<int>($"SELECT Id FROM Sales.Orders")
    where id > minId
    select id;
```

XML-doc states that LinqToDB generates an alias named `value` for the scalar value. For providers
that cannot use the generated wrapper syntax, make the raw SQL provide an alias named `value`.

## Alias Placement

When `FromSql<T>()` SQL needs an explicit position for the generated table alias, use
`Sql.AliasExpr()`. If the raw SQL contains at least one `Sql.AliasExpr()`, LinqToDB does not append
an automatic alias elsewhere.

```csharp
using LinqToDB;

var query = db.FromSql<Order>(
    $"SELECT * FROM Sales.Orders {Sql.AliasExpr()} WHERE Status = {status}");
```

This is a LinqToDB mechanism for alias placement in generated SQL. It is not a string placeholder
for user input.

## Direct Raw SQL Commands

Use `SetCommand(...)` when you want to execute command text directly. It returns a `CommandInfo`
builder; execution happens only when a terminal method such as `Execute`, `ExecuteAsync`, `Query`,
`QueryAsync`, `ExecuteReader`, or `ExecuteReaderAsync` is called.

```csharp
using LinqToDB.Data;

var rows = db.SetCommand(
        "UPDATE Sales.Orders SET Status = @status WHERE Id = @id",
        new DataParameter("@status", status),
        new DataParameter("@id", id))
    .Execute();
```

For result sets:

```csharp
using LinqToDB.Data;

var rows = db.SetCommand(
        "SELECT Id, Status FROM Sales.Orders WHERE CustomerId = @customerId",
        new DataParameter("@customerId", customerId))
    .Query<OrderSummary>()
    .ToList();
```

`SetCommand` / `CommandInfo` does not create a LINQ query root. Do not add LINQ operators after
`Query<T>()` expecting server-side query composition; the raw command has already been executed by
that terminal method.

## Generated SQL Inspection

Use `ToSqlQuery(...)` when you need to inspect SQL generated by LinqToDB for a LINQ or DML query.
It returns `QuerySql` with `Sql` and `Parameters`.

```csharp
using LinqToDB;

var sql = db.GetTable<Order>()
    .Where(o => o.CustomerId == customerId)
    .ToSqlQuery();

var text       = sql.Sql;
var parameters = sql.Parameters;
```

`ToSqlQuery(...)` is an immediate terminal inspection API. It does not execute the database query,
and it does not create a reusable raw SQL command by itself.

If `SqlGenerationOptions.InlineParameters` is used, the generated SQL text can inline parameter
values as literals. Treat that output as diagnostic SQL text, not as a safe way to build executable
SQL from user values.

## Common Mistakes

### Building SQL by concatenating user values

Wrong:

```csharp
var query = db.FromSql<Order>(
    "SELECT * FROM Sales.Orders WHERE CustomerId = " + customerId);
```

Correct:

```csharp
var query = db.FromSql<Order>(
    $"SELECT * FROM Sales.Orders WHERE CustomerId = {customerId}");
```

or:

```csharp
var query = db.FromSql<Order>(
    "SELECT * FROM Sales.Orders WHERE CustomerId = {0}",
    customerId);
```

### Using `FromSql<T>()` for immediate command execution

Wrong:

```csharp
db.FromSql<Order>("UPDATE Sales.Orders SET Status = {0}", status);
```

Correct:

```csharp
db.SetCommand(
        "UPDATE Sales.Orders SET Status = @status",
        new DataParameter("@status", status))
    .Execute();
```

### Using `SetCommand` when server-side LINQ composition is required

Wrong:

```csharp
var rows = db.SetCommand("SELECT * FROM Sales.Orders")
    .Query<Order>()
    .Where(o => o.IsActive);
```

Correct:

```csharp
var rows = db.FromSql<Order>("SELECT * FROM Sales.Orders")
    .Where(o => o.IsActive);
```

### Treating `QuerySql` as executable command input

Wrong:

```csharp
var generated = query.ToSqlQuery();
db.SetCommand(generated.Sql).Execute();
```

Correct:

```csharp
var generated = query.ToSqlQuery();
var sql        = generated.Sql;
var parameters = generated.Parameters;
```

Use `QuerySql` for inspection unless a task explicitly requires executing generated SQL text and
the parameter handling has been verified.

## API Lookup Anchors

Search `docs/api.md` for:

- `FromSql`
- `FromSqlScalar`
- `RawSqlString`
- `SetCommand`
- `CommandInfo`
- `ToSqlQuery`
- `QuerySql`
- `SqlGenerationOptions`
- `Sql.AliasExpr`

<!-- Generated from: Source/Skills/linq2db/docs/parameters.md -->

# Parameters

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

Use this guide when constructing a typed parameter, reading a stored-procedure output parameter,
or deciding whether a value should be sent to the database as a bound parameter or embedded as a
SQL literal.

## `DataParameter`

Typed factory methods (`DataParameter.Int32(name, value)`, `.VarChar(...)`, `.Guid(...)`, etc.) are
the most direct way to construct a parameter when the target `DataType` is known.

`DataParameter.Create(name, value)` infers `DataType` from the value's C# type, but its defaults do
**not** always match the same-named typed factory:

| Value type | `DataParameter.Create` picks | Not |
|---|---|---|
| `char` / `string` | `DataType.NChar` / `NVarChar` | `Char` / `VarChar` |
| `byte[]` / `Binary` | `DataType.VarBinary` | - |
| `DateTime` | `DataType.DateTime2` | `DateTime` |

`DataType`, `DbType`, `Size`, `Precision`, `Scale` are settable properties on `DataParameter`.

## Output and input-output parameters

```csharp
var input  = DataParameter.Int32("input", 1);
var output = new DataParameter("output", null, DataType.Int32) { Direction = ParameterDirection.Output };
var result = db.ExecuteProc("ExecuteProcIntParameters", input, output);
// output.Value is only populated here, after the call
```

`Direction = ParameterDirection.Output` (or `InputOutput`) marks a parameter for the database to
write back to. The initial `Value` is typically `null`; `DataType` must be set explicitly since
there is no input value to infer it from.

**Deferred-execution gotcha:** `QueryProc<T>`/`QueryProcAsync<T>` return a lazily-enumerated
sequence - `output.Value` stays `null` immediately after the call and is only populated once the
result is actually enumerated (`.ToList()` / `await ... .ToListAsync()`). `ExecuteProc`/
`ExecuteProcAsync` execute immediately, so `output.Value` is available right after the call
returns. Reading an output parameter from a `QueryProc` call before enumerating the result reads a
stale/unset value, not a query failure.

## Forcing a parameter or a literal for a specific value

| API | Scope | Effect |
|---|---|---|
| `.InlineParameters()` (LINQ method on `IQueryable<T>`) | whole query | Deferred, composable - forces parameterizable values in the query to be embedded as SQL literals. |
| `Sql.ToSql(value)` | one expression | Forces server-side evaluation **and** inlines the value as a literal. |
| `Sql.AsSql(value)` | one expression | Forces server-side evaluation; does **not** force inlining - the value may still be sent as a parameter. |
| `Sql.Parameter(value)` | one value | Forces generation of a real bound parameter. |
| `Sql.Constant(value)` | one value | Forces the value to be embedded as a SQL literal, not a parameter. |

`Sql.Parameter`/`Sql.Constant` are the value-level counterparts to `InlineParameters`/`ToSql`:
pick the value, not the whole query, as the scope of control.

```csharp
where q.ParentID == Sql.Parameter(someId)   // real bound parameter - same query plan across values
where q.ParentID == Sql.Constant(someId)    // literal in the SQL text - a new value produces a new query/cache entry
```

## Non-translatable expressions that do not reference query data

A subexpression that cannot be translated to SQL, but also does not reference any query data
(a table column), can be evaluated on the client and sent to SQL as a parameter - this is
intentional, not an error. A subexpression that cannot be translated **and** references query data
throws instead. See [`agent-antipatterns.md`](06-agent-antipatterns-and-ai-tags.md) anti-pattern #4 for the full
rule and examples; this guide does not repeat it.

## Do not invent automatic parameterization/inlining rules

Beyond `InlineParameters`/`ToSql`/`AsSql`/`Sql.Parameter`/`Sql.Constant` and the client-evaluation
rule above, whether linq2db decides to parameterize or inline a given C# value automatically is an
internal translator heuristic that package docs do not expose as a stable, documented contract. If
asked why a specific value appears as a literal or a parameter outside of these APIs, say that the
package docs do not confirm the exact rule - do not invent explanations such as "local variables are
always inlined" or "only captured fields are parameterized."

## Common Mistakes

### Reading an output parameter before enumerating a deferred `QueryProc` result

Wrong:

```csharp
var output = new DataParameter("output", null, DataType.Int32) { Direction = ParameterDirection.Output };
var persons = db.QueryProc<Person>("MyProc", output);
if (output.Value != null) { /* ... */ }   // still null - persons has not been enumerated yet
```

Correct:

```csharp
var output = new DataParameter("output", null, DataType.Int32) { Direction = ParameterDirection.Output };
var persons = db.QueryProc<Person>("MyProc", output).ToList();
if (output.Value != null) { /* ... */ }   // populated after enumeration
```

### Assuming `DataParameter.Create` matches the same-named typed factory

Wrong: using `DataParameter.Create("name", someChar)` and expecting `DataType.Char` to match a
`CHAR` column - `Create` picks `NChar` for `char`/`string` values.

Correct: use the explicit typed factory (`DataParameter.Char(...)`) when the target column type
must match exactly, or pass `DataType` explicitly.

## API Lookup Anchors

Search `docs/api.md` for:

- `DataParameter`
- `ParameterDirection`
- `InlineParameters`
- `Sql.ToSql`
- `Sql.AsSql`
- `Sql.Parameter`
- `Sql.Constant`
