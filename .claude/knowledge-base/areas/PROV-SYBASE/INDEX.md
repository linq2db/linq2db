---
area: PROV-SYBASE
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
coverage_tier_1: 10/10
coverage_tier_2: 6/6
---

# PROV-SYBASE -- SAP ASE (Sybase Adaptive Server Enterprise)

SAP Adaptive Server Enterprise (formerly Sybase ASE). **Not** SQL Anywhere. Single dialect at the linq2db level -- no version axis in provider names. Historical T-SQL heritage shared with Microsoft SQL Server (see [PROV-SQLSERVER](../PROV-SQLSERVER/INDEX.md)), but with meaningful divergences in identifier limits, type system, and paging.

---

## Subsystems

### Public surface (`Source/LinqToDB/DataProvider/Sybase/`)

`SybaseTools` -- static façade for registration and connection creation. Delegates detection to `SybaseProviderDetector` (held in an `internal static` field). Exposes `AutoDetectProvider`, `GetDataProvider(SybaseProvider, ...)`, `CreateDataConnection(...)` overloads (string / DbConnection / DbTransaction), and `ResolveSybase(path)` / `ResolveSybase(Assembly)` for assembly-resolver injection.
`Source/LinqToDB/DataProvider/Sybase/SybaseTools.cs:12`

`SybaseProvider` enum -- three values:
- `AutoDetect` -- let the detector pick at runtime.
- `Unmanaged` -- SAP SDK driver (`Sybase.AdoNet45.AseClient.dll`).
- `DataAction` -- open-source managed driver (`AdoNetCore.AseClient`, DataAction org on GitHub).
`Source/LinqToDB/DataProvider/Sybase/SybaseProvider.cs:8`

`SybaseOptions` -- single-field record (`BulkCopyType`). Default is `BulkCopyType.MultipleRows`, **not** `ProviderSpecific`, because the SAP native driver has known bugs: wrong `false` inserted for the first-record BIT field and an exception when identity columns are present during bulk copy. The record comment documents this explicitly.
`Source/LinqToDB/DataProvider/Sybase/SybaseOptions.cs:14`

`SybaseFactory` -- `DataProviderFactoryBase` subclass. Resolves assembly name from configuration attributes to one of the three `SybaseProvider` enum values and calls `SybaseTools.GetDataProvider`. Tagged `[UsedImplicitly]` -- loaded by the configuration infrastructure via reflection.
`Source/LinqToDB/DataProvider/Sybase/SybaseFactory.cs:13`

### Data provider (`Internal/DataProvider/Sybase/`)

`SybaseDataProvider` -- abstract, extends `DynamicDataProviderBase<SybaseProviderAdapter>`. Two concrete sealed subclasses in the same file:
- `SybaseDataProviderNative` (`ProviderName.Sybase`, `SybaseProvider.Unmanaged`)
- `SybaseDataProviderManaged` (`ProviderName.SybaseManaged`, `SybaseProvider.DataAction`)

`SqlProviderFlags` configuration set in the constructor:
- `AcceptsTakeAsParameter = false` -- `TOP n` is an inline literal, not a parameter.
- `IsSkipSupported = false` -- no native OFFSET; no paging beyond TOP.
- `IsSubQueryTakeSupported = false`
- `CanCombineParameters = false`
- `IsCrossJoinSupported = false`
- `IsDistinctSetOperationsSupported = false` -- TODO comment notes possible 16SP3 enablement.
- `IsWindowFunctionsSupported = false`
- `IsDerivedTableOrderBySupported = false`
- `IsOrderBySubQuerySupported = false`
- `IsUpdateTakeSupported = true` -- `UPDATE ... TOP n` is valid.
- `SupportsBooleanType = false` -- BIT cannot be NULL (see below).
- `SupportedCorrelatedSubqueriesLevel = 1`
- `IsCorrelatedSubQueryTakeSupported = false`
- `IsJoinDerivedTableWithTakeInvalid = true`
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseDataProvider.cs:39`

`SetParameter` overrides handle: `SByte -> Int16`, `TimeSpan -> DateTime(1900,1,1)+ts`, `Xml -> NVarChar`, `Guid -> Char(36)`, `char -> string` (native client only), `DateOnly -> DateTime`.
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseDataProvider.cs:141`

