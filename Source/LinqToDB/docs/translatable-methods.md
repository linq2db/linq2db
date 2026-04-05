# Translatable .NET Methods

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

**`Sql.*` helpers** — authoritative.
All public members of the `Sql` static class that translate to SQL are listed in the
[`Sql.*` helpers](#sql-helpers-sql-specific-functions) section below.
Absence from that table means the method does not exist in the API.

**Standard .NET methods** (String, Math, DateTime, Nullable, type conversions) — confirmed subset.
The tables below list the most commonly used registrations, verified against the base translator
source files. They are not a closed enumeration of every supported overload:
- **Absence from a table does not mean the method is unsupported.**
- To verify a specific method, search for it in the base translator source files:
  - `Source/LinqToDB/Internal/DataProvider/Translation/StringMemberTranslatorBase.cs`
  - `Source/LinqToDB/Internal/DataProvider/Translation/MathMemberTranslatorBase.cs`
  - `Source/LinqToDB/Internal/DataProvider/Translation/DateFunctionsTranslatorBase.cs` (if present)
  - `Source/LinqToDB/Internal/DataProvider/Translation/ConvertMemberTranslatorDefault.cs`
  - `Source/LinqToDB/Internal/DataProvider/Translation/GuidMemberTranslatorBase.cs`
  - Provider-specific translators under `Source/LinqToDB/Internal/DataProvider/<Provider>/Translation/`
- If a method has no registration for the active provider, a `LinqToDBException` is thrown at
  query execution time — not at compile time.

For the `Sql.*` helper API (functions with no standard .NET equivalent) see also the
[`Sql` API reference](https://linq2db.github.io/api/LinqToDB.Sql.html).

---

## Translation rules

- Methods are translated **only when the argument they operate on is a mapped column or a
  server-side expression**. If all arguments can be evaluated on the client (i.e. are local
  variables or constants), LinqToDB evaluates them client-side and passes the result as a
  parameter — no translation occurs and no exception is thrown.
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
| `string.Concat(a, b, ...)` | `a + b + ...` / `CONCAT(...)` |
| `string.Compare(a, b)` | `CASE WHEN a < b THEN -1 ...` |
| `string.Join(sep, source)` | `CONCAT_WS` / emulation |

> `Contains`, `StartsWith`, `EndsWith` accept `StringComparison` overloads; case-sensitivity
> depends on the database collation unless `StringComparison.OrdinalIgnoreCase` is passed, in
> which case LinqToDB emits a case-insensitive comparison where supported.

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

The `Sql` static class exposes functions with no direct .NET equivalent:

| Method | Description |
|---|---|
| `Sql.Between(x, lo, hi)` | `x BETWEEN lo AND hi` |
| `Sql.In(x, set)` | `x IN (...)` |
| `Sql.Like(s, pattern)` | `s LIKE pattern` |
| `Sql.CurrentTimestamp` | Server-side current timestamp (avoids client parameterization) |
| `Sql.GetDate()` | `GETDATE()` / `NOW()` |
| `Sql.IsNull(x, replacement)` | `ISNULL(x, replacement)` / `COALESCE(x, replacement)` |
| `Sql.Coalesce(a, b, ...)` | `COALESCE(a, b, ...)` |
| `Sql.NullIf(x, y)` | `NULLIF(x, y)` |
| `Sql.DateAdd(part, n, dt)` | `DATEADD(part, n, dt)` |
| `Sql.DatePart(part, dt)` | `DATEPART(part, dt)` |
| `Sql.DateDiff(part, start, end)` | `DATEDIFF(part, start, end)` |
| `Sql.MakeDateTime(y, m, d)` | `DATEFROMPARTS(y, m, d)` |
| `Sql.Abs(x)` | `ABS(x)` |
| `Sql.Round(x, digits)` | `ROUND(x, digits)` away-from-zero |
| `Sql.RoundToEven(x, digits)` | `ROUND(x, digits)` banker's rounding |
| `Sql.Power(x, y)` | `POWER(x, y)` |
| `Sql.Concat(a, b, ...)` | `a \|\| b \|\| ...` / `CONCAT(...)` |
| `Sql.Left(s, n)` | `LEFT(s, n)` |
| `Sql.Right(s, n)` | `RIGHT(s, n)` |
| `Sql.Stuff(s, pos, del, ins)` | `STUFF(s, pos, del, ins)` |
| `Sql.PadLeft(s, n, ch)` | `LPAD(s, n, ch)` |
| `Sql.PadRight(s, n, ch)` | `RPAD(s, n, ch)` |
| `Sql.Length(s)` | `LEN(s)` / `LENGTH(s)` |
| `Sql.Reverse(s)` | `REVERSE(s)` |
| `Sql.Upper(s)` / `Sql.Lower(s)` | `UPPER(s)` / `LOWER(s)` |
| `Sql.Trim(s)` | `TRIM(s)` |
| `Sql.CharIndex(sub, s)` | `CHARINDEX(sub, s)` |
| `Sql.Exists(subquery)` | `EXISTS (subquery)` |
| `Sql.AsSql(x)` | Forces server-side evaluation of `x` |
| `Sql.ToSql(x)` | Forces server-side evaluation with inlined literals |

For window / analytic functions (`Sql.Ext.Rank()`, `Sql.Ext.Sum()` etc.) and aggregate
functions (`Sql.StringAggregate`, `Sql.ConcatStrings`) see the
[Window Functions article](https://linq2db.github.io/articles/sql/Window-Functions-(Analytic-Functions).html)
and the [`Sql` API reference](https://linq2db.github.io/api/LinqToDB.Sql.html).

---

## Methods that are NOT translated

The following are evaluated on the **client** side when all inputs are constants or local
variables, but will throw a `LinqToDBException` (or produce unexpected results) when applied
to a mapped column inside a query:

- `string.Format(...)` — use string concatenation or `Sql.Concat` instead
- Regular expressions (`Regex.IsMatch`, etc.) — no SQL equivalent; use `Sql.Like` or
  provider-specific functions via `[Sql.Expression]`
- Collection methods that have no SQL counterpart (e.g. `List<T>.Sort`)
- Any custom method without a `[Sql.Expression]` or `[Sql.Function]` attribute

To add your own translatable methods, apply `[Sql.Expression("...")]` or
`[Sql.Function("...")]` to a static method, or use `[ExpressionMethod]` to substitute a
call with a LINQ expression tree. See `docs/custom-sql.md` for patterns, key properties,
provider-specific overloads, and supported extension points.
For a complete reference see the
[Extensible SQL mapping article](https://linq2db.github.io/articles/sql/Sql-Function.html).
