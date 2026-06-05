---
area: PROV-FIREBIRD
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
coverage_tier_1: 11/11
coverage_tier_2: 12/12
---

# PROV-FIREBIRD

Firebird provider for linq2db. Single ADO.NET client (`FirebirdSql.Data.FirebirdClient`, assembly name `FirebirdSql.Data.FirebirdClient`), four concrete dialect versions (v2.5, v3, v4, v5). No schemas -- Firebird is a schema-less database; all objects live in a single namespace per database file.

## Public surface

Entry point: `FirebirdTools` (`Source/LinqToDB/DataProvider/Firebird/FirebirdTools.cs`).

- `FirebirdTools.GetDataProvider(FirebirdVersion, ...)` -- returns the per-version `IDataProvider` from `FirebirdProviderDetector`.
- `FirebirdTools.CreateDataConnection(string/DbConnection/DbTransaction, FirebirdVersion)` -- convenience factory producing a configured `DataConnection`.
- `FirebirdTools.ResolveFirebird(string/Assembly)` -- registers an `AssemblyResolver` so the ADO.NET assembly can be found in a non-default path. Uses `FirebirdProviderAdapter.AssemblyName` = `"FirebirdSql.Data.FirebirdClient"`.
- `FirebirdTools.ClearAllPools()` -- delegates to `FirebirdProviderAdapter.Instance.ClearAllPools` (invokes `FbConnection.ClearAllPools()` via `TypeMapper`).
- `FirebirdTools.AutoDetectProvider` -- wraps `FirebirdProviderDetector.AutoDetectProvider`; toggle to disable server-version query.

Options record: `FirebirdOptions` (`Source/LinqToDB/DataProvider/Firebird/FirebirdOptions.cs`) -- sealed record extending `DataProviderOptions<FirebirdOptions>`. Key fields:
- `BulkCopyType` (default `MultipleRows`) -- chooses bulk copy strategy.
- `IdentifierQuoteMode` (default `Auto`) -- drives identifier quoting throughout `FirebirdSqlBuilder`.
- `IsLiteralEncodingSupported` (default `true`) -- enables UTF-8 hex-escape literals for non-ASCII strings.

Extension surface: `FirebirdExtensions` / `IFirebirdExtensions` (`Source/LinqToDB/DataProvider/Firebird/FirebirdExtensions.cs`, `IFirebirdExtensions.cs`) -- marker interface + one SQL extension method `UuidToChar` mapping to `UUID_TO_CHAR({guid})`.

Factory: `FirebirdFactory` (`Source/LinqToDB/DataProvider/Firebird/FirebirdFactory.cs`) -- `DataProviderFactoryBase` impl; reads `"version"` attribute from config and dispatches to `FirebirdTools.GetDataProvider`.

## Version enumeration

`FirebirdVersion` (`Source/LinqToDB/DataProvider/Firebird/FirebirdVersion.cs`):

| Value | Meaning |
|---|---|
| `AutoDetect` | query `rdb$get_context('SYSTEM', 'ENGINE_VERSION') from rdb$database` at connect time |
| `v25` | Firebird 2.5+ dialect (legacy: `FIRST n SKIP m`, generator-based identity, no BOOLEAN) |
| `v3` | Firebird 3+ (`OFFSET ... ROWS FETCH NEXT ... ROWS ONLY`, BOOLEAN, window functions, MERGE, RETURNING, predicates comparison) |
| `v4` | Firebird 4+ (`LATERAL`/`APPLY`, `DECFLOAT`, `INT128`, `TIMESTAMP WITH TIME ZONE`, `TIME WITH TIME ZONE`, `BINARY`/`VARBINARY`) |
| `v5` | Firebird 5+ (native `QUARTER` extract, `LIST DISTINCT`, extended `MERGE ... NOT MATCHED BY SOURCE`, larger IN-list limit: 65535 vs 1500) |

## Identifier quoting

`FirebirdIdentifierQuoteMode` (`Source/LinqToDB/DataProvider/Firebird/FirebirdIdentifierQuoteMode.cs`):

| Mode | Behaviour |
|---|---|
| `None` | Never quote. Developer ensures no reserved words or special chars. Only valid for Dialect 1 databases. |
| `Quote` | Always quote with `"..."`. Preserves case. Required when mixed-case identifiers are needed. |
| `Auto` (default) | Quote only when necessary: reserved word, non-ASCII, digit/`$`/`_` as first char, or anything outside `[A-Z0-9$_]`. |

