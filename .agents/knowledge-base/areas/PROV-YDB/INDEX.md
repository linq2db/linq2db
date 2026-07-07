---
area: PROV-YDB
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-07-05
last_verified_sha: 36ee4f82f06eaf242b052ade8c87121d251a6165
coverage_tier_1: 15/15
coverage_tier_2: 8/8
---

# PROV-YDB -- Yandex Database (YDB) Provider

YDB is a distributed NewSQL database by Yandex, queried via YQL (a SQL dialect). The linq2db provider wraps the `Ydb.Sdk` ADO.NET package dynamically and adds a built-in retry policy unique among all providers in this codebase.

## Subsystems

### Driver loading

`YdbProviderAdapter` (`Internal/DataProvider/Ydb/YdbProviderAdapter.cs:31`) is the single `IDynamicProviderAdapter` for this area. It loads two assemblies at runtime:

- `Ydb.Sdk` (`AssemblyName = "Ydb.Sdk"`) -- contains `Ydb.Sdk.Ado.YdbConnection`, `YdbCommand`, `YdbParameter`, `YdbDataReader`, `YdbTransaction`, the `YdbType.YdbDbType` enum, and `Ydb.Sdk.Ado.BulkUpsert.IBulkUpsertImporter`.
- `Ydb.Protos` (`ProtosAssemblyName = "Ydb.Protos"`) -- contains `Ydb.Value`, `Ydb.Type`, `Ydb.DecimalType`; used exclusively for the 128-bit decimal wire encoding.

All calls into these assemblies are bridged via `TypeMapper` wrappers. There is no provider-selection enum (single driver family, unlike PROV-SQLSERVER or PROV-MYSQL), and there is no `YdbProviderDetector`; provider detection is performed in `YdbTools.ProviderDetector` (`DataProvider/Ydb/YdbTools.cs:26`) by checking whether `ProviderName` or `ConfigurationString` contains `"Ydb"`.

For schema introspection, `YdbProviderAdapter` also wraps `Ydb.Sdk.Ado.Session.ISession`, `Ydb.Sdk.IDriver`, `Ydb.Sdk.Ado.YdbSchema` (internal-static `DescribeTable`, resolved via `[WrappedBindingFlags]`), `Ydb.Sdk.Ado.Schema.YdbTableDescription`, and `Ydb.Sdk.Ado.Schema.DescribeTableSettings`. `YdbProviderAdapter.GetPrimaryKeys(DbConnection, IEnumerable<string>)` (`YdbProviderAdapter.cs:225`) resolves the driver once per batch and reads each table's ordered PK columns via the gRPC describe call -- the only source for PK metadata, since neither SQL introspection nor `GetSchema("Columns")` expose it.

### Data provider

`YdbDataProvider` (`Internal/DataProvider/Ydb/YdbDataProvider.cs:21`) extends `DynamicDataProviderBase<YdbProviderAdapter>`. Notable `SqlProviderFlags` settings:

- `IsSkipSupported = false`, `IsSkipSupportedIfTake = true` -- YDB lacks standalone `OFFSET` without `LIMIT` (tracked as GitHub issue 11258 in the YDB project; a comment notes the `LIMIT big_num, X` workaround used by ClickHouse does not work for YDB).
- `IsComplexJoinConditionSupported = false`, `IsNestedJoinsSupported = false`, `IsCrossJoinSyntaxRequired = true`.
- `DefaultMultiQueryIsolationLevel = IsolationLevel.Serializable` -- only Serializable is supported.
- `IsCommonTableExpressionsSupported = true` but emulated via table expressions (see `YdbSqlOptimizer`).
- `SupportedCorrelatedSubqueriesLevel = 0` -- correlated subqueries not supported (simple correlated subqueries are handled via CTE promotion).
- `IsSupportedSimpleCorrelatedSubqueries = false` -- explicitly set alongside `SupportedCorrelatedSubqueriesLevel = 0` (`YdbDataProvider.cs:55`).
- `IsSubQueryOrderBySupported = true`, `IsUnionAllOrderBySupported = true`.
- `IsOrderByAggregateFunctionSupported = false` -- YQL rejects an aggregate function directly in `ORDER BY`; it must be projected as a column and referenced by alias, so the aggregate-projecting subquery is kept un-flattened (`YdbDataProvider.cs:36`).
- `IsDistinctSetOperationsSupported = false`.
- `DefaultNullsOrdering = NullsDefaultOrdering.Smallest` -- NULLS ordering is emulated (`IsNullsOrderingSupported` left unset); NULL sorts as the smallest value.
- `RowConstructorSupport = RowFeature.Equality | Comparisons | Between | In | UpdateLiteral`.
- `SupportsPredicatesComparison = true`, `IsDistinctFromSupported = true`.
- `IsSupportsJoinWithoutCondition = false` -- YQL rejects a `JOIN ON` without a real input-dependent predicate ("each equality predicate argument must depend on exactly one JOIN input"), so the engine must not emit `JOIN ON 1=1` (`YdbDataProvider.cs:59`).

`YdbDataProvider` overrides `SetParameter` with extensive numeric type coercion covering all integer widths (`sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`) and floating-point types (`float`, `double`, `decimal` coercions also cover `DataType.Single`, `DataType.Double`, `DataType.DecFloat`). `SetParameterType` maps linq2db `DataType` values to `YdbDbType` enum members. The `DataType.Array` flag is handled inline: if set, the `YdbDbType.List` flag is OR-ed into the resulting type.

Special parameter handling:

- `char` is always promoted to `string` (provider has no char parameter type).
- `DataType.Binary`/`VarBinary` string values are UTF-8-encoded before passing.
- `DataType.Yson` string values are UTF-8-encoded to `byte[]`.
- `DateTime` values with `DateTimeKind.Local` are converted to UTC; `Unspecified` kind is re-specified as UTC. `DateTimeOffset` values are converted to `UtcDateTime`. This applies to `DataType.Date`, `Date32`, `DateTime`, `DateTime64`, `DateTime2`, `DateTimeOffset`, `Timestamp64` (`YdbDataProvider.cs:375-392`).
- `GetSchemaProvider()` returns a `YdbSchemaProvider` instance (`YdbDataProvider.cs:119`) -- schema introspection is now implemented (see Schema provider subsystem below).
- `IsDBNullAllowed` always returns `true` (provider does not implement `GetSchemaTable`).

### Schema provider (`YdbSchemaProvider`)

`YdbSchemaProvider` (`Internal/DataProvider/Ydb/YdbSchemaProvider.cs:19`) extends `SchemaProviderBase`. YDB has no SQL-queryable catalog (no `information_schema`; `.sys` views are monitoring-only), so it reads metadata through the ADO `GetSchema` collections:

- `GetTables` reads `GetSchema("Tables")`, skips `.sys`-prefixed paths and rows typed `SYSTEM_TABLE`, and splits each YDB path (`dir/sub/Customers`) into `SchemaName`/`TableName` via `SplitPath` -- mirrors `YdbSqlBuilder.BuildObjectName`'s `/Database/Schema/Name` layout.
- `GetColumns` reads `GetSchema("Columns")`; `ParseType` splits a raw type string like `"Decimal(22, 9)"` into name/precision/scale.
- `GetPrimaryKeys` batches all table IDs into one call to `YdbProviderAdapter.Instance.GetPrimaryKeys` (one gRPC describe pass for the whole batch) -- `GetSchema("Columns")` doesn't expose PK flags.
- `GetForeignKeys` always returns empty -- YDB has no foreign keys.
- `GetDataTypes` always returns an empty list (`GetDbType`/`GetDataType` overloads return `null`); type resolution instead goes through a private `_typeMap` dictionary keyed by the YQL type name the driver reports (`SchemaUtils.YqlTableType`: `Utf8` -> `"Text"`, `String` -> `"Bytes"`, `Decimal` -> `"Decimal(p, s)"`, everything else the primitive enum name).

`GetServerVersion` executes `SELECT Version();`.

### SQL builder (`YdbSqlBuilder`)

`YdbSqlBuilder` (`Internal/DataProvider/Ydb/YdbSqlBuilder.cs:19`) extends `BasicSqlBuilder<YdbOptions>`.

