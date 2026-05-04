---
area: PROV-YDB
kind: area-index
sources:
  - "[code]"
confidence: high
coverage_tier_1: 13/13
coverage_tier_2: 8/8
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
---

# PROV-YDB — Yandex Database (YDB) Provider

YDB is a distributed NewSQL database by Yandex, queried via YQL (a SQL dialect). The linq2db provider wraps the `Ydb.Sdk` ADO.NET package dynamically and adds a built-in retry policy unique among all providers in this codebase.

## Subsystems

### Driver loading

`YdbProviderAdapter` (`Internal/DataProvider/Ydb/YdbProviderAdapter.cs:31`) is the single `IDynamicProviderAdapter` for this area. It loads two assemblies at runtime:

- `Ydb.Sdk` (`AssemblyName = "Ydb.Sdk"`) — contains `Ydb.Sdk.Ado.YdbConnection`, `YdbCommand`, `YdbParameter`, `YdbDataReader`, `YdbTransaction`, the `YdbType.YdbDbType` enum, and `Ydb.Sdk.Ado.BulkUpsert.IBulkUpsertImporter`.
- `Ydb.Protos` (`ProtosAssemblyName = "Ydb.Protos"`) — contains `Ydb.Value`, `Ydb.Type`, `Ydb.DecimalType`; used exclusively for the 128-bit decimal wire encoding.

All calls into these assemblies are bridged via `TypeMapper` wrappers. There is no provider-selection enum (single driver family, unlike PROV-SQLSERVER or PROV-MYSQL), and there is no `YdbProviderDetector`; provider detection is performed in `YdbTools.ProviderDetector` (`DataProvider/Ydb/YdbTools.cs:26`) by checking whether `ProviderName` or `ConfigurationString` contains `"Ydb"`.

### Data provider

`YdbDataProvider` (`Internal/DataProvider/Ydb/YdbDataProvider.cs:21`) extends `DynamicDataProviderBase<YdbProviderAdapter>`. Notable `SqlProviderFlags` settings:

- `IsSkipSupported = false`, `IsSkipSupportedIfTake = true` — YDB lacks standalone `OFFSET` without `LIMIT` (tracked as GitHub issue 11258 in the YDB project; a comment notes the `LIMIT big_num, X` workaround used by ClickHouse does not work for YDB).
- `IsComplexJoinConditionSupported = false`, `IsNestedJoinsSupported = false`, `IsCrossJoinSyntaxRequired = true`.
- `DefaultMultiQueryIsolationLevel = IsolationLevel.Serializable` — only Serializable is supported.
- `IsCommonTableExpressionsSupported = true` but emulated via table expressions (see `YdbSqlOptimizer`).
- `SupportedCorrelatedSubqueriesLevel = 0` — correlated subqueries not supported (simple correlated subqueries are handled via CTE promotion).

`YdbDataProvider` overrides `SetParameter` with extensive numeric type coercion covering all integer widths (`sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`) and `SetParameterType` maps linq2db `DataType` values to `YdbDbType` enum members. The `DataType.Array` flag is handled inline: if set, the `YdbDbType.List` flag is OR-ed into the resulting type.

Special parameter handling:

- `char` is always promoted to `string` (provider has no char parameter type).
- `DataType.Binary`/`VarBinary` string values are UTF-8-encoded before passing.
- `DataType.Yson` string values are UTF-8-encoded to `byte[]`.
- `DateTime` values are forced to UTC regardless of `DateTimeKind`.
- `GetSchemaProvider()` throws `NotImplementedException` — schema introspection is not implemented.
- `IsDBNullAllowed` always returns `true` (provider does not implement `GetSchemaTable`).

### SQL builder (`YdbSqlBuilder`)

`YdbSqlBuilder` (`Internal/DataProvider/Ydb/YdbSqlBuilder.cs:19`) extends `BasicSqlBuilder<YdbOptions>`.

