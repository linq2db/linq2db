# Null Semantics

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](../SKILL.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

Use this guide when the SQL generated for a null comparison looks more complex than the LINQ
expression that produced it, or when deciding how to compare nullable values.

## Baseline fact

A literal `null` in a query always compiles to `IS NULL` / `IS NOT NULL`, regardless of any
setting below.

```csharp
db.Person.Where(p => p.MiddleName == null)
```

```sql
SELECT ... FROM Person WHERE MiddleName IS NULL
```

## The controlling option: `CompareNulls`

`LinqOptions.CompareNulls` (configure via `DataOptions.UseCompareNulls(CompareNulls)`, or
`LinqOptions.WithCompareNulls(CompareNulls)` if building a `LinqOptions` value directly) decides how
`==`/`!=`/`Contains` behave when a *non-literal* nullable value is involved. Do not use the
obsolete `DataOptions.UseCompareNullsAsValues(bool)` - `true` maps to `LikeClr`, `false` to
`LikeSqlExceptParameters`; it is scheduled for removal in v7.

| Value | Behavior |
|---|---|
| `CompareNulls.LikeClr` (**default**) | C# equality semantics: two nulls compare equal. Can add `OR (... IS NULL AND ... IS NULL)` to preserve this - see below. May prevent index usage on the affected predicate. |
| `CompareNulls.LikeSql` | Straight translation to SQL operators; nulls compare as `UNKNOWN` (three-valued logic), matching raw SQL semantics. Parameter values are **not** sniffed for null. |
| `CompareNulls.LikeSqlExceptParameters` | Same as `LikeSql`, except a null-valued parameter still compiles to `IS NULL`. Kept for pre-6.0 backward compatibility; prefer `LikeSql` for new code. |

## Why a null-valued parameter becomes `IS NULL`

Under the default `LikeClr` (and under `LikeSqlExceptParameters`), linq2db inspects a captured
variable's runtime value for `Equal`/`NotEqual` comparisons. If it is `null` at query-build time,
the comparison compiles to `IS NULL` instead of `= @p`:

```csharp
string? name = null;
db.Person.Where(p => p.MiddleName == name)
```

```sql
SELECT ... FROM Person WHERE MiddleName IS NULL
```

This is intentional: `@p = NULL` in raw SQL evaluates to `UNKNOWN`, never `TRUE` - without this
sniffing, the query would silently match zero rows whenever the parameter happened to be null.
Under plain `LikeSql`, this sniffing does not happen; the comparison stays `= @p` and inherits
standard SQL `UNKNOWN` behavior for a null parameter.

## Why two nullable columns can generate `OR (... IS NULL AND ... IS NULL)`

C# `a == b` treats `null == null` as `true`. Plain SQL `a = b` treats `NULL = NULL` as `UNKNOWN`
(never true). Under the default `LikeClr`, linq2db closes this gap - but only when **both** sides
of the comparison are independently classified as nullable. If only one side can be null (or
neither), the simpler form is used instead.

```csharp
from e1 in db.Parent
from e2 in db.Parent
where e1.Value1 == e2.Value1   // both Value1 columns are nullable
select e1
```

```sql
... WHERE e1.Value1 = e2.Value1 OR (e1.Value1 IS NULL AND e2.Value1 IS NULL)
```

## Manual control

Use these when the default `CompareNulls.LikeClr` expansion is unwanted for a specific comparison,
without changing the setting globally.

| API | Effect |
|---|---|
| `Sql.AsNotNull(value)` / `Sql.AsNotNullable(value)` | Marks one side of a comparison as non-nullable for nullability analysis. Since the `OR (... IS NULL AND ...)` expansion only fires when **both** sides are classified nullable, marking either side non-nullable makes the comparison use the simpler form. |
| `.IsDistinctFrom(other)` / `.IsNotDistinctFrom(other)` | Extension methods mapping to SQL `IS [NOT] DISTINCT FROM` (or its provider-specific equivalent) - a null-safe comparison for one specific expression, without touching the `CompareNulls` setting at all. |
| `Sql.ToNullable(value)` (value types only) | Widens `T` to `T?` **as a real C# type change**, so the expression can be compared to `null` or assigned to a nullable-typed slot. Use this when the column's C# type will not otherwise let you write `== null`. |
| `Sql.ToNotNull(value)` / `Sql.ToNotNullable(value)` (value types only) | The reverse narrowing, `T?` to `T`. |
| `Sql.AsNullable(value)` | Annotates SQL-level nullability **without changing the C# type** (`T` in, `T` out - unlike `ToNullable`, which returns `T?`). Rarely needed directly; prefer `ToNullable` when you need the C# type itself to become nullable. |

### `AsNotNull` example

```csharp
// Wrong - relies on the default LikeClr expansion, unclear intent:
from p1 in db.Parent
from p2 in db.Parent
where p1.Value1 == p2.Value1
select p1;

// Correct - explicit that a null on either side should not match, simpler generated SQL:
from p1 in db.Parent
from p2 in db.Parent
where Sql.AsNotNull(p1.Value1) == Sql.AsNotNull(p2.Value1)
select p1;
```

The second form is equivalent to filtering with `p1.Value1 != null && p1.Value1 == p2.Value1` on
the client - either operand being null excludes the row, matching plain SQL equality.

## Common Mistakes

### Assuming the extra `OR (... IS NULL AND ...)` is a bug or an inefficiency to "fix" by rewriting the query

Wrong:

```csharp
// "Simplifying" a nullable comparison because the generated SQL looks redundant
where e1.Value1 == e2.Value1 && e1.Value1 != null
```

Correct: recognize this is `CompareNulls.LikeClr` deliberately preserving C# null-equality
semantics. If SQL `UNKNOWN`-based semantics are actually wanted, use `Sql.AsNotNull` on the
specific comparison, `IsDistinctFrom`/`IsNotDistinctFrom`, or set `CompareNulls.LikeSql` for the
whole query context - do not assume the current SQL is wrong.

### Switching to `CompareNulls.LikeSql` without checking parameter-null handling

Wrong: changing `CompareNulls` to `LikeSql` globally to get simpler SQL, without checking whether
any existing `== someNullableVariable` comparison depended on the default sniffing to match null
values via `IS NULL`.

Correct: under `LikeSql`, a null-valued parameter compiles to `= @p` with standard SQL `UNKNOWN`
semantics (never matches). Audit comparisons against nullable captured variables before switching
away from `LikeClr`/`LikeSqlExceptParameters`, or use `Sql.AsNotNull`/`IsDistinctFrom` per
comparison instead of a global setting change.

## API Lookup Anchors

Search `docs/api.md` for:

- `CompareNulls`
- `LikeClr`
- `LikeSql`
- `LikeSqlExceptParameters`
- `WithCompareNulls`
- `AsNotNull`
- `AsNotNullable`
- `AsNullable`
- `ToNullable`
- `ToNotNull`
- `ToNotNullable`
- `IsDistinctFrom`
- `IsNotDistinctFrom`
