---
area: SQL-PROVIDER
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-04-26
last_verified_sha: 3727a580c828e4f983da2de934d4cfc12d0cb255
coverage_tier_1: 4/4
coverage_tier_2: 43/47
---

# SQL-PROVIDER

Provider-agnostic SQL emission and statement-level optimization. Lives under `Source/LinqToDB/Internal/SqlProvider/` (the build/optimize machinery — `LinqToDB.Internal.SqlProvider` namespace) and `Source/LinqToDB/Sql/` (the legacy public `Sql.*` static class — namespace `LinqToDB`). Together these define the contract between the [SQL-AST](../SQL-AST/INDEX.md) (input) and the per-provider subclasses under `Internal/DataProvider/<Provider>/` (output).

The two anchor abstractions are `ISqlBuilder` (turns a `SqlStatement` into provider-specific SQL text) and `ISqlOptimizer` (rewrites a `SqlStatement` into a shape the builder can emit). Each in-tree provider ships exactly one concrete `<Provider>SqlBuilder : BasicSqlBuilder` and one `<Provider>SqlOptimizer : BasicSqlOptimizer`, registered via `IDataProvider.CreateSqlBuilder` (`Source/LinqToDB/Internal/DataProvider/DataProviderBase.cs:123`). The dialect override mechanism is plain virtual-method inheritance — `BasicSqlBuilder` exposes ~120 `protected virtual` hooks (sentence prefixes like `BuildXxx`, `IsXxxSupported`, `GetXxxName`) that subclasses override surgically.

`Source/LinqToDB/Sql/` is the user-facing surface: `public static partial class Sql` (`Source/LinqToDB/Sql/Sql.cs:27`) hosts every server-side helper a query can call (`Sql.AsSql`, `Sql.AsNullable`, `Sql.Between`, `Sql.Collate`, `Sql.GroupBy.Rollup`, the `Sql.DateAdd` family, the `Sql.Row(...)` constructors, …) plus the attribute hierarchy used by user code to declare custom translations (`Sql.ExpressionAttribute`, `Sql.FunctionAttribute`, `Sql.ExtensionAttribute`, `Sql.PropertyAttribute`, `Sql.TableFunctionAttribute`, `Sql.TableExpressionAttribute`, `Sql.QueryExtensionAttribute`).

## Key types

- **`ISqlBuilder`** (`Source/LinqToDB/Internal/SqlProvider/ISqlBuilder.cs:11`) — the emission contract. Entry point is `BuildSql(commandNumber, statement, sb, optimizationContext, aliases, nullabilityContext, startIndent)` (`ISqlBuilder.cs:15`); auxiliary surface includes `BuildObjectName` (qualified table/proc/CTE names), `Convert(sb, value, ConvertType)` (escape an identifier), `BuildExpression`, `GetIdentityExpression`, `PrintParameters`, `ApplyQueryHints`, `GetReserveSequenceValuesSql`, `GetMaxValueSql`. Carries five state properties: `Name`, `MappingSchema`, `StringBuilder`, `SqlProviderFlags`, `TableIDs`. The `CommandCount(SqlStatement)` overload (`ISqlBuilder.cs:13`) lets a provider emit multiple SQL statements per logical operation (e.g. SQL Server `INSERT … RETURNING` simulated via temp-table `OUTPUT INTO`, see `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlBuilder.cs:42`).

- **`ISqlOptimizer`** (`Source/LinqToDB/Internal/SqlProvider/ISqlOptimizer.cs:7`) — the rewrite contract. Three entry points: `Finalize(mappingSchema, statement, dataOptions)` is the run-everything pipeline; `IsParameterDependent` walks the tree looking for parameter-dependent shapes; `ConvertSkipTake(...)` produces dialect-specific `LIMIT`/`OFFSET`/`TOP` expressions and reports whether a `SKIP` is supported. Two factory methods (`CreateOptimizerVisitor`, `CreateConvertVisitor`) and one for `ISqlExpressionFactory` let providers swap in their own visitor subclasses while keeping the surrounding orchestration in `BasicSqlOptimizer`.

