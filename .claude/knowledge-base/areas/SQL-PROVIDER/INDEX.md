---
area: SQL-PROVIDER
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-06-14
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
coverage_tier_1: 4/4
coverage_tier_2: 47/47
---

# SQL-PROVIDER

Provider-agnostic SQL emission and statement-level optimization. Lives under `Source/LinqToDB/Internal/SqlProvider/` (the build/optimize machinery -- `LinqToDB.Internal.SqlProvider` namespace) and `Source/LinqToDB/Sql/` (the legacy public `Sql.*` static class -- namespace `LinqToDB`). Together these define the contract between the [SQL-AST](../SQL-AST/INDEX.md) (input) and the per-provider subclasses under `Internal/DataProvider/<Provider>/` (output).

The two anchor abstractions are `ISqlBuilder` (turns a `SqlStatement` into provider-specific SQL text) and `ISqlOptimizer` (rewrites a `SqlStatement` into a shape the builder can emit). Each in-tree provider ships exactly one concrete `<Provider>SqlBuilder : BasicSqlBuilder` and one `<Provider>SqlOptimizer : BasicSqlOptimizer`, registered via `IDataProvider.CreateSqlBuilder` (`Source/LinqToDB/Internal/DataProvider/DataProviderBase.cs:123`). The dialect override mechanism is plain virtual-method inheritance -- `BasicSqlBuilder` exposes ~120 `protected virtual` hooks (sentence prefixes like `BuildXxx`, `IsXxxSupported`, `GetXxxName`) that subclasses override surgically.

`Source/LinqToDB/Sql/` is the user-facing surface: `public static partial class Sql` (`Source/LinqToDB/Sql/Sql.cs:27`) hosts every server-side helper a query can call (`Sql.AsSql`, `Sql.AsNullable`, `Sql.Between`, `Sql.Collate`, `Sql.GroupBy.Rollup`, the `Sql.DateAdd` family, the `Sql.Row(...)` constructors, ...) plus the attribute hierarchy used by user code to declare custom translations (`Sql.ExpressionAttribute`, `Sql.FunctionAttribute`, `Sql.ExtensionAttribute`, `Sql.PropertyAttribute`, `Sql.TableFunctionAttribute`, `Sql.TableExpressionAttribute`, `Sql.QueryExtensionAttribute`).

## Key types

- **`ISqlBuilder`** (`Source/LinqToDB/Internal/SqlProvider/ISqlBuilder.cs:11`) -- the emission contract. Entry point is `BuildSql(commandNumber, statement, sb, optimizationContext, aliases, nullabilityContext, startIndent)` (`ISqlBuilder.cs:15`); auxiliary surface includes `BuildObjectName`, `Convert(sb, value, ConvertType)`, `BuildExpression`, `GetIdentityExpression`, `PrintParameters`, `ApplyQueryHints`, `GetReserveSequenceValuesSql`, `GetMaxValueSql`. Carries five state properties: `Name`, `MappingSchema`, `StringBuilder`, `SqlProviderFlags`, `TableIDs`. The `CommandCount(SqlStatement)` overload lets a provider emit multiple SQL statements per logical operation.

- **`ISqlOptimizer`** (`Source/LinqToDB/Internal/SqlProvider/ISqlOptimizer.cs:7`) -- the rewrite contract. Three entry points: `Finalize`, `IsParameterDependent`, `ConvertSkipTake`. Two factory methods (`CreateOptimizerVisitor`, `CreateConvertVisitor`) and `CreateSqlExpressionFactory` let providers swap visitor subclasses.