`ConvertParameterType` -- native client (`ProviderName.Sybase`) unwraps nullable types, maps `char/Guid -> string`, `TimeSpan -> DateTime` (because `AseBulkManager.IsWrongType` rejects them).
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseDataProvider.cs:82`

Provider fields: `DateTimeOffset` is read back as `new DateTimeOffset(r.GetDateTime(i), default)` (offset zero); `TimeSpan` is read back as `r.GetDateTime(i) - new DateTime(1900, 1, 1)`; `time`-typed columns go through `GetDateTimeAsTime` which strips the 1900-01-01 date part when present.
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseDataProvider.cs:61`

`SupportedTableOptions` -- supports temporary table prefix conventions (`#name` = local, `##name` = global), `CreateIfNotExists`, `DropIfExists`.
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseDataProvider.cs:100`

### ADO.NET adapter (`SybaseProviderAdapter`)

Loads one of two assemblies at runtime via reflection (`TypeMapper`). Both expose the same `AseXxx` type names under different namespaces:
- Native: `Sybase.AdoNet45.AseClient` / namespace `Sybase.Data.AseClient` / factory name `Sybase.Data.AseClient`.
- Managed: `AdoNetCore.AseClient` / namespace `AdoNetCore.AseClient` / no `DbProviderFactory` name (null).

`AseDbType` enum (wrapped) -- 35 values including `Unitext` (-10), `UniChar` (-8), `UniVarChar` (-9), `SmallMoney` (-201), `TimeStamp` (-203), `SmallDateTime` (-202), `Image` (-4), `Text` (-1). These are mapped in `SetParameterType`.
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseProviderAdapter.cs:186`

`BulkCopyAdapter` -- wraps `AseBulkCopy` and `AseBulkCopyColumnMapping`. **Only created for the native driver** (`supportsBulkCopy = true`). The managed DataAction driver does not expose `AseBulkCopy`; `BulkCopy` property is `null` for managed instances.
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseProviderAdapter.cs:136`

Singletons protected by `Lock` (one per driver variant): `_nativeInstance`, `_managedInstance`.
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseProviderAdapter.cs:15`

### Provider detection (`SybaseProviderDetector`)

`DetectProvider(ConnectionOptions)` checks `ProviderName` string, then `ConfigurationString` for "Managed"/"Native" substrings, then falls back to probing the assembly directory for `Sybase.AdoNet45.AseClient.dll`. If the file exists -> `Unmanaged`; otherwise -> `DataAction` (managed).
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseProviderDetector.cs:60`

`GetDataProvider` always re-invokes `DetectProvider` -- the `provider` parameter is ignored for `AutoDetect`.

### SQL builder (`SybaseSqlBuilder`)

Extends `BasicSqlBuilder`. No intermediate base class (unlike, e.g., PROV-ORACLE which has a `*SqlBuilderBase`).

Key overrides:
- `FirstFormat` -- returns `"TOP {0}"`. No OFFSET clause.
- `BuildGetIdentity` -- emits `SELECT @@IDENTITY` after insert.
- `BuildDataTypeFromDataType` -- `Guid -> VARCHAR(36)`, `DateTime2 -> DateTime`, `Money -> MONEY`, `SmallMoney -> SMALLMONEY`, `NVarChar` capped at 5461 (ASE page-size limit), `Decimal` uses explicit `DECIMAL(p, s)` syntax (ASE default is `DECIMAL(18,0)`, not `DECIMAL(18,10)` as in the mapping schema).
- `BuildCreateTableNullAttribute` -- suppresses NULL/NOT NULL for BIT fields (BIT cannot be nullable in ASE).
- `BuildDeleteClause` -- emits `DELETE TOP n FROM <table>` form.
- `BuildUpdateTableName` -- correctly targets the update table when joined.
- `BuildInsertOrUpdateQuery` -- delegates to `BuildInsertOrUpdateQueryAsUpdateInsert` (no native MERGE upsert idiom for this path).
- `BuildCreateTableIdentityAttribute1` -- emits `IDENTITY` keyword.
- `BuildCreateTablePrimaryKey` -- emits `CONSTRAINT pk PRIMARY KEY CLUSTERED (...)`.
- `BuildTruncateTable` / `CommandCount` / `BuildCommand` -- two-command truncate: `TRUNCATE TABLE` followed by `sp_chgattribute ..., 'identity_burn_max', 0, '0'` to reset the identity seed.
- `BuildDropTableStatement` -- uses `IF (OBJECT_ID(N'...') IS NOT NULL)` guard (no `DROP TABLE IF EXISTS` syntax).
- `BuildStartCreateTableStatement` / `BuildEndCreateTableStatement` -- for non-temporary tables with `CreateIfNotExists`: wraps the DDL in `IF (OBJECT_ID(...) IS NULL) EXECUTE('...')`.
- `BuildIsDistinctPredicate` -- falls back to the fallback implementation.

`Convert(sb, value, ConvertType)`:
- Parameter names (`NameToQueryParameter`, `NameToCommandParameter`, `NameToSprocParameter`): truncated to **26 characters** (hard limit in `SybaseSqlBuilder.cs:150`), prefixed with `@`.
- Identifiers (`NameToQueryField`, `NameToQueryFieldAlias`, `NameToQueryTableAlias`, `NameToDatabase`, `NameToSchema`, `NameToQueryTable`, `NameToProcedure`): quoted in `[...]` unless `_skipBrackets`, length > 28, already bracketed, or is a temp-table (`#`-prefixed) name.
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSqlBuilder.cs:141`

`_skipAliases` flag -- when building a SELECT's subbuilder, column aliases are suppressed to avoid Sybase alias-resolution quirks in nested contexts.
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSqlBuilder.cs:47`

