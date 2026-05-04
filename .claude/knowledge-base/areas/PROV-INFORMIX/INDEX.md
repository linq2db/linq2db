---
area: PROV-INFORMIX
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 10/10
coverage_tier_2: 5/5
---

# PROV-INFORMIX

IBM Informix Dynamic Server provider. Supports two distinct ADO.NET client families under one abstraction, with an Informix-specific SQL dialect that uses `FIRST`/`SKIP` paging, `SERIAL`/`BIGSERIAL` identity columns, `INTERVAL` types, `DATETIME YEAR TO FRACTION` range qualifiers, and no native schema separation.

## Subsystems

### ADO.NET client matrix

Three underlying clients exist, collapsed into two `InformixProvider` enum values (`Source/LinqToDB/DataProvider/Informix/InformixProvider.cs:8`):

| Enum value | Assembly | Notes |
|---|---|---|
| `InformixProvider.Informix` | `IBM.Data.Informix` | SQLI or IDS variant; .NET Framework only; IDS variant gains `IfxBulkCopy` |
| `InformixProvider.DB2` | `IBM.Data.DB2` | DRDA-based IDS driver; cross-platform (.NET Core / Linux / macOS) |

`InformixProviderAdapter` (`Source/LinqToDB/Internal/DataProvider/Informix/InformixProviderAdapter.cs`) is the unification layer. It exposes two parallel type-sets:

- **Ifx path** (`SetIfxType`/`GetIfxType`, `InformixBulkCopy`, `GetDecimalReaderMethod = "GetIfxDecimal"`) — populated when loading `IBM.Data.Informix` via reflection (`CreateIfxAdapter`, line 215).
- **DB2 path** (`SetDB2Type`/`GetDB2Type`, `DB2BulkCopy`) — populated when wrapping `DB2ProviderAdapter.Instance` (constructor at line 91). The DB2 path sets `DecimalType = null` (line 107) because `DB2Decimal(double)` is not implemented for Informix.

`IsIDSProvider` (`InformixProviderAdapter.cs:68,99`) is `true` for the IDS `IBM.Data.Informix` variant (when `IfxBulkCopy` is present) and always `true` for the DB2 path.

`IsParameterOrderDependent` is `!Adapter.IsIDSProvider` (`InformixDataProvider.cs:31`) — the older SQLI client uses positional `?` parameters; IDS/DB2 use named `@name`.

`InformixProviderAdapter.IfxType` (`InformixProviderAdapter.cs:328`) enumerates both SQLI and IDS type codes with IDS values used as the canonical set.

### Provider classes

`InformixDataProviderInformix` and `InformixDataProviderDB2` are concrete sealed subclasses of `InformixDataProvider` (`InformixDataProvider.cs:20-21`). Both are registered as lazy singletons in `InformixProviderDetector` (`InformixProviderDetector.cs:15-16`). `InformixDataProvider` extends `DynamicDataProviderBase<InformixProviderAdapter>`.

`InformixDataProvider` sets these `SqlProviderFlags` relevant to the dialect (`InformixDataProvider.cs:29-39`):
- `IsInsertOrUpdateSupported = false` — no MERGE-based upsert; the optimizer rewrites.
- `IsUpdateFromSupported = false`
- `IsSubQueryOrderBySupported = false`
- `IsSubQueryTakeSupported = false`
- `RowConstructorSupport = Equality | In`

### Public surface

`InformixTools` (`Source/LinqToDB/DataProvider/Informix/InformixTools.cs`) is the entry point. Key members:

- `AutoDetectProvider` — delegates to `InformixProviderDetector.AutoDetectProvider`.
- `GetDataProvider(InformixProvider, connectionString?, DbConnection?, DbTransaction?)` — returns the correct `IDataProvider` via detection.
- Three `CreateDataConnection` overloads (connection string, `DbConnection`, `DbTransaction`).