- **`BasicSqlBuilder`** (`Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs:26`) — abstract default implementation, ~4650 lines, partial across `BasicSqlBuilder.cs` and `BasicSqlBuilder.Merge.cs`. Walks the AST clause-by-clause via the `Step` enum (`BasicSqlBuilder.cs:4048`: `WithClause`, `SelectClause`, `FromClause`, `WhereClause`, `GroupByClause`, `HavingClause`, `OrderByClause`, `OffsetLimit`, `Tag`, `Output`, `QueryExtensions`, …). Statement dispatch happens in `BuildSqlImpl` (`BasicSqlBuilder.cs:367`) — a switch over `Statement.QueryType` calls `BuildSelectQuery`, `BuildDeleteQuery`, `BuildUpdateQuery`, `BuildInsertQuery`, `BuildInsertOrUpdateQuery`, `BuildCreateTableStatement`, `BuildDropTableStatement`, `BuildTruncateTableStatement`, `BuildMergeStatement`, `BuildMultiInsertQuery`. Critical hook properties: `OpenParens`, `Comma`, `InlineComma` (formatting), `IsRecursiveCteKeywordRequired`, `IsCteColumnListSupported`, `SupportsMaterializedCteHint`, `WrapJoinCondition`, `IsNestedJoinSupported`, `CteFirst`, `IsOverRequiredWithinGroup`. Hooks for sub-clauses to override: `BuildSelectClause`, `BuildColumns`, `BuildFromClause`, `BuildWhereClause`, `BuildOrderByClause`, `BuildOffsetLimit`, `BuildOutputSubclause`, `BuildSkipFirst`, `BuildLikePredicate`, `BuildInListPredicate`, `BuildBinaryExpression`, `BuildFunction`, `BuildSqlCastExpression`, `BuildDataTypeFromDataType`, `GetPhysicalTableName`, `IsReserved`, plus `protected abstract ISqlBuilder CreateSqlBuilder()` (`BasicSqlBuilder.cs:302`) — every provider overrides this to spawn a fresh same-typed builder for sub-queries.

- **`BasicSqlBuilder<T>`** (`Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder{T}.cs:8`) — typed-options façade. `T : DataProviderOptions<T>, IOptionSet, new()`. Exposes `ProviderOptions` lazy-resolved from `DataOptions.FindOrDefault`. Used by every provider that has its own options bag (`SqlServerSqlBuilder : BasicSqlBuilder<SqlServerOptions>`, `OracleSqlBuilder : BasicSqlBuilder<OracleOptions>`, etc.).

- **`BasicSqlOptimizer`** (`Source/LinqToDB/Internal/SqlProvider/BasicSqlOptimizer.cs:21`) — abstract default optimizer, ~2260 lines. The `Finalize` pipeline (`BasicSqlOptimizer.cs:51`) runs in fixed order: `FixEmptySelect` → `FinalizeCte` → `OptimizeQueries` (drives `SelectQueryOptimizerVisitor`) → conditional `JoinsOptimizer.Optimize` → `FinalizeInsert` → `FinalizeSelect` (`SqlRowExpandVisitor` rewrites `SqlRowExpression` columns into individual columns, `BasicSqlOptimizer.cs:408`) → `FixSetOperationValues` (casts NULLs and parameters across UNION arms when `RequiresCastingParametersForSetOperations`/`RequiresCastingNullValueForSetOperations` say so, `BasicSqlOptimizer.cs:513`) → `FinalizeStatement` (provider hook). Other key virtuals: `BasicCorrectUpdate` (rewrites `UPDATE … FROM` for providers without `UPDATE FROM`, `BasicSqlOptimizer.cs:253`), `FinalizeUpdate`/`FinalizeInsertOrUpdate`, `TransformStatement`, `CorrectOutputTables`, `CreateSqlExpressionFactory`. The `Alternative Builders` region (`BasicSqlOptimizer.cs:820`) holds reusable rewrite helpers (`GetAlternativeDelete`, `RemoveUpdateTableIfPossible`, `NeedsEnvelopingForUpdate`, `ReplaceTakeSkipWithRowNumber`) that providers compose into their `FinalizeStatement` overrides.

- **`SqlProviderFlags`** (`Source/LinqToDB/Internal/SqlProvider/SqlProviderFlags.cs:19`) — `[DataContract]`, ~70 mutable bool/enum fields populated by each `IDataProvider` ctor and consumed throughout `BasicSqlBuilder`/`BasicSqlOptimizer`. Examples: `IsSkipSupported`, `IsApplyJoinSupported`, `IsCommonTableExpressionsSupported`, `IsUpdateFromSupported`, `IsInsertOrUpdateSupported`, `OutputDeleteUseSpecialTable`, `IsWindowFunctionsSupported`, `RowConstructorSupport` (`RowFeature` flags, `RowFeature.cs:12`), `MaxInListValuesCount`. `[DataMember(Order = N)]` is mandatory because remoting (gRPC/WCF/HTTP) round-trips this object (`SqlProviderFlags.cs:14-17`). The `CustomFlags` set (`SqlProviderFlags.cs:25`) is a string-keyed escape hatch for provider-specific behavioural toggles.