`IsSqlValuesTableValueTypeRequired` -- forces explicit type cast for the first row of a VALUES table when the value is `uint`, `long`, `ulong`, `float`, `double`, `decimal`, or `null`. Complemented by `BuildTypedExpression` which corrects DECIMAL facets for typed expressions.
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSqlBuilder.cs:377`

Temporary table name handling -- `GetTablePhysicalName` prefixes names with `#` (local) or `##` (global) based on `TableOptions`. Database qualifier is stripped for temp tables.
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSqlBuilder.cs:256`

**Merge partial** (`SybaseSqlBuilder.Merge.cs`):
- `IsValuesSyntaxSupported = false` -- ASE MERGE does not support the `VALUES(...)` source syntax.
- `BuildMergeStatement` / `BuildMergeTerminator` -- wraps identity-insert-enabled targets with `SET IDENTITY_INSERT <table> ON/OFF` around the MERGE.
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSqlBuilder.Merge.cs:8`

### SQL optimizer (`SybaseSqlOptimizer`)

Extends `BasicSqlOptimizer`. `CreateConvertVisitor` instantiates `SybaseSqlExpressionConvertVisitor`.

`FinalizeUpdate`:
- Rejects `UPDATE ... TOP n ORDER BY` and `DELETE ... TOP n ORDER BY` (raises `LinqToDBException` with `ErrorHelper.Sybase.Error_UpdateWithTopOrderBy` / `Error_DeleteWithTopOrderBy`).
- Rejects `UPDATE/DELETE ... SKIP n` (`Error_UpdateWithSkip` / `Error_DeleteWithSkip`).
- For UPDATE: calls `CorrectSybaseUpdate` -- if the query is incompatible (contains subqueries or self-join of the update table) it promotes to `GetAlternativeUpdate`; otherwise tries `RemoveUpdateTableIfPossible` or falls back to `GetAlternativeUpdate`.
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSqlOptimizer.cs:21`

`TransformStatement` applies `CorrectMultiTableQueries` before calling the base.

### Expression convert visitor (`SybaseSqlExpressionConvertVisitor`)

Extends `SqlExpressionConvertVisitor`.

`LikeCharactersToEscape` -- `["_", "%", "[", "]", "^"]`. The `^` bracket negation character is included (absent in most other providers).

`ConvertSqlFunction`:
- `REPLACE -> Str_Replace`
- `LENGTH -> CHAR_LENGTH`
- `CharIndex(p0, p1, p2)` (3-arg form) -> `CharIndex(p0, Substring(p1, p2, Len(p1))) + (p2 - 1)` -- emulates positional search.
- `Stuff(s, pos, _, "")` (empty replacement string) -> `Stuff(s, pos, pos, null)` -- workaround for ASE Stuff semantics.
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSqlExpressionConvertVisitor.cs:40`

`VisitExistsPredicate` -- wraps EXISTS subqueries that have set operators (`UNION`/etc.) in an outer `SELECT * FROM (...)` to make them valid.

`WrapColumnExpression` -- adds mandatory `CAST` for untyped numeric literals and parameters (`uint`, `long`, `ulong`, `float`, `double`, `decimal`) in column position to prevent type inference errors.

### Mapping schema (`SybaseMappingSchema`)

Base `LockedMappingSchema` keyed on `ProviderName.Sybase`. Two sub-schemas:
- `NativeMappingSchema` -- keyed on `ProviderName.Sybase` ("Sybase").
- `ManagedMappingSchema` -- keyed on `ProviderName.SybaseManaged` ("Sybase.Managed").

Converters registered:
- `string` -- concatenation-based string literal builder using `char(N)` for non-ASCII.
- `char` -- same pattern.
- `TimeSpan` -- inline as `'hh:mm:ss.fff'` literal (or ticks for Int64 targets).
- `byte[]` / `Binary` -- `0x` hex prefix.
- `DateTime` -- formatted as `'yyyy-MM-dd HH:mm:ss.fff'` via `BuildDateTime`.
- `DateTimeOffset` -- `((DateTimeOffset)v).DateTime` extracted and formatted identically to `DateTime` via `BuildDateTime`.
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseMappingSchema.cs:24`

Default type mappings:
- `string -> NVarChar(255)`
- `decimal -> Decimal(18, 10)` (note: ASE server default is `DECIMAL(18,0)` but linq2db uses 10-digit scale by default for .NET `decimal`)
- `DateTime` default value -> `new DateTime(1753, 1, 1)` (ASE `datetime` lower bound).
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseMappingSchema.cs:32`

### Parameters normalizer (`SybaseParametersNormalizer`)

Extends `UniqueParametersNormalizer` with `MaxLength = 26`.

The `UniqueParametersNormalizer` base strips non-ASCII non-alphanumeric-non-underscore characters, enforces first-character as ASCII letter, deduplicates via `_N` suffix, and truncates to `MaxLength`. For Sybase, parameter names are capped at 26 characters before the `@` prefix is added by `SybaseSqlBuilder.Convert`. Note the apparent two-step constraint: the normalizer truncates the internal name to 26; `SybaseSqlBuilder.Convert` also independently truncates its value to 26 before prepending `@` -- ensuring the final `@name` form stays at or under 27 characters.
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseParametersNormalizer.cs:5`
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSqlBuilder.cs:149`

No reserved-word override is present (`IsReserved` stays at the base `false` implementation). Identifier quoting via `[...]` in the SQL builder is the defense against reserved words in field/table names.

### Bulk copy (`SybaseBulkCopy`)

Extends `BasicBulkCopy`. Limits: `MaxSqlLength = 65536`, `MaxParameters = 1999` (conservative, from SAP documentation).

Strategy:
- `ProviderSpecificCopy` uses `AseBulkCopy` (via `BulkCopyAdapter`) **only** when `Adapter.BulkCopy != null` (i.e., native driver) **and** the target table is not a temp table. The comment cites an ASE bug where native bulk copy fails on temp tables with a syntax error.
- Falls back to `MultipleRowsCopy` -> `MultipleRowsCopy2` (multi-row `INSERT` batches with empty `""` separator) when the conditions are not met.
- The `ProviderSpecificCopyInternal` path does **not** escape table/column names passed to `DestinationTableName` and column mappings, as ASE bulk copy chokes on bracketed identifiers.
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseBulkCopy.cs:195`

Transaction support: if a transaction is active, the provider transaction is extracted. A comment notes a Stack Overflow reference about `sp_oledb_columns` creating a temp table during bulk copy -- the transaction must be passed through to avoid issues.
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseBulkCopy.cs:95`

### Schema provider (`SybaseSchemaProvider`)