`InformixOptions` (`Source/LinqToDB/DataProvider/Informix/InformixOptions.cs`) is a `record` with:
- `BulkCopyType` — default `BulkCopyType.ProviderSpecific`.
- `ExplicitFractionalSecondsSeparator` — default `true`; must be enabled for Informix v11.70.xC8+ and v12.10.xC2+. Controls whether `TO_DATE` format uses `%Y-%m-%d %H:%M:%S.%F5` (explicit) or `%Y-%m-%d %H:%M:%S%F5` (`InformixMappingSchema.cs:16-17`).

`InformixFactory` (`Source/LinqToDB/DataProvider/Informix/InformixFactory.cs`) is the `DataProviderFactoryBase` used by the connection-string configuration system; it maps `assemblyName` attributes to `InformixProvider` enum values.

### Provider detection

`InformixProviderDetector` (`Source/LinqToDB/Internal/DataProvider/Informix/InformixProviderDetector.cs`) extends `ProviderDetectorBase<InformixProvider>`.

Detection priority (`DetectProvider` / `DetectProvider(options, provider)`, lines 18-106):
1. Explicit `InformixProvider.Informix` or `.DB2` → returned as-is.
2. `ProviderName` string matching: `"IBM.Data.Informix"` → Informix; DB2 namespace variants → DB2.
3. `ConfigurationString` containing `"DB2"` → DB2; containing `"Informix"` → Informix.
4. Filesystem probe: looks for `IBM.Data.Informix.dll` next to the assembly. On .NET non-Framework, falls through to `DB2` if the Ifx DLL is absent (`InformixProviderDetector.cs:99-104`).

The DB2 provider detector also participates: `DB2ProviderDetector.DetectProvider` short-circuits when the configuration string contains `"Informix"` (cross-reference: PROV-DB2 area).

### SQL dialect — InformixSqlBuilder

`InformixSqlBuilder` (`Source/LinqToDB/Internal/DataProvider/Informix/InformixSqlBuilder.cs`) extends `BasicSqlBuilder`.

Key overrides:

- **Paging**: `FirstFormat` returns `"FIRST {0}"`, `SkipFormat` returns `"SKIP {0}"` (`InformixSqlBuilder.cs:103-104`). Paging is inline in the `SELECT` clause, not `OFFSET`/`FETCH`.
- **No `FROM` dual needed for constant SELECT**: instead uses `table(set{1})` as `FakeTable` (`InformixSqlBuilder.Merge.cs:14`) — a set-literal table constructor available since IDS 9.x, with a comment noting `sysmaster:sysdual` exists from 11.70.
- **`VALUES(...)` not supported** in `InformixSqlBuilder.Merge.cs:10`; `IsValuesSyntaxSupported = false`.
- **Row expressions**: `BuildSqlRow` emits `ROW(a, b)` syntax (`InformixSqlBuilder.cs:352`), not the bare `(a, b)` default.
- **Identity columns**: `BuildCreateTableFieldType` maps `DataType.Int32` identity → `SERIAL`, `DataType.Int64` identity → `SERIAL8` (`InformixSqlBuilder.cs:222-232`). After `TRUNCATE TABLE ... RESET IDENTITY`, a second command of `ALTER TABLE ... MODIFY col SERIAL(1)` is issued (line 48-52). The inserted-row identity is retrieved with `SELECT DBINFO('sqlca.sqlerrd1') FROM systables where tabid = 1` (line 56).
- **Type mappings** (`BuildDataTypeFromDataType`, lines 127-158): `VarBinary` → `BYTE`; `DateTime` → `datetime year to second`; `DateTime2` → `datetime year to fraction`; `Time` → `INTERVAL HOUR TO FRACTION(N)`; `Date` → `DATETIME YEAR TO DAY`; `Boolean` → `BOOLEAN`; `NVarChar` capped at 255 characters.
- **Object names** (`BuildObjectName`, line 247): `database@server:schema.table` syntax; schema without server requires database; no schema separator otherwise. Reference: IBM docs SSGU8G_12.1.0 ids_sqs_1652.
- **Parameters**: SQLI client uses positional `?`; IDS/DB2 uses `@name` (line 200-203). Stored procedure parameters use `:name` prefix (line 206-208).
- **`NULL IN (...)` fix**: Informix rejects bare `NULL` in `IN`/`NOT IN` predicates; both `BuildInListPredicate` and `BuildInSubQueryPredicate` wrap a `null` parameter value in `SqlCastExpression` (`InformixSqlBuilder.cs:414-443`).
- **`NULL IS NULL` / `NULL IS NOT NULL`**: replaced with `1=1` / `1=0` in `BuildSql` post-processing (`InformixSqlBuilder.cs:77-79`).
- **Typed cast syntax**: `BuildTypedExpression` uses `expr::type` (double-colon cast, line 311).
- **`LIKE` predicate**: not rewritten to `MATCHES`; uses standard SQL `LIKE` with optional `ESCAPE`.
- **No `MERGE` `VALUES` syntax**: `IsValuesSyntaxSupported = false`; `MERGE INTO` is emitted but with a hint slot (`BuildMergeInto`, `InformixSqlBuilder.Merge.cs:20-35`).