- **`OptimizationContext`** (`Source/LinqToDB/Internal/SqlProvider/OptimizationContext.cs:13`) — bag passed from the LINQ pipeline into the SQL builder. Carries `EvaluationContext`, `DataOptions`, `SqlProviderFlags`, `MappingSchema`, the two visitor instances (`OptimizerVisitor`, `ConvertVisitor`), a `Factory` (`ISqlExpressionFactory`), a parameter-deduplication map (`AddParameter`, `OptimizationContext.cs:67`), and a `SuggestDynamicParameter` cache (`OptimizationContext.cs:98`) keyed by `(DbDataType, value)`. The two `OptimizeAndConvert*` methods (`OptimizationContext.cs:122-145`) drive the two-phase optimize-then-convert visitor pipeline: `SqlExpressionOptimizerVisitor` first (constant folding, predicate reduction, NULL-aware rewrites), `SqlExpressionConvertVisitor` second (dialect-specific transforms).

- **`SqlExpressionOptimizerVisitor`** (`Source/LinqToDB/Internal/SqlProvider/SqlExpressionOptimizerVisitor.cs:17`) — provider-agnostic shape-preserving rewrites. ~2185 lines, derives from `SqlQueryVisitor`. Threads `EvaluationContext`, `NullabilityContext`, `DataOptions`, `MappingSchema` through every visit. Used in two modes (`Modify` vs `Transform`) controlled by `allowModify` ctor flag.

- **`SqlExpressionConvertVisitor`** (`Source/LinqToDB/Internal/SqlProvider/SqlExpressionConvertVisitor.cs:19`) — provider-specific `IQueryElement` transforms. ~2105 lines. The base implementation handles cross-provider concerns (`WrapBooleanExpression`, `WrapColumnExpression`, output-clause boolean rewrite); provider subclasses (under `Internal/DataProvider/<P>/`) override individual `Visit*` methods to convert `PseudoFunctions.LENGTH` to `LEN` for SQL Server, `STRING_AGG` to `LISTAGG` for Oracle, etc. Per-feature support gates: `SupportsBooleanInColumn`, `SupportsNullInColumn`, `SupportsDistinctAsExistsIntersect`, `SupportsNullIf`.

- **`JoinsOptimizer`** (`Source/LinqToDB/Internal/SqlProvider/JoinsOptimizer.cs:10`, partial across `JoinsOptimizer.RemoveDuplicates.cs`) — sealed; runs only when `LinqOptions.OptimizeJoins` is on. Two passes: `RemoveUnusedLeftJoins` drops LEFT JOINs to tables not referenced anywhere outside the join condition (`JoinsOptimizer.cs:27`); `RemoveDuplicateJoins` (`JoinsOptimizer.RemoveDuplicates.cs:18`) merges joins to the same table on equal keys via `TryMergeSources`/`TryMergeSources2`. The static `UnnestJoins` method (`JoinsOptimizer.cs:46`) flattens nested JOINs to a single level when the parent/child join types compose safely.

- **`SqlExpressionFactory`** (`Source/LinqToDB/Internal/SqlProvider/SqlExpressionFactory.cs:9`) — minimal default implementation of `ISqlExpressionFactory` (interface lives under `Linq.Translation` in `EXPR-TRANS` scope). Just resolves `DbDataType` for an `ISqlExpression` or `Type` via `MappingSchema.GetDbDataType`. Providers that need richer factories override `BasicSqlOptimizer.CreateSqlExpressionFactory` (`BasicSqlOptimizer.cs:2228`).

- **`ConvertType`** (`Source/LinqToDB/Internal/SqlProvider/ConvertType.cs:3`) — enum tagging *what kind of identifier* an `ISqlBuilder.Convert` call is escaping (`NameToQueryField`, `NameToQueryTable`, `NameToCteName`, `NameToDatabase`, `NameToServer`, `NameToQueryParameter`, `NameToSprocParameter`, `SprocParameterToName`, `NameToProcedure`, `SequenceName`, `ExceptionToErrorNumber`, `ExceptionToErrorMessage`, …). Each provider's `Convert` override switches on this to apply dialect-specific quoting (e.g. `[ ]` for SQL Server, `" "` for PostgreSQL, `@` prefix for parameter names).

- **`OptimizationContext`'s parameters normalizer** + **`IQueryParametersNormalizer`** — pluggable name normalization (e.g. converting C# parameter names to SQL Server `@p1`, `@p2`). Driven by `_parametersNormalizerFactory` ctor argument; `SqlOptimizerExtensions.PrepareStatementForRemoting` injects `NoopQueryParametersNormalizer` when the destination is a remote pipe (`SqlOptimizerExtensions.cs:25`).