Uses ASE system tables directly -- not `INFORMATION_SCHEMA`:
- `sysobjects` -- tables/views (`type IN ('U','V')`).
- `syscolumns` + `systypes` -- column info, including `status & 0x08` for nullability, `status & 0x80` for identity.
- `sysindexes` + `syscolumns` -- primary key detection via `status2 & 2 = 2` (unique) and `status & 2048 = 2048` (PK).
- `sysreferences` + `sysconstraints` -- foreign keys; UNION-based loop over up to 16 key columns.
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSchemaProvider.cs:74`

Stored procedures: uses `sp_oledb_stored_procedures` system procedure. Parameters: uses `sp_oledb_getprocedurecolumns`. Procedure schema inspection uses `SET FMTONLY ON/OFF`; the managed DataAction driver requires a different code path (workaround for `AdoNetCore.AseClient` issue #189 -- executes reader directly instead of using `base.GetProcedureSchema`).
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSchemaProvider.cs:193`

Unicode size correction: `@@unicharsize` and `@@ncharsize` are read once via `InitProvider` and used to convert byte-lengths to character-lengths for `unichar`/`univarchar` and `nchar`/`nvarchar` columns respectively.
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSchemaProvider.cs:24`

`GetDataTypes` returns a manually constructed list (both native and managed) -- the native provider's `GetSchema("DataTypes")` returns incomplete information. Includes ASE-specific extended types: `usmallint`, `uint`, `ubigint`, `bigdatetime`, `bigtime`, `unitext`, `unichar`, `univarchar`.
`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSchemaProvider.cs:332`

`GetProcedureParameters` -- cannot be called inside a transaction; will throw `LinqToDBException` with a message directing the caller to disable `GetSchemaOptions.GetProcedures` or remove the transaction.

### Member translator (`SybaseMemberTranslator`)

Extends `ProviderMemberTranslatorDefault`. Sub-translators:
- `SqlTypesTranslation` -- maps `DateTime2` and `DateTimeOffset` SQL types to `DataType.DateTime`.
- `DateFunctionsTranslator` -- `DatePart(part, expr)`, `DateAdd(part, inc, expr)`. Now-translation (PR #5467) uses the five-virtual split:
  - `TranslateServerNow` -> `GetDate()` (emits server local time as `DateTime`).
  - `TranslateNow` -> returns `null` (falls back to client-side evaluation; no server-side `DateTime.Now` equivalent on ASE).
  - `TranslateUtcNow` -> `GETUTCDATE()` (returns `DateTime`).
  - `TranslateZonedUtcNow` -> `GETUTCDATE()` (same function, `DateTimeOffset` result type passed through from caller).
  - `TranslateZonedNow` -- not overridden; inherits base behavior (no translation / null).
  `MakeDateTime` constructs via string concatenation with `RIGHT('0'+CAST(part AS VARCHAR(N)), N)` padding and a final `CAST`. `TranslateDateTimeTruncationToDate` uses `CONVERT(<sourceDbType>, expr)` -- the result type is taken from the source expression's existing `DbDataType` (PR #5517: preserves column `DbType` rather than forcing a fixed date type).
- `SybaseStingMemberTranslator` -- `String.Join` emulated via `SUBSTRING(concat_result, LEN(separator)+1, 8000)` pattern (no native `STRING_AGG` or `CONCAT_WS`).
- `SybaseMathMemberTranslator` -- `Round` overloads force explicit `0` precision when none provided.
- `GuidMemberTranslator` -- `Guid.ToString()` -> `Lower(Convert(NVarChar(36), guid))`.
- `TranslateNewGuidMethod` -> `NewID(1)` (non-pure function).
`Source/LinqToDB/Internal/DataProvider/Sybase/Translation/SybaseMemberTranslator.cs:13`

---

## ASE-specific SQL quirks (summary)

| Quirk | Detail | Source |
|---|---|---|
| Paging | `TOP n` only; no `OFFSET`/`FETCH` | `SybaseDataProvider.cs:41` |
| IDENTITY retrieval | `SELECT @@IDENTITY` after INSERT | `SybaseSqlBuilder.cs:37` |
| BIT nullability | BIT column cannot be NULL in ASE; NULL attribute suppressed | `SybaseSqlBuilder.cs:97` |
| Parameter length | Max 26 chars (before `@` prefix) | `SybaseParametersNormalizer.cs:5`, `SybaseSqlBuilder.cs:149` |
| Identifier length | Max 28 chars before quoting is skipped | `SybaseSqlBuilder.cs:160` |
| NVarChar max | 5461 chars (page limit) | `SybaseSqlBuilder.cs:74` |
| CreateIfNotExists | Via `IF (OBJECT_ID(...) IS NULL) EXECUTE('DDL')` | `SybaseSqlBuilder.cs:330` |
| LIKE escape | `_`, `%`, `[`, `]`, `^` | `SybaseSqlExpressionConvertVisitor.cs:18` |
| MERGE identity | `SET IDENTITY_INSERT ON/OFF` wrapping | `SybaseSqlBuilder.Merge.cs:13` |
| Bulk copy native limit | Only non-temp tables; unescaped names | `SybaseBulkCopy.cs:41`, `195` |
| System tables | `sysobjects`, `syscolumns`, `sysreferences`, `sysindexes` | `SybaseSchemaProvider.cs:78` |
| Truncate + identity | Two commands: `TRUNCATE TABLE` + `sp_chgattribute` | `SybaseSqlBuilder.cs:241` |
| DateTime lower bound | 1753-01-01 | `SybaseMappingSchema.cs:36` |
| DateTime.Now | Returns `null` (client-side only; no ASE server-local equivalent) | `SybaseMemberTranslator.cs:191` |
| DateTimeOffset literal | Offset stripped; stored/emitted as `DateTime` | `SybaseMappingSchema.cs:30`, `SybaseMemberTranslator.cs:44` |

---

## Key types

| Type | File | Role |
|---|---|---|
| `SybaseDataProvider` | `Internal/.../SybaseDataProvider.cs` | Abstract base; `SybaseDataProviderNative` / `SybaseDataProviderManaged` are the concrete singletons |
| `SybaseSqlBuilder` | `Internal/.../SybaseSqlBuilder.cs` (+`.Merge.cs`) | SQL text generation; T-SQL/ASE dialect |
| `SybaseSqlOptimizer` | `Internal/.../SybaseSqlOptimizer.cs` | Statement rewrites (UPDATE compatibility, CorrectMultiTableQueries) |
| `SybaseSqlExpressionConvertVisitor` | `Internal/.../SybaseSqlExpressionConvertVisitor.cs` | Function name mapping, LIKE escapes, EXISTS wrapping, column type casts |
| `SybaseMappingSchema` | `Internal/.../SybaseMappingSchema.cs` | Type defaults, literal converters (string/char/TimeSpan/binary/DateTime/DateTimeOffset); sub-schemas for native/managed |
| `SybaseProviderAdapter` | `Internal/.../SybaseProviderAdapter.cs` | Runtime ADO.NET type loading; `AseDbType` enum; `BulkCopyAdapter` (native only) |
| `SybaseProviderDetector` | `Internal/.../SybaseProviderDetector.cs` | Auto-detect native vs managed driver |
| `SybaseBulkCopy` | `Internal/.../SybaseBulkCopy.cs` | `AseBulkCopy` (native) or `MultipleRowsCopy2` fallback |
| `SybaseSchemaProvider` | `Internal/.../SybaseSchemaProvider.cs` | ASE system-table queries, procedure discovery |
| `SybaseParametersNormalizer` | `Internal/.../SybaseParametersNormalizer.cs` | 26-char parameter name truncation |
| `SybaseMemberTranslator` | `Internal/.../Translation/SybaseMemberTranslator.cs` | .NET member -> ASE SQL function translation; Now-split into 5 virtuals (PR #5467) |
| `SybaseTools` | `DataProvider/Sybase/SybaseTools.cs` | Public registration/connection API |
| `SybaseProvider` | `DataProvider/Sybase/SybaseProvider.cs` | ADO.NET client enum (`AutoDetect`, `Unmanaged`, `DataAction`) |
| `SybaseOptions` | `DataProvider/Sybase/SybaseOptions.cs` | `BulkCopyType` option (default `MultipleRows` due to native driver bugs) |
| `SybaseFactory` | `DataProvider/Sybase/SybaseFactory.cs` | Configuration-system factory, loaded via reflection |

---

## Files (Tier 1 / Tier 2)

**Tier 1** (10 files, all read in full):

| File | Notes |
|---|---|
| `Internal/DataProvider/Sybase/SybaseDataProvider.cs` | Provider flags, parameter handling, bulk copy dispatch |
| `Internal/DataProvider/Sybase/SybaseSqlBuilder.cs` | SQL text builder, identifier/parameter quoting |
| `Internal/DataProvider/Sybase/SybaseSqlOptimizer.cs` | Statement rewrite, UPDATE/DELETE guard |
| `Internal/DataProvider/Sybase/SybaseMappingSchema.cs` | Type defaults and literal converters |
| `Internal/DataProvider/Sybase/SybaseProviderAdapter.cs` | ADO.NET wrapper, `AseDbType`, bulk copy adapter |
| `Internal/DataProvider/Sybase/SybaseProviderDetector.cs` | Auto-detect native vs managed |
| `Internal/DataProvider/Sybase/SybaseBulkCopy.cs` | Bulk copy strategy |
| `DataProvider/Sybase/SybaseTools.cs` | Public entry point |
| `DataProvider/Sybase/SybaseProvider.cs` | ADO.NET client enum |
| `DataProvider/Sybase/SybaseOptions.cs` | Provider options |

**Tier 2** (6 files, all read in full):

| File | Notes |
|---|---|
| `Internal/DataProvider/Sybase/SybaseSqlBuilder.Merge.cs` | MERGE partial -- identity insert wrapping, no VALUES syntax |
| `Internal/DataProvider/Sybase/SybaseSqlExpressionConvertVisitor.cs` | Function renames, EXISTS wrapping, column type casts, LIKE chars |
| `Internal/DataProvider/Sybase/Translation/SybaseMemberTranslator.cs` | Date/string/math/Guid LINQ translation; Now-split (PR #5467); Date truncation DbType preservation (PR #5517) |
| `Internal/DataProvider/Sybase/SybaseSchemaProvider.cs` | System-table schema queries |
| `Internal/DataProvider/Sybase/SybaseParametersNormalizer.cs` | MaxLength=26 override |
| `DataProvider/Sybase/SybaseFactory.cs` | Configuration factory |

Tier 3: none identified.

---

## Inbound / outbound dependencies

**Inbound:**
- `LinqToDB.DataProvider.Sybase` namespace types are part of the public API (`SybaseTools`, `SybaseProvider`, `SybaseOptions`).
- `SybaseFactory` is loaded via the linq2db configuration infrastructure reflectively.

**Outbound:**
- `DynamicDataProviderBase<SybaseProviderAdapter>` -- [INTERNAL-API](../INTERNAL-API/INDEX.md)
- `BasicSqlBuilder`, `BasicSqlOptimizer`, `SqlExpressionConvertVisitor` -- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md)
- `LockedMappingSchema`, `SqlDataType` -- [MAPPING](../MAPPING/INDEX.md)
- `SchemaProviderBase` -- [METADATA](../METADATA/INDEX.md)
- `ProviderMemberTranslatorDefault`, `DateFunctionsTranslatorBase`, `StringMemberTranslatorBase`, `MathMemberTranslatorBase` -- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md)
- T-SQL dialect heritage shared with [PROV-SQLSERVER](../PROV-SQLSERVER/INDEX.md): `TOP`, `IDENTITY`, `@@IDENTITY`, `CONVERT`, `DatePart`, `DateAdd`, temp-table `#`/`##` prefixes, `OBJECT_ID()` for existence checks, `SET IDENTITY_INSERT ON/OFF`.