### SQL optimizer — InformixSqlOptimizer

`InformixSqlOptimizer` (`Source/LinqToDB/Internal/DataProvider/Informix/InformixSqlOptimizer.cs`) extends `BasicSqlOptimizer`.

Key behaviors:

- **`TransformStatement`**: calls `GetAlternativeDelete` and `GetAlternativeUpdate` because Informix does not support `UPDATE FROM` or `DELETE JOIN` syntax directly (`InformixSqlOptimizer.cs:131-144`). Sets alias `"$"` on the derived table for alternative delete (`line 136`).
- **`FixSetOperationValues`** (lines 71-122): works around an `IBM.Data.Db2` provider bug where a nullable column in a UNION/INTERSECT is typed as non-nullable if the first branch has a non-nullable column. Wraps affected columns with `NVL(x, NULL)` to force nullable typing. Tracked by `Issue4220Test`.
- **`Finalize`**: forces `TimeSpan` parameters to non-query-parameter (literal) mode for IDS provider because IDS does not support interval parameters explicitly (`InformixSqlOptimizer.cs:56-58`).
- **`FinalizeStatement`**: calls `WrapParameters` to handle CTE derived columns and boolean parameters, using flags `InSelect | InBinary | InFunctionParameters | CastBoolean` (`InformixSqlOptimizer.cs:162-168`).
- **`IsParameterDependedElement`**: marks `LikePredicate` as parameter-dependent when `Expr2` is not a literal value (needed because SQLI client cannot process parameters in `LIKE` patterns, line 28-36).

### Expression conversion — InformixSqlExpressionConvertVisitor

`InformixSqlExpressionConvertVisitor` (`Source/LinqToDB/Internal/DataProvider/Informix/InformixSqlExpressionConvertVisitor.cs`) extends `SqlExpressionConvertVisitor`.

- **`COALESCE`** → `NVL(a, b)` via `ConvertCoalesceToBinaryFunc` (line 69).
- **Bitwise ops**: `~` → `BITNOT(x)`; `&` → `BitAnd`; `|` → `BitOr`; `^` → `BitXor` (lines 41-54).
- **`%`** (modulo) → `Mod(a, b)` (line 50).
- **String concat** → `||` operator (line 54).
- **Length**: `PseudoFunctions.LENGTH` → `CHAR_LENGTH(value + ".") - 1` (lines 295-308) — a workaround for Informix not returning correct CHAR_LENGTH for trailing-space-trimmed strings.
- **Date/time conversions**: `DateTime` to string → `To_Char(dt, "%Y-%m-%d %H:%M:%S.%F")`; number to string → `To_Char(n)`; string to `Date` → `Date(To_Date(s, "%Y-%m-%d"))`; string to datetime → `To_Date(s, "%Y-%m-%d %H:%M:%S")` (lines 84-143).
- **Boolean column wrapping**: bare non-boolean column expressions of type `bool` are wrapped in `CAST(... AS BOOLEAN)` (line 245).
- **`NULL IN`** at visitor level: same `SqlCastExpression` wrapping as in `SqlBuilder`, applied to `SqlValue { Value: null }` at optimization phase (lines 200-231).
- **`SupportsNullInColumn = false`** — Informix cannot use `NULL` as an untyped column expression (line 18).