Firebird folds unquoted identifiers to **uppercase** (opposite convention from PostgreSQL which folds to lowercase). A name like `MyTable` stored unquoted is physically `MYTABLE`; reading `rdb$relation_name` via schema discovery must account for this. `FirebirdSqlBuilder` converts unquoted identifiers to uppercase when comparing against `rdb$relations`, `rdb$generators`, `rdb$triggers` system tables during DDL (`Source/LinqToDB/Internal/DataProvider/Firebird/FirebirdSqlBuilder.cs:359-362`).

`IsValidIdentifier` in `FirebirdSqlBuilder` (`FirebirdSqlBuilder.cs:208-218`) enforces: first char `[A-Z]`, remaining `[A-Z0-9$_]`, not a reserved word. The check is uppercase-only because pre-quoting normalization is uppercase.

## Per-version provider hierarchy

### Concrete providers

Four sealed classes in `FirebirdDataProvider.cs:19-22`:

```
FirebirdDataProvider25 : FirebirdDataProvider  (ProviderName.Firebird25, FirebirdVersion.v25)
FirebirdDataProvider3  : FirebirdDataProvider  (ProviderName.Firebird3,  FirebirdVersion.v3)
FirebirdDataProvider4  : FirebirdDataProvider  (ProviderName.Firebird4,  FirebirdVersion.v4)
FirebirdDataProvider5  : FirebirdDataProvider  (ProviderName.Firebird5,  FirebirdVersion.v5)
```

All extend `FirebirdDataProvider : DynamicDataProviderBase<FirebirdProviderAdapter>`.

`FirebirdDataProvider.CreateSqlBuilder` dispatch (`FirebirdDataProvider.cs:108-114`):

| Version | Builder |
|---|---|
| `v3` | `Firebird3SqlBuilder` |
| `>= v4` | `Firebird4SqlBuilder` |
| `v25` (default) | `FirebirdSqlBuilder` |

`FirebirdDataProvider.GetSqlOptimizer` (`FirebirdDataProvider.cs:119-122`): returns stored `_sqlOptimizer` field (initialized in constructor: `v3+` -> `Firebird3SqlOptimizer`; `v25` -> `FirebirdSqlOptimizer`).

