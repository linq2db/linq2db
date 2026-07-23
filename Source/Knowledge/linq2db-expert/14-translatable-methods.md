<!-- Generated from: Source/Skills/linq2db/docs/translatable-methods.md -->

# Translatable .NET Methods

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> You are here if you need to:
> - use `String`, `Math`, `DateTime`, or `Nullable` methods inside a LINQ query
> - verify whether a specific .NET method translates to SQL or requires a workaround
> - use `Sql.*` helper functions (e.g. `Sql.CurrentTimestamp`, `Sql.Between`)

This document lists the standard .NET methods and properties that LinqToDB can translate to
SQL when they appear inside an `IQueryable<T>` expression tree.

All entries in this document are grounded in actual translator registrations in the LinqToDB
source code.

## Completeness and verification

This document covers two distinct scopes with different completeness guarantees:

**`Sql.*` helpers** - commonly used subset, not exhaustive.
The [`Sql.*` helpers](#sql-helpers-sql-specific-functions) section below lists the commonly used
public members of the `Sql` static class that translate to SQL, plus explicit call-outs for a few
plausible-looking members that do **not** exist (`Sql.In`, `Sql.IsNull`, `Sql.Coalesce`,
`Sql.Exists`). It does not enumerate niche/metadata helpers (`Sql.Row`, `Sql.Collate`,
`Sql.GroupBy`/`Sql.Grouping`, `Sql.FieldName`/`Sql.TableName` and similar) or the full trig/rounding
overload set. To verify whether a specific `Sql.*` member exists, search `docs/api.md` or
`Source/LinqToDB/Sql/Sql.cs` directly rather than assuming from this table's absence/presence alone.

**Standard .NET methods** (String, Math, DateTime, Nullable, type conversions) - confirmed subset.
The tables below list the most commonly used registrations, verified against source. They are not
a closed enumeration of every supported overload:
- **Absence from a table does not mean the method is unsupported.**
- Method registrations are split across several places, not one file per category - check all of:
  - `Source/LinqToDB/Internal/DataProvider/Translation/StringMemberTranslatorBase.cs`,
    `MathMemberTranslatorBase.cs`, `DateFunctionsTranslatorBase.cs`, `GuidMemberTranslatorBase.cs`
  - `Source/LinqToDB/Internal/DataProvider/Translation/ConvertMemberTranslatorDefault.cs` (`System.Convert.To*`, `Sql.Convert`/`Sql.ConvertTo<>.From` - not `.Parse` methods)
  - `Source/LinqToDB/Linq/Expressions.cs` - the legacy expression map; covers `.Parse` methods (mapped to `Sql.ConvertTo<T>.From`), `Math.Floor`/`Math.Ceiling`/`Math.Truncate`, and other entries not yet migrated to the newer translator classes above
  - `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuildVisitor.cs` - some string methods (e.g. `Contains`/`StartsWith`/`EndsWith`) are hard-coded directly in the expression builder rather than registered in a translator
  - Provider-specific translators under `Source/LinqToDB/Internal/DataProvider/<Provider>/Translation/`
- If a method has no registration for the active provider, a `LinqToDBException` is thrown at
  query execution time - not at compile time.

For the `Sql.*` helper API (functions with no standard .NET equivalent) see also the
[`Sql` API reference](16-xml-doc.md).

---

## Translation rules

- Methods are translated **only when the argument they operate on is a mapped column or a
  server-side expression**. If all arguments can be evaluated on the client (i.e. are local
  variables or constants), LinqToDB evaluates them client-side and passes the result as a
  parameter - no translation occurs and no exception is thrown.
- If a method has no translation for the selected provider, a runtime exception is thrown
  when the query is executed.
- Explicit casts (`(int)`, `(string)`, etc.) are translated via `Sql.ConvertTo<T>`.

---

## String

| .NET expression | SQL equivalent (typical) |
|---|---|
| `s.Length` | `LEN(s)` / `LENGTH(s)` |
| `s.ToUpper()` | `UPPER(s)` |
| `s.ToLower()` | `LOWER(s)` |
| `s.Trim()` | `TRIM(s)` |
| `s.TrimStart()` / `s.TrimStart(ch)` | `LTRIM(s)` |
| `s.TrimEnd()` / `s.TrimEnd(ch)` | `RTRIM(s)` |
| `s.Contains(sub)` | `s LIKE '%sub%'` |
| `s.StartsWith(prefix)` | `s LIKE 'prefix%'` |
| `s.EndsWith(suffix)` | `s LIKE '%suffix'` |
| `s.Replace(old, new)` | `REPLACE(s, old, new)` |
| `s.Substring(start)` | `SUBSTRING(s, start+1, ...)` |
| `s.Substring(start, len)` | `SUBSTRING(s, start+1, len)` |
| `s.IndexOf(sub)` | `CHARINDEX(sub, s) - 1` |
| `s.IndexOf(sub, start)` | `CHARINDEX(sub, s, start+1) - 1` |
| `s.LastIndexOf(sub)` | reverse-CHARINDEX emulation |
| `s.Insert(pos, value)` | `STUFF(s, pos+1, 0, value)` |
| `s.Remove(start)` | `LEFT(s, start)` |
| `s.Remove(start, count)` | `STUFF(s, start+1, count, '')` |
| `s.PadLeft(width)` / `s.PadLeft(width, ch)` | `LPAD(s, width, ch)` (or emulation) |
| `s.PadRight(width)` / `s.PadRight(width, ch)` | `RPAD(s, width, ch)` (or emulation) |
| `s.CompareTo(other)` | `CASE WHEN s < other THEN -1 ...` |
| `string.IsNullOrEmpty(s)` | `s IS NULL OR LEN(s) = 0` |
| `string.IsNullOrWhiteSpace(s)` | provider-specific emulation |
| `string.Concat(a, b, ...)` / `a + b` | provider concat operator/function; LinqToDB preserves C# null-as-empty semantics |
| `string.Compare(a, b)` | `CASE WHEN a < b THEN -1 ...` |
| `string.Join(sep, source)` | `CONCAT_WS` / emulation |

> `Contains`, `StartsWith`, `EndsWith` accept `StringComparison` overloads; case-sensitivity
> depends on the database collation unless `StringComparison.OrdinalIgnoreCase` is passed, in
> which case LinqToDB emits a case-insensitive comparison where supported.

### String concatenation null semantics

Use the XML-doc/API extract to distinguish the two concat APIs:

- C# string concatenation (`a + b`) and `string.Concat(...)` use C# null-as-empty semantics.
  LinqToDB translates those expressions through its string translator and preserves that
  semantic when building SQL.
- `Sql.Concat(...)` is a SQL helper. Its SQL translation uses the provider's native concat
  operator/function and does not normalize per-operand null handling across providers. Its
  in-memory implementation still delegates to `string.Concat(...)`, so client and SQL behavior
  can diverge for null inputs.

For list/aggregate string concatenation, check `Sql.ConcatStrings(...)`,
`Sql.ConcatStringsNullable(...)`, and `string.Join(...)` separately; they are not the same API
as scalar `Sql.Concat(...)`.

---

## Math

| .NET expression | SQL equivalent (typical) |
|---|---|
| `Math.Abs(x)` | `ABS(x)` |
| `Math.Max(x, y)` | `GREATEST(x, y)` / `CASE` emulation |
| `Math.Min(x, y)` | `LEAST(x, y)` / `CASE` emulation |
| `Math.Round(x)` | `ROUND(x, 0)` (banker's rounding via `ROUND_HALF_EVEN` where available) |
| `Math.Round(x, digits)` | `ROUND(x, digits)` |
| `Math.Floor(x)` | `FLOOR(x)` |
| `Math.Ceiling(x)` | `CEILING(x)` / `CEIL(x)` |
| `Math.Pow(x, y)` | `POWER(x, y)` |
| `Math.Truncate(x)` | `TRUNCATE(x, 0)` / `TRUNC(x)` |

`Math.Round` uses .NET's default `MidpointRounding.ToEven` (banker's rounding). Use
`Sql.Round(x, digits)` (away-from-zero) or `Sql.RoundToEven(x, digits)` when you need an
explicit rounding mode.

---

## DateTime and DateTimeOffset

### Properties (translated to `DATEPART` or equivalent)

| .NET member | Date part |
|---|---|
| `.Year` | `YEAR` |
| `.Month` | `MONTH` |
| `.Day` | `DAY` |
| `.Hour` | `HOUR` |
| `.Minute` | `MINUTE` |
| `.Second` | `SECOND` |
| `.Millisecond` | `MILLISECOND` |
| `.DayOfYear` | `DAYOFYEAR` |
| `.DayOfWeek` | `WEEKDAY` |
| `.Date` | truncation to date (time zeroed out) |
| `.TimeOfDay` | time component extraction |

### Methods

| .NET expression | SQL equivalent (typical) |
|---|---|
| `dt.AddYears(n)` | `DATEADD(YEAR, n, dt)` |
| `dt.AddMonths(n)` | `DATEADD(MONTH, n, dt)` |
| `dt.AddDays(n)` | `DATEADD(DAY, n, dt)` |
| `dt.AddHours(n)` | `DATEADD(HOUR, n, dt)` |
| `dt.AddMinutes(n)` | `DATEADD(MINUTE, n, dt)` |
| `dt.AddSeconds(n)` | `DATEADD(SECOND, n, dt)` |
| `dt.AddMilliseconds(n)` | `DATEADD(MILLISECOND, n, dt)` |
| `new DateTime(y, m, d)` | `DATEFROMPARTS(y, m, d)` / equivalent |
| `new DateTime(y, m, d, h, min, s)` | `DATETIMEFROMPARTS(...)` / equivalent |

### Static members

| .NET expression | SQL equivalent |
|---|---|
| `DateTime.Now` | server-current-timestamp (via `Sql.GetDate()`) |
| `DateTime.UtcNow` | server-UTC-timestamp |
| `DateTimeOffset.Now` | server-current-timestamp |
| `DateTimeOffset.UtcNow` | server-UTC-timestamp |

---

## DateOnly (net6+)

`DateOnly` properties and constructor forms parallel `DateTime` above:
`.Year`, `.Month`, `.Day`, `.DayOfYear`, `.DayOfWeek`, `.AddYears`, `.AddMonths`, `.AddDays`,
`new DateOnly(y, m, d)`.

Support varies by provider; some providers do not have a native `DATE` type and emulate it.

---

## Nullable

| .NET expression | SQL equivalent |
|---|---|
| `v.HasValue` | `v IS NOT NULL` |
| `v.Value` | `v` (direct unwrap) |
| `v == null` | `v IS NULL` |
| `v != null` | `v IS NOT NULL` |

---

## Type conversions

Explicit C# casts inside an expression tree are translated using the provider's `CAST` /
`CONVERT` mechanism:

```csharp
// Translated to CAST(p.Price AS INT) or CONVERT(INT, p.Price)
from p in db.Products
select (int)p.Price
```

`Sql.ConvertTo<TTarget>.From(value)` is the explicit SQL-cast helper when you need to control
the output type precisely.

Parse methods (`int.Parse(s)`, `decimal.Parse(s)`, `DateTime.Parse(s)`, etc.) are translated
to the equivalent `Sql.ConvertTo<T>.From(s)` cast.

---

## Sql.* helpers (SQL-specific functions)

The `Sql` static class exposes functions with no direct .NET equivalent. This table lists the
commonly used ones - it is **not** a closed enumeration; for niche/metadata helpers (`Sql.Row`,
`Sql.Collate`, `Sql.GroupBy`/`Sql.Grouping`, `Sql.FieldName`/`Sql.TableName` and friends) search
`docs/api.md` or `Source/LinqToDB/Sql/Sql.cs` directly.

> `Sql.In`, `Sql.IsNull`, `Sql.Coalesce`, and `Sql.Exists` do **not** exist as members of `Sql` -
> do not write code that calls them. Use the real equivalents instead:
> - `x IN (...)` → `value.In(set)` / `value.NotIn(set)` (extension methods on `LinqToDB.SqlExtensions`, not `Sql`)
> - `ISNULL`/`COALESCE` → the C# `??` operator translates directly to SQL `COALESCE`
> - `EXISTS (subquery)` → `.Any()` on a subquery translates to `EXISTS`

| Method | Description |
|---|---|
| `Sql.Between(x, lo, hi)` | `x BETWEEN lo AND hi` |
| `Sql.Like(s, pattern)` | `s LIKE pattern` |
| `Sql.CurrentTimestamp` | Server-side current timestamp (avoids client parameterization) |
| `Sql.CurrentTimestampUtc` | Server-side current UTC timestamp |
| `Sql.GetDate()` | `GETDATE()` / `NOW()` |
| `Sql.NullIf(x, y)` | `NULLIF(x, y)` |
| `Sql.DateAdd(part, n, dt)` | `DATEADD(part, n, dt)` |
| `Sql.DatePart(part, dt)` | `DATEPART(part, dt)` |
| `Sql.DateDiff(part, start, end)` | `DATEDIFF(part, start, end)` |
| `Sql.MakeDateTime(y, m, d)` | `DATEFROMPARTS(y, m, d)` |
| `Sql.Abs(x)` | `ABS(x)` |
| `Sql.Round(x, digits)` | `ROUND(x, digits)` away-from-zero |
| `Sql.RoundToEven(x, digits)` | `ROUND(x, digits)` banker's rounding |
| `Sql.Power(x, y)` | `POWER(x, y)` |
| `Sql.Sqrt(x)`, `Sql.Exp(x)`, `Sql.Log(x)` / `Sql.Log(newBase, x)`, `Sql.Log10(x)` | Standard math functions |
| `Sql.Ceiling(x)`, `Sql.Floor(x)`, `Sql.Truncate(x)`, `Sql.Sign(x)` | Rounding/sign functions |
| `Sql.Sin(x)`, `Sql.Cos(x)`, `Sql.Tan(x)`, `Sql.Cot(x)`, `Sql.Asin(x)`, `Sql.Acos(x)`, `Sql.Atan(x)`, `Sql.Atan2(x, y)`, `Sql.Sinh(x)`, `Sql.Cosh(x)`, `Sql.Tanh(x)`, `Sql.Degrees(x)` | Trigonometric functions |
| `Sql.Concat(a, b, ...)` | provider native concat operator/function; SQL null handling follows provider rules |
| `Sql.Substring(s, start, length)` | `SUBSTRING(s, start, length)` |
| `Sql.Replace(s, old, new)` | `REPLACE(s, old, new)` |
| `Sql.Left(s, n)` | `LEFT(s, n)` |
| `Sql.Right(s, n)` | `RIGHT(s, n)` |
| `Sql.Stuff(s, pos, del, ins)` | `STUFF(s, pos, del, ins)` |
| `Sql.PadLeft(s, n, ch)` | `LPAD(s, n, ch)` |
| `Sql.PadRight(s, n, ch)` | `RPAD(s, n, ch)` |
| `Sql.TrimLeft(s[, ch])` / `Sql.TrimRight(s[, ch])` | `LTRIM(s)` / `RTRIM(s)` |
| `Sql.Length(s)` | `LEN(s)` / `LENGTH(s)` |
| `Sql.Reverse(s)` | `REVERSE(s)` |
| `Sql.Upper(s)` / `Sql.Lower(s)` | `UPPER(s)` / `LOWER(s)` |
| `Sql.Trim(s)` | `TRIM(s)` |
| `Sql.CharIndex(sub, s)` | `CHARINDEX(sub, s)` |
| `Sql.NewGuid()` / `Sql.NewGuid7()` | New random GUID / UUIDv7 |
| `Sql.Convert<TTo,TFrom>(...)` / `Sql.ConvertTo<TTo>.From(value)` | Explicit `CAST`/`CONVERT` - see [Type conversions](#type-conversions) above |
| `Sql.TryConvert<TTo,TFrom>(...)` / `Sql.TryConvertOrDefault<TTo,TFrom>(...)` | `CAST`/`CONVERT` variants that return `null`/a default instead of throwing on failure |
| `Sql.AsSql(x)` | Forces server-side evaluation of `x` |
| `Sql.ToSql(x)` | Forces server-side evaluation with inlined literals |

For nullability control (`Sql.AsNotNull`, `Sql.AsNullable`, `Sql.ToNullable`, `Sql.IsDistinctFrom`,
etc.) see [`docs/null-semantics.md`](10-query-composition.md). For forcing a value to a bound parameter
vs. a SQL literal (`Sql.Parameter`, `Sql.Constant`, `InlineParameters`) see
[`docs/parameters.md`](13-extensions.md).

For window / analytic functions (`Sql.Ext.Rank()`, `Sql.Ext.Sum()` etc.) and aggregate
functions (`Sql.StringAggregate`, `Sql.ConcatStrings`) see the
[Window Functions article](https://linq2db.github.io/articles/sql/Window-Functions-(Analytic-Functions).html)
and the [`Sql` API reference](16-xml-doc.md).

---

## Methods that are NOT translated

The following are evaluated on the **client** side when all inputs are constants or local
variables, but will throw a `LinqToDBException` (or produce unexpected results) when applied
to a mapped column inside a query:

- `string.Format(...)` - use string concatenation or `Sql.Concat` instead
- Regular expressions (`Regex.IsMatch`, etc.) - no SQL equivalent; use `Sql.Like` or
  provider-specific functions via `[Sql.Expression]`
- Collection methods that have no SQL counterpart (e.g. `List<T>.Sort`)
- Any custom method without a `[Sql.Expression]` or `[Sql.Function]` attribute

To add your own translatable methods, apply `[Sql.Expression("...")]` or
`[Sql.Function("...")]` to a static method, or use `[ExpressionMethod]` to substitute a
call with a LINQ expression tree. See `docs/extensions.md` for patterns, key properties,
provider-specific overloads, and supported extension points.
For a complete reference see the
[Extensible SQL mapping article](https://linq2db.github.io/articles/sql/Sql-Function.html).
