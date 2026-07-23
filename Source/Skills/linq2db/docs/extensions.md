# Extension Mechanisms

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](../SKILL.md).
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
directly, or inspect generated SQL text, use [`raw-sql.md`](raw-sql.md) instead.

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
[`docs/hints.md`](hints.md), [`docs/hints-api-map.md`](hints-api-map.md), and the provider
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

- [`Sql` API reference](https://linq2db.github.io/api/LinqToDB.Sql.html)
- [Extensible SQL mapping article](https://linq2db.github.io/articles/sql/Sql-Function.html)
- [Window / analytic functions](https://linq2db.github.io/articles/sql/Window-Functions-(Analytic-Functions).html)
