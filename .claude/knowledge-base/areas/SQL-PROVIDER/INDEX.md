---
area: SQL-PROVIDER
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
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

- **`BasicSqlBuilder<T>`** (`Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder{T}.cs:8`) -- typed-options facade. `T : DataProviderOptions<T>, IOptionSet, new()`.

- **`BasicSqlOptimizer`** (`Source/LinqToDB/Internal/SqlProvider/BasicSqlOptimizer.cs:21`) -- abstract default optimizer, ~2260 lines. The `Finalize` pipeline (line 51) runs in fixed order: `FixEmptySelect` -> `FinalizeCte` -> `OptimizeQueries` -> conditional `JoinsOptimizer.Optimize` -> `FinalizeInsert` -> `FinalizeSelect` -> `FixSetOperationValues` -> `FinalizeStatement` (provider hook). The `Alternative Builders` region (line 820) holds reusable rewrite helpers (`GetAlternativeDelete`, `RemoveUpdateTableIfPossible`, `NeedsEnvelopingForUpdate`, `ReplaceTakeSkipWithRowNumber`).

- **`SqlProviderFlags`** (`Source/LinqToDB/Internal/SqlProvider/SqlProviderFlags.cs:19`) -- `[DataContract]`, ~70 mutable bool/enum fields. `[DataMember(Order = N)]` is mandatory because remoting (gRPC/WCF/HTTP) round-trips this object. The `CustomFlags` set is a string-keyed escape hatch.

- **`OptimizationContext`** (`Source/LinqToDB/Internal/SqlProvider/OptimizationContext.cs:13`) -- bag passed from LINQ pipeline into SQL builder. Carries `EvaluationContext`, `DataOptions`, `SqlProviderFlags`, `MappingSchema`, the two visitor instances, a `Factory` (`ISqlExpressionFactory`), parameter-dedup map, `SuggestDynamicParameter` cache. The two `OptimizeAndConvert*` methods drive the two-phase optimize-then-convert pipeline.

- **`SqlExpressionOptimizerVisitor`** -- provider-agnostic shape-preserving rewrites. ~2185 lines.