---

## Known issues / debt

- `IsDistinctSetOperationsSupported = false` and `IsWindowFunctionsSupported = false` carry a `TODO` noting potential enablement at ASE 16SP3; no version detection is implemented.
- `SybaseSqlExpressionConvertVisitor.cs:13` has a commented-out `SupportsDistinctAsExistsIntersect` property guarded by the same SP03 caveat.
- Native driver bulk copy has known bugs with BIT and IDENTITY fields; `SybaseOptions.BulkCopyType` defaults to `MultipleRows` as a permanent defensive workaround rather than a version-gated fix.
- `GetProcedureParameters` throws when called inside a transaction -- a hard limitation of `sp_oledb_getprocedurecolumns`. No workaround path exists; callers must disable `GetSchemaOptions.GetProcedures`.
- Managed DataAction driver does not support `AseBulkCopy`; `BulkCopy` is `null` on that adapter, causing silent fallback to multi-row INSERT. Users expecting provider-specific bulk performance must use the native driver.
- `MultipleRowsCopy2` uses empty `""` as the record separator -- intent is inherited from `BasicBulkCopy` but the comment is absent; may warrant documentation.
- The 26-character parameter-name limit is enforced in two independent places (`SybaseParametersNormalizer.MaxLength` and `SybaseSqlBuilder.Convert`). The former is the primary enforcement point; the latter is a belt-and-suspenders guard.
- `TranslateNow` returns `null` -- `DateTime.Now` has no ASE server-side equivalent and falls back to client-side evaluation. This is by design but may surprise callers who expect a server timestamp.
- `DateTimeOffset` is stored as `DateTime` (offset stripped) at both the mapping-schema level (literal emission) and the SQL-types-translation level. Round-trip fidelity for non-UTC offsets is lost.