### Mapping schema — InformixMappingSchema

`InformixMappingSchema` (`Source/LinqToDB/Internal/DataProvider/Informix/InformixMappingSchema.cs`) is a `LockedMappingSchema`.

- `ColumnNameComparer = StringComparer.OrdinalIgnoreCase` (line 31).
- `bool` literal → `'t'::BOOLEAN` / `'f'::BOOLEAN` (line 33).
- `string` default type → `NVarChar(255)` (line 36); `byte` → `Int16` (line 37).
- `DateTime` → `TO_DATE(...)` literals; fractional seconds respect `InformixOptions.ExplicitFractionalSecondsSeparator`.
- `TimeSpan` → `INTERVAL(d hh:mm:ss.fffff) DAY TO FRACTION(5)` literal (lines 48-61).
- String escaping uses `||` concatenation and `chr(n)` for `\r`/`\n` (line 74).
- `IfxMappingSchema` chains adapter mapping schema (from `IBM.Data.Informix`) over the base; `DB2MappingSchema` chains DB2 adapter schema (line 129-131).

### Bulk copy — InformixBulkCopy

`InformixBulkCopy` (`Source/LinqToDB/Internal/DataProvider/Informix/InformixBulkCopy.cs`) extends `BasicBulkCopy`.

- `MaxSqlLength = 32767` (line 18).
- **Strategy selection** (for all three sync/async/IAsyncEnumerable overloads):
  1. If `Adapter.InformixBulkCopy != null` → `IDSProviderSpecificCopy` using `IfxBulkCopy.WriteToServer` (IDS variant of `IBM.Data.Informix` only).
  2. Else if `Adapter.DB2BulkCopy != null` → `DB2BulkCopyShared.ProviderSpecificCopyImpl` (reused from PROV-DB2 shared implementation).
  3. Else → `MultipleRowsCopy` fallback (SQLI `IBM.Data.Informix` — no bulk copy API).
  - Both provider-specific paths require no active transaction (`dataConnection.Transaction == null`, line 31).
- `IfxBulkCopyOptions.KeepIdentity` and `.TableLock` are mapped from `BulkCopyOptions` (lines 149-150).
- `MultipleRowsCopy` / `MultipleRowsCopyAsync` wrap execution in `InvariantCultureRegion` (lines 197-218) — critical because Informix decimal parsing is locale-sensitive.
- Async paths for IDS call the synchronous `WriteToServer` (noted in comments, lines 69-70, 104-105) — `IfxBulkCopy` has no async overload.

### Member translation — InformixMemberTranslator

`InformixMemberTranslator` (`Source/LinqToDB/Internal/DataProvider/Informix/Translation/InformixMemberTranslator.cs`) extends `ProviderMemberTranslatorDefault`.

Date functions (`DateFunctionsTranslator`):
- `DateParts.Year/Month/Day` → `Year()`/`Month()`/`Day()` functions.
- `DateParts.Hour/Minute/Second` → triple-cast through `EXTEND` interval types to `CHAR` then `INT` (lines 193-227).
- `DateParts.Week` → `((Extend(date, year to day) - Mdy(12, 31-WeekDay(Mdy(1,1,year)), year-1)) / 7 + INTERVAL(1) DAY TO DAY)` (lines 151-186).
- `DateParts.Millisecond` → returns `null` (not supported).
- `DateAdd` for `Millisecond` → returns `null` (lines 268-275).
- `MakeDateTime` → `Mdy(m, d, y)` for date-only; `To_Date(string, "%Y-%m-%d %H:%M:%S")` for datetime (lines 52-113).
- UTC current timestamp → complex `dbinfo('utc_current')` expression (lines 327-333).
- Truncate to date → `Extend(dt, Year to Day)` (line 301).
- Truncate to time → cast through `datetime Hour to Second` → `CHAR(8)` (lines 306-317).

String translation:
- `String.Join` → `AggregateFunctionBuilder` with `SUBSTRING(... FROM len+1)` (lines 337-356).