- **`SqlExpressionConvertVisitor`** -- provider-specific `IQueryElement` transforms. ~2105 lines.

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
6. **Statement-level optimization** -- `BasicSqlOptimizer.Finalize`. Drives `SelectQueryOptimizerVisitor`, then optionally `JoinsOptimizer`, then per-verb finalization, then per-provider `FinalizeStatement`. NULL/parameter casting across UNION arms uses `RequiresCastingParametersForSetOperations` / `RequiresCastingNullValueForSetOperations`. `SqlRowExpandVisitor` expands `SqlRowExpression` columns before emission.
7. **Two-phase visitor pipeline** -- `OptimizationContext.OptimizeAndConvert<T>` (`OptimizationContext.cs:131`) runs `SqlExpressionOptimizerVisitor.Optimize` (algebraic simplification) followed by `SqlExpressionConvertVisitor.Convert` (dialect-specific translation).
8. **Update-path rewrite helpers** -- `BasicCorrectUpdate` and `RemoveUpdateTableIfPossible` handle the gap between `UPDATE` semantics across providers.
9. **Public `Sql.*` extension surface** -- `Source/LinqToDB/Sql/`. Every server-side call lands here as `[Expression]`/`[Function]`/`[Extension]`-decorated method.
10. **Hint/extension builders** -- `HintExtensionBuilder` and three siblings convert a `SqlQueryExtension` AST node back into the textual form the provider wants. Resolves `Sql.SqlID` placeholders through `ISqlBuilder.BuildSqlID`.
11. **DateTime current-time surface** (delta: PR #5467) -- four related properties on `Sql` cover the "current timestamp" use case: `Sql.CurrentTimestamp` (`[ServerSideOnly]`, no client fallback), `Sql.CurrentTimestamp2` (dual-mode, client returns `DateTime.Now`), `Sql.CurrentTimestampUtc` (new, no attribute, translator-driven via `DateFunctionsTranslatorBase.TranslateUtcNow`, client fallback `DateTime.UtcNow`), and `Sql.CurrentTzTimestamp` (new, `DateTimeOffset`-typed, five provider-specific `[Function]`/`[Property]` attributes, client fallback `DateTimeOffset.Now`). `GetDate()` remains the original dual-mode variant. See Known issues for the `CurrentTimestampUtc` attribute-gap debt.
12. **`DateDiff` provider matrix** (delta: PR #5467) -- the `DateDiff` builder dispatch table is now uniform across all three `DateTime`/`DateOnly`/`DateTimeOffset` overloads: all carry entries for `PN.ClickHouse` (`DateDiffBuilderClickHouse`), `PN.Ydb` (`DateDiffBuilderYdb`), and `PN.DuckDB` (routes to `DateDiffBuilderPostgreSql`). `Sql.DateOnly.cs:65-69` and `Sql.DateTimeOffset.cs:72-77` previously lacked these three providers.

## Interactions

**Inputs.** `BasicSqlBuilder.BuildSql` consumes a finalized `SqlStatement` (see [SQL-AST](../SQL-AST/INDEX.md)) plus an `OptimizationContext`. The statement reaches it after `BasicSqlOptimizer.Finalize` has run.

**Outputs.** A `StringBuilder` of dialect-specific SQL plus `OptimizationContext._actualParameters` (the deduplicated `SqlParameter` list).

**Provider extension points.** Each `<Provider>SqlBuilder` extends `BasicSqlBuilder<TOptions>` and overrides ~5-40 virtuals. Each `<Provider>SqlOptimizer` extends `BasicSqlOptimizer`.

**`Sql.*` consumption.** `Sql.<method>` calls in user LINQ queries are picked up by [EXPR-TRANS](../EXPR-TRANS/INDEX.md). `Sql.CurrentTimestampUtc` and `Sql.CurrentTzTimestamp` are dispatched through `DateFunctionsTranslatorBase` registration hooks (`DateFunctionsTranslatorBase.cs:63-74`).

**Inbound.**
- [LINQ](../LINQ/INDEX.md) -- calls `ISqlOptimizer.Finalize` and `ISqlBuilder.BuildSql` from the query runner.
- [EXPR-TRANS](../EXPR-TRANS/INDEX.md) -- reads `Sql.*` attribute decorations to translate method calls.
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

## See also

- [SQL-AST](../SQL-AST/INDEX.md) -- the input/output AST.
- [EXPR-TRANS](../EXPR-TRANS/INDEX.md) -- the producer of `SqlStatement` instances; also hosts `DateFunctionsTranslatorBase.TranslateUtcNow` which drives `Sql.CurrentTimestampUtc` server-side translation.
- [LINQ](../LINQ/INDEX.md) -- orchestrates calls to `ISqlOptimizer.Finalize` and `ISqlBuilder.BuildSql`.
- [MAPPING](../MAPPING/INDEX.md) -- `MappingSchema`, `ValueToSqlConverter`, `EntityDescriptor`, `ColumnDescriptor`.
- `PROV-*` -- concrete builder/optimizer subclasses per database.
- [architecture/sql-provider.md](../../architecture/sql-provider.md), [architecture/overview.md](../../architecture/overview.md), [architecture/query-pipeline.md](../../architecture/query-pipeline.md).

## Pointers

- Build/optimize entry points: `ISqlBuilder.BuildSql` (`Source/LinqToDB/Internal/SqlProvider/ISqlBuilder.cs:15`), `ISqlOptimizer.Finalize` (`Source/LinqToDB/Internal/SqlProvider/ISqlOptimizer.cs:13`).
- Default implementations: `BasicSqlBuilder` (`Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs:26`), `BasicSqlOptimizer` (`Source/LinqToDB/Internal/SqlProvider/BasicSqlOptimizer.cs:21`).
- Step enum (clause-by-clause emission state): `Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs:4048`.
- Two-phase visitor pipeline: `OptimizationContext.OptimizeAndConvert*` (`Source/LinqToDB/Internal/SqlProvider/OptimizationContext.cs:122-145`).
- Public `Sql.*` static partial: `Source/LinqToDB/Sql/Sql.cs:27`.
- Attribute hierarchy root: `Source/LinqToDB/Sql/Sql.ExpressionAttribute.cs:32`.
- `Sql.CurrentTimestampUtc` (no-attribute, translator-driven): `Source/LinqToDB/Sql/Sql.cs:1184`.
- `Sql.CurrentTzTimestamp` (attribute-decorated, 5 providers): `Source/LinqToDB/Sql/Sql.cs:1193-1198`.
- `DateDiff` builder implementations (ClickHouse, Ydb, PostgreSQL/DuckDB): `Source/LinqToDB/Sql/Sql.DateTime.cs:459-538`.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 4 / 4 (unchanged from build run)
- Tier 2 (visited / total): 47 / 47 (100%)
- Tier 3 (skipped, logged): 0

Read (this run -- delta, SHA 4a478ff14):
- `Source/LinqToDB/Sql/Sql.cs` (lines 1140-1270): Sql.CurrentTimestampUtc (no attribute), Sql.CurrentTzTimestamp (5-provider attribute matrix)
- `Source/LinqToDB/Sql/Sql.DateOnly.cs` (full): DateDiff now includes ClickHouse/Ydb/DuckDB builders
- `Source/LinqToDB/Sql/Sql.DateTimeOffset.cs` (full): same DateDiff alignment
- `Source/LinqToDB/Sql/Sql.DateTime.cs` (full): DateDiffBuilderClickHouse, DateDiffBuilderYdb implementations confirmed; PR #5517 DateTime.Date truncation fix is in EXPR-TRANS (DateFunctionsTranslatorBase), not this file's public API

</details>