`FirebirdDataProvider.CreateMemberTranslator` (`FirebirdDataProvider.cs:92-99`): three-way dispatch -- `>= v5` -> `Firebird5MemberTranslator`; `>= v4` -> `Firebird4MemberTranslator`; `_` -> `FirebirdMemberTranslator`. (Previously v4 shared `FirebirdMemberTranslator`; `Firebird4MemberTranslator` is new as of PR #5467.)

`CreateIdentifierService` (`FirebirdDataProvider.cs:102-105`): max identifier length = 31 chars for `v3` and below; 63 chars for `v4+`.

### SqlProviderFlags distinctions

Set in `FirebirdDataProvider` constructor (`FirebirdDataProvider.cs:32-54`):

- `IsWindowFunctionsSupported`: v3+
- `IsApplyJoinSupported`, `IsOuterApplyJoinSupportsCondition`: v4+
- `SupportsPredicatesComparison`, `SupportsBooleanType`: v3+
- `MaxInListValuesCount`: v5 -> 65535; others -> 1500
- `IsDistinctSetOperationsSupported`: false (never)
- `IsUpdateFromSupported`: false
- `OutputUpdateUseSpecialTables`, `OutputMergeUseSpecialTables`: true (uses `RETURNING ... INTO` style, not `OUTPUT`)
- `SupportedCorrelatedSubqueriesLevel`: 2

## SqlBuilder hierarchy

```
BasicSqlBuilder<FirebirdOptions>
  +-- FirebirdSqlBuilder          (v25 and default; partial + FirebirdSqlBuilder.Merge.cs)
        +-- Firebird3SqlBuilder   (v3: OFFSET/FETCH pagination, suppresses FIRST/SKIP)
              +-- Firebird4SqlBuilder  (v4: LATERAL/CROSS JOIN LATERAL, BINARY/VARBINARY types, GUID -> BINARY(16))
```

v5 uses `Firebird4SqlBuilder` (no dedicated subclass); v5 differences are in the optimizer flag (`MaxInListValuesCount`) and the member translator (`Firebird5MemberTranslator`).

### FirebirdSqlBuilder (v25) key overrides

- `CteFirst = false` -- CTEs come after the SELECT clause, not before (`Source/LinqToDB/Internal/DataProvider/Firebird/FirebirdSqlBuilder.cs:24`).
- `BuildSelectClause` (`FirebirdSqlBuilder.cs:40-61`): empty FROM -> `FROM rdb$database` (Firebird requires a FROM clause; `rdb$database` is the canonical single-row dummy table).
- Pagination (`FirebirdSqlBuilder.cs:63-97`): `FIRST {n}` / `SKIP {n}` syntax for SELECT; UPDATE/DELETE use `ROWS n + 1 TO n + take` syntax.
- `IsRecursiveCteKeywordRequired = true` -- emits `WITH RECURSIVE` keyword.
- `GetIdentityExpression` (`FirebirdSqlBuilder.cs:120-124`): uses `GEN_ID(generatorName, 1)` when `SequenceAttributes` present.
- `BuildGetIdentity` (`FirebirdSqlBuilder.cs:108-118`): emits `RETURNING <field>` after INSERT.
- `BuildDropTableStatement` / `BuildCreateTableStatement` (`FirebirdSqlBuilder.cs:320-629`): wraps complex DDL (identity support via triggers + generators, DROP/CREATE IF EXISTS checks) inside `EXECUTE BLOCK ... AS BEGIN ... END` PSQL blocks. Generator named `GIDENTITY_<tablename>`, trigger named `TIDENTITY_<tablename>`, BEFORE INSERT, calls `GEN_ID(..., 1)`.
- `BuildDataTypeFromDataType` (`FirebirdSqlBuilder.cs:128-196`): maps `INT128`, `DECFLOAT`, `TIMESTAMP WITH TIME ZONE`, `TIME WITH TIME ZONE`; VarChar defaults to 255 with `CHARACTER SET UNICODE_FSS`; Guid -> `CHAR(16) CHARACTER SET OCTETS`; VarBinary > 32765 -> `BLOB`.
- `BuildInsertOrUpdateQuery` (`FirebirdSqlBuilder.cs:255-258`): redirects to `BuildInsertOrUpdateQueryAsMerge` with fake table `rdb$database`.
- `BuildParameter` (`FirebirdSqlBuilder.cs:266-308`): casts parameters that `NeedsCast = true` using `BuildTypedExpression`.
- `NullCharSize = 1`, `UnknownCharSize = 8191` (v25 virtual fields, overridable): used in `BuildTypedExpression` for NVarChar/NChar sizing.
- `BuildObjectName` (`FirebirdSqlBuilder.cs:405-421`): no schema/catalog support; only package + name (Firebird has packages, not schemas). Catalog is silently dropped.

### Firebird3SqlBuilder overrides

`Source/LinqToDB/Internal/DataProvider/Firebird/Firebird3SqlBuilder.cs`:
- `LimitFormat` -> `"FETCH NEXT {0} ROWS ONLY"` (standard SQL pagination).
- `OffsetFormat` -> `"OFFSET {0} ROWS"`.
- `OffsetFirst = true` -- OFFSET clause precedes FETCH.
- `BuildSkipFirst` suppressed (no-op) -- FIRST/SKIP not used.

### Firebird4SqlBuilder overrides

`Source/LinqToDB/Internal/DataProvider/Firebird/Firebird4SqlBuilder.cs`:
- `BuildJoinType`: `CrossApply` -> `CROSS JOIN LATERAL`; `OuterApply` -> `LEFT JOIN LATERAL`.
- `BuildDataTypeFromDataType`: `Guid` -> `BINARY(16)`; `Binary` -> `BINARY(n)` / `BINARY`; `VarBinary` -> `VARBINARY(n)` / `BLOB` (v4 added native binary types).

### FirebirdSqlBuilder.Merge.cs (partial class on FirebirdSqlBuilder)

`Source/LinqToDB/Internal/DataProvider/Firebird/FirebirdSqlBuilder.Merge.cs`:
- `IsValuesSyntaxSupported = false` -- MERGE source must use SELECT, not VALUES (Firebird limitation: parameters in MERGE source VALUES cause "Data type unknown").
- `FakeTable = "rdb$database"` -- used as MERGE source dummy table.
- `IsSqlValuesTableValueTypeRequired`: type inference for MERGE source columns -- strings need type hints to avoid CHAR padding; numeric types (uint, long, ulong, float, double, decimal) need type casting in row 0.
- `BuildMergeOperationDeleteBySource` / `BuildMergeOperationUpdateBySource` -- emits `WHEN NOT MATCHED BY SOURCE ... THEN DELETE/UPDATE` (FB5+).

## SqlOptimizer hierarchy

```
BasicSqlOptimizer
  +-- FirebirdSqlOptimizer        (v25: base)
        +-- Firebird3SqlOptimizer  (v3+: uses Firebird3SqlExpressionConvertVisitor)
```

`FirebirdSqlOptimizer` (`Source/LinqToDB/Internal/DataProvider/Firebird/FirebirdSqlOptimizer.cs`):
- `TransformStatement`: rewrites DELETE/UPDATE via `GetAlternativeDelete` / `GetAlternativeUpdate` (Firebird does not support `DELETE/UPDATE ... FROM`; uses subquery form).
- `FinalizeStatement`: calls `WrapParameters` -- wraps parameters in `CAST()` across select columns, UPDATE sets, INSERT/UPDATE merge, output, function parameters, and binary expressions. This is required because Firebird does not infer parameter types from context reliably (`FirebirdSqlOptimizer.cs:83-99`).
- `IsParameterDependedElement`: LIKE predicates with non-literal operands are parameter-depended; SearchString predicates are not (they use Firebird-specific functions).
- `CreateConvertVisitor` (v25): `FirebirdSqlExpressionConvertVisitor`.

`Firebird3SqlOptimizer` (`Source/LinqToDB/Internal/DataProvider/Firebird/Firebird3SqlOptimizer.cs`):
- Delegates to `Firebird3SqlExpressionConvertVisitor`.

## Expression convert visitor hierarchy

```
SqlExpressionConvertVisitor
  +-- FirebirdSqlExpressionConvertVisitor   (v25)
        +-- Firebird3SqlExpressionConvertVisitor  (v3+)
```

`FirebirdSqlExpressionConvertVisitor` (`Source/LinqToDB/Internal/DataProvider/Firebird/FirebirdSqlExpressionConvertVisitor.cs`):
- `LikeCharactersToEscape`: `_` and `%` only (standard).
- `LikeValueParameterSupport = false`.
- Bitwise NOT -> `BIN_NOT(...)`.
- Bitwise ops (`&`, `|`, `^`) -> `Bin_And`, `Bin_Or`, `Bin_Xor` functions.
- `%` -> `Mod(...)`.
- String `+` -> `||`.
- `ConvertSearchStringPredicate`: maps StartsWith -> `STARTING WITH`; Contains (case-insensitive) -> `CONTAINING`; EndsWith / Contains (case-sensitive) -> via LIKE with `CAST(... AS BLOB)` to force case-sensitivity.
- `ConvertConversion`: bool cast -> SearchCondition (`<> 0`); Guid->string -> `lower(UUID_TO_CHAR(...))`; string->Guid -> `CHAR_TO_UUID(...)`; Decimal normalization (`18, 10` defaults).
- `VisitExprPredicate`: non-boolean `SqlParameter` in predicate position -> `p = TRUE`.
- `ConvertSqlFunction`: `LENGTH` -> `CHAR_LENGTH`.
- `WrapColumnExpression`: wraps numeric value types (uint, long, ulong, float, double, decimal) in CAST when appearing in column position.

`Firebird3SqlExpressionConvertVisitor` (`Source/LinqToDB/Internal/DataProvider/Firebird/Firebird3SqlExpressionConvertVisitor.cs`):
- `GetCaseSensitiveParameter`: uses `EvaluateBoolExpression` (v3 has native BOOLEAN; v25 version uses char-based evaluation).
- `ConvertCastToPredicate`: null `SqlValue` cast to predicate is left as-is (v3 BOOLEAN avoids the NOT EQUAL rewrite for null).

## Member translator hierarchy

```
ProviderMemberTranslatorDefault
  +-- FirebirdMemberTranslator    (v25, v3)
        +-- Firebird4MemberTranslator  (v4)  [NEW -- PR #5467]
              +-- Firebird5MemberTranslator  (v5)
```

`FirebirdMemberTranslator` (`Source/LinqToDB/Internal/DataProvider/Firebird/Translation/FirebirdMemberTranslator.cs`):
- Date functions: `Extract(part from date)`, `DateAdd(part, n, date)`, `LOCALTIMESTAMP` for `Sql.GetDate()`. `Quarter` = `(Month - 1) / 3 + 1`. `DateAdd` increment is forced to non-query-parameter (`MarkAsNonQueryParameters`) because Firebird does not support dynamic increment in `DateAdd`.
- `TranslateNow`: returns `null` -- v25/v3 does not emit a server-side `NOW()` translation (`FirebirdMemberTranslator.cs:246-249`).
- `TranslateMakeDateTime`: builds a string concat via `LPad` then `CAST(... AS TIMESTAMP)`.
- `TranslateDateTimeTruncationToDate`: `CAST(... AS DATE)` with `forceCast: true` (`FirebirdMemberTranslator.cs:237-243`).
- String JOIN -> `LIST(value, separator)` aggregate function.
- `Guid.NewGuid()` -> `Gen_Uuid()`.
- `Guid.ToString()` -> `lower(UUID_TO_CHAR(...))`.

`Firebird4MemberTranslator` (`Source/LinqToDB/Internal/DataProvider/Firebird/Translation/Firebird4MemberTranslator.cs`) -- **added in PR #5467**:
- Extends `FirebirdMemberTranslator`; introduces `Firebird4DateFunctionsTranslator : FirebirdDateFunctionsTranslator`.
- `TranslateServerNow` -> `CURRENT_TIMESTAMP` (typed as `DateTime`).
- `TranslateUtcNow` -> `CURRENT_TIMESTAMP AT TIME ZONE 'UTC'` (typed as `DateTime`).
- `TranslateZonedUtcNow` -> `CURRENT_TIMESTAMP AT TIME ZONE 'UTC'` (typed as `DateTimeOffset`/`dbDataType` passed in). FB4+ supports `TIMESTAMP WITH TIME ZONE`, enabling these translations that are `null` (unsupported) in v25/v3.

`Firebird5MemberTranslator` (`Source/LinqToDB/Internal/DataProvider/Firebird/Translation/Firebird5MemberTranslator.cs`):
- Now extends `Firebird4MemberTranslator` (previously extended `FirebirdMemberTranslator` directly).
- `Firebird5DateFunctionsTranslator` now extends `Firebird4DateFunctionsTranslator` -- inherits the v4 now-translations while adding v5 native `QUARTER`.
- `Quarter` date part -> native `Extract(quarter from ...)` (v5 added this; v25-v4 emulate it arithmetically).
- `LIST DISTINCT` aggregate: `IsDistinctSupported = true` (v5 only).

### Now-translation matrix by version

| Version | `DateTime.Now` / `ServerNow` | `DateTime.UtcNow` | `DateTimeOffset.UtcNow` (`ZonedUtcNow`) |
|---|---|---|---|
| v25, v3 | `null` (not translated) | `null` | `null` |
| v4 | `CURRENT_TIMESTAMP` | `CURRENT_TIMESTAMP AT TIME ZONE 'UTC'` | `CURRENT_TIMESTAMP AT TIME ZONE 'UTC'` |
| v5 | `CURRENT_TIMESTAMP` (inherited) | `CURRENT_TIMESTAMP AT TIME ZONE 'UTC'` (inherited) | `CURRENT_TIMESTAMP AT TIME ZONE 'UTC'` (inherited) |

## Mapping schema hierarchy

```
LockedMappingSchema("Firebird") -- FirebirdMappingSchema (base, shared)
  Firebird25MappingSchema  (+ FirebirdAdapterMappingSchema)
  Firebird3MappingSchema   (+ FirebirdAdapterMappingSchema)
  Firebird4MappingSchema   (+ FirebirdAdapterMappingSchema)
  Firebird5MappingSchema   (+ FirebirdAdapterMappingSchema)
```

`FirebirdMappingSchema` (`Source/LinqToDB/Internal/DataProvider/Firebird/FirebirdMappingSchema.cs`):
- `ColumnNameComparer = StringComparer.OrdinalIgnoreCase` (Firebird identifiers are case-insensitive by default).
- `string` -> `NVarChar(255)` default.
- `decimal` -> `Decimal(18, 10)`.
- `ulong` -> `Decimal(20, 0)`.
- Non-ASCII / special-char strings encoded as `_utf8 x'...'` hex literals when `IsLiteralEncodingSupported = true` (`FirebirdMappingSchema.cs:156-166`).
- Floating-point special values (NaN, +/-Infinity): encoded as `LOG(...)` expressions (Firebird lacks literal infinity support; `FirebirdMappingSchema.cs:56-79`).
- Guid stored as `CHAR(16) CHARACTER SET OCTETS` (binary); byte-order reversal applied on little-endian platforms (`FirebirdMappingSchema.cs:108-119`).
- `BigInteger` -> `INT128`.
- `bool` -> `BOOLEAN` (v3+); v25 uses `CHAR(1)` with `'1'`/`'0'` representation via `Firebird25MappingSchemaBase`.

`FirebirdProviderAdapter.MappingSchema` (`FirebirdAdapterMappingSchema`) registers FB4+ types: `FbDecFloat` -> `DataType.DecFloat`; `FbZonedDateTime` -> `DataType.DateTimeOffset`; `FbZonedTime` -> `DataType.TimeTZ` -- conditionally, when the loaded assembly version supports these types (FB client 7.10.0+).

## Provider adapter

`FirebirdProviderAdapter` (`Source/LinqToDB/Internal/DataProvider/Firebird/FirebirdProviderAdapter.cs`):
- Single assembly: `FirebirdSql.Data.FirebirdClient`.
- Types loaded: `FbConnection`, `FbDataReader`, `FbParameter`, `FbCommand`, `FbTransaction`, `FbDbType`.
- Optionally loaded (FB client 7.10.0+): `FbDecFloat`, `FbZonedDateTime`, `FbZonedTime` from `FirebirdSql.Data.Types` namespace.
- `FbDbType` enum covers standard types + v4 additions: `TimeStampTZ(17)`, `TimeStampTZEx(18)`, `TimeTZ(19)`, `TimeTZEx(20)`, `Dec16(21)`, `Dec34(22)`, `Int128(23)`.
- `IsDateOnlySupported`: true when assembly version >= 9.0.0.
- `SetDbType` / `GetDbType`: via `TypeMapper` accessor on `FbParameter.FbDbType`.
- `ClearAllPools`: invokes `FbConnection.ClearAllPools()` via `TypeMapper`.
- Singleton: `FirebirdProviderAdapter.Instance` (lazy, thread-safe).

## Provider detection

`FirebirdProviderDetector` (`Source/LinqToDB/Internal/DataProvider/Firebird/FirebirdProviderDetector.cs`):
- Extends `ProviderDetectorBase<Provider, FirebirdVersion>` with `defaultVersion = FirebirdVersion.v25`.
- `DetectProvider`: matches `ProviderName.Firebird25/3/4/5` directly; falls through to configuration-string heuristics (`"2.5"`, `"25"`, `"5"`, `"4"`, `"3"` substrings); if `AutoDetectProvider`, calls `DetectServerVersion`.
- `DetectServerVersion` (`FirebirdProviderDetector.cs:83-101`): runs `SELECT rdb$get_context('SYSTEM', 'ENGINE_VERSION') from rdb$database` (requires FB 2.1+); parses `major` version: `< 3` -> v25, `< 4` -> v3, `< 5` -> v4, else -> v5.
- Four static lazy providers (one per version) -- singletons.

## SetParameterType

`FirebirdDataProvider.SetParameterType` (`FirebirdDataProvider.cs:146-175`):
- `DateTimeOffset` -> `FbDbType.TimeStampTZ` (via provider parameter if accessible).
- Type promotions: `SByte` -> Int16, `UInt16` -> Int32, `UInt32` -> Int64, `UInt64` -> Decimal, `VarNumeric` -> Decimal, `DateTime2` -> DateTime.
- `SetParameter`: converts `DateOnly` to `DateTime` when `!Adapter.IsDateOnlySupported` (client < 9.0.0).

## Bulk copy

`FirebirdBulkCopy` (`Source/LinqToDB/Internal/DataProvider/Firebird/FirebirdBulkCopy.cs`):
- No native bulk-copy protocol. Only `MultipleRows` strategy (multi-row INSERT ... SELECT ... FROM rdb$database UNION ALL).
- `MaxSqlLength`: v25 -> 65535 bytes; v3+ -> 10 MB.
- `MaxParameters`: 32767 (DSQL compiler limit: 65536/2 - 1).
- `MaxMultipleRows`: v25 -> 127; v3+ -> 254 (Firebird "Too many Contexts" limit; v25 requires half).
- `CastFirstRowLiteralOnUnionAll`, `CastFirstRowParametersOnUnionAll`, `CastAllRowsParametersOnUnionAll`: all true.
- `CastLiteral`: casts VarChar/NVarChar literals to avoid CHAR padding on UNION ALL.
- `KeepIdentity = true` throws: Firebird has no built-in identity management; identity is trigger + generator based and triggers must be disabled manually.
- Uses `MultipleRowsCopy2` (base class helper) with fake FROM `rdb$database`.

## Schema provider

`FirebirdSchemaProvider` (`Source/LinqToDB/Internal/DataProvider/Firebird/FirebirdSchemaProvider.cs`):
- Extends `SchemaProviderBase`.
- Tables, primary keys, columns, foreign keys via ADO.NET `DbConnection.GetSchema("Tables"/"PrimaryKeys"/"Columns"/"ForeignKeyColumns")`.
- `GetDatabaseName`: strips path and extension from the connection's database string (Firebird uses file paths as database names).
- `IsDefaultSchema`: schema name `"SYSDBA"` is the default.
- `GetProcedures` / `GetProcedureParameters`: queries `RDB$PROCEDURES`, `RDB$FUNCTIONS`, `RDB$PROCEDURE_PARAMETERS`, `RDB$FUNCTION_ARGUMENTS`, `RDB$FIELDS` directly via SQL. Two variants per method -- one for v3+ (includes `RDB$PACKAGE_NAME`, functions with packages, private-flag filter) and one for v25.
- Procedure types: `'P'` = stored procedure, `'TF'` = table function (selectable), `'F'` = function.
- `CreateTypeName` (`FirebirdSchemaProvider.cs:327-354`): translates raw Firebird type codes (integer, sub-type, scale triple) to string type names -- covers BLOB, DECIMAL/NUMERIC, TIMESTAMP WITH TIME ZONE, DECFLOAT, INT128.
- `GetDataTypes`: supplements ADO.NET `GetSchema("DataTypes")` with manually added rows for `boolean`, `int128`, `decfloat`, `timestamp with time zone`, `time with time zone` because the FB client's metadata XML doesn't include them.
- `GetProcedureSchema` catches `"SQL error code = -84"` (non-value procedures) and `"is not selectable"` errors -- returns null to skip schema read.
- `LoadProcedureTableSchema`: removes output parameters that duplicate result columns in FOR SELECT procedures.
- `BuildTableFunctionLoadTableSchemaCommand`: wraps as `SELECT * FROM proc(NULL, NULL, ...)` for table functions.

## Known issues / debt

1. `FirebirdSqlOptimizer.cs:8-9` -- TODO: implement recursive CTE outer-join optimization. Firebird disallows a recursive CTE reference in an outer join (`A recursive reference cannot participate in an outer join`); the optimizer falls back to CROSS JOIN which fails for some test cases (`Issue3360_TypeByOtherQuery_AllProviders`).
2. `FirebirdSqlExpressionConvertVisitor.cs:166` -- code duplication comment: `GuidMemberTranslator.TranslateGuildToString` logic (`UUID_TO_CHAR` + `lower`) is duplicated in `ConvertConversion` (Guid->string cast path). Marked with `// TODO`.
3. `FirebirdSqlBuilder.cs:277` -- TODO: "We should avoid such tricks, proper TypeMapping required" in `BuildParameter` where `DataType.Undefined` is resolved via `MappingSchema.GetDataType`.
4. `FirebirdSqlBuilder.cs:297` -- TODO: "temporary guard against cast to unknown type (Variant)" in `BuildParameter`.
5. `FirebirdBulkCopy.cs:55` -- `KeepIdentity = true` always throws; there is no workaround short of manually disabling triggers, which the error message instructs the user to do.
6. `FirebirdSchemaProvider.cs:424` -- comment notes the FB ADO.NET client's `FbMetaData.xml` doesn't register FB4+ types in `GetSchema("DataTypes")`, requiring manual addition.
7. `FirebirdSqlBuilder.cs:658-662` -- v25 `NullCharSize = 1` workaround for bad row-size calculation (64KB row limit); this virtual field is a latent complexity point.

## Files (Tier 1 / Tier 2)

### Tier 1 (11 files, all visited)

| File | Role |
|---|---|
| `Internal/DataProvider/Firebird/FirebirdDataProvider.cs` | Abstract base + 4 concrete version providers; `SqlProviderFlags`, BulkCopy dispatch, parameter type mapping |
| `Internal/DataProvider/Firebird/FirebirdSqlBuilder.cs` | v25 SQL builder base; pagination, DDL, identifier quoting, type mapping, CAST wrapping |
| `Internal/DataProvider/Firebird/FirebirdSqlOptimizer.cs` | Statement rewriter; WrapParameters; DELETE/UPDATE alternative form |
| `DataProvider/Firebird/FirebirdTools.cs` | Public registration / factory entry point |
| `DataProvider/Firebird/FirebirdVersion.cs` | Version enum (AutoDetect, v25, v3, v4, v5) |
| `DataProvider/Firebird/FirebirdOptions.cs` | Provider options record (BulkCopyType, IdentifierQuoteMode, IsLiteralEncodingSupported) |
| `DataProvider/Firebird/FirebirdIdentifierQuoteMode.cs` | None/Quote/Auto enum |
| `Internal/DataProvider/Firebird/FirebirdProviderAdapter.cs` | ADO.NET dynamic adapter; FbDbType enum; optional FB4+ type support |
| `Internal/DataProvider/Firebird/FirebirdProviderDetector.cs` | Auto-detection via `rdb$get_context`; name-based heuristics |
| `Internal/DataProvider/Firebird/FirebirdMappingSchema.cs` | Per-version mapping schemas; literal encoding; float special values; Guid binary format |
| `Internal/DataProvider/Firebird/FirebirdBulkCopy.cs` | Multi-row INSERT bulk copy; v25/v3+ size limits |

### Tier 2 (12 files, all visited; 1 new file added to set)

| File | Role |
|---|---|
| `Internal/DataProvider/Firebird/Firebird3SqlBuilder.cs` | OFFSET/FETCH pagination; suppresses FIRST/SKIP |
| `Internal/DataProvider/Firebird/Firebird4SqlBuilder.cs` | LATERAL/APPLY joins; BINARY/VARBINARY/GUID type overrides |
| `Internal/DataProvider/Firebird/Firebird3SqlOptimizer.cs` | Delegates to `Firebird3SqlExpressionConvertVisitor` |
| `Internal/DataProvider/Firebird/FirebirdSqlBuilder.Merge.cs` | MERGE source type inference; VALUES not supported; FB5 `NOT MATCHED BY SOURCE` ops |
| `Internal/DataProvider/Firebird/FirebirdSqlExpressionConvertVisitor.cs` | Bitwise ops, string ops, CONTAINING/STARTING WITH, Guid conversions, CAST normalization |
| `Internal/DataProvider/Firebird/Firebird3SqlExpressionConvertVisitor.cs` | v3 BOOLEAN-aware bool predicate handling |
| `Internal/DataProvider/Firebird/Translation/FirebirdMemberTranslator.cs` | Date/string/Guid member translations; `LIST` aggregate; `Gen_Uuid`; `DateAdd` non-parameter constraint; `TranslateNow` returns null |
| `Internal/DataProvider/Firebird/Translation/Firebird4MemberTranslator.cs` | v4 Now/UtcNow/ZonedUtcNow translations via `CURRENT_TIMESTAMP AT TIME ZONE 'UTC'` [added PR #5467] |
| `Internal/DataProvider/Firebird/Translation/Firebird5MemberTranslator.cs` | Native `QUARTER` extract; `LIST DISTINCT` support; extends `Firebird4MemberTranslator` |
| `Internal/DataProvider/Firebird/FirebirdSchemaProvider.cs` | Schema via ADO.NET GetSchema + direct RDB$ queries; `CreateTypeName` type-code mapping |
| `DataProvider/Firebird/FirebirdExtensions.cs` | `UuidToChar` SQL extension method |
| `DataProvider/Firebird/FirebirdFactory.cs` | Config-based factory (reads `version` attribute) |
| `DataProvider/Firebird/IFirebirdExtensions.cs` | Marker interface for Firebird SQL extensions |

## Inbound / outbound dependencies

**Inbound:**
- `DATA` / `CORE` -- calls `FirebirdTools.CreateDataConnection`, `FirebirdTools.GetDataProvider`.
- `INTERNAL-API` -- `DynamicDataProviderBase<FirebirdProviderAdapter>` base class; `ProviderDetectorBase`; `BasicBulkCopy`; `SchemaProviderBase`; `MemberTranslatorBase`; `TypeMapper`.
- `MAPPING` -- `LockedMappingSchema`; `MappingSchema`.

**Outbound:**
- `SQL-PROVIDER` -- extends `BasicSqlBuilder<FirebirdOptions>`, `BasicSqlOptimizer`, `SqlExpressionConvertVisitor`.
- `SQL-AST` -- `SqlStatement`, `SelectQuery`, `SqlMergeOperationClause`, `SqlValuesTable`, etc. (read-only consumption).
- `INTERNAL-API` (`Translation`) -- `ProviderMemberTranslatorDefault`, `DateFunctionsTranslatorBase`, `StringMemberTranslatorBase`, `GuidMemberTranslatorBase`, `SqlTypesTranslationDefault`.

## See also

- [BasicSqlBuilder, BasicSqlOptimizer](../../architecture/sql-provider.md) -- base SQL emission and optimization.
- [DataProviderBase, DynamicDataProviderBase](../INTERNAL-API/INDEX.md) -- provider base classes, TypeMapper, ProviderDetectorBase.
- [MappingSchema, LockedMappingSchema](../MAPPING/INDEX.md) -- mapping schema hierarchy.
- [PROV-POSTGRES INDEX](../PROV-POSTGRES/INDEX.md) -- comparable `IdentifierQuoteMode` enum pattern.
- [PROV-ORACLE INDEX](../PROV-ORACLE/INDEX.md) -- comparable generator-based identity, DDL-inside-PL/SQL-block pattern.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 11 / 11
- Tier 2 (visited / total): 12 / 12 (100%)
- Tier 3 (skipped, logged): 0

Read (this run -- delta):
- `Source/LinqToDB/Internal/DataProvider/Firebird/FirebirdDataProvider.cs` -- CreateMemberTranslator three-way dispatch added
- `Source/LinqToDB/Internal/DataProvider/Firebird/FirebirdMappingSchema.cs` -- no structural delta
- `Source/LinqToDB/Internal/DataProvider/Firebird/Translation/Firebird4MemberTranslator.cs` (NEW) -- AT TIME ZONE 'UTC' overrides
- `Source/LinqToDB/Internal/DataProvider/Firebird/Translation/Firebird5MemberTranslator.cs` -- now inherits Firebird4MemberTranslator
- `Source/LinqToDB/Internal/DataProvider/Firebird/Translation/FirebirdMemberTranslator.cs` -- TranslateNow returns null; TranslateDateTimeTruncationToDate uses forceCast=true

</details>