Guid translation:
- `Guid.ToString()` → `Lower(To_Char(guid))` (lines 360-371).

### Schema provider — InformixSchemaProvider

`InformixSchemaProvider` (`Source/LinqToDB/Internal/DataProvider/Informix/InformixSchemaProvider.cs`) extends `SchemaProviderBase`.

- **`GetTables`**: queries `systables` where `tabid >= 100` (user tables only). Owner `'informix'` is mapped to `IsDefaultSchema = true` (line 109-119).
- **`GetColumns`**: queries `systables JOIN syscolumns`; decodes Informix raw type codes (bitfield in `coltype`) into type names. Nullability: `(typeid & 0x100) != 0x100` (line 289). `SERIAL`/`SERIAL8`/`BIGSERIAL` columns are flagged `IsIdentity = SkipOnInsert = SkipOnUpdate = true` (lines 308, 318, 361).
- **`GetPrimaryKeys`**: queries `systables JOIN sysindexes` where `idxtype = 'U'`. Index column parts are resolved from `syscolumns` via per-column subqueries (up to 16 parts, lines 152-170).
- **`GetForeignKeys`**: joins `sysreferences`, `sysconstraints`, `sysindexes` for both the referencing and referenced sides, resolving column names from `syscolumns` (lines 379-490). Auto-generates `FK_ThisTable_OtherTable` names for system-generated constraint names matching the `r{tabid}_{constrid}` pattern.
- `SetDate` helper (line 188): decodes Informix's packed `coltype`/`collength` integer for `DATETIME` and `INTERVAL` columns into range-qualified type strings like `DATETIME YEAR TO FRACTION(5)`.
- No `GetProcedures` override — Informix stored procedures are not indexed.

### Parameter binding

`SetParameter` (`InformixDataProvider.cs:117`):
- `TimeSpan` → `IfxTimeSpan` factory if available and not `DataType.Int64`.
- `Guid` → `string` (char) representation.
- `byte` typed as `Int16` → promoted to `short`.
- `bool` in `BulkCopyReader.Parameter` context → `(short)(b ? 1 : 0)` + `DataType.Int16`; in regular SQL → `'t'`/`'f'` + `DataType.Char` (lines 137-148).
- `DateOnly` → `DateTime` (line 151-153).

`SetParameterType` (`InformixDataProvider.cs:159`): skips processing for `BulkCopyReader.Parameter`. For `Text`/`NText` sets provider-specific type to `IfxType.Clob` or `DB2Type.Clob`. Falls through to type remapping for unsigned integers.

`ExecuteScope` returns `InvariantCultureRegion(null)` — wraps every command execution to protect decimal parsing.

## Key types

| Type | File | Role |
|---|---|---|
| `InformixDataProvider` | `Internal/DataProvider/Informix/InformixDataProvider.cs` | Abstract provider base; concrete subclasses `InformixDataProviderInformix` / `InformixDataProviderDB2` |
| `InformixSqlBuilder` | `Internal/DataProvider/Informix/InformixSqlBuilder.cs` (+`.Merge.cs`) | SQL generation for Informix dialect |
| `InformixSqlOptimizer` | `Internal/DataProvider/Informix/InformixSqlOptimizer.cs` | Statement rewrites, NVL workaround, parameter finalization |
| `InformixSqlExpressionConvertVisitor` | `Internal/DataProvider/Informix/InformixSqlExpressionConvertVisitor.cs` | Expression-level rewrites (COALESCE, bitwise, conversions) |
| `InformixProviderAdapter` | `Internal/DataProvider/Informix/InformixProviderAdapter.cs` | Reflection-based ADO.NET bridge for both client families |
| `InformixProviderDetector` | `Internal/DataProvider/Informix/InformixProviderDetector.cs` | Auto-detect which ADO.NET client to use |
| `InformixMappingSchema` | `Internal/DataProvider/Informix/InformixMappingSchema.cs` | Type mappings and SQL literal generation |
| `InformixBulkCopy` | `Internal/DataProvider/Informix/InformixBulkCopy.cs` | Three-path bulk copy (IfxBulkCopy / DB2BulkCopy / MultipleRows) |
| `InformixSchemaProvider` | `Internal/DataProvider/Informix/InformixSchemaProvider.cs` | Schema discovery from `systables`/`syscolumns`/`sysindexes` |
| `InformixMemberTranslator` | `Internal/DataProvider/Informix/Translation/InformixMemberTranslator.cs` | LINQ member → Informix SQL function translation |
| `InformixTools` | `DataProvider/Informix/InformixTools.cs` | Public API entry point |
| `InformixOptions` | `DataProvider/Informix/InformixOptions.cs` | Provider-level options |
| `InformixProvider` | `DataProvider/Informix/InformixProvider.cs` | ADO.NET client selector enum |
| `InformixFactory` | `DataProvider/Informix/InformixFactory.cs` | Config-system factory |