- **`Sql.cs`** (`Source/LinqToDB/Sql/Sql.cs:27`) — the public `Sql` static class. Note the namespace: `LinqToDB`, not `LinqToDB.Sql` (`Sql.cs:24`, with a `// ReSharper disable CheckNamespace` to silence the linter). `Sql.AsSql<T>(T)` / `Sql.ToSql<T>(T)` force server-side evaluation, `Sql.AsNullable` / `Sql.AsNotNull` / `Sql.AsNotNullable` annotate nullability, `Sql.AllColumns()` emits `*`, `Sql.Default<T>()` emits `DEFAULT`. The class is `partial` and split across ~28 files by topic.

- **`Sql.ExpressionAttribute`** (`Source/LinqToDB/Sql/Sql.ExpressionAttribute.cs:32`) — the foundational attribute. `[Expression("LEN({0})")]` on a method binds it to a server-side fragment. Subclassed by `Sql.PropertyAttribute` (single value/property, `Sql.PropertyAttribute.cs:20`), `Sql.FunctionAttribute` (named function, `Sql.FunctionAttribute.cs:23`), `Sql.ExtensionAttribute` (`Sql.ExtensionAttribute.cs` — full builder hooks via `IExtensionCallBuilder`/`ISqlExtensionBuilder`), `Sql.TableFunctionAttribute` (`Sql.TableFunctionAttribute.cs:18`), `Sql.TableExpressionAttribute` (`Sql.TableExpressionAttribute.cs:18`).

- **`Sql.QueryExtensionAttribute`** + **`Sql.QueryExtensionScope`** (`Sql.QueryExtensionAttribute.cs:21`, `Sql.QueryExtensionScope.cs:10`) — declare query-shape extensions (table hints, index hints, join hints, query hints, table-name hints). Bound to a `Type extensionBuilderType` that implements `ISqlQueryExtensionBuilder`. The four built-in builders live in this folder: `HintExtensionBuilder` (raw text, `HintExtensionBuilder.cs:7`), `HintWithParameterExtensionBuilder` (`hint(arg)`), `HintWithParametersExtensionBuilder` (`hint(arg1, arg2, …)` with delimiters), `HintWithFormatParametersExtensionBuilder` (format-string interpolation with `String.Format`).

- **`Sql.SqlID`** (`Source/LinqToDB/Sql/Sql.TableID.cs:11`) — typed `(SqlIDType, string)` pair used by hint extensions to refer back to a tagged table by ID rather than by string concatenation. `SqlIDType` (`Sql.TableIDType.cs:5`) is `TableAlias` / `TableName` / `TableSpec`. The hint builders call `sqlBuilder.BuildSqlID(id)` (`ISqlBuilder.cs:46`) to resolve.

- **`Sql.AggregateModifier` / `From` / `Nulls` / `NullsPosition`** (`Source/LinqToDB/Sql/Sql.Analytic.cs:5-31`) — nominal enums for window-function clauses: `AggregateModifier.{None,Distinct,All}`, `From.{None,First,Last}` (for `FIRST_VALUE` / `LAST_VALUE`), `Nulls.{None,Respect,Ignore}`, `NullsPosition.{None,First,Last}` (for `ORDER BY … NULLS FIRST/LAST`).

- **`Sql.IsNullableType`** (`Source/LinqToDB/Sql/Sql.IsNullableType.cs:10`) — nullability propagation rules for `[Expression]` decorations: `Undefined`, `Nullable`, `NotNullable`, `IfAnyParameterNullable`, `SameAs{First,Second,Third,Last}Parameter`, `IfAllParametersNullable`. Mirrors `ParametersNullabilityType` in [SQL-AST](../SQL-AST/INDEX.md).

## Files (Tier 1 / Tier 2)

**Tier 1** (4 / 4 visited in full):

- `Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs`
- `Source/LinqToDB/Internal/SqlProvider/BasicSqlOptimizer.cs`
- `Source/LinqToDB/Internal/SqlProvider/ISqlBuilder.cs`
- `Source/LinqToDB/Internal/SqlProvider/ISqlOptimizer.cs`

**Tier 2** (47 candidates: 19 under `Internal/SqlProvider/` after Tier-1 deduction, 28 under `Sql/`).

Visited under `Internal/SqlProvider/` (19/19): `BasicSqlBuilder{T}.cs`, `BasicSqlBuilder.Merge.cs`, `SqlExpressionOptimizerVisitor.cs`, `SqlExpressionConvertVisitor.cs`, `JoinsOptimizer.cs`, `JoinsOptimizer.RemoveDuplicates.cs`, `OptimizationContext.cs`, `SqlExpressionFactory.cs`, `SqlOptimizerExtensions.cs`, `SqlProviderFlags.cs`, `ConvertType.cs`, `RowFeature.cs`, `TableIDInfo.cs`, `TableOptionsExtensions.cs`, `HintExtensionBuilder.cs`, `HintWithParameterExtensionBuilder.cs`, `HintWithFormatParametersExtensionBuilder.cs`, `HintWithParametersExtensionBuilder.cs`.

