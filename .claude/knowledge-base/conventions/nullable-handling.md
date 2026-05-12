---
area: GLOBAL
kind: convention
sources: [code]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Nullable Handling

## Rule

**`#nullable enable` is the default for every project.** `Directory.Build.props:26` sets `<Nullable>enable</Nullable>` solution-wide. Every file is compiled under NRT unless it opts out. `TreatWarningsAsErrors` is also set (`Directory.Build.props:33`), so nullable warnings are hard errors.

**C# `?` annotations express reference nullability at the API boundary.** The standard `[NotNull]`, `[NotNullWhen]`, `[NotNullIfNotNull]`, `[MaybeNull]` flow attributes from `System.Diagnostics.CodeAnalysis` are used in internal APIs to communicate nullability across call sites the compiler cannot infer.

**SQL NULL semantics are separate from C# nullability.** The `NullabilityContext` class (`Source/LinqToDB/Internal/SqlQuery/NullabilityContext.cs`) tracks whether a SQL expression `CanBeNull` within a specific query context (accounting for outer joins and nullable annotations). `SqlNullabilityExpression` (`Source/LinqToDB/Internal/SqlQuery/SqlNullabilityExpression.cs:9`) wraps any `ISqlExpression` with an explicit nullable annotation overriding inferred nullability.

**The `CompareNulls` mode controls how C# null comparisons translate to SQL.** `Source/LinqToDB/CompareNulls.cs` defines the enum: `LikeClr` (default -- C# semantics, adds `IS NULL` guards), `LikeSql` (SQL three-valued logic, no guards), `LikeSqlExceptParameters` (legacy compat).

**`IsDistinctFrom` / `IsNotDistinctFrom`** are the library SQL-NULL-safe equality operators -- LINQ extension methods translating to `IS [NOT] DISTINCT FROM` on supporting databases and to `IS NULL` expansion on others. Defined in `Source/LinqToDB/Sql/Sql.cs:117-126`.

## Examples

```csharp
// Directory.Build.props:26 -- NRT enabled globally
<Nullable>enable</Nullable>

// Source/LinqToDB/Internal/SqlQuery/NullabilityContext.cs:12
public sealed class NullabilityContext
// Usage: tracks CanBeNull for each ISqlExpression in the current query

// Source/LinqToDB/Internal/SqlQuery/SqlNullabilityExpression.cs:9,14
public sealed class SqlNullabilityExpression : SqlExpressionBase
public SqlNullabilityExpression(ISqlExpression sqlExpression, bool isNullable)
// Wraps an expression to override its nullability annotation in the AST

// Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryVisitor.cs:52
[return: NotNullIfNotNull(nameof(element))]
// Flow attribute on visitor return: callers passing non-null get non-null back

// Source/LinqToDB/Internal/SqlQuery/Visitors/SqlQueryValidatorVisitor.cs:92
public bool IsValidSubQuery(SelectQuery selectQuery, [NotNullWhen(false)] out string? errorMessage)
// [NotNullWhen(false)]: errorMessage is non-null exactly when the method returns false

// Source/LinqToDB/Sql/Sql.cs:117,123
public static bool IsDistinctFrom<T>(this T value, T other) => ...
public static bool IsNotDistinctFrom<T>(this T value, T other) => ...
// SQL-NULL-safe equality LINQ operators

// Source/LinqToDB/CompareNulls.cs:36-42
LikeClr,                    // adds IS NULL guards -- default
LikeSql,                    // pure SQL three-valued logic
LikeSqlExceptParameters,    // legacy compat
```

## SQL NULL vs C# null -- key distinction

C# `string?` means the reference may be null at runtime. SQL `NULL` means unknown value in three-valued logic. These are different: a nullable C# column property does not automatically mean the SQL expression `CanBeNull` in all join configurations (outer joins introduce nullable columns from non-nullable sources). `NullabilityContext.CanBeNull(expr)` resolves this at query-build time.

## See also

- [areas/SQL-AST/INDEX.md](../areas/SQL-AST/INDEX.md) -- `NullabilityContext` and `SqlNullabilityExpression` in the AST area
- `Source/LinqToDB/CompareNulls.cs` -- `CompareNulls` enum with full doc comments