## Files (Tier 1 / Tier 2)

### Tier 1 (10 files — all read in full)

| File | Purpose |
|---|---|
| `Internal/DataProvider/Informix/InformixDataProvider.cs` | Provider core, flags, parameter binding |
| `Internal/DataProvider/Informix/InformixSqlBuilder.cs` | SQL generation (main partial) |
| `Internal/DataProvider/Informix/InformixSqlOptimizer.cs` | Statement transformation |
| `Internal/DataProvider/Informix/InformixProviderAdapter.cs` | Dual-client ADO.NET bridge |
| `Internal/DataProvider/Informix/InformixProviderDetector.cs` | Client auto-detection |
| `Internal/DataProvider/Informix/InformixMappingSchema.cs` | Type mappings and literal formatters |
| `Internal/DataProvider/Informix/InformixBulkCopy.cs` | Bulk copy strategies |
| `DataProvider/Informix/InformixTools.cs` | Public entry point |
| `DataProvider/Informix/InformixOptions.cs` | Provider options |
| `DataProvider/Informix/InformixProvider.cs` | Client selector enum |

### Tier 2 (5 files — all read in full)

| File | Purpose |
|---|---|
| `Internal/DataProvider/Informix/InformixSqlBuilder.Merge.cs` | MERGE partial: FakeTable, VALUES syntax flag, MERGE INTO syntax |
| `Internal/DataProvider/Informix/InformixSqlExpressionConvertVisitor.cs` | Expression rewrite visitor |
| `Internal/DataProvider/Informix/Translation/InformixMemberTranslator.cs` | LINQ date/string/guid → SQL |
| `Internal/DataProvider/Informix/InformixSchemaProvider.cs` | Schema discovery via systables/syscolumns |
| `DataProvider/Informix/InformixFactory.cs` | Config-system factory |

## Inbound / outbound dependencies

**Inbound:**
- `LinqToDB.DataProvider.Informix.InformixTools` — the only public gateway; consumers call `GetDataProvider` or `CreateDataConnection`.
- `InformixFactory` — invoked by the connection-string configuration subsystem.

**Outbound:**
- `DynamicDataProviderBase<InformixProviderAdapter>` — from [INTERNAL-API](../INTERNAL-API/INDEX.md).
- `BasicSqlBuilder`, `BasicSqlOptimizer`, `SqlExpressionConvertVisitor`, `BasicBulkCopy`, `SchemaProviderBase` — from [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md).
- `DB2ProviderAdapter.Instance`, `DB2BulkCopyShared.ProviderSpecificCopyImpl` — from [PROV-DB2](../PROV-DB2/INDEX.md). The DB2 adapter wraps `InformixProviderAdapter` for the DB2 client path.
- `LockedMappingSchema`, `MappingSchema` — from [MAPPING](../MAPPING/INDEX.md).
- `SchemaProviderBase` — from [METADATA](../METADATA/INDEX.md).
- `ProviderMemberTranslatorDefault`, `DateFunctionsTranslatorBase`, `StringMemberTranslatorBase` — from the translation subsystem (see [INTERNAL-API](../INTERNAL-API/INDEX.md)).
- `InvariantCultureRegion` — applied in `ExecuteScope`, `GetFloat/Double/Decimal`, and all `MultipleRowsCopy*` paths to guard against locale-sensitive decimal parsing.