YQL identifier quoting uses backticks (`` ` ``). Quoting is required when an identifier starts with a digit, contains non-ASCII-alphanumeric/underscore characters, or is a reserved word. The `__ydb_` prefix is disallowed even in quoted mode.

CTE emission is non-standard: each CTE is written as `$name = SELECT ...;` (dollar-sign prefix, assignment syntax) rather than standard `WITH name AS (...)`. `BuildWithClause` iterates clauses and emits each as a standalone assignment terminated with `;`. The `IsCteColumnListSupported` property returns `false`.

Table path construction (`BuildObjectName`) supports YDB's hierarchical path format: `/database/path/tablename` assembled from `SqlObjectName.Database`, `Schema`, and `Name` components.

`CAST` is wrapped in `Unwrap(...)` (`BuildSqlCastExpression`) only when the cast result cannot be null -- YQL `CAST` yields `Optional<T>`, but `Unwrap()` on a nullable cast throws "Failed to unwrap empty optional" at runtime when the value is actually NULL (`YdbSqlBuilder.cs:585-600`).

`RETURNING` is supported for identity retrieval (`BuildGetIdentity`) but RETURNING columns cannot be table-qualified (`_buildTableName = false` during `BuildOutputColumnExpressions`).

Identity field DDL uses PostgreSQL-style serials: `SMALLSERIAL`, `SERIAL`, `BIGSERIAL` for `Int16`/`Int32`/`Int64` identity columns.

`BuildMergeStatement` throws `LinqToDBException` -- YDB does not support SQL MERGE.

`ORDER BY` skips constant expressions and sorts by alias name when the column has an alias (documented in code as a YDB bug). When an `SqlOrderByItem` has `NullsPosition != Sql.NullsPosition.None`, `NULLS FIRST` or `NULLS LAST` is appended to the order expression (`YdbSqlBuilder.cs:544-549`); this path is only reached when the engine leaves a native nulls-position directive rather than emitting a CASE-key rewrite.

`CanSkipRootAliases` returns `false` -- duplicate aliases in the final SELECT are not supported by YDB (`YdbSqlBuilder.cs:100`).

`BuildInListPredicate` materialises parameters from list-typed `SqlParameter` values and re-emits them as individual `$Ids{bucket}_{n}` named parameters to work around YDB's lack of array-typed IN parameters. It also strips NULL items from the materialized list and re-adds their semantics explicitly: under `CompareNulls.LikeClr` a NULL in the list becomes an `OR ... IS NULL`; under `LikeSql` a NULL anywhere in a `NOT IN` list collapses the whole predicate to `false` (three-valued logic), and an all-NULL `IN` list also collapses to `false` -- YQL forbids a NULL literal directly inside an `IN` list (`YdbSqlBuilder.cs:460-492`).

**String concatenation via `ConcatStyle` (PR #5504):** `YdbSqlBuilder` overrides `ConcatStyle` to return `ConcatBuildStyle.Pipes` (`YdbSqlBuilder.cs:36`). This instructs `BasicSqlBuilder` to emit `||` when building `SqlConcatExpression` nodes introduced by PR #5504. This is the builder-level concat path; the visitor-level binary `+` rewrite in `YdbSqlExpressionConvertVisitor` now covers only `Binary`/`VarBinary`/`Blob` byte concatenation (the string-type `+` arm was removed -- see Expression conversion visitor section).

**VALUES-table typing and from-less/empty sources:** `IsSqlValuesTableValueTypeRequired` forces `CAST(value AS <type>)` on a VALUES cell when the first row holds a bare numeric literal (YDB may infer a narrower type than the column) or when every cell in a column is untyped NULL -- mirrors the PostgreSQL builder (`YdbSqlBuilder.cs:40-47`). `SupportsColumnAliasesInScalarSource => false` -- a derived column list is legal only on `VALUES`, not a scalar/raw-SQL subquery source. `BuildSelectClause`/`BuildEmptyValuesFrom` supply a one-row dummy source `FROM (SELECT 1) AS dual` for a from-less filtered constant query or an empty VALUES projection -- YQL rejects `WHERE` without `FROM` and has no `DUAL` (mirrors `MySql57SqlBuilder`) (`YdbSqlBuilder.cs:312-329`).

`BuildSql` pre-registers every CTE name with the parameter normalizer before the statement body is built, so a parameter name generated during the build can't collide with a `$name` CTE variable -- resolves the conflict noted in `BasicSqlOptimizer.FinalizeCte` (`YdbSqlBuilder.cs:333-354`). `BuildFromClause` is skipped entirely for UPDATE statements (`YdbSqlBuilder.cs:377-381`).

`BuildDataTypeFromDataType` renders DDL/CAST target type names (`Bool`, `Int8`/`Uint8`/`Int16`/`Uint16`/`Int32`/`Uint32`/`Int64`/`Uint64`, `Float`, `Double`, `DyNumber`, `Bytes`, `Text`, `Json`, `JsonDocument`, `Yson`, `Uuid`, `Date`/`Date32`/`Datetime`/`Datetime64`/`Timestamp`/`Timestamp64`, `Interval`/`Interval64`, `Tz*` variants, `Decimal(p,s)`), wrapping the result in `List<...>` when `DataType.Array` is set.

### SQL optimizer (`YdbSqlOptimizer`)

`YdbSqlOptimizer` (`Internal/DataProvider/Ydb/YdbSqlOptimizer.cs:11`) extends `BasicSqlOptimizer`.

`TransformStatement` disables table aliases on DELETE and UPDATE (sets alias to `$`; the dollar-sign is YDB's anonymous scope marker). UPDATE is rewritten to alternative form via `GetAlternativeUpdate`. INSERT with a `SELECT` sub-query has column aliases injected from the insert items so that column names propagate correctly (`CorrectInsertStatement`).

`Finalize` promotes all scalar correlated sub-queries to CTEs via `MoveScalarSubQueriesToCte`. The transformation walks the AST and wraps any single-column, non-dependent `SelectQuery` found in a column, predicate, or set-expression position into a `CteClause`+`SqlCteTable` pair. After promotion it calls `FinalizeCte` to finalise names.

The rewrite itself now lives in a dedicated visitor, `YdbScalarSubQueryToCteVisitor` (`Internal/DataProvider/Ydb/YdbScalarSubQueryToCteVisitor.cs:25`), pooled via `ObjectPool<YdbScalarSubQueryToCteVisitor>` (`Pool`, capacity 100). It classifies each element's *position* (`Scalar` -- column/predicate/scalar-expression slot, becomes a bare `$cte` reference; `Wrapped` -- an IN/EXISTS operand, becomes a trivial `SELECT <col> FROM $cte`; `Excluded` -- table source, statement root, or an equality-comparison operand) via `ClassifyChildren`, bottom-up in a single finalize-phase pass (`VisitMode.Modify`). `subquery = x` / `subquery <> x` comparisons on a single-column non-correlated subquery rewrite to `x [NOT] IN (subquery)` (`TryRewriteScalarComparisonToIn`) before the generic position-based rewrite runs. Structurally-identical subqueries (compared via `ISqlExpressionEqualityComparer`) share one CTE (`_scalarCtes` dictionary), so a subquery duplicated across clauses -- e.g. a SELECT-column copy plus the rendered INSERT/UPDATE item -- collapses to a single named query. `YdbSqlOptimizer.MoveScalarSubQueriesToCte` also walks `SqlUpdateStatement.Update` (so `UPDATE SET` subqueries move to CTEs, processed first so duplicate SELECT-column copies dedup against them) and the bodies of any CTEs that already existed before this pass (e.g. the eager-load union carrier's `query.Count()` FROM-less scalar subquery), iterating only the pre-pass CTE count since lifted CTEs carry no further inline subqueries.

`ReplaceTableAll` clears `SELECT *` from `EXISTS` sub-queries and replaces them with `SELECT 1` to avoid ambiguous duplicate column names.

`SetQueryParameter` forces `Date32`/`DateTime64`/`Timestamp64`/`Interval64` parameters whose .NET type is `int`/`long` to be emitted as literals (`IsQueryParameter = false`) because the provider does not accept these as bound parameters.

### Expression conversion visitor (`YdbSqlExpressionConvertVisitor`)

`YdbSqlExpressionConvertVisitor` (`Internal/DataProvider/Ydb/YdbSqlExpressionConvertVisitor.cs:11`) extends `SqlExpressionConvertVisitor`.

- Byte concatenation (`+` on `Binary`/`VarBinary`/`Blob`) is rewritten to `||`. **Note (PR #5504):** the previous string-type concatenation arm (`+` on `NVarChar`/`Char`/`VarChar` -> `||`) has been removed; string concat is now handled by `ConcatStyle.Pipes` in `YdbSqlBuilder` via the `SqlConcatExpression` AST node.
- Bitwise operations (`&`, `|`, `^`) require unsigned operands; operands are cast to unsigned via `SqlCastExpression` when needed. `ConvertSqlUnaryExpression` applies the same treatment to bitwise negation (`~`), casting a signed operand to unsigned when needed (`YdbSqlExpressionConvertVisitor.cs:75-88`).
- `AlignFloatingDecimal` (called from `ConvertSqlBinaryExpression` for `+ - * / %`) casts a `Decimal` operand to `Double` when the other operand is floating-point -- YQL arithmetic rejects mixing a floating-point operand with a Decimal operand (`YdbSqlExpressionConvertVisitor.cs:131-150`).
- `ConvertSqlFunction` routes `Math::Floor`/`Math::Ceil`/`Math::Trunc` through `Double` for a `Decimal` argument (those UDFs are Double-only), casting the result back to the original Decimal type.
- `ConvertConversion` overrides several CAST rewrites that work around gaps in YQL's `CAST`:
  - string -> `Datetime`/`Timestamp`: rewrites the conventional `'yyyy-MM-dd HH:mm:ss'` text to ISO-8601 (`Unicode::ReplaceAll` space->`'T'`, append `'Z'`) and parses via `DateTime::ParseIso8601` + `DateTime::MakeDatetime`/`MakeTimestamp` -- a bare `CAST` only accepts ISO input and would return an empty Optional (`Unwrap` then throws "Failed to unwrap empty optional").
  - string -> `Interval`/`Time`: splits on `:` (`Unicode::SplitToList`/`ListHead`/`ListSkip`) and sums the parts as seconds via `DateTime::IntervalFromSeconds`, since `CAST(string AS Interval)` only parses ISO-8601 durations and silently zeroes a conventional `'H:MM:SS'` string.
  - floating -> `Decimal`: routed through a string cast (`CAST(CAST(x AS Text) AS Decimal(p,s))`) -- YQL has no direct float->Decimal cast.
  - `Decimal` -> integer: routed through `Double` -- YQL's `CAST(Decimal AS int)` rounds, but C# `(int)decimal` truncates toward zero; `Double`'s truncating cast matches C# semantics.
- `VisitInSubQueryPredicate` folds a row-valued (`SqlRowExpression`) `IN`-subquery projecting more than one column into a single tuple column (`(a, b) IN (SELECT (c1, c2) ...)`) -- YQL's `IN` requires a single-column source and rejects the row-valued N-column form; the correlated-EXISTS rewrite the base class would otherwise use is unavailable since YDB doesn't support correlated subqueries.
- `TO_LOWER`/`TO_UPPER`/`REPLACE` pseudo-functions map to `Unicode::ToLower`, `Unicode::ToUpper`, `Unicode::ReplaceAll`.
- LIKE uses `%` and `_` as escape characters. Case-insensitive search emits `ILIKE`.
- `CASE WHEN ... THEN ... END` without `ELSE` gets a `NULL` ELSE appended (required by YQL).
- `ConcatRequiresExplicitStringCast => false` -- YQL's `||` auto-coerces non-string operands to text, so an explicit `CAST(x AS String)` on concat operands is redundant.
- `SupportsNullIf => false` -- YQL has no `NULLIF` builtin; the `CASE WHEN a = b THEN NULL ELSE a END` form is kept instead of folding to `NULLIF`.
- `SupportsNullInColumn` returns `false`.

### Member translator (`YdbMemberTranslator`)

`YdbMemberTranslator` (`Internal/DataProvider/Ydb/Translation/YdbMemberTranslator.cs:15`) extends `ProviderMemberTranslatorDefault`.

Date functions call the `DateTime::` module (e.g. `DateTime::GetYear`, `DateTime::GetDayOfMonth`, `DateTime::GetWeekOfYearIso8601`, `DateTime::ShiftYears`, `DateTime::ShiftMonths`, `DateTime::IntervalFromDays/Hours/Minutes/Seconds/Milliseconds`). `DateTimeOffset` date-part extraction delegates to the same `DateTime::` functions as `DateTime` (via `TranslateDateTimeOffsetDatePart` forwarding to `TranslateDateTimeDatePart`). `DateTimeOffset` truncation is not supported (throws `NotSupportedException`).

Quarter date-add uses `DateTime::ShiftMonths` with `increment * 3` (`YdbMemberTranslator.cs:569-572`), then wraps via `DateTime::MakeDatetime`.

"Now" dispatch (PR #5467, `YdbMemberTranslator.cs:460-483`):

- `TranslateServerNow` -- emits `CurrentUtcTimestamp()` typed as `DateTime`.
- `TranslateNow` -- returns `null` (local-time "now" is unsupported; YDB has no local-time server clock).
- `TranslateUtcNow` -- emits `CurrentUtcTimestamp()` typed as `DateTime`.
- `TranslateZonedUtcNow` -- emits `CurrentUtcTimestamp()` using the caller-supplied `dbDataType` (supports `DateTimeOffset`-typed UTC-now).

All four overrides resolve to `CurrentUtcTimestamp()` or unsupported; there is no fallback to `CURRENT_TIMESTAMP`.

`DateTime::TimeOfDay` is used for `TranslateDateTimeTruncationToTime` (returns `Interval`-typed result). `TranslateMakeDateTime` builds a YQL timestamp/date from constructor parts by assembling the conventional `'yyyy-MM-dd HH:mm:ss[.fff]'` text (zero-padding each component via `Unicode::Substring(CAST(100/1000 + x AS String), ...)`, which also keeps the expression out of client-side constant folding) and `CAST`ing it -- the string->Datetime/Timestamp rewrite in `YdbSqlExpressionConvertVisitor.ConvertConversion` owns the actual ISO parsing, so the format lives in one place. `TranslateDateTimeTruncationToDate` truncates via `DateTime::Split` -> `DateTime::StartOfDay` -> `DateTime::MakeTimestamp` rather than a `CAST(Timestamp AS Date AS Timestamp)` round-trip, because `SqlCastExpression.IsMandatory` doesn't survive the remote LinqService serialization boundary and the round-trip would collapse (silently skipping truncation) only on the remote side.

String functions: `PadLeft` -- `String::LeftPad`; `Replace` -- `Unicode::ReplaceAll`; `String.Join` emits `AGGREGATE_LIST` + `Unicode::JoinFromList` with complex ORDER BY simulation via `ListSort`+key-lambda+`ListMap`. `ListConcat`+`AsList`+`ListNotNull` handle `CONCAT_WS`.

**`TrimStart`/`TrimEnd` via RE2 (PR #5515):** `TranslateTrimStart` and `TranslateTrimEnd` are now implemented via `TranslateRegexTrim` (`YdbMemberTranslator.cs:94`). YDB/YQL has no native `LTRIM`/`RTRIM`. The translator emits `CAST(Re2::Replace({pattern})(CAST({value} AS String?), '') AS Utf8?)` with an anchored RE2 character-class pattern: `^\s+` / `\s+$` for whitespace-only trim (no `trimChars` argument), or `^[chars]+` / `[chars]+$` for an explicit constant character set. Special RE2 metacharacters (`\`, `]`, `^`, `-`, `[`) in `chars` are escaped. When `trimChars` is not a constant `SqlValue<string>` at translation time, the method returns `null` (falls back to client-side evaluation).

**`String.Join` ORDER BY sort encoding (current implementation):** The `WithSort` path builds a tuple `(k1, k2, ..., value)` per row, aggregates via `AGGREGATE_LIST`, then sorts with `ListSort(tuplesArr, () -> { return (nullsKey, key, ...) })`. Numeric DESC is encoded by negation; DateTime DESC by `CAST AS Int64` then negation; string DESC (only valid as the first and only string key) triggers `reverseWholeArray = true` and a final `ListReverse`. DISTINCT is applied before sort via `ListUniq`. Unsupported ORDER patterns (string DESC after the first, or string DESC with other string keys following) fall back via `SetFallback(fc => fc.AllowOrderBy(false))` (`YdbMemberTranslator.cs:255-259`).

**`String.Join` without separator (PR #5504):** `TranslateStringJoin` now accepts `withoutSeparator` bool. When true, the aggregate builder uses `HasSequenceIndex(0)` (no separator argument consumed from the call) and `separator` is set to `factory.Value(valueType, string.Empty)`. `ConfigureConcatWs` receives `withoutSeparator:` forwarded, enabling `string.Concat`-style aggregation over sequences without an explicit separator.

Math: `Max`/`Min` -- `MAX_OF`/`MIN_OF`; `Pow` -- `Math::Pow` (operands are cast to `double` when not already `float`/`double`; result is cast back to the original type if different); `RoundToEven` -- `Math::NearbyInt` with `Math::RoundToNearest()`.

Guid-to-string -- `CAST AS Utf8`. `Guid.NewGuid()` is commented out (would generate the same UUID for all invocations in a single query).

`SqlTypesTranslation.ConvertBit` throws `NotSupportedException`. `SqlTypesTranslation.ConvertDateOnly` (under `#if SUPPORTS_DATEONLY`) also throws `NotSupportedException("52")` (`YdbMemberTranslator.cs:423-428`).

**Window functions (`YdbWindowFunctionsMemberTranslator`):** a nested `WindowFunctionsMemberTranslator` override (`YdbMemberTranslator.cs:784-799`), wired via `CreateWindowFunctionsMemberTranslator()`. Validated against `ydbplatform/local-ydb`: only `ROWS` frames are accepted (`IsFrameRangeSupported`, `IsFrameGroupsSupported`, `IsFrameExclusionSupported` all `false`); `PERCENTILE_CONT`/`PERCENTILE_DISC` unsupported (`IsPercentileContSupported`/`IsPercentileDiscSupported` `false`); `IGNORE`/`RESPECT NULLS` works for `FIRST_VALUE`/`LAST_VALUE`/`NTH_VALUE` (`IsValueNullTreatmentSupported = true`) but not `LEAD`/`LAG`; `LEAD`/`LAG` accept only value+offset, no default-value argument (`IsLeadLagDefaultSupported = false`); there is no `NTH_VALUE ... FROM LAST`.

### Bulk copy (`YdbBulkCopy`)

`YdbBulkCopy` (`Internal/DataProvider/Ydb/YdbBulkCopy.cs:18`) extends `BasicBulkCopy`.

Default bulk copy type (from `YdbOptions.BulkCopyType`) is `BulkCopyType.ProviderSpecific`. The provider-specific path calls `YdbProviderAdapter.BeginBulkCopy` which wraps `YdbConnection.BeginBulkUpsertImport(tableName, columnNames, cancellationToken)` -- this returns `IBulkUpsertImporter`. Rows are streamed through `IBulkUpsertImporter.AddRowAsync` and periodically flushed via `FlushAsync` every `MaxBatchSize` rows (default 10,000). Values are passed as provider-typed `DbParameter.Value` objects (not re-serialised as strings). If no unwrapped provider connection is available, falls back to `MultipleRowsCopy1`.

`ProviderSpecificCopy`/`ProviderSpecificCopyAsync` also fall back to `MultipleRowsCopy`/`MultipleRowsCopyAsync` (row-by-row `INSERT`) whenever a transaction is already open on the connection -- YDB's `BulkUpsert` API cannot run inside an active transaction and takes no transaction argument; mirrors `InformixBulkCopy`'s handling of the same constraint.

### Retry policy (area-unique)

`YdbRetryPolicy` (`Internal/DataProvider/Ydb/YdbRetryPolicy.cs:13`) and `YdbTransientExceptionDetector` (`Internal/DataProvider/Ydb/YdbTransientExceptionDetector.cs:19`) together form a retry subsystem that does **not** exist in any other provider area.

`YdbRetryPolicy` extends `RetryPolicyBase` (`Data/RetryPolicy/RetryPolicyBase.cs`) from the INFRA area. It is not wired automatically by `YdbDataProvider` -- it must be passed to `DataOptions` by the consumer (`DataOptions.UseRetryPolicy(...)`).

Default parameters mirror the YDB SDK defaults: 10 attempts; fast backoff base 5 ms, cap 500 ms; slow backoff base 50 ms, cap 5000 ms; idempotence disabled.

`GetNextDelay` overrides base-class logic: for YDB-typed exceptions it dispatches on the status code name (string comparison, no hard SDK dependency):
- `BadSession`/`SessionBusy`/`SessionExpired` -- 0 ms.
- `Aborted`/`Undetermined` -- Full Jitter (fast parameters).
- `Unavailable`/`ClientTransportUnknown`/`ClientTransportUnavailable` -- Equal Jitter (fast).
- `Overloaded`/`ClientTransportResourceExhausted` -- Equal Jitter (slow).
- Non-YDB exceptions -- base exponential retry.

`YdbTransientExceptionDetector` inspects `Ydb.Sdk.Ado.YdbException` without a compile-time reference to the SDK: it walks the exception chain looking for a type whose `FullName` equals `"Ydb.Sdk.Ado.YdbException"`, then reads `IsTransient` and `Code` properties via reflection. This avoids any hard dependency on `Ydb.Sdk` in the core library.

### Mapping schema (`YdbMappingSchema`)

`YdbMappingSchema` (`Internal/DataProvider/Ydb/YdbMappingSchema.cs:15`) extends `LockedMappingSchema`.

Key constants: `DEFAULT_DECIMAL_PRECISION = 22`, `DEFAULT_DECIMAL_SCALE = 9`, `DEFAULT_TIMEZONE = "GMT"`, `MAX_DECIMAL_PRECISION = 35` -- YQL rejects `Decimal` with precision above 35 (`Decimal(36,*)` -> type-annotation error).

`GetCommonDecimalType(x, y)` (`YdbMappingSchema.cs:31`) widens two Decimal `DbDataType`s to a common precision/scale (max integer digits, max scale, capped at 35, preserving the integer part in full) so YQL constructs requiring identical Decimal types on both sides -- e.g. `MIN_OF`/`MAX_OF` in `YdbMemberTranslator.TranslateMinMax` -- can align mismatched operands without overflow.

Type literal formats:
- Integers: suffix-typed (`0ut` = UInt8, `0t` = Int8, `0s` = Int16, `0us` = UInt16, bare = Int32, `0u` = UInt32, `0l` = Int64, `0ul` = UInt64).
- Floats: `Float('...')`, `Double('...')`, with special literals for infinity.
- Decimal: `Decimal('value', precision, scale)`.
- DyNumber: `DyNumber('value')` (YDB-specific arbitrary-precision decimal).
- String: single-quoted with suffix (`u` for UTF-8 text, `s` for bytes, `j` for JSON, `y` for YSON).
- Binary: hex-encoded with suffix.
- Bool: `true`/`false`.
- UUID: `Uuid('...')`.
- Dates: `Date('...')`, `Datetime('...')`, `Timestamp('...')`, `Date32('...')`, `Datetime64('...')`, `Timestamp64('...')`.
- Tz variants: `TzDate('...,GMT')`, etc.
- Interval: `Interval('PT...')` / `Interval64('PT...')` (ISO 8601, truncated to 100 ns).
- List literals: `[v1, v2, ...]` (currently only `bool[]`/`List<bool>` registered).

`AddScalarType` registrations: `DateTimeOffset` -- `DateTime2`, `TimeSpan` -- `Interval`, `MemoryStream` -- `VarBinary`.

The provider-specific decimal wire encoding in `YdbProviderAdapter.MakeDecimalFromString` constructs a `YdbValue` from a 128-bit integer (via `Int128` on net8+ or `BigInteger` on earlier targets) built from `Ydb.DecimalType` + `Ydb.Value` protos.

### Options

`YdbOptions` (`DataProvider/Ydb/YdbOptions.cs:19`) is a `DataProviderOptions<YdbOptions>` record with two fields:
- `BulkCopyType` -- default `ProviderSpecific`.
- `UseParametrizedDecimal` -- when `true` (default), emits `Decimal(p,s)` with explicit precision/scale in DDL; when `false`, emits bare `Decimal`.

### Public-surface entry points

`YdbTools` (`DataProvider/Ydb/YdbTools.cs:19`) is the static public-API class: `GetDataProvider()`, `CreateDataConnection(string/DbConnection/DbTransaction)`, `ResolveYdb(string/Assembly)`, `ClearAllPools()`, `ClearPool(DbConnection)`. Provider detection uses `ProviderDetectorBase<Fake>.CreateDataProvider<YdbDataProvider>()` (no version enum; single provider instance).

`IYdbSpecificQueryable<T>` and `IYdbSpecificTable<T>` mark provider-specific query chains. `YdbSpecificExtensions.AsYdb<T>` wraps any `ITable<T>` or `IQueryable<T>` into the YDB-specific variant. `YdbHints` exposes `QueryHint`, `UniqueHint`, `DistinctHint` extension methods emitting `--+ hint(...)` YQL comment directives.

## Key types

| Type | File | Role |
|---|---|---|
| `YdbDataProvider` | `Internal/DataProvider/Ydb/YdbDataProvider.cs` | Main provider, `DynamicDataProviderBase<YdbProviderAdapter>` |
| `YdbProviderAdapter` | `Internal/DataProvider/Ydb/YdbProviderAdapter.cs` | Dynamic driver loader; loads `Ydb.Sdk` + `Ydb.Protos` |
| `YdbSchemaProvider` | `Internal/DataProvider/Ydb/YdbSchemaProvider.cs` | ADO-`GetSchema`-based schema introspection (Tables/Columns/PK via gRPC describe) |
| `YdbSqlBuilder` | `Internal/DataProvider/Ydb/YdbSqlBuilder.cs` | YQL SQL emission, backtick quoting, CTE-as-assignment, `ConcatStyle.Pipes`, `NULLS FIRST/LAST` |
| `YdbSqlOptimizer` | `Internal/DataProvider/Ydb/YdbSqlOptimizer.cs` | Scalar-subquery--CTE promotion (delegates to `YdbScalarSubQueryToCteVisitor`), alias fixups, literal coercion |
| `YdbScalarSubQueryToCteVisitor` | `Internal/DataProvider/Ydb/YdbScalarSubQueryToCteVisitor.cs` | Pooled, position-classifying visitor lifting scalar subqueries to CTEs |
| `YdbSqlExpressionConvertVisitor` | `Internal/DataProvider/Ydb/YdbSqlExpressionConvertVisitor.cs` | Expression rewrites for YQL specifics (CAST rewrites, bitwise/decimal alignment, row-valued IN) |
| `YdbMappingSchema` | `Internal/DataProvider/Ydb/YdbMappingSchema.cs` | Literal generators for all YDB types; common-Decimal-type widening |
| `YdbBulkCopy` | `Internal/DataProvider/Ydb/YdbBulkCopy.cs` | `IBulkUpsertImporter`-based native bulk upsert |
| `YdbRetryPolicy` | `Internal/DataProvider/Ydb/YdbRetryPolicy.cs` | Full/Equal Jitter retry; mirrors YDB SDK defaults |
| `YdbTransientExceptionDetector` | `Internal/DataProvider/Ydb/YdbTransientExceptionDetector.cs` | Reflection-based YDB exception inspector (no hard SDK dep) |
| `YdbMemberTranslator` | `Internal/DataProvider/Ydb/Translation/YdbMemberTranslator.cs` | LINQ member--YQL function translation |
| `YdbWindowFunctionsMemberTranslator` | `Internal/DataProvider/Ydb/Translation/YdbMemberTranslator.cs` (nested) | Window-function capability flags (ROWS-only frames, no PERCENTILE_CONT/DISC) |
| `YdbOptions` | `DataProvider/Ydb/YdbOptions.cs` | Provider options: bulk copy type, decimal notation |
| `YdbTools` | `DataProvider/Ydb/YdbTools.cs` | Public-surface static factory + pool management |
| `YdbHints` | `DataProvider/Ydb/YdbHints.cs` | YQL query hints |
| `IYdbSpecificQueryable<T>` | `DataProvider/Ydb/IYdbSpecificQueryable.cs` | Provider-specific query marker interface |
| `IYdbSpecificTable<T>` | `DataProvider/Ydb/IYdbSpecificTable.cs` | Provider-specific table marker interface |

## Files (Tier 1 / Tier 2)

### Tier 1 (read in full -- proposed anchors)

| File | Purpose |
|---|---|
| `Internal/DataProvider/Ydb/YdbDataProvider.cs` | Main provider class |
| `Internal/DataProvider/Ydb/YdbSqlBuilder.cs` | YQL SQL builder |
| `Internal/DataProvider/Ydb/YdbSqlOptimizer.cs` | SQL optimizer |
| `Internal/DataProvider/Ydb/YdbScalarSubQueryToCteVisitor.cs` | Scalar-subquery-to-CTE rewrite visitor (extracted from optimizer) |
| `Internal/DataProvider/Ydb/YdbProviderAdapter.cs` | Dynamic adapter / driver wrapper |
| `Internal/DataProvider/Ydb/YdbSchemaProvider.cs` | Schema introspection (new) |
| `Internal/DataProvider/Ydb/YdbMappingSchema.cs` | Type mapping + literal generation |
| `Internal/DataProvider/Ydb/YdbBulkCopy.cs` | Bulk upsert implementation |
| `Internal/DataProvider/Ydb/YdbSqlExpressionConvertVisitor.cs` | Expression conversion |
| `Internal/DataProvider/Ydb/Translation/YdbMemberTranslator.cs` | Member translation |
| `Internal/DataProvider/Ydb/YdbRetryPolicy.cs` | Retry policy (area-unique) |
| `Internal/DataProvider/Ydb/YdbTransientExceptionDetector.cs` | Transient exception detector (area-unique) |
| `DataProvider/Ydb/YdbTools.cs` | Public-surface factory |
| `DataProvider/Ydb/YdbOptions.cs` | Provider options |
| `DataProvider/Ydb/YdbHints.cs` | YQL query hints |

### Tier 2 (all read in full -- small area)

| File | Notes |
|---|---|
| `DataProvider/Ydb/YdbHints.generated.cs` | T4-generated `UniqueHint`/`DistinctHint` overloads |
| `DataProvider/Ydb/IYdbSpecificQueryable.cs` | Marker interface |
| `DataProvider/Ydb/IYdbSpecificTable.cs` | Marker interface |
| `DataProvider/Ydb/YdbSpecificExtensions.cs` | `AsYdb<T>` extension methods |
| `Internal/DataProvider/Ydb/YdbSpecificQueryable.cs` | Internal `DatabaseSpecificQueryable<T>` impl |
| `Internal/DataProvider/Ydb/YdbSpecificTable.cs` | Internal `DatabaseSpecificTable<T>` impl |

(`DataProvider/Ydb/YdbHints.tt` -- T4 template source; Tier 3, generator input.)

## Known issues / debt

- **Schema provider has narrow coverage.** `YdbSchemaProvider` (new) reads tables/columns via ADO `GetSchema` and PKs via a gRPC describe call, but `GetForeignKeys` always returns empty (YDB has no FKs) and `GetDataTypes` always returns an empty list -- type resolution is a private name->`(DataType,Type)` map, not a queryable provider catalog.
- **Correlated subqueries not supported.** `SupportedCorrelatedSubqueriesLevel = 0` and `IsSupportedSimpleCorrelatedSubqueries = false`; the optimizer promotes scalar correlated sub-queries to CTEs via `YdbScalarSubQueryToCteVisitor`, but multi-column or deeply nested correlated patterns have no fallback.
- **`OFFSET` without `LIMIT` not supported.** Tracked in YDB issue 11258. No workaround available (the CH `LIMIT big_num, X` workaround is documented as not working for YDB).
- **Retry policy is opt-in.** `YdbRetryPolicy` is not wired automatically by `YdbDataProvider`; callers must register it explicitly via `DataOptions.UseRetryPolicy(new YdbRetryPolicy())`. A TODO comment in `YdbProviderAdapter.cs` notes the intent to add provider-specific retry with `IsTransientWhenIdempotent` support.
- **`BeginTransaction(TxMode)` not supported.** YDB's extended transaction modes (snapshot, online RO, stale RO) are not exposed. Only Serializable read-write is used.
- **`YdbStruct` not implemented.** A `TODO: YdbStruct` comment in `YdbProviderAdapter.cs` indicates struct support is absent.
- **`Guid.NewGuid()` translation commented out.** `YdbMemberTranslator.cs` notes that `RandomUuid` generates the same UUID for all invocations in a single query.
- **`String.Join` ORDER BY with string DESC is partially restricted.** The translator falls back to no-ORDER when the pattern is unsupported (e.g., string DESC on a non-first key).
- **`DateTimeOffset` truncation methods throw.** `TranslateDateTimeOffsetTruncationToDate` and `TranslateDateTimeOffsetTruncationToTime` throw `NotSupportedException` (`YdbMemberTranslator.cs:438`, `:444`). Date-part extraction for `DateTimeOffset` is supported (delegates to `DateTime::` functions); truncation for plain `DateTime` is supported (`TranslateDateTimeTruncationToDate`/`ToTime`).
- **`SqlTypesTranslation.ConvertBit` throws `NotSupportedException`. `SqlTypesTranslation.ConvertDateOnly` (under `#if SUPPORTS_DATEONLY`) also throws `NotSupportedException("52")` (`YdbMemberTranslator.cs:423-428`).** Mapping `SqlTypes.SqlBoolean` is not implemented.
- **`SqlTypesTranslation.ConvertDateOnly` throws `NotSupportedException`.**  Under `#if SUPPORTS_DATEONLY`, `ConvertDateOnly` throws `NotSupportedException("52")` (`YdbMemberTranslator.cs:423-428`).
- **`modulo` operator commented out.** The `%` rewrite for decimal types in `YdbSqlExpressionConvertVisitor` is commented out; the comment suggests correctness concerns remain.
- **`TrimStart`/`TrimEnd` with non-constant `trimChars` falls back to client-side eval.** `TranslateRegexTrim` returns `null` when `trimChars` is not a compile-time-constant `SqlValue<string>` -- the RE2 pattern must be a static literal for the YDB query planner.
- **Window-frame support restricted to ROWS.** `YdbWindowFunctionsMemberTranslator` rejects RANGE/GROUPS frames and frame EXCLUDE, and `PERCENTILE_CONT`/`PERCENTILE_DISC`; `LEAD`/`LAG` has no default-value argument and there is no `NTH_VALUE ... FROM LAST`.

## Inbound / outbound dependencies

**Inbound:**
- `YdbTools.ProviderDetector` is registered with `ProviderDetectorBase` -- called by the global connection-string dispatcher.
- `YdbRetryPolicy` is consumed by user code via `DataOptions`.

**Outbound:**
- `Internal/DataProvider/DataProviderBase` / `DynamicDataProviderBase`, `SchemaProviderBase` -- INTERNAL-API area.
- `BasicSqlBuilder`, `BasicSqlOptimizer`, `SqlExpressionConvertVisitor` -- SQL-PROVIDER area.
- `BasicBulkCopy`, `BulkCopyReader` -- INTERNAL-API area.
- `RetryPolicyBase` (`Data/RetryPolicy/`) -- INFRA area.
- `ProviderMemberTranslatorDefault`, `WindowFunctionsMemberTranslator` -- INTERNAL-API area.
- `TypeMapper` (`Internal/Expressions/Types/TypeMapper`) -- INTERNAL-API area.
- `Ydb.Sdk` assembly (dynamically loaded at runtime -- not a compile-time reference).
- `Ydb.Protos` assembly (dynamically loaded at runtime -- decimal wire encoding only).

## See also

- [SQL-PROVIDER area](../SQL-PROVIDER/INDEX.md) -- `BasicSqlBuilder`, `BasicSqlOptimizer` base classes.
- [INTERNAL-API area](../INTERNAL-API/INDEX.md) -- `DynamicDataProviderBase`, `BasicBulkCopy`, `TypeMapper`, `MemberTranslatorBase`, `SchemaProviderBase`.
- [INFRA area](../INFRA/INDEX.md) -- `RetryPolicyBase`, retry policy infrastructure.
- [architecture/interceptors.md](../../architecture/interceptors.md) -- retry policy wiring via `DataOptions`.

<details><summary>Coverage</summary>

**Tier 1 -- read in full (15/15):**
- `Internal/DataProvider/Ydb/YdbDataProvider.cs`
- `Internal/DataProvider/Ydb/YdbSqlBuilder.cs`
- `Internal/DataProvider/Ydb/YdbSqlOptimizer.cs`
- `Internal/DataProvider/Ydb/YdbScalarSubQueryToCteVisitor.cs`
- `Internal/DataProvider/Ydb/YdbProviderAdapter.cs`
- `Internal/DataProvider/Ydb/YdbSchemaProvider.cs`
- `Internal/DataProvider/Ydb/YdbMappingSchema.cs`
- `Internal/DataProvider/Ydb/YdbBulkCopy.cs`
- `Internal/DataProvider/Ydb/YdbSqlExpressionConvertVisitor.cs`
- `Internal/DataProvider/Ydb/Translation/YdbMemberTranslator.cs`
- `Internal/DataProvider/Ydb/YdbRetryPolicy.cs`
- `Internal/DataProvider/Ydb/YdbTransientExceptionDetector.cs`
- `DataProvider/Ydb/YdbTools.cs`
- `DataProvider/Ydb/YdbOptions.cs`
- `DataProvider/Ydb/YdbHints.cs`

**Tier 2 -- read in full (7/8):**
- `DataProvider/Ydb/YdbHints.generated.cs` -- read
- `DataProvider/Ydb/IYdbSpecificQueryable.cs` -- read
- `DataProvider/Ydb/IYdbSpecificTable.cs` -- read
- `DataProvider/Ydb/YdbSpecificExtensions.cs` -- read
- `Internal/DataProvider/Ydb/YdbSpecificQueryable.cs` -- read
- `Internal/DataProvider/Ydb/YdbSpecificTable.cs` -- read

**Tier 2 -- skipped (1/8):**
- `DataProvider/Ydb/YdbHints.tt` -- T4 template source; classified Tier 3 (generator input).

**Delta (build-time run -- sha 4a478ff14):**
- `Internal/DataProvider/Ydb/Translation/YdbMemberTranslator.cs` -- re-read for PR #5467. Changes: (1) `TranslateServerNow`/`TranslateNow`/`TranslateUtcNow`/`TranslateZonedUtcNow` overrides split "now" dispatch into four explicit methods; all UTC variants emit `CurrentUtcTimestamp()`, `TranslateNow` returns null. (2) `TranslateDateTimeOffsetDatePart` now forwards to `TranslateDateTimeDatePart` rather than throwing. Prior INDEX.md wording corrected accordingly.

**Read (this run -- delta sha 2e67bafc9):**
- `Internal/DataProvider/Ydb/YdbSqlBuilder.cs` -- PR #5504: added `ConcatStyle` override returning `ConcatBuildStyle.Pipes`; builder now handles `SqlConcatExpression` nodes via `||` emission directly.
- `Internal/DataProvider/Ydb/YdbSqlExpressionConvertVisitor.cs` -- PR #5504: removed the string-type (`NVarChar`/`Char`/`VarChar`) `+` -> `||` rewrite from `ConvertSqlBinaryExpression`; only the byte-type (`Binary`/`VarBinary`/`Blob`) arm remains. String concat now owned by `ConcatStyle.Pipes` in builder.
- `Internal/DataProvider/Ydb/Translation/YdbMemberTranslator.cs` -- PR #5515: `TranslateTrimStart`/`TranslateTrimEnd` now implemented via `TranslateRegexTrim`; emits `CAST(Re2::Replace(pattern)(CAST(value AS String?), '') AS Utf8?)` with anchored RE2 character-class patterns; falls back to null (client-side) when `trimChars` is not a constant literal. PR #5504: `TranslateStringJoin` gained `withoutSeparator` bool parameter; when true uses empty-string separator and `HasSequenceIndex(0)` to handle `string.Concat`-style joins without an explicit separator.


**Read (this run -- delta sha b3340aa9d):**
- `DataProvider/Ydb/YdbOptions.cs` -- no change from prior INDEX.md description; two fields (`BulkCopyType`, `UseParametrizedDecimal`) unchanged.
- `Internal/DataProvider/Ydb/YdbDataProvider.cs` -- added `IsSupportedSimpleCorrelatedSubqueries = false` flag (line 55); `SetParameter` now also handles `DataType.Single`, `DataType.Double`, `DataType.DecFloat`, and `DataType.Decimal` numeric coercions; DateTime UTC-normalization switch now explicitly covers `DataType.Date32`/`DateTime64`/`Timestamp64` in addition to `Date`/`DateTime`/`DateTime2`.
- `Internal/DataProvider/Ydb/YdbSqlBuilder.cs` -- added `CanSkipRootAliases` override returning `false` (line 100); `BuildOrderByClause` now emits `NULLS FIRST`/`NULLS LAST` when `NullsPosition != Sql.NullsPosition.None` (lines 544-549).
- `Internal/DataProvider/Ydb/Translation/YdbMemberTranslator.cs` -- `SqlTypesTranslation.ConvertDateOnly` added (under `#if SUPPORTS_DATEONLY`), throws `NotSupportedException("52")`; `TranslatePow` now casts non-float/double operands to `double` and back; `WithSort` in `TranslateStringJoin` rewritten to tuple-based sort key encoding with `ListSort`+key-lambda (`ListReverse` for string DESC first key), `ListUniq` for DISTINCT before sort; quarter date-add uses `DateTime::ShiftMonths` * 3 then `DateTime::MakeDatetime`.

**Read (this run -- delta sha 36ee4f82f):**
- `Internal/DataProvider/Ydb/YdbSchemaProvider.cs` -- new file. Implements `SchemaProviderBase`: `GetTables`/`GetColumns` via ADO `GetSchema("Tables"/"Columns")`, `GetPrimaryKeys` via `YdbProviderAdapter.GetPrimaryKeys` (gRPC describe), `GetForeignKeys` always empty, `GetDataTypes` always empty (private `_typeMap` drives `GetDataType`/`GetSystemType`). Directly contradicts the prior INDEX.md's "no YdbSchemaProvider.cs exists" / "GetSchemaProvider() throws NotImplementedException" claims -- corrected in place, see AUDIT-NOTE.
- `Internal/DataProvider/Ydb/YdbScalarSubQueryToCteVisitor.cs` -- new file. Extracts the scalar-subquery-to-CTE rewrite previously described inline under `YdbSqlOptimizer.MoveScalarSubQueriesToCte` into a dedicated pooled `QueryElementVisitor` with explicit position classification (Scalar/Wrapped/Excluded), an equality-comparison-to-IN rewrite, and CTE dedup via `ISqlExpressionEqualityComparer`.
- `Internal/DataProvider/Ydb/YdbSqlOptimizer.cs` -- `MoveScalarSubQueriesToCte` now delegates to `YdbScalarSubQueryToCteVisitor.Pool`; also now walks `SqlUpdateStatement.Update` and pre-existing CTE bodies (previously undocumented).
- `Internal/DataProvider/Ydb/YdbDataProvider.cs` -- added flags: `IsSubQueryOrderBySupported`, `IsUnionAllOrderBySupported`, `IsOrderByAggregateFunctionSupported = false`, `IsDistinctSetOperationsSupported = false`, `DefaultNullsOrdering = Smallest`, `RowConstructorSupport`, `SupportsPredicatesComparison = true`, `IsDistinctFromSupported = true`, `IsSupportsJoinWithoutCondition = false`. `GetSchemaProvider()` now returns `new YdbSchemaProvider()` instead of throwing (see AUDIT-NOTE). `SetParameter`'s DateTime-normalization switch now also covers `DataType.DateTimeOffset`.
- `Internal/DataProvider/Ydb/YdbSqlBuilder.cs` -- added `IsSqlValuesTableValueTypeRequired`, `SupportsColumnAliasesInScalarSource => false`, `BuildSelectClause`/`BuildEmptyValuesFrom` dummy-FROM handling, `BuildSql` CTE-name pre-registration, `BuildFromClause` UPDATE skip, `BuildInListPredicate` NULL-in-list handling (LikeClr/LikeSql semantics), full `BuildDataTypeFromDataType` DDL type-name switch. `BuildSqlCastExpression`'s `Unwrap(...)` wrapping is now conditional on non-nullability rather than unconditional (see AUDIT-NOTE).
- `Internal/DataProvider/Ydb/YdbSqlExpressionConvertVisitor.cs` -- added `ConvertSqlUnaryExpression` (unsigned-cast for bitwise negation), `AlignFloatingDecimal` (mixed float/Decimal arithmetic), `ConvertSqlFunction` (Math::Floor/Ceil/Trunc Decimal routing), a substantial `ConvertConversion` override (string->Datetime/Timestamp ISO rewrite, string->Interval split-and-sum, float->Decimal via string, Decimal->int via Double), `VisitInSubQueryPredicate` (row-valued IN tuple folding), `ConcatRequiresExplicitStringCast => false`, `SupportsNullIf => false`.
- `Internal/DataProvider/Ydb/Translation/YdbMemberTranslator.cs` -- added nested `YdbWindowFunctionsMemberTranslator` (window-frame/percentile/null-treatment capability flags) wired via `CreateWindowFunctionsMemberTranslator()`; added `TranslateMakeDateTime` (constructor-parts -> ISO string -> CAST) and `TranslateDateTimeTruncationToDate` (Split/StartOfDay/MakeTimestamp chain) on the nested `DateFunctionsTranslator`.
- `Internal/DataProvider/Ydb/YdbBulkCopy.cs` -- no structural change; confirmed the transaction-open fallback to row-by-row `MultipleRowsCopy` (mirrors `InformixBulkCopy`), now documented explicitly.
- `Internal/DataProvider/Ydb/YdbMappingSchema.cs` -- added `MAX_DECIMAL_PRECISION = 35` constant and `GetCommonDecimalType(x, y)` (common-Decimal-type widening for `MIN_OF`/`MAX_OF`).
- `Internal/DataProvider/Ydb/YdbProviderAdapter.cs` -- added schema-provider primary-key wrapper chain (`ISession`, `IDriver`, `YdbSchema.DescribeTable` via `[WrappedBindingFlags]`, `YdbTableDescription`, `DescribeTableSettings`) and `GetPrimaryKeys(DbConnection, IEnumerable<string>)`, backing the new `YdbSchemaProvider`.
- `Internal/DataProvider/Ydb/YdbRetryPolicy.cs` -- `SessionExpired` added alongside `BadSession`/`SessionBusy` in the 0 ms delay bucket.
- `Internal/DataProvider/Ydb/YdbTransientExceptionDetector.cs` -- no change; `ShouldRetryOn`'s idempotent-retry code list already included `SessionExpired`.
- `DataProvider/Ydb/YdbTools.cs` -- no change from prior description.
- `DataProvider/Ydb/YdbHints.cs` -- no change from prior description (`QueryHint` overloads, `Unique`/`Distinct` constants, `--+ hint(...)` builder).
- `DataProvider/Ydb/IYdbSpecificQueryable.cs`, `DataProvider/Ydb/IYdbSpecificTable.cs`, `DataProvider/Ydb/YdbSpecificExtensions.cs` -- re-confirmed, no change (marker interfaces / `AsYdb<T>` extensions).
</details>