---

## See also

- [SQL-PROVIDER/INDEX.md](../SQL-PROVIDER/INDEX.md) -- `BasicSqlBuilder`, `BasicSqlOptimizer`, SQL AST.
- [INTERNAL-API/INDEX.md](../INTERNAL-API/INDEX.md) -- `DynamicDataProviderBase`, `IDataProvider`, `IDynamicProviderAdapter`.
- [MAPPING/INDEX.md](../MAPPING/INDEX.md) -- `LockedMappingSchema`, `MappingSchema`.
- [METADATA/INDEX.md](../METADATA/INDEX.md) -- `SchemaProviderBase`, `ISchemaProvider`.
- [PROV-SQLSERVER/INDEX.md](../PROV-SQLSERVER/INDEX.md) -- T-SQL dialect origin; compare `TOP`, temp tables, IDENTITY, `@@IDENTITY`, `OBJECT_ID` checks.

<details><summary>Coverage</summary>

**Tier 1 -- read in full (10/10):**
- `Source/LinqToDB/Internal/DataProvider/Sybase/SybaseDataProvider.cs`
- `Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSqlBuilder.cs`
- `Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSqlOptimizer.cs`
- `Source/LinqToDB/Internal/DataProvider/Sybase/SybaseMappingSchema.cs`
- `Source/LinqToDB/Internal/DataProvider/Sybase/SybaseProviderAdapter.cs`
- `Source/LinqToDB/Internal/DataProvider/Sybase/SybaseProviderDetector.cs`
- `Source/LinqToDB/Internal/DataProvider/Sybase/SybaseBulkCopy.cs`
- `Source/LinqToDB/DataProvider/Sybase/SybaseTools.cs`
- `Source/LinqToDB/DataProvider/Sybase/SybaseProvider.cs`
- `Source/LinqToDB/DataProvider/Sybase/SybaseOptions.cs`