- **`BasicSqlBuilder`** (`Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs:26`) -- abstract default implementation, ~4650 lines, partial across `BasicSqlBuilder.cs` and `BasicSqlBuilder.Merge.cs`. Walks the AST clause-by-clause via the `Step` enum (`BasicSqlBuilder.cs:4048`). Statement dispatch happens in `BuildSqlImpl` (`BasicSqlBuilder.cs:367`) -- a switch over `Statement.QueryType` calls `BuildSelectQuery`, `BuildDeleteQuery`, `BuildUpdateQuery`, `BuildInsertQuery`, `BuildInsertOrUpdateQuery`, `BuildCreateTableStatement`, etc. Critical hook properties: `OpenParens`, `Comma`, `InlineComma`, `IsRecursiveCteKeywordRequired`, `IsCteColumnListSupported`, `SupportsMaterializedCteHint`, `WrapJoinCondition`, `IsNestedJoinSupported`, `CteFirst`, `IsOverRequiredWithinGroup`. Hooks for sub-clauses to override: `BuildSelectClause`, `BuildColumns`, `BuildFromClause`, etc. plus `protected abstract ISqlBuilder CreateSqlBuilder()` -- every provider overrides this to spawn a fresh same-typed builder for sub-queries.

  **Delta (PR #5504):** `BasicSqlBuilder` gains the concat-emission subsystem:
  - `ConcatBuildStyle` enum (`BasicSqlBuilder.cs:3874`) -- three values: `Plus` (`a + b`; SQL Server pre-2025, SqlCe, Access), `Pipes` (`a || b`; ANSI standard -- PostgreSQL, Oracle, SQLite, DB2, Firebird, Informix, SAP HANA, DuckDB, Sybase ASE, SQL Server 2025+), `Function` (`CONCAT(a, b, c)`; MySQL, ClickHouse).
  - `protected virtual ConcatBuildStyle ConcatStyle` (`BasicSqlBuilder.cs:3888`) -- defaults to `Plus`. Provider subclasses override to select their style.
  - `protected virtual string ConcatFunctionName` (`BasicSqlBuilder.cs:3894`) -- defaults to `"CONCAT"`; overridable for case-sensitive dialects (e.g. ClickHouse `concat`).
  - `protected virtual void BuildSqlConcatExpression(SqlConcatExpression element)` (`BasicSqlBuilder.cs:3896`) -- the primary emission hook; dispatches on `ConcatStyle` to either `BuildSqlConcatOperatorChain` (for `Plus`/`Pipes`) or `BuildSqlConcatFunctionCall` (for `Function`). Override this only when none of the three styles fits.
  - Expression dispatch: `QueryElementType.SqlConcat` routes to `BuildSqlConcatExpression` at `BasicSqlBuilder.cs:3243-3245`.

- **`BasicSqlBuilder<T>`** (`Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder{T}.cs:8`) -- typed-options facade. `T : DataProviderOptions<T>, IOptionSet, new()`.

- **`BasicSqlOptimizer`** (`Source/LinqToDB/Internal/SqlProvider/BasicSqlOptimizer.cs:21`) -- abstract default optimizer, ~2260 lines. The `Finalize` pipeline (line 51) runs in fixed order: `FixEmptySelect` -> `FinalizeCte` -> `SqlNullsOrderingLoweringVisitor.LowerNullsOrdering` (when `!IsNullsOrderingSupported`) -> `OptimizeQueries` -> conditional `JoinsOptimizer.Optimize` -> `FinalizeInsert` -> `FinalizeSelect` -> `FixSetOperationValues` -> `FinalizeStatement` (provider hook). The `Alternative Builders` region (line 820) holds reusable rewrite helpers (`GetAlternativeDelete`, `RemoveUpdateTableIfPossible`, `NeedsEnvelopingForUpdate`, `ReplaceTakeSkipWithRowNumber`).

- **`SqlProviderFlags`** (`Source/LinqToDB/Internal/SqlProvider/SqlProviderFlags.cs:19`) -- `[DataContract]`, ~72 mutable bool/enum fields. `[DataMember(Order = N)]` is mandatory because remoting (gRPC/WCF/HTTP) round-trips this object. The `CustomFlags` set is a string-keyed escape hatch.

  **Delta (NULLS ordering flags):** Two new `[DataMember]` fields: `IsNullsOrderingSupported` (Order=69, bool, default false, `SqlProviderFlags.cs:604`) -- native NULLS FIRST/NULLS LAST vs CASE emulation; and `DefaultNullsOrdering` (Order=70, NullsDefaultOrdering, default Unknown, `SqlProviderFlags.cs:614`) -- natural null placement in ORDER BY. ABI-additive: old payloads deserialize with safe defaults.

- **`OptimizationContext`** (`Source/LinqToDB/Internal/SqlProvider/OptimizationContext.cs:13`) -- bag passed from LINQ pipeline into SQL builder. Carries `EvaluationContext`, `DataOptions`, `SqlProviderFlags`, `MappingSchema`, the two visitor instances, a `Factory` (`ISqlExpressionFactory`), parameter-dedup map, `SuggestDynamicParameter` cache. The two `OptimizeAndConvert*` methods drive the two-phase optimize-then-convert pipeline.

- **`SqlExpressionOptimizerVisitor`** -- provider-agnostic shape-preserving rewrites. ~2185 lines.

  **Delta (PR #5502):** `VisitSqlCoalesceExpression` (`SqlExpressionOptimizerVisitor.cs:1416`) gains two rewrites in one pass: (1) **Nested coalesce flattening** -- when an operand is itself a `SqlCoalesceExpression` (possibly in `SqlNullabilityExpression`), its children are inlined, eliminating `COALESCE(COALESCE(a,b),c)` -> `COALESCE(a,b,c)` shapes. (2) **Early-termination** -- when a mid-chain expression is provably non-nullable and not the last, the chain is truncated. Eliminates spurious `COALESCE(agg_subquery, 0)` patterns.

- **`SqlExpressionConvertVisitor`** -- provider-specific `IQueryElement` transforms. ~2105 lines.

  **Delta (PR #5504):** Gains the concat-lowering subsystem:
  - `protected virtual bool ConcatRequiresExplicitStringCast` (`SqlExpressionConvertVisitor.cs:1259`) -- when `true` (default), wraps every non-string operand in an explicit `CAST(... AS VARCHAR(N))` before the concat chain. Needed for `+` providers (SQL Server pre-2025, SqlCe, Access) where SQL data-type precedence would otherwise coerce string operands to the non-string type. `||` and `CONCAT(...)` providers override this to `false`. **Sybase ASE exception** (`SqlExpressionConvertVisitor.cs:1289`): Sybase ASE emits `||` but keeps `ConcatRequiresExplicitStringCast = true` because ASE requires explicit `convert()` for non-character operands.
  - `public virtual ISqlExpression ConvertConcat(SqlConcatExpression element)` (`SqlExpressionConvertVisitor.cs:1261`) -- the per-element lowering called from `VisitSqlConcatExpression`. Pipeline: (1) single-operand identity pass; (2) `FlattenNestedConcat` -- flattens same-`PreserveNull`-semantic nested `SqlConcatExpression` children (addresses the `string + string + string` arrives as `Concat(Concat(a,b),c)` pattern from `TranslateBinaryStringConcat`); (3) per-operand cast-to-string when `ConcatRequiresExplicitStringCast`; (4) per-nullable-operand `Coalesce(item, '')` wrap when `!element.PreserveNull` (null-as-empty semantic). Idempotence is guarded by `IsConcatCoalesceWrap` which detects four shape variants the optimizer can produce from the same logical wrap.
  - `VisitSqlConcatExpression` (`SqlExpressionConvertVisitor.cs:1170`) -- override that calls `ConvertConcat` and re-enters `Visit` on any replacement.
  - `RemoveNullValues(SqlCoalesceExpression element)` (`SqlExpressionConvertVisitor.cs:1247`, protected) -- strips NULL-literal operands before `ConvertCoalesce` folds. Prevents Informix `Nvl(x, NULL)` / Access `IIF(x IS NULL, NULL, x)` artifacts (issue #5531).

- **`JoinsOptimizer`** -- sealed; runs only when `LinqOptions.OptimizeJoins` is on. Two passes: `RemoveUnusedLeftJoins`, `RemoveDuplicateJoins`. Static `UnnestJoins` flattens nested JOINs.

- **`SqlExpressionFactory`** -- minimal default `ISqlExpressionFactory`.

- **`ConvertType`** -- enum tagging *what kind of identifier* an `ISqlBuilder.Convert` call is escaping.

- **`Sql.cs`** (`Source/LinqToDB/Sql/Sql.cs:27`) -- the public `Sql` static class. Namespace: `LinqToDB`, not `LinqToDB.Sql`. The class is `partial` and split across ~28 files by topic.

- **`Sql.ExpressionAttribute`** + subclasses (`Sql.PropertyAttribute`, `Sql.FunctionAttribute`, `Sql.ExtensionAttribute`, `Sql.TableFunctionAttribute`, `Sql.TableExpressionAttribute`) -- foundational attribute hierarchy.

- **`Sql.QueryExtensionAttribute`** + **`Sql.QueryExtensionScope`** -- declare query-shape extensions. Four built-in builders: `HintExtensionBuilder`, `HintWithParameterExtensionBuilder`, `HintWithParametersExtensionBuilder`, `HintWithFormatParametersExtensionBuilder`.

- **`Sql.SqlID`** -- typed `(SqlIDType, string)` pair for hint extensions to refer back to a tagged table.

- **`Sql.AggregateModifier` / `From` / `Nulls` / `NullsPosition`** -- nominal enums for window-function clauses.

- **`Sql.IsNullableType`** -- nullability propagation rules for `[Expression]` decorations.

- **`Sql.CurrentTimestampUtc`** (`Source/LinqToDB/Sql/Sql.cs:1184`) -- **new `DateTime` property added in PR #5467**. No `[Function]`/`[Property]`/`[ServerSideOnly]` attribute decoration; translation is entirely translator-driven via `DateFunctionsTranslatorBase.TranslateUtcNow` in the EXPR-TRANS layer. Client-side fallback returns `DateTime.UtcNow`. Providers without a `TranslateUtcNow` override silently fall through to client-side evaluation -- see Known issues.

- **`Sql.CurrentTzTimestamp`** (`Source/LinqToDB/Sql/Sql.cs:1193-1198`) -- **new `DateTimeOffset` property added in PR #5467**. Attribute-decorated with per-provider mappings: `[Function(PN.SqlServer, "SYSDATETIMEOFFSET", ServerSideOnly=true)]`, `[Function(PN.PostgreSQL, "now", ServerSideOnly=true)]`, `[Property(PN.Oracle, "SYSTIMESTAMP", ServerSideOnly=true)]`, `[Function(PN.ClickHouse, "now", ServerSideOnly=true)]`, `[Function(PN.Ydb, "CurrentUtcTimestamp", ServerSideOnly=true)]`. Client-side fallback returns `DateTimeOffset.Now`. Providers not listed (e.g. MySQL, SQLite, DB2, Firebird) have no server-side mapping -- queries using `CurrentTzTimestamp` on those providers will evaluate client-side without error.

- **`Sql.Expr<T>`** (`Source/LinqToDB/Sql/Sql.Expressions.cs:559-570`) -- **delta (PR #5526):** `[SqlQueryDependentParams]` removed from the `parameters: object[]` argument of `Sql.Expr<T>(RawSqlString sql, params object[] parameters)`. The `[SqlQueryDependent]` annotation on the `sql` parameter is retained. `[SqlQueryDependentParams]` is now absent from the entire codebase -- searches confirm zero remaining uses across `Source/LinqToDB/`.

## Files (Tier 1 / Tier 2)

**Tier 1** (4 / 4 visited in full):

- `Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs`
- `Source/LinqToDB/Internal/SqlProvider/BasicSqlOptimizer.cs`
- `Source/LinqToDB/Internal/SqlProvider/ISqlBuilder.cs`
- `Source/LinqToDB/Internal/SqlProvider/ISqlOptimizer.cs`

**Tier 2** (47 candidates: 19 under `Internal/SqlProvider/`, 28 under `Sql/`):

Visited under `Internal/SqlProvider/` (19/19): `BasicSqlBuilder{T}.cs`, `BasicSqlBuilder.Merge.cs`, `SqlExpressionOptimizerVisitor.cs`, `SqlExpressionConvertVisitor.cs`, `JoinsOptimizer.cs`, `JoinsOptimizer.RemoveDuplicates.cs`, `OptimizationContext.cs`, `SqlExpressionFactory.cs`, `SqlOptimizerExtensions.cs`, `SqlProviderFlags.cs`, `ConvertType.cs`, `RowFeature.cs`, `TableIDInfo.cs`, `TableOptionsExtensions.cs`, `HintExtensionBuilder.cs`, `HintWithParameterExtensionBuilder.cs`, `HintWithFormatParametersExtensionBuilder.cs`, `HintWithParametersExtensionBuilder.cs`.

Visited under `Sql/` (28/28): `Sql.cs`, `Sql.ExpressionAttribute.cs`, `Sql.ExtensionAttribute.cs`, `Sql.Expressions.cs`, `Sql.PropertyAttribute.cs`, `Sql.FunctionAttribute.cs`, `Sql.QueryExtensionAttribute.cs`, `Sql.QueryExtensionScope.cs`, `Sql.TableID.cs`, `Sql.TableIDType.cs`, `Sql.IsNullableType.cs`, `Sql.Aggregate.cs`, `Sql.Analytic.cs`, `Sql.GroupBy.cs`, `Sql.Window.cs`, `Sql.Between.cs`, `Sql.Special.cs`, `Sql.Types.cs`, `Sql.Collate.cs`, `Sql.EnumAttribute.cs`, `Sql.TableExpressionAttribute.cs`, `Sql.TableFunctionAttribute.cs`, `Sql.Strings.cs`, `Sql.DateTime.cs`, `Sql.Row.cs`, `Sql.DateOnly.cs`, `Sql.DateTimeOffset.cs`, `Sql.Row.generated.cs` (delta read -- confirmed generator output, no new public API).

## Subsystems

1. **Statement-level orchestration** -- `BasicSqlBuilder.BuildSql` (`BasicSqlBuilder.cs:179`) dispatches by `Statement.QueryType`. Per-verb builders set `BuildStep` to one of the `Step` enum values (`BasicSqlBuilder.cs:4048`) and call clause-emitting methods in dialect order.
2. **Clause emission** -- one virtual per clause. Sub-tree emission delegates to `BuildExpression(StringBuilder sb, ISqlExpression expr, ...)` (`BasicSqlBuilder.cs:3036`) which dispatches by `ElementType`.
3. **Identifier escaping** -- `Convert(StringBuilder, string, ConvertType)` (`BasicSqlBuilder.cs:606`, default no-op). Provider subclasses override; call site indicates *which kind* of identifier via `ConvertType`.
4. **CTE emission** -- `BuildWithClause` (`BasicSqlBuilder.cs:670`). Reads `IsRecursiveCteKeywordRequired`, `IsCteColumnListSupported`, `SupportsMaterializedCteHint`, `CteFirst`. The `Materialized` annotation pathway lets PostgreSQL 12+, SQLite 3.35+, ClickHouse 26.3+ emit `AS MATERIALIZED` / `AS NOT MATERIALIZED`.
5. **MERGE emission** -- split into `BasicSqlBuilder.Merge.cs` (504 lines). `BuildMergeStatement` (`BasicSqlBuilder.Merge.cs:48`) drives `BuildMergeInto` -> `BuildMergeSource` -> `BuildMergeOn` -> per-operation `BuildMergeOperation` -> `BuildOutputSubclause` -> `BuildMergeTerminator`. Hooks `IsValuesSyntaxSupported`, `IsEmptyValuesSourceSupported`, `FakeTable`, `FakeTableSchema`, `RequiresConstantColumnAliases`, `SupportsColumnAliasesInSource` accommodate dialects without `VALUES (...)` source syntax.
6. **String-concat emission** (delta: PR #5504) -- `BuildSqlConcatExpression` (`BasicSqlBuilder.cs:3896`) lowers `SqlConcatExpression` AST nodes to dialect SQL. Three built-in strategies via `ConcatBuildStyle`: `Plus` (`a + b + c`), `Pipes` (`a || b || c`), `Function` (`CONCAT(a, b, c)`). Provider subclasses set `ConcatStyle` and optionally `ConcatFunctionName`; override `BuildSqlConcatExpression` only for non-standard shapes. Pre-emission operand normalization (cast to string, null-as-empty coalesce wrap) is done by `SqlExpressionConvertVisitor.ConvertConcat` in the convert phase -- the builder receives an already-normalized operand list.
7. **Statement-level optimization** -- `BasicSqlOptimizer.Finalize`. Drives `SqlNullsOrderingLoweringVisitor` (new -- when `!IsNullsOrderingSupported`), then `SelectQueryOptimizerVisitor`, then optionally `JoinsOptimizer`, then per-verb finalization, then per-provider `FinalizeStatement`. NULL/parameter casting across UNION arms uses `RequiresCastingParametersForSetOperations` / `RequiresCastingNullValueForSetOperations`. `SqlRowExpandVisitor` expands `SqlRowExpression` columns before emission.
8. **Two-phase visitor pipeline** -- `OptimizationContext.OptimizeAndConvert<T>` (`OptimizationContext.cs:131`) runs `SqlExpressionOptimizerVisitor.Optimize` (algebraic simplification) followed by `SqlExpressionConvertVisitor.Convert` (dialect-specific translation).
9. **COALESCE chain simplification** (delta: PR #5502) -- `SqlExpressionOptimizerVisitor.VisitSqlCoalesceExpression` (`SqlExpressionOptimizerVisitor.cs:1416`) performs two rewrites in one pass: (1) nested coalesce flattening (inlines `SqlCoalesceExpression` children into the outer list); (2) early-termination on a provably non-nullable mid-chain expression. `SqlExpressionConvertVisitor.RemoveNullValues` (`SqlExpressionConvertVisitor.cs:1247`) strips NULL-literal operands before provider-specific folding, preventing Informix/Access artifacts (issue #5531).
10. **NULLS ordering lowering** (delta -- this run) -- `SqlNullsOrderingLoweringVisitor` (`BasicSqlOptimizer.cs:62`) translates `NULLS FIRST`/`NULLS LAST` ORDER BY clauses into `CASE WHEN <expr> IS NULL THEN 0/1 END` sort keys for providers with `!IsNullsOrderingSupported`. Runs before `OptimizeQueries` so downstream rewrites treat CASE expressions as ordinary derived order expressions. Controlled by `IsNullsOrderingSupported` and `DefaultNullsOrdering` on `SqlProviderFlags`.

10. **Update-path rewrite helpers** -- `BasicCorrectUpdate` and `RemoveUpdateTableIfPossible` handle the gap between `UPDATE` semantics across providers.
11. **Public `Sql.*` extension surface** -- `Source/LinqToDB/Sql/`. Every server-side call lands here as `[Expression]`/`[Function]`/`[Extension]`-decorated method.
12. **Hint/extension builders** -- `HintExtensionBuilder` and three siblings convert a `SqlQueryExtension` AST node back into the textual form the provider wants. Resolves `Sql.SqlID` placeholders through `ISqlBuilder.BuildSqlID`.
13. **DateTime current-time surface** (delta: PR #5467) -- four related properties on `Sql` cover the "current timestamp" use case: `Sql.CurrentTimestamp` (`[ServerSideOnly]`, no client fallback), `Sql.CurrentTimestamp2` (dual-mode, client returns `DateTime.Now`), `Sql.CurrentTimestampUtc` (new, no attribute, translator-driven via `DateFunctionsTranslatorBase.TranslateUtcNow`, client fallback `DateTime.UtcNow`), and `Sql.CurrentTzTimestamp` (new, `DateTimeOffset`-typed, five provider-specific `[Function]`/`[Property]` attributes, client fallback `DateTimeOffset.Now`). `GetDate()` remains the original dual-mode variant. See Known issues for the `CurrentTimestampUtc` attribute-gap debt.
14. **`DateDiff` provider matrix** (delta: PR #5467) -- the `DateDiff` builder dispatch table is now uniform across all three `DateTime`/`DateOnly`/`DateTimeOffset` overloads: all carry entries for `PN.ClickHouse` (`DateDiffBuilderClickHouse`), `PN.Ydb` (`DateDiffBuilderYdb`), and `PN.DuckDB` (routes to `DateDiffBuilderPostgreSql`). `Sql.DateOnly.cs:65-69` and `Sql.DateTimeOffset.cs:72-77` previously lacked these three providers.

## Interactions

**Inputs.** `BasicSqlBuilder.BuildSql` consumes a finalized `SqlStatement` (see [SQL-AST](../SQL-AST/INDEX.md)) plus an `OptimizationContext`. The statement reaches it after `BasicSqlOptimizer.Finalize` has run.

**Outputs.** A `StringBuilder` of dialect-specific SQL plus `OptimizationContext._actualParameters` (the deduplicated `SqlParameter` list).

**Provider extension points.** Each `<Provider>SqlBuilder` extends `BasicSqlBuilder<TOptions>` and overrides ~5-40 virtuals. Each `<Provider>SqlOptimizer` extends `BasicSqlOptimizer`. For concat, providers override `ConcatStyle` (and optionally `ConcatFunctionName`, `ConcatRequiresExplicitStringCast`). For NULLS ordering, providers set `IsNullsOrderingSupported` and `DefaultNullsOrdering` on `SqlProviderFlags`.

**`Sql.*` consumption.** `Sql.<method>` calls in user LINQ queries are picked up by [EXPR-TRANS](../EXPR-TRANS/INDEX.md). `Sql.CurrentTimestampUtc` and `Sql.CurrentTzTimestamp` are dispatched through `DateFunctionsTranslatorBase` registration hooks (`DateFunctionsTranslatorBase.cs:63-74`).

**Inbound.**
- [LINQ](../LINQ/INDEX.md) -- calls `ISqlOptimizer.Finalize` and `ISqlBuilder.BuildSql` from the query runner.
- [EXPR-TRANS](../EXPR-TRANS/INDEX.md) -- reads `Sql.*` attribute decorations to translate method calls; produces `SqlConcatExpression` via `TranslateBinaryStringConcat`.
- All `PROV-*` areas -- every concrete builder/optimizer overrides this area's virtuals.
- [REMOTE-CLIENT](../REMOTE-CLIENT/INDEX.md) -- uses `SqlOptimizerExtensions.PrepareStatementForRemoting`.

**Outbound.**
- [SQL-AST](../SQL-AST/INDEX.md) -- the entire input/output language.
- [MAPPING](../MAPPING/INDEX.md) -- `MappingSchema.GetDbDataType` and `ValueToSqlConverter` are read constantly during emission.
- `Linq.Translation.ISqlExpressionFactory` -- produced by `BasicSqlOptimizer.CreateSqlExpressionFactory`.

## Known issues / debt

Pointers only:

- **`BasicSqlBuilder` is monolithic.** ~4650 lines in one partial type with ~120 virtual hooks.
- **`SqlProviderFlags` shape changes are ABI-breaking on the remote channel.**
- **`Sql.EnumAttribute` is undocumented.** XML doc placeholder reads "TODO: write xml doc what it does (I have no idea)".
- **`Sql.Window` is half-implemented.** Marked "Hidden until fully implemented".
- **Two near-identical date helper files.** `Sql.DateOnly.cs` and `Sql.DateTimeOffset.cs` mirror most of `Sql.DateTime.cs`'s API by data type.
- **`SqlExpressionConvertVisitor` is becoming the catch-all.**
- **`Sql.CurrentTimestampUtc` has no attribute decoration.** Unlike `Sql.CurrentTimestamp` (`[ServerSideOnly]`) and `Sql.CurrentTzTimestamp` (five provider `[Function]`/`[Property]` attributes), `Sql.CurrentTimestampUtc` carries no `[Function]`, `[Property]`, or `[ServerSideOnly]` attribute (`Sql.cs:1184`). Translation depends entirely on `DateFunctionsTranslatorBase.TranslateUtcNow` being overridden by the provider. Providers without that override silently evaluate client-side -- no compile-time or runtime warning. New debt from PR #5467.
- **`Sql.CurrentTzTimestamp` has no mapping for MySQL, SQLite, DB2, Firebird, SAP HANA, Informix, Sybase, Access, SQL CE.** Queries using `Sql.CurrentTzTimestamp` on those providers evaluate client-side without any warning.
- **`ConcatBuildStyle.Plus` is the default `ConcatStyle`** but most modern providers use `Pipes` or `Function`. Providers that do not explicitly override `ConcatStyle` silently emit `+`-style concat. (Deliberate default: the base class cannot assume `||` support.)
- ** ABI additive change.** The two new  fields (, ) are additive but any serialized payload from an older build will deserialize without them (defaults: /).
- ** is  for all providers by default.** Providers that do not set this flag emit CASE emulation sort keys even when ORDER BY direction already matches the natural null placement -- harmless but verbose.

## See also

- [SQL-AST](../SQL-AST/INDEX.md) -- the input/output AST; `SqlConcatExpression` is defined there.
- [EXPR-TRANS](../EXPR-TRANS/INDEX.md) -- the producer of `SqlStatement` instances; also hosts `DateFunctionsTranslatorBase.TranslateUtcNow` which drives `Sql.CurrentTimestampUtc` server-side translation; `TranslateBinaryStringConcat` produces `SqlConcatExpression`.
- [LINQ](../LINQ/INDEX.md) -- orchestrates calls to `ISqlOptimizer.Finalize` and `ISqlBuilder.BuildSql`.
- [MAPPING](../MAPPING/INDEX.md) -- `MappingSchema`, `ValueToSqlConverter`, `EntityDescriptor`, `ColumnDescriptor`.
- `PROV-*` -- concrete builder/optimizer subclasses per database.
- [architecture/sql-provider.md](../../architecture/sql-provider.md), [architecture/overview.md](../../architecture/overview.md), [architecture/query-pipeline.md](../../architecture/query-pipeline.md).

## Pointers

- Build/optimize entry points: `ISqlBuilder.BuildSql` (`Source/LinqToDB/Internal/SqlProvider/ISqlBuilder.cs:15`), `ISqlOptimizer.Finalize` (`Source/LinqToDB/Internal/SqlProvider/ISqlOptimizer.cs:13`).
- Default implementations: `BasicSqlBuilder` (`Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs:26`), `BasicSqlOptimizer` (`Source/LinqToDB/Internal/SqlProvider/BasicSqlOptimizer.cs:21`).
- Step enum (clause-by-clause emission state): `Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs:4048`.
- Two-phase visitor pipeline: `OptimizationContext.OptimizeAndConvert*` (`Source/LinqToDB/Internal/SqlProvider/OptimizationContext.cs:122-145`).
- Concat enum + hooks: `ConcatBuildStyle` (`BasicSqlBuilder.cs:3874`), `ConcatStyle` (`BasicSqlBuilder.cs:3888`), `ConcatFunctionName` (`BasicSqlBuilder.cs:3894`), `BuildSqlConcatExpression` (`BasicSqlBuilder.cs:3896`).
- Concat lowering in convert phase: `ConvertConcat` (`SqlExpressionConvertVisitor.cs:1261`), `ConcatRequiresExplicitStringCast` (`SqlExpressionConvertVisitor.cs:1259`).
- NULLS ordering lowering: `SqlNullsOrderingLoweringVisitor` invoked at `BasicSqlOptimizer.cs:62`; flags `IsNullsOrderingSupported` (`SqlProviderFlags.cs:604`), `DefaultNullsOrdering` (`SqlProviderFlags.cs:614`).
- COALESCE chain rewrites: `VisitSqlCoalesceExpression` (`SqlExpressionOptimizerVisitor.cs:1416`), nested-flattening + early-termination in one loop; `RemoveNullValues` (`SqlExpressionConvertVisitor.cs:1247`), NULL-literal stripping before provider fold.
- Public `Sql.*` static partial: `Source/LinqToDB/Sql/Sql.cs:27`.
- Attribute hierarchy root: `Source/LinqToDB/Sql/Sql.ExpressionAttribute.cs:32`.
- `Sql.CurrentTimestampUtc` (no-attribute, translator-driven): `Source/LinqToDB/Sql/Sql.cs:1184`.
- `Sql.CurrentTzTimestamp` (attribute-decorated, 5 providers): `Source/LinqToDB/Sql/Sql.cs:1193-1198`.
- `DateDiff` builder implementations (ClickHouse, Ydb, PostgreSQL/DuckDB): `Source/LinqToDB/Sql/Sql.DateTime.cs:459-538`.
- `Sql.Expr<T>` (no `[SqlQueryDependentParams]` on `parameters` since PR #5526): `Source/LinqToDB/Sql/Sql.Expressions.cs:564-570`.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 4 / 4 (unchanged from build run)
- Tier 2 (visited / total): 47 / 47 (100%)
- Tier 3 (skipped, logged): 0

Read (prior run -- delta, SHA 4a478ff14):
- `Source/LinqToDB/Sql/Sql.cs` (lines 1140-1270): Sql.CurrentTimestampUtc (no attribute), Sql.CurrentTzTimestamp (5-provider attribute matrix)
- `Source/LinqToDB/Sql/Sql.DateOnly.cs` (full): DateDiff now includes ClickHouse/Ydb/DuckDB builders
- `Source/LinqToDB/Sql/Sql.DateTimeOffset.cs` (full): same DateDiff alignment
- `Source/LinqToDB/Sql/Sql.DateTime.cs` (full): DateDiffBuilderClickHouse, DateDiffBuilderYdb implementations confirmed; PR #5517 DateTime.Date truncation fix is in EXPR-TRANS (DateFunctionsTranslatorBase), not this file's public API

Read (this run -- delta, SHA 2e67bafc9):
- `Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs` (lines 1-200, 3235-3250, 3867-3938): `ConcatBuildStyle` enum, `ConcatStyle`/`ConcatFunctionName` virtual properties, `BuildSqlConcatExpression`/`BuildSqlConcatOperatorChain`/`BuildSqlConcatFunctionCall` -- PR #5504 concat-emission subsystem confirmed at lines 3874-3936; `QueryElementType.SqlConcat` dispatch at line 3243.
- `Source/LinqToDB/Internal/SqlProvider/SqlExpressionConvertVisitor.cs` (lines 1-100, 1152-1390): `VisitSqlConcatExpression`, `ConcatRequiresExplicitStringCast`, `ConvertConcat`, `FlattenNestedConcat`, `IsConcatCoalesceWrap` -- PR #5504 convert-phase lowering confirmed; `VisitSqlCoalesceExpression` confirmed unchanged in this file.
- `Source/LinqToDB/Internal/SqlProvider/SqlExpressionOptimizerVisitor.cs` (lines 1-100, 953-1005, 1300-1365, 1390-1455): PR #5502 -- `VisitSqlCoalesceExpression` early-termination on non-nullable mid-chain expression at line 1426 confirmed.
- `Source/LinqToDB/Internal/SqlProvider/BasicSqlOptimizer.cs` (lines 1-100, 401-468, 514-548): `FinalizeSelect`, `FixSetOperationValues` -- no new code attributable to PR #5502 or #5504 in these sections; #5502 fix is in `SqlExpressionOptimizerVisitor`, not `BasicSqlOptimizer`.
- `Source/LinqToDB/Sql/Sql.Expressions.cs` (full, 574 lines): PR #5526 -- `Sql.Expr<T>(RawSqlString, params object[])` at line 567 has no `[SqlQueryDependentParams]` on `parameters`; `[SqlQueryDependentParams]` is absent from entire file; codebase-wide search confirms zero remaining uses.
- `Source/LinqToDB/Sql/Sql.cs` (lines 1-100): no new public API beyond prior delta run.


Read (this run -- delta, SHA b3340aa9):
-  (lines 1-100, 3850-3970):  doc updated to include Sybase ASE.
-  (lines 1-150):  gains  at line 61-62.
-  (lines 1-100, 1155-1300):  extracted at line 1247; Sybase ASE exception at line 1289.
-  (lines 1-100, 1395-1465): nested COALESCE flattening at lines 1429-1437; early-termination at line 1439.
-  (full): two new  fields Order=69/70; / updated.
-  (full): no changes.
-  (lines 1-100, 1170-1190): no new public API.
</details>
