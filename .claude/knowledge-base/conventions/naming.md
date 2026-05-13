---
area: GLOBAL
kind: convention
sources: [code]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Naming Conventions

## Rule

**SQL AST node types** carry a `Sql` prefix: `SqlSelectClause`, `SqlWhereClause`, `SqlPredicate.ExprExpr`, `SqlNullabilityExpression`, etc. All live in the `LinqToDB.Internal.SqlQuery` namespace. See [code-design.md](../../docs/code-design.md) for the SQL AST namespace placement invariant.

**Internal namespace** prefix `LinqToDB.Internal.*` marks types that are library infrastructure, not stable public API. The convention is namespace-based -- there is no `[InternalsVisibleTo]` gate. Examples: `LinqToDB.Internal.SqlQuery`, `LinqToDB.Internal.SqlProvider`, `LinqToDB.Internal.DataProvider.*`.

**Data-provider implementation classes** use the pattern `<Database>DataProvider` and extend `DynamicDataProviderBase<T>`: `AccessDataProvider`, `ClickHouseDataProvider`, `DB2DataProvider`, `PostgreSQLDataProvider`. Dialect variants are registered as `sealed` subclasses with no additional public members (e.g. `DB2LUWDataProvider`, `DB2zOSDataProvider`).

**SQL builder classes** use `<Database>SqlBuilder`: `ClickHouseSqlBuilder`, `SqlServerSqlBuilder`, `YdbSqlBuilder`. They extend `BasicSqlBuilder` or `BasicSqlBuilder<TOptions>`.

**Provider adapter classes** use `<Database>ProviderAdapter` and implement `IDynamicProviderAdapter`: `AccessProviderAdapter`, `ClickHouseProviderAdapter`, `DB2ProviderAdapter`.

**Test fixtures** follow `<Topic>Tests` (e.g. `ConvertTests`, `DisposeTests`, `MergeTests`) and inherit `TestBase` or `DataProviderTestBase`. A class testing obsoleted API is itself marked `[Obsolete]` and named `Old<Topic>Tests`.

**Legacy infrastructure** inside `Source/LinqToDB/` that bridges old and new models carries a `Legacy` prefix: `LegacyMergeExtensions`, `LegacyMemberConverterBase`, `LegacyVisitorBase`.

## Examples

```
// Source/LinqToDB/Internal/SqlQuery/SqlSelectClause.cs:15
public sealed class SqlSelectClause : ClauseBase, IQueryElement

// Source/LinqToDB/Internal/SqlQuery/SqlPredicate.cs:230
public sealed class ExprExpr : Expr          // nested inside SqlPredicate

// Source/LinqToDB/Internal/SqlQuery/SqlNullabilityExpression.cs:9
public sealed class SqlNullabilityExpression : SqlExpressionBase

// Source/LinqToDB/Internal/DataProvider/DB2/DB2DataProvider.cs:19-20
sealed class DB2LUWDataProvider : DB2DataProvider { ... }
sealed class DB2zOSDataProvider : DB2DataProvider { ... }

// Source/LinqToDB/Internal/DataProvider/ClickHouse/ClickHouseDataProvider.cs:26-28
sealed class ClickHouseOctonicaDataProvider : ClickHouseDataProvider { ... }
sealed class ClickHouseDriverDataProvider   : ClickHouseDataProvider { ... }
sealed class ClickHouseMySqlDataProvider    : ClickHouseDataProvider { ... }

// Source/LinqToDB/Data/LegacyMergeExtensions.cs:17
public static class LegacyMergeExtensions   // obsoleted merge API bridge

// Tests/Linq/Update/OldMergeTests.cs:19
[Obsolete(...)]
public class OldMergeTests : TestBase       // tests for the obsoleted API
```

## Counter-examples

`SqlFrameClause` still lives under `LinqToDB.SqlQuery` (public namespace) -- pre-existing technical debt. New AST types must NOT follow this pattern. See [code-design.md](../../docs/code-design.md) for the namespace placement rule.

## See also

- [code-design.md](../../docs/code-design.md) -- SQL AST namespace placement invariant
- [conventions/public-api-discipline.md](public-api-discipline.md) -- what `LinqToDB.Internal.*` means for API stability
- [conventions/legacy-patterns.md](legacy-patterns.md) -- `Legacy*` / `Old*` naming in context