**Tier 2 -- read in full (6/6):**
- `Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSqlBuilder.Merge.cs`
- `Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSqlExpressionConvertVisitor.cs`
- `Source/LinqToDB/Internal/DataProvider/Sybase/Translation/SybaseMemberTranslator.cs`
- `Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSchemaProvider.cs`
- `Source/LinqToDB/Internal/DataProvider/Sybase/SybaseParametersNormalizer.cs`
- `Source/LinqToDB/DataProvider/Sybase/SybaseFactory.cs`

**Read (this delta run -- changed files re-read at sha 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7):**
- `Source/LinqToDB/Internal/DataProvider/Sybase/SybaseDataProvider.cs` -- two new `SqlProviderFlags` (`IsCorrelatedSubQueryTakeSupported`, `IsJoinDerivedTableWithTakeInvalid`); `DateTimeOffset` provider-field reader documented.
- `Source/LinqToDB/Internal/DataProvider/Sybase/SybaseMappingSchema.cs` -- `DateTime` and `DateTimeOffset` value-to-SQL converters added to documented converter list.
- `Source/LinqToDB/Internal/DataProvider/Sybase/Translation/SybaseMemberTranslator.cs` -- Now-translation refactored into five virtuals (PR #5467: `TranslateServerNow`, `TranslateNow`, `TranslateUtcNow`, `TranslateZonedUtcNow`); `TranslateDateTimeTruncationToDate` uses source expression's `DbDataType` (PR #5517).

**Tier 3:** none.

</details>
