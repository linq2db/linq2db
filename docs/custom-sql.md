# Custom SQL Translation

This document describes how to map application-defined C# methods and properties to SQL
expressions, functions, and fragments so they translate correctly inside LinqToDB
`IQueryable<T>` expression trees.

---

## Extension points overview

| Attribute | Use case |
|---|---|
| `[Sql.Expression("...")]` | Map to an arbitrary SQL template |
| `[Sql.Function("name")]` | Map to a standard SQL function call |
| `[ExpressionMethod("helper")]` | Replace a method/property with a LINQ expression tree |

All three attributes are in the `LinqToDB` namespace and can be stacked with a
`Configuration` argument to provide provider-specific overloads.

---

## `[Sql.Expression]`

Maps a static C# method or property to an arbitrary SQL template.
Parameters are substituted positionally using `{0}`, `{1}`, etc.

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
| `IsPredicate` | `false` | Expression returns a boolean predicate — omits `= 1` wrapping |
| `IsAggregate` | `false` | Expression is an aggregate function (e.g. `SUM`, `COUNT`) |
| `IsWindowFunction` | `false` | Expression is a window / analytic function |
| `IsPure` | `true` | Same inputs always produce the same output (enables optimizer hints) |

### Argument reordering

Pass explicit `argIndices` to reorder parameters:

```csharp
// Maps to: CONVERT({1}, {0}) — type first, value second
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

## Not supported

- Instance methods on non-entity types annotated with `[Sql.Expression]` or
  `[Sql.Function]` — the target must be `static`.
- `void`-returning methods with `[ExpressionMethod]`.
- Dynamic (runtime-constructed) SQL templates — templates must be compile-time constants.
- `[Sql.Expression]` on types, constructors, or operators directly — use a static wrapper
  method instead.

---

## Further reading

- [`Sql` API reference](https://linq2db.github.io/api/LinqToDB.Sql.html)
- [Extensible SQL mapping article](https://linq2db.github.io/articles/sql/Sql-Function.html)
- [Window / analytic functions](https://linq2db.github.io/articles/sql/Window-Functions-(Analytic-Functions).html)