Visited under `Sql/` (24/28): `Sql.cs`, `Sql.ExpressionAttribute.cs` (head), `Sql.ExtensionAttribute.cs` (head), `Sql.Expressions.cs` (head), `Sql.PropertyAttribute.cs` (head), `Sql.FunctionAttribute.cs` (head), `Sql.QueryExtensionAttribute.cs` (head), `Sql.QueryExtensionScope.cs`, `Sql.TableID.cs`, `Sql.TableIDType.cs`, `Sql.IsNullableType.cs`, `Sql.Aggregate.cs`, `Sql.Analytic.cs`, `Sql.GroupBy.cs`, `Sql.Window.cs` (head), `Sql.Between.cs`, `Sql.Special.cs`, `Sql.Types.cs`, `Sql.Collate.cs` (head), `Sql.EnumAttribute.cs`, `Sql.TableExpressionAttribute.cs` (head), `Sql.TableFunctionAttribute.cs` (head), `Sql.Strings.cs` (head), `Sql.DateTime.cs` (head), `Sql.Row.cs` (head). Skipped under `Sql/` (4 — all noted): `Sql.Row.generated.cs` (1009-line generated near-duplicate of `Sql.Row.cs`), `Sql.DateOnly.cs` (date-helper near-duplicate of `Sql.DateTime.cs`), `Sql.DateTimeOffset.cs` (same), `Sql.Window.cs` only sampled at the head — internal interface-only file (rest is interface ladder, hidden until fully implemented per file comment at `Sql.Window.cs:15`).

## Subsystems

