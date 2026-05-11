---
area: PROV-DB2
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
coverage_tier_1: 11/11
coverage_tier_2: 11/11
---

# PROV-DB2

DB2 provider for linq2db. Covers **two distinct server families** sharing a single ADO.NET client family:
- **DB2 LUW** (Linux/Unix/Windows, Db2 modern brand) -- `DB2LUWDataProvider`, `DB2LUWSqlBuilder`, `DB2LUWSchemaProvider`
- **DB2 z/OS** (IBM Z mainframe) -- `DB2zOSDataProvider`, `DB2zOSSqlBuilder`, `DB2zOSSchemaProvider`

Both families share `DB2DataProvider` (abstract base), `DB2SqlBuilderBase` + `DB2SqlBuilderBase.Merge.cs`, one `DB2SqlOptimizer`, one `DB2ProviderAdapter`, one `DB2MappingSchema`.

IBM i (iSeries) is **not** covered here; see third-party [`linq2db4iSeries`](https://www.nuget.org/packages/linq2db4iSeries).

## Public surface

`DataProvider/DB2/`:
- `DB2Tools` -- static entry point: `GetDataProvider`, `CreateDataConnection`, `ResolveDB2(path/assembly)`. Default `CreateDataConnection` uses `DB2Version.LUW`. `Source/LinqToDB/DataProvider/DB2/DB2Tools.cs:23`
- `DB2Version` -- `AutoDetect | LUW | zOS`. `Source/LinqToDB/DataProvider/DB2/DB2Version.cs:8`
- `DB2Options` -- record: `BulkCopyType` (default `MultipleRows`), `IdentifierQuoteMode` (default `Auto`). `Source/LinqToDB/DataProvider/DB2/DB2Options.cs:19`
- `DB2IdentifierQuoteMode` -- `None | Quote | Auto`. `Auto` quotes identifiers starting with `_`, containing lowercase, or whitespace. `Source/LinqToDB/DataProvider/DB2/DB2IdentifierQuoteMode.cs:1`
- `DB2Factory` -- `[UsedImplicitly]` config-file factory; maps `version` attribute string `"LUW"` / `"zOS"` / `"z/OS"` to `DB2Version`. `Source/LinqToDB/DataProvider/DB2/DB2Factory.cs:14`

## Key types

| Type | File | Role |
|---|---|---|
| `DB2DataProvider` (abstract) | `Internal/DataProvider/DB2/DB2DataProvider.cs` | Core provider base; registers provider fields, sets `SqlProviderFlags`, dispatches bulk copy |
| `DB2LUWDataProvider` | same file:19 | Concrete sealed provider for LUW |
| `DB2zOSDataProvider` | same file:20 | Concrete sealed provider for z/OS |
| `DB2SqlBuilderBase` (abstract, partial) | `Internal/DataProvider/DB2/DB2SqlBuilderBase.cs` | SQL emission base; paging, identity, temp tables, object names, parameter wrapping |
| `DB2SqlBuilderBase` (Merge partial) | `Internal/DataProvider/DB2/DB2SqlBuilderBase.Merge.cs` | `IsSqlValuesTableValueTypeRequired` -- typed-NULL enforcement for VALUES tables |
| `DB2LUWSqlBuilder` | `Internal/DataProvider/DB2/DB2LUWSqlBuilder.cs` | LUW overrides: table functions (`TABLE(name)`), package-qualified names, `VARBINARY` max 32672 |
| `DB2zOSSqlBuilder` | `Internal/DataProvider/DB2/DB2zOSSqlBuilder.cs` | z/OS override: `VARBINARY` max 32704; `DateTimeOffset` -> `TIMESTAMP WITH TIME ZONE` |
| `DB2SqlOptimizer` | `Internal/DataProvider/DB2/DB2SqlOptimizer.cs` | Shared; alternative DELETE/UPDATE; `WrapParameters` in `FinalizeStatement` |
| `DB2SqlExpressionConvertVisitor` | `Internal/DataProvider/DB2/DB2SqlExpressionConvertVisitor.cs` | Bitwise rewrites, string concat `||`, type casts, `NULLIF`/`NULL IN COLUMN` suppressed |
| `DB2MappingSchema` | `Internal/DataProvider/DB2/DB2MappingSchema.cs` | Date/timestamp format chains; binary as `BX'hex'`; `DB2LUWMappingSchema` / `DB2zOSMappingSchema` leaf schemas |
| `DB2ProviderAdapter` | `Internal/DataProvider/DB2/DB2ProviderAdapter.cs` | Dynamic load of IBM assembly; wraps `DB2BulkCopy`, `DB2Parameter.DB2Type`, `DB2Connection.eServerType` |
| `DB2ProviderDetector` | `Internal/DataProvider/DB2/DB2ProviderDetector.cs` | Auto-detect via `eServerType == DB2_390 -> zOS`; fall-through from Informix guarded |
| `DB2BulkCopy` | `Internal/DataProvider/DB2/DB2BulkCopy.cs` | Provider-specific + multi-row path; z/OS multi-row uses `SYSIBM.SYSDUMMY1` source clause |
| `DB2BulkCopyShared` | `Internal/DataProvider/DB2/DB2BulkCopyShared.cs` | **`public static`** -- shared impl used by `linq2db4iSeries`; sets `KeepIdentity` / `TableLock` flags |
| `DB2MemberTranslator` | `Internal/DataProvider/DB2/Translation/DB2MemberTranslator.cs` | LUW member translations: date parts, date add/truncate, `LISTAGG`, `GREATEST`/`LEAST`, GUID-to-string via hex + substr; `TranslateNow` returns null |
| `DB2zOSMemberTranslator` | `Internal/DataProvider/DB2/Translation/DB2zOSMemberTranslator.cs` | z/OS member translations: extends `DB2MemberTranslator`; overrides `CreateDateMemberTranslator()` to emit `CURRENT TIMESTAMP WITH TIME ZONE` for `TranslateServerNow` |
| `DB2LUWSchemaProvider` | `Internal/DataProvider/DB2/DB2LUWSchemaProvider.cs` | Queries `SYSCAT.*`; ADO.NET `GetSchema("DataTypes")` for type map |
| `DB2zOSSchemaProvider` | `Internal/DataProvider/DB2/DB2zOSSchemaProvider.cs` | Queries `SYSIBM.SYS*`; static `GetDataTypes` list; FK resolution via `SYSIBM.SYSRELS + SYSFOREIGNKEYS` |
| `DB2Extensions` | `Internal/DataProvider/DB2/DB2Extensions.cs` | Internal: `DbDataReader.ToString(i)` with `TrimEnd()` for trailing-space trimming |

## ADO.NET clients

Single assembly family from IBM. `DB2ProviderAdapter` loads:
- .NET Framework: `IBM.Data.DB2` (namespace `IBM.Data.DB2`)
- .NET Core/5+: `IBM.Data.Db2` (namespace `IBM.Data.Db2`); falls back to `IBM.Data.DB2.Core` (namespace `IBM.Data.DB2.Core`) for older installs

`Source/LinqToDB/Internal/DataProvider/DB2/DB2ProviderAdapter.cs:22`

The adapter wraps `DB2Connection.eServerType` (type `DB2ServerTypes`) for server-family detection. `DB2_390 -> zOS`; all others -> LUW.

## Server-family matrix

| Feature | DB2 LUW | DB2 z/OS |
|---|---|---|
| `SqlBuilder` | `DB2LUWSqlBuilder` | `DB2zOSSqlBuilder` |
| `SchemaProvider` | `DB2LUWSchemaProvider` | `DB2zOSSchemaProvider` |
| `MemberTranslator` | `DB2MemberTranslator` | `DB2zOSMemberTranslator` |
| Catalog views | `SYSCAT.*` | `SYSIBM.SYS*` |
| `SqlOptimizer` | shared `DB2SqlOptimizer` | shared `DB2SqlOptimizer` |
| `DateTime.Now` / server now | not translated (returns null) | `CURRENT TIMESTAMP WITH TIME ZONE` (z/OS `TranslateServerNow`) |
| `DateTime.UtcNow` | `CURRENT TIMESTAMP - CURRENT TIMEZONE` | `CURRENT TIMESTAMP - CURRENT TIMEZONE` (inherited) |
| `DateTimeOffset` column type | `TIMESTAMP` | `TIMESTAMP WITH TIME ZONE` |
| Identity retrieval | `SELECT ... FROM NEW TABLE (INSERT ...)` wrapping | second command `SELECT IDENTITY_VAL_LOCAL() FROM SYSIBM.SYSDUMMY1` |
| UPDATE TAKE/SKIP | supported | not supported |
| VARBINARY max | 32672 | 32704 |
| Multi-row INSERT source | standard VALUES | `VALUES ... FROM SYSIBM.SYSDUMMY1` |
| Procedures catalog | `SYSCAT.PROCEDURES` + `SYSCAT.FUNCTIONS` | `SYSIBM.SYSROUTINES` + `SYSIBM.SYSPARMS` |
| DataTypes info | ADO.NET `GetSchema("DataTypes")` | static list in `DB2zOSSchemaProvider.GetDataTypes` |

## SQL dialect features

### Paging
`DB2SqlBuilderBase` emits `OFFSET {0} ROWS` / `FETCH NEXT {0} ROWS ONLY` with `OffsetFirst = true`. `Source/LinqToDB/Internal/DataProvider/DB2/DB2SqlBuilderBase.cs:144`

Commented-out fallback (`ROW_NUMBER`-based) exists in `DB2SqlOptimizer.TransformStatement` for older LUW 9/10 (pre-OFFSET). `Source/LinqToDB/Internal/DataProvider/DB2/DB2SqlOptimizer.cs:19` -- not active; enabling it requires version tracking to be introduced first.

### IDENTITY columns
`GENERATED ALWAYS AS IDENTITY` in `BuildCreateTableIdentityAttribute1`. `Source/LinqToDB/Internal/DataProvider/DB2/DB2SqlBuilderBase.cs:252`

LUW wraps the INSERT in `SELECT ... FROM NEW TABLE (...)` to retrieve the generated value. z/OS issues `SELECT IDENTITY_VAL_LOCAL() FROM SYSIBM.SYSDUMMY1` as a separate command (`CommandCount` -> 2 if identity field not found). `Source/LinqToDB/Internal/DataProvider/DB2/DB2SqlBuilderBase.cs:37`

TRUNCATE with `ResetIdentity` emits `ALTER TABLE ... ALTER <col> RESTART WITH 1` per identity column. `Source/LinqToDB/Internal/DataProvider/DB2/DB2SqlBuilderBase.cs:57`

### MERGE / INSERT OR UPDATE
`BuildInsertOrUpdateQuery` delegates to `BuildInsertOrUpdateQueryAsMerge` with the dummy source `FROM SYSIBM.SYSDUMMY1 FETCH FIRST 1 ROW ONLY`. `Source/LinqToDB/Internal/DataProvider/DB2/DB2SqlBuilderBase.cs:237`

### Temp tables
- `IsLocalTemporary*` -> `DECLARE GLOBAL TEMPORARY TABLE` with `SESSION` schema prefix. `Source/LinqToDB/Internal/DataProvider/DB2/DB2SqlBuilderBase.cs:342`
- `IsGlobalTemporaryStructure` -> `CREATE GLOBAL TEMPORARY TABLE`.
- Automatically appends `ON COMMIT DELETE ROWS` (transaction scope) or `ON COMMIT PRESERVE ROWS`. `Source/LinqToDB/Internal/DataProvider/DB2/DB2SqlBuilderBase.cs:407`
- PRIMARY KEY constraints are silently suppressed on temp tables (DB2 does not support them). `Source/LinqToDB/Internal/DataProvider/DB2/DB2SqlBuilderBase.cs:416`

### DROP / CREATE IF EXISTS
Both use `BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '<code>' BEGIN END; EXECUTE IMMEDIATE '...'; END` blocks. DROP: SQLSTATE `42704`; CREATE: SQLSTATE `42710`. `Source/LinqToDB/Internal/DataProvider/DB2/DB2SqlBuilderBase.cs:315`

### SELECT without FROM
Redirected to `FROM SYSIBM.SYSDUMMY1` (single-row dummy table). `Source/LinqToDB/Internal/DataProvider/DB2/DB2SqlBuilderBase.cs:136`

### CTEs
`CteFirst = false` -- WITH clause after the SELECT keyword in standard DB2 position. Supported: `IsCommonTableExpressionsSupported = true`. Recursive CTE join-with-condition not supported (`IsRecursiveCTEJoinWithConditionSupported = false`). `Source/LinqToDB/Internal/DataProvider/DB2/DB2DataProvider.cs:38`

### Parameter wrapping (CAST injection)
DB2 ignores parameter type information in SELECT column positions. `DB2SqlOptimizer.FinalizeStatement` runs `WrapParameters` which inserts explicit `CAST(... AS <type>)` for parameters in SELECT columns, INSERT/UPDATE setters, and function arguments. `Source/LinqToDB/Internal/DataProvider/DB2/DB2SqlOptimizer.cs:56`

### Values table typed NULLs
`IsSqlValuesTableValueTypeRequired` forces a typed cast in two cases: (1) the first row when an entire column contains only NULLs, to avoid DB2 `SQL0418N` (`untyped parameter marker`); (2) any cell whose value is a `SqlParameter` -- DB2 rejects untyped parameter markers inside VALUES regardless of null status. `Source/LinqToDB/Internal/DataProvider/DB2/DB2SqlBuilderBase.Merge.cs:28`

### Expression conversions
`DB2SqlExpressionConvertVisitor`:
- `%` -> `Mod(Int(x), y)` (integer argument required)
- `&`/`|`/`^` -> `BitAnd`/`BitOr`/`BitXor` functions
- `~` (bitwise negation) -> `BITNOT`
- string `+` -> `||`
- `LENGTH` -> `CHAR_LENGTH`
- Conversions to string -> `RTrim(Char(...))`
- `NULLIF` not supported (`SupportsNullIf = false`)
- `NULL` literal in column not supported (`SupportsNullInColumn = false`); boolean expressions in column position get mandatory `CAST(... AS BOOLEAN)`

### Type mapping notable points
- `bool` -> `smallint` (0/1)
- `Guid` -> `char(16) for bit data` / stored as `byte[16]` via `ToByteArray()`
- `DateTime`/`DateTime2` -> `timestamp`
- `DateTimeOffset` -> `TIMESTAMP` (LUW) / `TIMESTAMP WITH TIME ZONE` (z/OS, via `DB2zOSSqlBuilder.BuildDataTypeFromDataType`)
- `NVarChar` capped at 8168 characters
- `DECIMAL` default scale in `DB2MappingSchema`: precision 18, scale 10
- Timestamp literal format: `'yyyy-MM-dd-HH.mm.ss.ffffff'` (DB2's non-ISO separator)
- z/OS `DateTimeOffset` literal format: `'yyyy-MM-dd-HH.mm.ss.ffffffzzz'` (precision-aware, timezone offset suffix), handled by `DB2zOSMappingSchema.ConvertDateTimeOffsetToSql`. `Source/LinqToDB/Internal/DataProvider/DB2/DB2MappingSchema.cs:279`
- Binary literal: `BX'hexstring'`
- `sbyte`/`byte` parameters promoted to `Int16` in `SetParameter`. `Source/LinqToDB/Internal/DataProvider/DB2/DB2DataProvider.cs:127`

### Member translations
- Date parts: `EXTRACT(part FROM ...)` for Year/Month/Day/Hour/Minute/Second; `To_Number(To_Char(..., 'Q'))` for Quarter; `Microsecond/1000` for Millisecond
- Date add: arithmetic with `n YEAR/MONTH/DAY/HOUR/MINUTE/SECOND`; Millisecond -> `n*1000 MICROSECONDS`
- `string.Join` -> `LISTAGG(value, sep) WITHIN GROUP (ORDER BY ...)` with `CONCAT_WS` emulation
- `Math.Max`/`Math.Min` -> `GREATEST`/`LEAST`
- `Guid.ToString()` -> hex/substr assembly (same logic as SQLite)
- LUW `DateTime.Now`: not translated -- `TranslateNow` returns null. `Source/LinqToDB/Internal/DataProvider/DB2/Translation/DB2MemberTranslator.cs:269`
- LUW/z/OS `DateTime.UtcNow`: `CURRENT TIMESTAMP - CURRENT TIMEZONE`. `Source/LinqToDB/Internal/DataProvider/DB2/Translation/DB2MemberTranslator.cs:274`
- z/OS server now (`DateTimeOffset.Now` / `TranslateServerNow`): `CURRENT TIMESTAMP WITH TIME ZONE` via `DB2zOSMemberTranslator.ZOsDateFunctionsTranslator`. `Source/LinqToDB/Internal/DataProvider/DB2/Translation/DB2zOSMemberTranslator.cs:18`
- `DB2DataProvider.CreateMemberTranslator()` dispatches `DB2zOSMemberTranslator` for z/OS, `DB2MemberTranslator` for LUW. `Source/LinqToDB/Internal/DataProvider/DB2/DB2DataProvider.cs:107`

## Bulk copy

`DB2BulkCopy` extends `BasicBulkCopy`:
- `MaxParameters = 1999` (SQL limits reference), `MaxSqlLength = 327670`
- `ProviderSpecificCopy*` methods call `DB2BulkCopyShared.ProviderSpecificCopyImpl` when a `DataConnection` is available. Note: async paths drain `IAsyncEnumerable` to synchronous before calling `WriteToServer` (IBM's `DB2BulkCopy.WriteToServer` is sync-only). `Source/LinqToDB/Internal/DataProvider/DB2/DB2BulkCopy.cs:73`
- `MultipleRowsCopy` dispatches: z/OS -> `MultipleRowsCopy2` with ` FROM SYSIBM.SYSDUMMY1` suffix; LUW -> `MultipleRowsCopy1`.
- `DB2BulkCopyShared` is intentionally `public` to allow reuse by `linq2db4iSeries`. `Source/LinqToDB/Internal/DataProvider/DB2/DB2BulkCopyShared.cs:13`
- `DB2BulkCopyOptions` flags: `KeepIdentity = 1`, `TableLock = 2`, `Truncate = 4`.

## Schema providers

### LUW -- `DB2LUWSchemaProvider`
- Tables/views from `connection.GetSchema("Tables")` (ADO.NET)
- Columns from `SYSCAT.COLUMNS`; detects `CHARACTER`/`VARCHAR` with codepage 0 as bit-data types
- PKs from `SYSCAT.INDEXES WHERE UNIQUERULE = 'P'`; parses `COLNAMES` by splitting on `+`
- FKs from `SYSCAT.REFERENCES`; column matching by longest-name-first heuristic (handles space-separated column name concatenation)
- Procedures/functions: `SYSCAT.PROCEDURES` UNION `SYSCAT.FUNCTIONS`; supports module objects via `SYSCAT.MODULEOBJECTS`
- Table functions scaffolded as `SELECT * FROM TABLE(name(NULL, ...))`
- `DefaultSchema` determined by `SELECT current_schema FROM sysibm.sysdummy1`

### z/OS -- `DB2zOSSchemaProvider` (extends LUW)
- Overrides `GetDataTypes` with static list (no ADO.NET schema call)
- PKs from `SYSIBM.SYSCOLUMNS JOIN SYSIBM.SYSINDEXES`; uses `KEYSEQ > 0 AND UNIQUERULE = 'P'`
- Columns from `SYSIBM.SYSCOLUMNS`; identity detection via `DEFAULT IN ('I', 'J')`
- FKs from `SYSIBM.SYSRELS JOIN SYSIBM.SYSFOREIGNKEYS`; resolves FK target columns by position in PK list
- Procedures from `SYSIBM.SYSROUTINES`; parameters from `SYSIBM.SYSPARMS`

## Provider flags (shared)

`Source/LinqToDB/Internal/DataProvider/DB2/DB2DataProvider.cs:30`

| Flag | Value |
|---|---|
| `IsSubQueryOrderBySupported` | `false` |
| `IsUnionAllOrderBySupported` | `true` |
| `AcceptsTakeAsParameter` | `false` |
| `AcceptsTakeAsParameterIfSkip` | `true` |
| `IsCommonTableExpressionsSupported` | `true` |
| `IsUpdateFromSupported` | `false` |
| `IsCrossJoinSupported` | `false` |
| `SupportedCorrelatedSubqueriesLevel` | `1` |
| `IsDistinctFromSupported` | `true` |
| `SupportsPredicatesComparison` | `true` |
| `IsRecursiveCTEJoinWithConditionSupported` | `false` |
| `IsUpdateTakeSupported` | LUW only |
| `IsUpdateSkipTakeSupported` | LUW only |
| `RowConstructorSupport` | Equality, Comparisons, Update, UpdateLiteral, Overlaps, Between |

`SupportedTableOptions`: IsTemporary, IsLocalTemporaryStructure, IsGlobalTemporaryStructure, IsLocalTemporaryData, CreateIfNotExists, DropIfExists.

## Files (Tier 1 / Tier 2)

**Tier 1 (11 files)**

| File | Role |
|---|---|
| `Internal/DataProvider/DB2/DB2DataProvider.cs` | Provider base + concrete LUW/zOS sealed types |
| `Internal/DataProvider/DB2/DB2SqlBuilderBase.cs` | SQL emission base |
| `Internal/DataProvider/DB2/DB2SqlOptimizer.cs` | Statement rewrite + parameter wrapping |
| `DataProvider/DB2/DB2Tools.cs` | Public registration entry point |
| `DataProvider/DB2/DB2Version.cs` | Server-family enum |
| `DataProvider/DB2/DB2Options.cs` | Provider options record |
| `DataProvider/DB2/DB2IdentifierQuoteMode.cs` | Identifier quoting enum |
| `Internal/DataProvider/DB2/DB2ProviderAdapter.cs` | ADO.NET dynamic adapter |
| `Internal/DataProvider/DB2/DB2ProviderDetector.cs` | Auto-detection via `eServerType` |
| `Internal/DataProvider/DB2/DB2MappingSchema.cs` | Type mapping + literal converters |
| `Internal/DataProvider/DB2/DB2BulkCopy.cs` | Bulk copy orchestration |

**Tier 2 (11 files)**

| File | Role |
|---|---|
| `Internal/DataProvider/DB2/DB2SqlBuilderBase.Merge.cs` | Typed-NULL enforcement for VALUES tables |
| `Internal/DataProvider/DB2/DB2LUWSqlBuilder.cs` | LUW dialect overrides |
| `Internal/DataProvider/DB2/DB2zOSSqlBuilder.cs` | z/OS dialect overrides + `DateTimeOffset` -> `TIMESTAMP WITH TIME ZONE` |
| `Internal/DataProvider/DB2/DB2SqlExpressionConvertVisitor.cs` | Expression-level SQL conversions |
| `Internal/DataProvider/DB2/Translation/DB2MemberTranslator.cs` | LUW LINQ member -> SQL function translations |
| `Internal/DataProvider/DB2/Translation/DB2zOSMemberTranslator.cs` | z/OS overrides: `CURRENT TIMESTAMP WITH TIME ZONE` for server now (added SHA `4a478ff1`) |
| `Internal/DataProvider/DB2/DB2LUWSchemaProvider.cs` | LUW schema introspection via `SYSCAT.*` |
| `Internal/DataProvider/DB2/DB2zOSSchemaProvider.cs` | z/OS schema introspection via `SYSIBM.SYS*` |
| `Internal/DataProvider/DB2/DB2BulkCopyShared.cs` | Shared `ProviderSpecificCopyImpl` (public for iSeries) |
| `Internal/DataProvider/DB2/DB2Extensions.cs` | `DbDataReader.ToString` with trailing-space trim |
| `DataProvider/DB2/DB2Factory.cs` | Config-file factory |

## Inbound / outbound dependencies

**Inbound:**
- `linq2db4iSeries` external package uses `DB2BulkCopyShared` (public API contract). `Source/LinqToDB/Internal/DataProvider/DB2/DB2BulkCopyShared.cs:13`
- Consumer code enters via `DB2Tools.GetDataProvider` / `DB2Tools.CreateDataConnection`.

**Outbound:**
- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md): `BasicSqlBuilder<DB2Options>`, `BasicSqlOptimizer`, `BasicBulkCopy`
- [INTERNAL-API](../INTERNAL-API/INDEX.md): `DynamicDataProviderBase<DB2ProviderAdapter>`, `SchemaProviderBase`, `ProviderDetectorBase`, `MemberTranslatorBase`, `TypeMapper`, `BulkCopyReader`
- [MAPPING](../MAPPING/INDEX.md): `LockedMappingSchema`, `MappingSchema`
- [METADATA](../METADATA/INDEX.md): `SchemaProviderBase` (via `ISchemaProvider`)

## Known issues / debt

1. **ROW_NUMBER paging disabled** -- `DB2SqlOptimizer.TransformStatement` has commented-out code for LUW 9/10 `ROW_NUMBER`-based pagination (needed when OFFSET is unavailable). Enabling it requires adding version tracking to the DB2 provider (analogous to `DB2Version.LUW_9` etc.). `Source/LinqToDB/Internal/DataProvider/DB2/DB2SqlOptimizer.cs:19`
2. **Async bulk copy is sync under the hood** -- `ProviderSpecificCopyAsync` drains `IAsyncEnumerable` into a synchronous enumerator before calling `WriteToServer`. IBM's `DB2BulkCopy` does not expose an async `WriteToServerAsync`. `Source/LinqToDB/Internal/DataProvider/DB2/DB2BulkCopy.cs:80`
3. **FK column matching in LUW schema provider** -- `GetForeignKeys` resolves column names by string-prefix matching on the space-separated `FK_COLNAMES`/`PK_COLNAMES` from `SYSCAT.REFERENCES`, ordered by longest-name-first. This heuristic can mis-match if column names are substrings of each other.
4. **`BuildParameter` TODO** -- comment in `DB2SqlBuilderBase.BuildParameter` notes it is a copy of Firebird's implementation and a `SqlProviderFlags` refactor would deduplicate it. `Source/LinqToDB/Internal/DataProvider/DB2/DB2SqlBuilderBase.cs:429`
5. **`DB2DateTimeType` optional** -- adapter loads `DB2DateTime` as optional (comment: "not sure if still actual"). `Source/LinqToDB/Internal/DataProvider/DB2/DB2ProviderAdapter.cs:174`
6. **`DB2TimeSpanType` optional obsolete** -- loaded with `obsolete: true`; recent IBM providers include it as an `[Obsolete]` stub. Not mapped in `DB2DataProvider`.

## See also

- [SQL-PROVIDER area](../SQL-PROVIDER/INDEX.md) -- `BasicSqlBuilder`, `BasicSqlOptimizer`
- [INTERNAL-API area](../INTERNAL-API/INDEX.md) -- `DynamicDataProviderBase`, `ProviderDetectorBase`, `TypeMapper`
- [MAPPING area](../MAPPING/INDEX.md) -- `LockedMappingSchema`
- [METADATA area](../METADATA/INDEX.md) -- `SchemaProviderBase`
- PROV-INFORMIX (not yet indexed) -- shares the IBM ODS ADO.NET driver; `DB2ProviderDetector.DetectProvider` guards against Informix config strings

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 11 / 11
- Tier 2 (visited / total): 11 / 11 (100%)
- Tier 3 (skipped, logged): 0

Read (this run -- delta):
- Source/LinqToDB/Internal/DataProvider/DB2/DB2MappingSchema.cs -- z/OS DateTimeOffset format chain
- Source/LinqToDB/Internal/DataProvider/DB2/DB2SqlBuilderBase.Merge.cs -- SqlParameter cell triggers type cast
- Source/LinqToDB/Internal/DataProvider/DB2/DB2zOSSqlBuilder.cs -- DateTimeOffset -> TIMESTAMP WITH TIME ZONE
- Source/LinqToDB/Internal/DataProvider/DB2/Translation/DB2MemberTranslator.cs -- TranslateNow null; UtcNow/ZonedUtcNow emit CURRENT TIMESTAMP - CURRENT TIMEZONE
- Source/LinqToDB/Internal/DataProvider/DB2/Translation/DB2zOSMemberTranslator.cs (NEW) -- ZOsDateFunctionsTranslator emits CURRENT TIMESTAMP WITH TIME ZONE
- Source/LinqToDB/Internal/DataProvider/DB2/DB2DataProvider.cs -- CreateMemberTranslator dispatches DB2zOSMemberTranslator for z/OS

</details>