## Known issues / debt

- `SetParameter` has a `TODO` comment noting that the `DataType.Int64` guard for `TimeSpan` parameters "pollutes multiple places and will not work with other not-interval mappings" (`InformixDataProvider.cs:121`). Related: IDS provider deprecates `IfxTimeSpan`; the adapter handles this by treating it as `null` when the type carries `ObsoleteAttribute`.
- `InformixSqlExpressionConvertVisitor` has `//TODO: Move everything to SQLBuilder` (line 73 of the convert visitor).
- `DateParts.Millisecond` in both `TranslateDateTimeDatePart` and `TranslateDateTimeDateAdd` returns `null` (unsupported). The `DateAdd` millisecond path has a non-working code comment (lines 268-275).
- `InformixSqlBuilder.IsValidIdentifier` has two `TODO` comments about a missing reserved-words list and incomplete locale support (`InformixSqlBuilder.cs:167-169`).
- The `SQLI` provider (`IBM.Data.Informix` without `IfxBulkCopy`) falls back to `MultipleRowsCopy`; no native bulk load path exists for that client.
- The async bulk-copy path for IDS calls the synchronous `WriteToServer` — no true async on `IfxBulkCopy`.
- Collection types (`SET`, `MULTISET`, `LIST`, `ROW`) are enumerated in `GetDataTypes` but commented out (`InformixSchemaProvider.cs:50-54`).

## Pointers

- [PROV-DB2/INDEX.md](../PROV-DB2/INDEX.md) — `DB2ProviderAdapter`, `DB2BulkCopyShared`, and the shared detection guard for Informix connection strings.
- [SQL-PROVIDER/INDEX.md](../SQL-PROVIDER/INDEX.md) — `BasicSqlBuilder`, `BasicSqlOptimizer`, `WrapParametersVisitor`.
- [INTERNAL-API/INDEX.md](../INTERNAL-API/INDEX.md) — `DynamicDataProviderBase`, `ProviderDetectorBase`, `TypeMapper`, `TypeWrapper`.
- [MAPPING/INDEX.md](../MAPPING/INDEX.md) — `LockedMappingSchema`, `DataType`, `DbDataType`.
- [METADATA/INDEX.md](../METADATA/INDEX.md) — `SchemaProviderBase`, `GetSchemaOptions`.

<details><summary>Coverage</summary>

**Tier 1 — 10/10 files read in full:**

- `Source/LinqToDB/Internal/DataProvider/Informix/InformixDataProvider.cs`
- `Source/LinqToDB/Internal/DataProvider/Informix/InformixSqlBuilder.cs`
- `Source/LinqToDB/Internal/DataProvider/Informix/InformixSqlOptimizer.cs`
- `Source/LinqToDB/Internal/DataProvider/Informix/InformixProviderAdapter.cs`
- `Source/LinqToDB/Internal/DataProvider/Informix/InformixProviderDetector.cs`
- `Source/LinqToDB/Internal/DataProvider/Informix/InformixMappingSchema.cs`
- `Source/LinqToDB/Internal/DataProvider/Informix/InformixBulkCopy.cs`
- `Source/LinqToDB/DataProvider/Informix/InformixTools.cs`
- `Source/LinqToDB/DataProvider/Informix/InformixOptions.cs`
- `Source/LinqToDB/DataProvider/Informix/InformixProvider.cs`

**Tier 2 — 5/5 files read in full:**

- `Source/LinqToDB/Internal/DataProvider/Informix/InformixSqlBuilder.Merge.cs`
- `Source/LinqToDB/Internal/DataProvider/Informix/InformixSqlExpressionConvertVisitor.cs`
- `Source/LinqToDB/Internal/DataProvider/Informix/Translation/InformixMemberTranslator.cs`
- `Source/LinqToDB/Internal/DataProvider/Informix/InformixSchemaProvider.cs`
- `Source/LinqToDB/DataProvider/Informix/InformixFactory.cs`

**Tier 3 — 0 files (none identified).**

No files were deferred. All 15 in-scope files were read in full this run.

</details>