1. **Statement-level orchestration** — `BasicSqlBuilder.BuildSql` (`BasicSqlBuilder.cs:179`) dispatches by `Statement.QueryType`. Per-verb builders (`BuildSelectQuery`, `BuildDeleteQuery`, `BuildUpdateQuery`, `BuildInsertQuery`, `BuildInsertOrUpdateQuery`, `BuildMergeStatement`, DDL variants `BuildCreateTableStatement`/`BuildDropTableStatement`/`BuildTruncateTableStatement`, `BuildMultiInsertQuery`) each set `BuildStep` to one of the `Step` enum values (`BasicSqlBuilder.cs:4048`) and call clause-emitting methods in dialect order.
2. **Clause emission** — one virtual per clause (`BuildSelectClause`, `BuildFromClause`, `BuildWhereClause`, `BuildGroupByClause`, `BuildHavingClause`, `BuildOrderByClause`, `BuildOffsetLimit`, `BuildOutputSubclause`, `BuildWithClause`, `BuildSetOperation`). Sub-tree emission delegates to `BuildExpression(StringBuilder sb, ISqlExpression expr, …)` (`BasicSqlBuilder.cs:3036`) which dispatches by `ElementType` to `BuildSqlCastExpression`, `BuildSqlConditionExpression`, `BuildSqlCaseExpression`, `BuildBinaryExpression`, `BuildUnaryExpression`, `BuildFunction`, `BuildParameter`, `BuildValue`, `BuildSqlRow`, `BuildSqlExtendedFunction`, `BuildAnchor`, etc. Predicate emission through `BuildLikePredicate`, `BuildInListPredicate`, `BuildInSubQueryPredicate`.
3. **Identifier escaping** — `Convert(StringBuilder, string, ConvertType)` (`BasicSqlBuilder.cs:606`, default no-op). Provider subclasses override to wrap identifiers in `[]` / `""` / `` ` `` per dialect; the call site indicates *which kind* of identifier via `ConvertType` so the same method can apply different rules to a parameter name vs a CTE name vs a stored procedure name.
4. **CTE emission** — `BuildWithClause` (`BasicSqlBuilder.cs:670`). Reads `IsRecursiveCteKeywordRequired`, `IsCteColumnListSupported`, `SupportsMaterializedCteHint`, `CteFirst`. The `Materialized` annotation pathway (`ShouldEmitMaterializedCteHint`, `BasicSqlBuilder.cs:632`) lets PostgreSQL 12+, SQLite 3.35+, ClickHouse 26.3+ emit `AS MATERIALIZED` / `AS NOT MATERIALIZED`; Oracle uses a separate hint-comment override (`BasicSqlBuilder.cs:658`).
5. **MERGE emission** — split into `BasicSqlBuilder.Merge.cs` (504 lines). `BuildMergeStatement` (`BasicSqlBuilder.Merge.cs:48`) drives `BuildMergeInto` → `BuildMergeSource` → `BuildMergeOn` → per-operation `BuildMergeOperation` → `BuildOutputSubclause` → `BuildMergeTerminator`. Hooks `IsValuesSyntaxSupported`, `IsEmptyValuesSourceSupported`, `FakeTable`, `FakeTableSchema`, `RequiresConstantColumnAliases`, `SupportsColumnAliasesInSource` accommodate dialects without `VALUES (…)` source syntax.
6. **Statement-level optimization** — `BasicSqlOptimizer.Finalize` (`BasicSqlOptimizer.cs:51`). Drives `SelectQueryOptimizerVisitor` (allocated from a pool inside `OptimizeQueries`, `BasicSqlOptimizer.cs:2135`), then optionally `JoinsOptimizer`, then per-verb finalization (`FinalizeInsert`, `FinalizeSelect`, `FinalizeUpdate`, `FinalizeInsertOrUpdate`), then per-provider `FinalizeStatement`. NULL/parameter casting across UNION arms uses the two `RequiresCastingParametersForSetOperations` / `RequiresCastingNullValueForSetOperations` flags (`BasicSqlOptimizer.cs:34-35`). The pipeline runs `SqlRowExpandVisitor` (`BasicSqlOptimizer.cs:408`) before emission to expand `SqlRowExpression` columns into individual columns — `(a,b)` is conceptually a single value but most dialects emit `a, b`.
7. **Two-phase visitor pipeline** — `OptimizationContext.OptimizeAndConvert<T>` (`OptimizationContext.cs:131`) runs `SqlExpressionOptimizerVisitor.Optimize` (provider-agnostic algebraic simplification, NULL-folding, predicate canonicalisation) followed by `SqlExpressionConvertVisitor.Convert` (provider-specific dialect translation). The split exists so optimizer rules don't have to know about every dialect quirk; conversion happens after the AST is in a canonical shape.
8. **Update-path rewrite helpers** — `BasicCorrectUpdate` (`BasicSqlOptimizer.cs:253`) and `RemoveUpdateTableIfPossible` (`BasicSqlOptimizer.cs:876`) handle the gap between `UPDATE` semantics across providers. Providers without `UPDATE … FROM` get a join rewrite that builds an `EXISTS`/correlated form; providers with `UPDATE … FROM` get the simpler concat-WHERE form.
9. **Public `Sql.*` extension surface** — `Source/LinqToDB/Sql/`. Every server-side call a query uses (`Sql.AsSql`, `Sql.AsNullable`, `Sql.Between`, `Sql.Collate`, `Sql.GroupBy.Rollup`, `Sql.DateAdd`, `Sql.Row(...)`, `Sql.Spread`, `Sql.Ordinal`, `Sql.AllColumns`, the entire `Sql.Types.*` family, `Sql.Grouping`, `Sql.StringAggregate`) lands here as `[Expression]`/`[Function]`/`[Extension]`-decorated method on the `Sql` static class. The `Sql.Window` tower (`Sql.Window.cs`) declares the fluent window-function API surface but its implementation is gated as "Hidden until fully implemented" (`Sql.Window.cs:15`).
10. **Hint/extension builders** — `HintExtensionBuilder` and its three siblings convert a `SqlQueryExtension` AST node back into the textual form the provider wants (`hint`, `hint(arg)`, `hint(arg1, arg2)`, or formatted templates). Resolves `Sql.SqlID` placeholders through `ISqlBuilder.BuildSqlID` so a hint can refer to a tagged table.

## Interactions

**Inputs.** `BasicSqlBuilder.BuildSql` consumes a finalized `SqlStatement` (see [SQL-AST](../SQL-AST/INDEX.md)) plus an `OptimizationContext`. The statement reaches it after `BasicSqlOptimizer.Finalize` has run. The orchestration is in [LINQ](../LINQ/INDEX.md) (`ExpressionBuilder.BuildQuery` → `IDataContext.GetQueryRunner` → `QueryRunner.SetCommand`).

**Outputs.** A `StringBuilder` of dialect-specific SQL plus `OptimizationContext._actualParameters` (the deduplicated `SqlParameter` list, `OptimizationContext.cs:65-93`). The query runner copies these into a `DbCommand`. The number of distinct commands per logical query is `ISqlBuilder.CommandCount(statement)` (default 1; SQL Server's identity rewrite returns 2 for `INSERT … RETURNING`).

**Provider extension points.** Each `<Provider>SqlBuilder` (under `Source/LinqToDB/Internal/DataProvider/<Provider>/`, e.g. `SqlServerSqlBuilder` at `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlBuilder.cs:19`) extends `BasicSqlBuilder<TOptions>` and overrides ~5–40 virtuals. Each `<Provider>SqlOptimizer` extends `BasicSqlOptimizer` and at minimum overrides `Finalize` or `FinalizeStatement` to bolt on dialect-specific corrections, plus often `CreateConvertVisitor` to inject a provider `SqlExpressionConvertVisitor` subclass. Wired up by the `IDataProvider.CreateSqlBuilder(MappingSchema, DataOptions)` factory (`DataProviderBase.cs:123`) and the `IDataProvider.GetSqlOptimizer(DataOptions)` accessor.

**`Sql.*` consumption.** `Sql.<method>` calls in user LINQ queries are picked up by [EXPR-TRANS](../EXPR-TRANS/INDEX.md) (`ExpressionBuilder` reads `[Expression]`/`[Function]`/`[Extension]` attributes via `Mapping/`). The translator turns the call into an `SqlFunction` / `SqlExpression` node bound for the AST. The `Sql.Ext` and `Sql.Window` markers (`Sql.ExtensionAttribute.cs:30-31`) are method-target receivers that exist purely to host extension methods.

**Inbound.**
- [LINQ](../LINQ/INDEX.md) — calls `ISqlOptimizer.Finalize` and `ISqlBuilder.BuildSql` from the query runner.
- [EXPR-TRANS](../EXPR-TRANS/INDEX.md) — reads `Sql.*` attribute decorations to translate method calls; calls `OptimizationContext.AddParameter` / `SuggestDynamicParameter` while emitting parameter-bearing nodes.
- `PROV-*` areas (`PROV-SQLSERVER`, `PROV-POSTGRES`, `PROV-MYSQL`, `PROV-ORACLE`, `PROV-SQLITE`, `PROV-FIREBIRD`, `PROV-DB2`, `PROV-ACCESS`, `PROV-INFORMIX`, `PROV-SYBASE`, `PROV-SAPHANA`, `PROV-CLICKHOUSE`, `PROV-SQLCE`, `PROV-YDB`) — every concrete builder/optimizer overrides this area's virtuals.
- [REMOTE-CLIENT](../REMOTE-CLIENT/INDEX.md) — uses `SqlOptimizerExtensions.PrepareStatementForRemoting` (`SqlOptimizerExtensions.cs:9`) to fully optimize before serializing the AST across the wire.

**Outbound.**
- [SQL-AST](../SQL-AST/INDEX.md) — the entire input/output language. Every node type the builder encounters lives there.
- [MAPPING](../MAPPING/INDEX.md) — `MappingSchema.GetDbDataType` and `ValueToSqlConverter` are read constantly during emission. `EntityDescriptor` / `ColumnDescriptor` flow through `GetMaxValueSql`, `BuildCreateTableField*`, identity-field detection.
- `Linq.Translation.ISqlExpressionFactory` — produced by `BasicSqlOptimizer.CreateSqlExpressionFactory`, consumed by `SqlExpressionConvertVisitor` and provider-specific member translators.

## Known issues / debt

Pointers only — full entries collected in `tech-debt.md` after step 11:

- **`BasicSqlBuilder` is monolithic.** ~4650 lines in one partial type with ~120 virtual hooks — high cognitive cost to add a new clause type or change emission order. The `Step` enum + per-verb dispatcher is the closest thing to a state machine.
- **`SqlProviderFlags` shape changes are ABI-breaking on the remote channel.** Field reordering breaks gRPC/WCF/HTTP serialization (`SqlProviderFlags.cs:14-17`). New flags must use a fresh `Order` and avoid changing existing ones.
- **`Sql.EnumAttribute` is undocumented.** XML doc placeholder reads "TODO: write xml doc what it does (I have no idea)" (`Source/LinqToDB/Sql/Sql.EnumAttribute.cs:11`).
- **`Sql.Window` is half-implemented.** Marked "Hidden until fully implemented" (`Sql.Window.cs:15`); the public-facing window API is split across this file and `Sql.Analytic.cs`.
- **Two near-identical date helper files.** `Sql.DateOnly.cs` and `Sql.DateTimeOffset.cs` mirror most of `Sql.DateTime.cs`'s API by data type — opportunity for a generic surface, blocked by attribute-binding constraints.
- **`SqlExpressionConvertVisitor` is becoming the catch-all.** ~2105 lines with provider-overridable Visit* surface that grows every release.

## See also

- [SQL-AST](../SQL-AST/INDEX.md) — the input/output AST.
- [EXPR-TRANS](../EXPR-TRANS/INDEX.md) — the producer of `SqlStatement` instances.
- [LINQ](../LINQ/INDEX.md) — orchestrates calls to `ISqlOptimizer.Finalize` and `ISqlBuilder.BuildSql`.
- [MAPPING](../MAPPING/INDEX.md) — `MappingSchema`, `ValueToSqlConverter`, `EntityDescriptor`, `ColumnDescriptor`.
- `PROV-*` — concrete builder/optimizer subclasses per database.
- [architecture/sql-provider.md](../../architecture/sql-provider.md) — narrative cross-area write-up.
- [architecture/overview.md](../../architecture/overview.md) — global pipeline overview.
- [architecture/query-pipeline.md](../../architecture/query-pipeline.md) — full LINQ → SQL pipeline narrative.

## Pointers

- Build/optimize entry points: `ISqlBuilder.BuildSql` (`Source/LinqToDB/Internal/SqlProvider/ISqlBuilder.cs:15`), `ISqlOptimizer.Finalize` (`Source/LinqToDB/Internal/SqlProvider/ISqlOptimizer.cs:13`).
- Default implementations: `BasicSqlBuilder` (`Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs:26`), `BasicSqlOptimizer` (`Source/LinqToDB/Internal/SqlProvider/BasicSqlOptimizer.cs:21`).
- Step enum (clause-by-clause emission state): `Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs:4048`.
- Provider override example: `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlBuilder.cs:19`.
- Provider flags type: `Source/LinqToDB/Internal/SqlProvider/SqlProviderFlags.cs:19`.
- Two-phase visitor pipeline: `OptimizationContext.OptimizeAndConvert*` (`Source/LinqToDB/Internal/SqlProvider/OptimizationContext.cs:122-145`).
- Public `Sql.*` static partial: `Source/LinqToDB/Sql/Sql.cs:27`.
- Attribute hierarchy root: `Source/LinqToDB/Sql/Sql.ExpressionAttribute.cs:32`.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 4 / 4 ✓
  - Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs (full + targeted re-reads through line 4174)
  - Source/LinqToDB/Internal/SqlProvider/BasicSqlOptimizer.cs (full pipeline + alternative-builders region + visitors region)
  - Source/LinqToDB/Internal/SqlProvider/ISqlBuilder.cs (full)
  - Source/LinqToDB/Internal/SqlProvider/ISqlOptimizer.cs (full)
- Tier 2 (visited / total): 43 / 47 (91.5%) ✓
  - Internal/SqlProvider/ (19/19): BasicSqlBuilder{T}.cs, BasicSqlBuilder.Merge.cs, SqlExpressionOptimizerVisitor.cs, SqlExpressionConvertVisitor.cs, JoinsOptimizer.cs, JoinsOptimizer.RemoveDuplicates.cs, OptimizationContext.cs, SqlExpressionFactory.cs, SqlOptimizerExtensions.cs, SqlProviderFlags.cs, ConvertType.cs, RowFeature.cs, TableIDInfo.cs, TableOptionsExtensions.cs, HintExtensionBuilder.cs, HintWithParameterExtensionBuilder.cs, HintWithFormatParametersExtensionBuilder.cs, HintWithParametersExtensionBuilder.cs.
  - Sql/ (24/28): Sql.cs, Sql.ExpressionAttribute.cs, Sql.ExtensionAttribute.cs, Sql.Expressions.cs, Sql.PropertyAttribute.cs, Sql.FunctionAttribute.cs, Sql.QueryExtensionAttribute.cs, Sql.QueryExtensionScope.cs, Sql.TableID.cs, Sql.TableIDType.cs, Sql.IsNullableType.cs, Sql.Aggregate.cs, Sql.Analytic.cs, Sql.GroupBy.cs, Sql.Window.cs, Sql.Between.cs, Sql.Special.cs, Sql.Types.cs, Sql.Collate.cs, Sql.EnumAttribute.cs, Sql.TableExpressionAttribute.cs, Sql.TableFunctionAttribute.cs, Sql.Strings.cs, Sql.DateTime.cs, Sql.Row.cs.
  - skipped: Sql.Row.generated.cs (1009-line generated near-duplicate of Sql.Row.cs — generator output)
  - skipped: Sql.DateOnly.cs (date-helper near-duplicate of Sql.DateTime.cs surface)
  - skipped: Sql.DateTimeOffset.cs (date-helper near-duplicate of Sql.DateTime.cs surface)
  - skipped: full body of Sql.Window.cs (interface ladder hidden until fully implemented per file comment)
- Tier 3 (skipped, logged): 0 (no bin/obj/generated assets in scope beyond Sql.Row.generated.cs already counted in Tier 2)
</details>