YQL identifier quoting uses backticks (`` ` ``). Quoting is required when an identifier starts with a digit, contains non-ASCII-alphanumeric/underscore characters, or is a reserved word. The `__ydb_` prefix is disallowed even in quoted mode.

CTE emission is non-standard: each CTE is written as `$name = SELECT ...;` (dollar-sign prefix, assignment syntax) rather than standard `WITH name AS (...)`. `BuildWithClause` iterates clauses and emits each as a standalone assignment terminated with `;`. The `IsCteColumnListSupported` property returns `false`.

Table path construction (`BuildObjectName`) supports YDB's hierarchical path format: `/database/path/tablename` assembled from `SqlObjectName.Database`, `Schema`, and `Name` components.

`CAST` is wrapped in `Unwrap(...)` (`BuildSqlCastExpression`) because YDB's `CAST` returns an Optional type.

`RETURNING` is supported for identity retrieval (`BuildGetIdentity`) but RETURNING columns cannot be table-qualified (`_buildTableName = false` during `BuildOutputColumnExpressions`).

Identity field DDL uses PostgreSQL-style serials: `SMALLSERIAL`, `SERIAL`, `BIGSERIAL` for `Int16`/`Int32`/`Int64` identity columns.

`BuildMergeStatement` throws `LinqToDBException` — YDB does not support SQL MERGE.

`ORDER BY` skips constant expressions and sorts by alias name when the column has an alias (documented in code as a YDB bug).

`BuildInListPredicate` materialises parameters from list-typed `SqlParameter` values and re-emits them as individual `$Ids{bucket}_{n}` named parameters to work around YDB's lack of array-typed IN parameters.

### SQL optimizer (`YdbSqlOptimizer`)

`YdbSqlOptimizer` (`Internal/DataProvider/Ydb/YdbSqlOptimizer.cs:11`) extends `BasicSqlOptimizer`.

`TransformStatement` disables table aliases on DELETE and UPDATE (sets alias to `$`; the dollar-sign is YDB's anonymous scope marker). UPDATE is rewritten to alternative form via `GetAlternativeUpdate`. INSERT with a `SELECT` sub-query has column aliases injected from the insert items so that column names propagate correctly (`CorrectInsertStatement`).

`Finalize` promotes all scalar correlated sub-queries to CTEs via `MoveScalarSubQueriesToCte`. The transformation walks the AST and wraps any single-column, non-dependent `SelectQuery` found in a column, predicate, or set-expression position into a `CteClause`+`SqlCteTable` pair. After promotion it calls `FinalizeCte` to finalise names.

`ReplaceTableAll` clears `SELECT *` from `EXISTS` sub-queries and replaces them with `SELECT 1` to avoid ambiguous duplicate column names.

`SetQueryParameter` forces `Date32`/`DateTime64`/`Timestamp64`/`Interval64` parameters whose .NET type is `int`/`long` to be emitted as literals (`IsQueryParameter = false`) because the provider does not accept these as bound parameters.

### Expression conversion visitor (`YdbSqlExpressionConvertVisitor`)

`YdbSqlExpressionConvertVisitor` (`Internal/DataProvider/Ydb/YdbSqlExpressionConvertVisitor.cs:11`) extends `SqlExpressionConvertVisitor`.

- String concatenation (`+` on `NVarChar`/`Char`/`VarChar`) is rewritten to `||`.
- Byte concatenation (`+` on `Binary`/`VarBinary`/`Blob`) is rewritten to `||`.
- Bitwise operations (`&`, `|`, `^`) require unsigned operands; operands are cast to unsigned via `SqlCastExpression` when needed.
- `TO_LOWER`/`TO_UPPER`/`REPLACE` pseudo-functions map to `Unicode::ToLower`, `Unicode::ToUpper`, `Unicode::ReplaceAll`.
- LIKE uses `%` and `_` as escape characters. Case-insensitive search emits `ILIKE`.
- `CASE WHEN ... THEN ... END` without `ELSE` gets a `NULL` ELSE appended (required by YQL).
- `SupportsNullInColumn` returns `false`.

### Member translator (`YdbMemberTranslator`)

`YdbMemberTranslator` (`Internal/DataProvider/Ydb/Translation/YdbMemberTranslator.cs:15`) extends `ProviderMemberTranslatorDefault`.

Date functions call the `DateTime::` module (e.g. `DateTime::GetYear`, `DateTime::GetDayOfMonth`, `DateTime::GetWeekOfYearIso8601`, `DateTime::ShiftYears`, `DateTime::ShiftMonths`, `DateTime::IntervalFromDays/Hours/Minutes/Seconds/Milliseconds`). `DateTimeOffset` date parts and truncation are not supported (throw `NotSupportedException`). `CurrentUtcTimestamp()` is used for both `GetDate` and `CurrentTimestampUtc`.

String functions: `PadLeft` → `String::LeftPad`; `Replace` → `Unicode::ReplaceAll`; `String.Join` emits `AGGREGATE_LIST` + `Unicode::JoinFromList` with complex ORDER BY simulation via `ListSort`+key-lambda+`ListMap`. `ListConcat`+`AsList`+`ListNotNull` handle `CONCAT_WS`.

Math: `Max`/`Min` → `MAX_OF`/`MIN_OF`; `Pow` → `Math::Pow`; `RoundToEven` → `Math::NearbyInt` with `Math::RoundToNearest()`.

Guid-to-string → `CAST AS Utf8`. `Guid.NewGuid()` is commented out (would generate the same UUID for all invocations in a single query).

`SqlTypesTranslation.ConvertBit` throws `NotSupportedException`.

### Bulk copy (`YdbBulkCopy`)

`YdbBulkCopy` (`Internal/DataProvider/Ydb/YdbBulkCopy.cs:18`) extends `BasicBulkCopy`.

Default bulk copy type (from `YdbOptions.BulkCopyType`) is `BulkCopyType.ProviderSpecific`. The provider-specific path calls `YdbProviderAdapter.BeginBulkCopy` which wraps `YdbConnection.BeginBulkUpsertImport(tableName, columnNames, cancellationToken)` — this returns `IBulkUpsertImporter`. Rows are streamed through `IBulkUpsertImporter.AddRowAsync` and periodically flushed via `FlushAsync` every `MaxBatchSize` rows (default 10,000). Values are passed as provider-typed `DbParameter.Value` objects (not re-serialised as strings). If no unwrapped provider connection is available, falls back to `MultipleRowsCopy1`.

### Retry policy (area-unique)

`YdbRetryPolicy` (`Internal/DataProvider/Ydb/YdbRetryPolicy.cs:13`) and `YdbTransientExceptionDetector` (`Internal/DataProvider/Ydb/YdbTransientExceptionDetector.cs:19`) together form a retry subsystem that does **not** exist in any other provider area.

`YdbRetryPolicy` extends `RetryPolicyBase` (`Data/RetryPolicy/RetryPolicyBase.cs`) from the INFRA area. It is not wired automatically by `YdbDataProvider` — it must be passed to `DataOptions` by the consumer (`DataOptions.UseRetryPolicy(...)`).

Default parameters mirror the YDB SDK defaults: 10 attempts; fast backoff base 5 ms, cap 500 ms; slow backoff base 50 ms, cap 5000 ms; idempotence disabled.

`GetNextDelay` overrides base-class logic: for YDB-typed exceptions it dispatches on the status code name (string comparison, no hard SDK dependency):
- `BadSession`/`SessionBusy` → 0 ms.
- `Aborted`/`Undetermined` → Full Jitter (fast parameters).
- `Unavailable`/`ClientTransportUnknown`/`ClientTransportUnavailable` → Equal Jitter (fast).
- `Overloaded`/`ClientTransportResourceExhausted` → Equal Jitter (slow).
- Non-YDB exceptions → base exponential retry.

`YdbTransientExceptionDetector` inspects `Ydb.Sdk.Ado.YdbException` without a compile-time reference to the SDK: it walks the exception chain looking for a type whose `FullName` equals `"Ydb.Sdk.Ado.YdbException"`, then reads `IsTransient` and `Code` properties via reflection. This avoids any hard dependency on `Ydb.Sdk` in the core library.

### Mapping schema (`YdbMappingSchema`)

`YdbMappingSchema` (`Internal/DataProvider/Ydb/YdbMappingSchema.cs:15`) extends `LockedMappingSchema`.

Key constants: `DEFAULT_DECIMAL_PRECISION = 22`, `DEFAULT_DECIMAL_SCALE = 9`, `DEFAULT_TIMEZONE = "GMT"`.

Type literal formats:
- Integers: suffix-typed (`0ut` = UInt8, `0t` = Int8, `0s` = Int16, `0us` = UInt16, bare = Int32, `0u` = UInt32, `0l` = Int64, `0ul` = UInt64).
- Floats: `Float('…')`, `Double('…')`, with special literals for infinity.
- Decimal: `Decimal('value', precision, scale)`.
- DyNumber: `DyNumber('value')` (YDB-specific arbitrary-precision decimal).
- String: single-quoted with suffix (`u` for UTF-8 text, `s` for bytes, `j` for JSON, `y` for YSON).
- Binary: hex-encoded with suffix.
- Bool: `true`/`false`.
- UUID: `Uuid('…')`.
- Dates: `Date('…')`, `Datetime('…')`, `Timestamp('…')`, `Date32('…')`, `Datetime64('…')`, `Timestamp64('…')`.
- Tz variants: `TzDate('…,GMT')`, etc.
- Interval: `Interval('PT…')` / `Interval64('PT…')` (ISO 8601, truncated to 100 ns).
- List literals: `[v1, v2, …]` (currently only `bool[]`/`List<bool>` registered).

`AddScalarType` registrations: `DateTimeOffset` → `DateTime2`, `TimeSpan` → `Interval`, `MemoryStream` → `VarBinary`.

The provider-specific decimal wire encoding in `YdbProviderAdapter.MakeDecimalFromString` constructs a `YdbValue` from a 128-bit integer (via `Int128` on net8+ or `BigInteger` on earlier targets) built from `Ydb.DecimalType` + `Ydb.Value` protos.

### Options

`YdbOptions` (`DataProvider/Ydb/YdbOptions.cs:19`) is a `DataProviderOptions<YdbOptions>` record with two fields:
- `BulkCopyType` — default `ProviderSpecific`.
- `UseParametrizedDecimal` — when `true` (default), emits `Decimal(p,s)` with explicit precision/scale in DDL; when `false`, emits bare `Decimal`.

### Public-surface entry points

`YdbTools` (`DataProvider/Ydb/YdbTools.cs:19`) is the static public-API class: `GetDataProvider()`, `CreateDataConnection(string/DbConnection/DbTransaction)`, `ResolveYdb(string/Assembly)`, `ClearAllPools()`, `ClearPool(DbConnection)`. Provider detection uses `ProviderDetectorBase<Fake>.CreateDataProvider<YdbDataProvider>()` (no version enum; single provider instance).

`IYdbSpecificQueryable<T>` and `IYdbSpecificTable<T>` mark provider-specific query chains. `YdbSpecificExtensions.AsYdb<T>` wraps any `ITable<T>` or `IQueryable<T>` into the YDB-specific variant. `YdbHints` exposes `QueryHint`, `UniqueHint`, `DistinctHint` extension methods emitting `--+ hint(...)` YQL comment directives.

## Key types

| Type | File | Role |
|---|---|---|
| `YdbDataProvider` | `Internal/DataProvider/Ydb/YdbDataProvider.cs` | Main provider, `DynamicDataProviderBase<YdbProviderAdapter>` |
| `YdbProviderAdapter` | `Internal/DataProvider/Ydb/YdbProviderAdapter.cs` | Dynamic driver loader; loads `Ydb.Sdk` + `Ydb.Protos` |
| `YdbSqlBuilder` | `Internal/DataProvider/Ydb/YdbSqlBuilder.cs` | YQL SQL emission, backtick quoting, CTE-as-assignment |
| `YdbSqlOptimizer` | `Internal/DataProvider/Ydb/YdbSqlOptimizer.cs` | Scalar-subquery→CTE promotion, alias fixups, literal coercion |
| `YdbSqlExpressionConvertVisitor` | `Internal/DataProvider/Ydb/YdbSqlExpressionConvertVisitor.cs` | Expression rewrites for YQL specifics |
| `YdbMappingSchema` | `Internal/DataProvider/Ydb/YdbMappingSchema.cs` | Literal generators for all YDB types |
| `YdbBulkCopy` | `Internal/DataProvider/Ydb/YdbBulkCopy.cs` | `IBulkUpsertImporter`-based native bulk upsert |
| `YdbRetryPolicy` | `Internal/DataProvider/Ydb/YdbRetryPolicy.cs` | Full/Equal Jitter retry; mirrors YDB SDK defaults |
| `YdbTransientExceptionDetector` | `Internal/DataProvider/Ydb/YdbTransientExceptionDetector.cs` | Reflection-based YDB exception inspector (no hard SDK dep) |
| `YdbMemberTranslator` | `Internal/DataProvider/Ydb/Translation/YdbMemberTranslator.cs` | LINQ member→YQL function translation |
| `YdbOptions` | `DataProvider/Ydb/YdbOptions.cs` | Provider options: bulk copy type, decimal notation |
| `YdbTools` | `DataProvider/Ydb/YdbTools.cs` | Public-surface static factory + pool management |
| `YdbHints` | `DataProvider/Ydb/YdbHints.cs` | YQL hint comment builder (`--+ unique(...)`) |
| `IYdbSpecificQueryable<T>` | `DataProvider/Ydb/IYdbSpecificQueryable.cs` | Provider-specific query marker interface |
| `IYdbSpecificTable<T>` | `DataProvider/Ydb/IYdbSpecificTable.cs` | Provider-specific table marker interface |

## Files (Tier 1 / Tier 2)

### Tier 1 (read in full — proposed anchors)

| File | Purpose |
|---|---|
| `Internal/DataProvider/Ydb/YdbDataProvider.cs` | Main provider class |
| `Internal/DataProvider/Ydb/YdbSqlBuilder.cs` | YQL SQL builder |
| `Internal/DataProvider/Ydb/YdbSqlOptimizer.cs` | SQL optimizer |
| `Internal/DataProvider/Ydb/YdbProviderAdapter.cs` | Dynamic adapter / driver wrapper |
| `Internal/DataProvider/Ydb/YdbMappingSchema.cs` | Type mapping + literal generation |
| `Internal/DataProvider/Ydb/YdbBulkCopy.cs` | Bulk upsert implementation |
| `Internal/DataProvider/Ydb/YdbSqlExpressionConvertVisitor.cs` | Expression conversion |
| `Internal/DataProvider/Ydb/Translation/YdbMemberTranslator.cs` | Member translation |
| `Internal/DataProvider/Ydb/YdbRetryPolicy.cs` | Retry policy (area-unique) |
| `Internal/DataProvider/Ydb/YdbTransientExceptionDetector.cs` | Transient exception detector (area-unique) |
| `DataProvider/Ydb/YdbTools.cs` | Public-surface factory |
| `DataProvider/Ydb/YdbOptions.cs` | Provider options |
| `DataProvider/Ydb/YdbHints.cs` | YQL query hints |

### Tier 2 (all read in full — small area)

| File | Notes |
|---|---|
| `DataProvider/Ydb/YdbHints.generated.cs` | T4-generated `UniqueHint`/`DistinctHint` overloads |
| `DataProvider/Ydb/YdbHints.tt` | T4 source for generated hints; not read (Tier 3 — generator input) |
| `DataProvider/Ydb/IYdbSpecificQueryable.cs` | Marker interface |
| `DataProvider/Ydb/IYdbSpecificTable.cs` | Marker interface |
| `DataProvider/Ydb/YdbSpecificExtensions.cs` | `AsYdb<T>` extension methods |
| `Internal/DataProvider/Ydb/YdbSpecificQueryable.cs` | Internal `DatabaseSpecificQueryable<T>` impl |
| `Internal/DataProvider/Ydb/YdbSpecificTable.cs` | Internal `DatabaseSpecificTable<T>` impl |

## Known issues / debt

- **`GetSchemaProvider()` not implemented.** `YdbDataProvider.GetSchemaProvider()` throws `NotImplementedException` (`YdbDataProvider.cs:109`). Schema introspection is absent — no `YdbSchemaProvider.cs` exists.
- **Correlated subqueries not supported.** `SupportedCorrelatedSubqueriesLevel = 0`; the optimizer promotes scalar correlated sub-queries to CTEs, but multi-column or deeply nested correlated patterns have no fallback.
- **`OFFSET` without `LIMIT` not supported.** Tracked in YDB issue 11258. No workaround available (the CH `LIMIT big_num, X` workaround is documented as not working for YDB).
- **Retry policy is opt-in.** `YdbRetryPolicy` is not wired automatically by `YdbDataProvider`; callers must register it explicitly via `DataOptions.UseRetryPolicy(new YdbRetryPolicy())`. A TODO comment in `YdbProviderAdapter.cs:27` notes the intent to add provider-specific retry with `IsTransientWhenIdempotent` support.
- **`BeginTransaction(TxMode)` not supported.** YDB's extended transaction modes (snapshot, online RO, stale RO) are not exposed. Only Serializable read-write is used.
- **`YdbStruct` not implemented.** A `TODO: YdbStruct` comment in `YdbProviderAdapter.cs:66` indicates struct support is absent.
- **`Guid.NewGuid()` translation commented out.** `YdbMemberTranslator.cs` notes that `RandomUuid` generates the same UUID for all invocations in a single query.
- **`String.Join` ORDER BY with string DESC is partially restricted.** The translator falls back to no-ORDER when the pattern is unsupported (e.g., string DESC on a non-first key).
- **`DateTimeOffset` date-part/truncation methods throw.** `TranslateDateTimeOffsetDatePart`, `TranslateDateTimeOffsetTruncationToDate`, `TranslateDateTimeOffsetTruncationToTime` all throw `NotSupportedException`.
- **`SqlTypesTranslation.ConvertBit` throws `NotSupportedException`.** Mapping `SqlTypes.SqlBoolean` is not implemented.
- **`modulo` operator commented out.** The `%` rewrite for decimal types in `YdbSqlExpressionConvertVisitor` is commented out; the comment suggests correctness concerns remain.

## Inbound / outbound dependencies

**Inbound:**
- `YdbTools.ProviderDetector` is registered with `ProviderDetectorBase` — called by the global connection-string dispatcher.
- `YdbRetryPolicy` is consumed by user code via `DataOptions`.

**Outbound:**
- `Internal/DataProvider/DataProviderBase` / `DynamicDataProviderBase` — INTERNAL-API area.
- `BasicSqlBuilder`, `BasicSqlOptimizer`, `SqlExpressionConvertVisitor` — SQL-PROVIDER area.
- `BasicBulkCopy`, `BulkCopyReader` — INTERNAL-API area.
- `RetryPolicyBase` (`Data/RetryPolicy/`) — INFRA area.
- `ProviderMemberTranslatorDefault` — INTERNAL-API area.
- `TypeMapper` (`Internal/Expressions/Types/TypeMapper`) — INTERNAL-API area.
- `Ydb.Sdk` assembly (dynamically loaded at runtime — not a compile-time reference).
- `Ydb.Protos` assembly (dynamically loaded at runtime — decimal wire encoding only).

## See also

- [SQL-PROVIDER area](../SQL-PROVIDER/INDEX.md) — `BasicSqlBuilder`, `BasicSqlOptimizer` base classes.
- [INTERNAL-API area](../INTERNAL-API/INDEX.md) — `DynamicDataProviderBase`, `BasicBulkCopy`, `TypeMapper`, `MemberTranslatorBase`.
- [INFRA area](../INFRA/INDEX.md) — `RetryPolicyBase`, retry policy infrastructure.
- [architecture/interceptors.md](../../architecture/interceptors.md) — retry policy wiring via `DataOptions`.

<details><summary>Coverage</summary>

**Tier 1 — read in full (13/13):**
- `Internal/DataProvider/Ydb/YdbDataProvider.cs`
- `Internal/DataProvider/Ydb/YdbSqlBuilder.cs`
- `Internal/DataProvider/Ydb/YdbSqlOptimizer.cs`
- `Internal/DataProvider/Ydb/YdbProviderAdapter.cs`
- `Internal/DataProvider/Ydb/YdbMappingSchema.cs`
- `Internal/DataProvider/Ydb/YdbBulkCopy.cs`
- `Internal/DataProvider/Ydb/YdbSqlExpressionConvertVisitor.cs`
- `Internal/DataProvider/Ydb/Translation/YdbMemberTranslator.cs`
- `Internal/DataProvider/Ydb/YdbRetryPolicy.cs`
- `Internal/DataProvider/Ydb/YdbTransientExceptionDetector.cs`
- `DataProvider/Ydb/YdbTools.cs`
- `DataProvider/Ydb/YdbOptions.cs`
- `DataProvider/Ydb/YdbHints.cs`

**Tier 2 — read in full (7/8):**
- `DataProvider/Ydb/YdbHints.generated.cs` — read
- `DataProvider/Ydb/IYdbSpecificQueryable.cs` — read
- `DataProvider/Ydb/IYdbSpecificTable.cs` — read
- `DataProvider/Ydb/YdbSpecificExtensions.cs` — read
- `Internal/DataProvider/Ydb/YdbSpecificQueryable.cs` — read
- `Internal/DataProvider/Ydb/YdbSpecificTable.cs` — read

**Tier 2 — skipped (1/8):**
- `DataProvider/Ydb/YdbHints.tt` — T4 template source; classified Tier 3 (generator input, no runtime content beyond what the generated `.cs` contains). Counted in denominator as Tier 2 because it matched the glob; no information loss from skipping.

**Tier 3 — not read:**
- None beyond the `.tt` file noted above.

</details>
