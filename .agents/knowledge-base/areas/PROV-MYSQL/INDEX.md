---
area: PROV-MYSQL
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-07-05
last_verified_sha: 36ee4f82f06eaf242b052ade8c87121d251a6165
coverage_tier_1: 11/11
coverage_tier_2: 19/19
---

# PROV-MYSQL

MySQL and MariaDB data provider. Covers two independent axes:
- **Product axis**: MySQL 5.7, MySQL 8.0+, MariaDB 10+
- **Client axis**: MySql.Data (Oracle official connector) and MySqlConnector (open-source replacement)

The two axes compose into six concrete IDataProvider instances. Both axes are fully orthogonal -- every product version works with either client.

## Subsystems

### Provider registration and auto-detection

MySqlTools (Source/LinqToDB/DataProvider/MySql/MySqlTools.cs) is the public entry point. It exposes GetDataProvider(version, provider, connectionString, ...), CreateDataConnection(...), ResolveMySql(path, assemblyName) for assembly-resolver registration, and the AutoDetectProvider flag delegated to MySqlProviderDetector.

MySqlProviderDetector (Source/LinqToDB/Internal/DataProvider/MySql/MySqlProviderDetector.cs:12) extends ProviderDetectorBase<MySqlProvider, MySqlVersion> with DefaultVersion = MySqlVersion.MySql57. Detection logic:

1. DetectProvider(ConnectionOptions) -- resolves which ADO.NET client to use. Checks options.ProviderName against known string constants; falls back to probing via Common.Tools.IsProviderAssemblyPresent(MySqlProviderAdapter.MySqlDataAssemblyName) when the provider is ambiguous.
2. DetectServerVersion(DbConnection, DbTransaction?) -- runs SELECT VERSION(), parses the version string, detects the -MariaDB suffix, returns one of MySql57 / MySql80 / MariaDB10. Version dispatch: major < 8 => MySql57; major >= 10 AND isMariaDB => MariaDB10; _ => MySql80 (note: MariaDB 8.x/9.x would fall through to MySql80 -- only MariaDB with major >= 10 is recognized as MariaDB10). MariaDB < 10 (version 5.x based on MySQL 5.x) maps to MySql57.
3. The ClickHouse-over-MySQL protocol guard: if ProviderName or ConfigurationString contains 'ClickHouse' the detector returns null immediately (MySqlProviderDetector.cs:27).

Six singletons live in MySqlProviderDetector as lazy statics, one per (product, client) combination.

MySqlFactory (Source/LinqToDB/DataProvider/MySql/MySqlFactory.cs) handles XML-config / connection-string providerName attributes and maps version strings ('5.7', '8.0', '10', etc.) to MySqlVersion before delegating to MySqlTools.GetDataProvider.

### ADO.NET adapter layer

MySqlProviderAdapter (Source/LinqToDB/Internal/DataProvider/MySql/MySqlProviderAdapter.cs:19) is the abstract base for both client adapters. It loads the chosen assembly at runtime via reflection and TypeMapper wrappers. Two sealed inner classes:

- MySqlData.MySqlDataProviderAdapter (MySqlProviderAdapter.cs:153) -- loads MySql.Data.dll. Maps MySqlDecimal, MySqlDateTime, MySqlGeometry. BulkCopy = null (no native bulk-copy in MySql.Data). GetDateTimeOffsetMethodName = null (no DateTimeOffset support). IsPackageProceduresSupported = false.
- MySqlConnector.MySqlConnectorProviderAdapter (MySqlProviderAdapter.cs:307) -- loads MySqlConnector.dll. Version-guards: bulk copy available from 0.67, MySqlDecimal from ~2.0, DateOnly from 2.0. GetDateTimeOffsetMethodName = 'GetDateTimeOffset'. IsPackageProceduresSupported = true.

Capability flags on the base class that differ between clients:
- MySqlDecimalType -- null for MySqlConnector < 2.0.
- BulkCopy -- non-null only for MySqlConnector >= 0.67; MySql.Data has no native bulk-copy API.
- IsDateOnlySupported -- true for MySqlConnector >= 2.0.
- GetDateTimeOffsetMethodName -- non-null only for MySqlConnector.

### Product/version matrix

MySqlDataProvider (Source/LinqToDB/Internal/DataProvider/MySql/MySqlDataProvider.cs:28) is the abstract base, extending DynamicDataProviderBase<MySqlProviderAdapter>. It holds Version and Provider properties.

Six sealed subclasses at MySqlDataProvider.cs:20--25 cover all (version, client) combinations:

| Class | ProviderName constant | Version | Client |
|---|---|---|---|
| MySql57DataProviderMySqlData | MySql57MySqlData | MySql57 | MySqlData |
| MySql57DataProviderMySqlConnector | MySql57MySqlConnector | MySql57 | MySqlConnector |
| MySql80DataProviderMySqlData | MySql80MySqlData | MySql80 | MySqlData |
| MySql80DataProviderMySqlConnector | MySql80MySqlConnector | MySql80 | MySqlConnector |
| MariaDB10DataProviderMySqlData | MariaDB10MySqlData | MariaDB10 | MySqlData |
| MariaDB10DataProviderMySqlConnector | MariaDB10MySqlConnector | MariaDB10 | MySqlConnector |

SqlProviderFlags per version (MySqlDataProvider.cs:36--72):
- IsCommonTableExpressionsSupported -- only when version > MySql57
- IsAllSetOperationsSupported / IsDistinctSetOperationsSupported -- only when version > MySql57
- IsApplyJoinSupported / IsCrossApplyJoinSupportsCondition / IsOuterApplyJoinSupportsCondition -- only MySql80 (MariaDB explicitly excluded via comment referencing MDEV-6373/19078)
- IsWindowFunctionsSupported -- version >= MySql80
- SupportedCorrelatedSubqueriesLevel -- null (unlimited) for MySql80 only; 1 for MySql57 and MariaDB10
- MaxColumnCount = 4096 (delta, currentSha 36ee4f82f0): a hard cap on projected column count, applied uniformly across all six subclasses.
- IsInsertOrUpdateWithPredicateSupported = false (delta): MySQL/MariaDB emit InsertOrUpdate as INSERT ... ON DUPLICATE KEY UPDATE, which has no WHERE clause on the UPDATE branch; Upsert.Update.When is instead routed through the alternative UPDATE-then-INSERT emulation (MySqlDataProvider.cs:62--65).
- IsUpsertWithMergeLoweringSupported = false (delta): MySQL/MariaDB have no MERGE statement, so Upsert configurations that would require MERGE lowering (bulk source, non-PK match, Insert.When, SkipInsert/SkipUpdate) surface a descriptive error via Error_Upsert_MergeLowering_NotSupported instead of attempting a MERGE-based rewrite (MySqlDataProvider.cs:67--70).
- DefaultNullsOrdering = NullsDefaultOrdering.Smallest -- MySQL/MariaDB sort NULL as the smallest value (already covered under Member translator's GROUP_CONCAT NULLS emulation below).

CreateSqlBuilder dispatches on Version:
```
MySql57 -> MySql57SqlBuilder
MySql80 -> MySql80SqlBuilder
_ (MariaDB10) -> MariaDBSqlBuilder
```

CreateMemberTranslator dispatches on Version, one subclass per MySqlVersion value (MySqlDataProvider.cs:110--119, delta -- was previously a 2-way switch sharing MySql80MemberTranslator between MySql80 and MariaDB10):
```
MariaDB10 -> MariaDBMemberTranslator
MySql80   -> MySql80MemberTranslator
MySql57   -> MySql57MemberTranslator
_         -> MySqlMemberTranslator
```

Each version now has its own translator subclass rather than sharing one across two versions (version-aware-translators-derive-a-subclass pattern). MariaDBMemberTranslator extends MySql80MemberTranslator, so it still inherits the REGEXP_REPLACE-based TrimStart/TrimEnd (ICU regex in MySQL 8+, PCRE in MariaDB 10+) that the two dialects share, while layering MariaDB-only window-function and UUID_v7() behavior on top (see Member translator subsystem below). MySql57MemberTranslator extends the MySqlMemberTranslator base directly and disables window functions at the translator level.

GetMappingSchema dispatches on (provider, version) producing one of six MySqlMappingSchema subclasses.

### SQL builder hierarchy

```
BasicSqlBuilder<MySqlOptions>
  +-- MySqlSqlBuilder           (abstract base -- all shared SQL emission)
        +-- MySql57SqlBuilder   (MySQL 5.7 quirks)
        +-- MySql80SqlBuilder   (MySQL 8.0+ additions)
        +-- MariaDBSqlBuilder   (MariaDB additions)
```

All three share MySqlSqlBuilder (Source/LinqToDB/Internal/DataProvider/MySql/MySqlSqlBuilder.cs). Key behaviors in the base:

**ConcatStyle**: set to ConcatBuildStyle.Function (MySqlSqlBuilder.cs:33). This routes string.Concat expressions through the SqlConcatExpression function path introduced in PR #5504, rather than the older binary-+ flattening.

**Identifier quoting**: backtick quoting (backtick-escaping of embedded backticks) via Convert (MySqlSqlBuilder.cs:302--338). All identifier convert types (table, field, alias, database, schema, package, CTE, procedure) use backticks.

**Parameter syntax**: @name prefix for both query and command parameters (MySqlSqlBuilder.cs:306--317).

**LIMIT/OFFSET syntax**: MySQL uses LIMIT skip, take not OFFSET skip LIMIT take. Implemented in BuildOffsetLimit (MySqlSqlBuilder.cs:59--75); calls SqlOptimizer.ConvertSkipTake first, then emits LIMIT {skip}, {take}. When skip is null falls back to base LIMIT {take}.

**Upsert**: BuildInsertOrUpdateQuery (MySqlSqlBuilder.cs:366--408) emits INSERT ... ON DUPLICATE KEY UPDATE ... when there are update items. When no update items, converts to INSERT IGNORE by string-patching in the accumulated SQL buffer. See also the new IsInsertOrUpdateWithPredicateSupported / IsUpsertWithMergeLoweringSupported SqlProviderFlags under Product/version matrix above, which route unsupported Upsert shapes to emulation or a descriptive error instead of reaching this builder path.

**Hints infrastructure**: HintBuilder (MySqlSqlBuilder.cs:561) accumulates hint text; StartStatementQueryExtensions injects QB_NAME(queryName) if the select has a query block name; FinalizeBuildQuery wraps accumulated hints in /*+ ... */ and splices them into the statement at _hintPosition. Table hints and index hints (placed after the table alias) are handled in BuildTableExtensions.

**NULL-safe equality**: BuildIsDistinctPredicate emits expr1 <=> expr2 (MySQL null-safe equality operator).

**Type mappings in CAST**: a large dispatch table in BuildDataTypeFromDataType (MySqlSqlBuilder.cs:82--147) maps LinqToDB DataType to MySQL cast-safe type names (SIGNED, UNSIGNED, CHAR(N), BINARY(N), DECIMAL, JSON, etc.). FLOAT is mapped to DOUBLE in CASTs due to MySQL bug #87794.

**For CREATE TABLE**: a second dispatch table (MySqlSqlBuilder.cs:150--233) maps to full MySQL DDL type names including TINYINT UNSIGNED, SMALLINT UNSIGNED, INT UNSIGNED, BIGINT UNSIGNED, TINYBLOB/BLOB/MEDIUMBLOB/LONGBLOB by size, text types by size, and BIT(n) with size derived from the .NET type when no explicit length is given.

**Temporary tables**: CreateTemporaryTable/DropTemporaryTable use CREATE TEMPORARY TABLE / DROP TEMPORARY TABLE syntax; IsTemporaryTable (MySqlSqlBuilder.cs:486) resolves TableOptions flags.

**MERGE**: explicitly throws LinqToDBException -- MySQL has no native MERGE statement (MySqlSqlBuilder.cs:507).

**ROLLUP/CUBE**: emits MySQL-specific GROUP BY ... WITH ROLLUP / WITH CUBE (MySqlSqlBuilder.cs:511--550).

**Object name**: BuildObjectName (MySqlSqlBuilder.cs:429--451) supports database.table but skips schema (MySQL has no schemas -- database IS the schema). Package is supported for stored procedure calls.

**UPDATE quirk**: BuildUpdateClause emits the FROM clause as the UPDATE clause (MySQL multi-table UPDATE syntax uses UPDATE t1, t2 rather than UPDATE t1 FROM t2).

**Identity**: CommandCount returns 2 when NeedsIdentity; second command is SELECT LAST_INSERT_ID() (MySqlSqlBuilder.cs:50).

**Per-subclass overrides**:
- MySql57SqlBuilder: adds FROM DUAL when WHERE is present but no FROM clause (MySQL 5.7 requires FROM in WHERE queries). Overrides FLOAT/DOUBLE casts to use DECIMAL (FLOAT/DOUBLE in CAST added only in MySQL 8.0.17).
- MySql80SqlBuilder: enables SupportsColumnAliasesInSource = true. Implements INNER JOIN LATERAL / LEFT JOIN LATERAL for CrossApply/OuterApply (falls back to INNER JOIN / LEFT JOIN when the joined table is a function). Adds VECTOR(n) type in BuildDataTypeFromDataType.
- MariaDBSqlBuilder: enables SupportsColumnAliasesInSource = true. Adds VECTOR(n) type but requires explicit length (no default size in MariaDB -- falls through to base if Length == null).

### SQL optimizer

MySqlSqlOptimizer (Source/LinqToDB/Internal/DataProvider/MySql/MySqlSqlOptimizer.cs:9) extends BasicSqlOptimizer.

- RequiresCastingNullValueForSetOperations = true
- CreateConvertVisitor returns MySqlSqlExpressionConvertVisitor
- TransformStatement applies two post-base transforms:
  - CorrectMySqlUpdate -- MySQL forbids referencing the UPDATE target table in a subquery within the same statement. Any non-target SqlTable node that refers to the same table is wrapped in a sub-select (new SelectQuery { DoNotRemove = true }). Also calls SqlQueryColumnNestingCorrector if changes were made. SKIP in UPDATE throws LinqToDBException with ErrorHelper.MySql.Error_SkipInUpdate.
  - PrepareDelete -- when the DELETE has a single unjoined table and no SKIP/TAKE, sets Alias = '$' to produce a table alias; MySQL DELETE syntax requires the alias form in multi-table cases.

MySqlSqlExpressionConvertVisitor (Source/LinqToDB/Internal/DataProvider/MySql/MySqlSqlExpressionConvertVisitor.cs):
- ConcatRequiresExplicitStringCast: returns false -- suppresses extra CAST nodes that the base SqlConcatExpression rewrite would otherwise insert around string operands (PR #5504).
- ConvertConversion: suppresses CAST when converting decimal->float/double (avoids precision loss via intermediate cast).
- ConvertSqlBinaryExpression: adjusts | bitwise OR precedence (MySQL gives | lower priority than &); flattens string-concatenation + chains into multi-argument Concat(...) calls.
- ConvertSearchStringPredicate: case-insensitive Contains translates to LOCATE(search, data) > 0; case-sensitive adds COLLATE utf8_bin to the data expression.
- ConvertSqlFunction: maps LENGTH pseudo-function to CHAR_LENGTH.
- WrapColumnExpression: wraps uint, ulong, long, double, decimal values and parameters in mandatory CAST nodes for set operations.

### Type mapping and mapping schemas

MySqlMappingSchema (Source/LinqToDB/Internal/DataProvider/MySql/MySqlMappingSchema.cs:16) is a LockedMappingSchema keyed on ProviderName.MySql. Root-level conversions:
- String SQL literal: single-quote with backslash-quote and backslash-backslash escaping (MySqlMappingSchema.cs:68--73).
- Binary: 0x hex literal.
- float[] (vector): hex-encoding of little-endian IEEE 754 floats.
- BitArray -> DataParameter(DataType.UInt64) -- MySQL BIT fields mapped to ulong.
- byte[] -> float[] FromDatabase conversion for VECTOR columns.
- ReadOnlyMemory<float> -> float[] FromDatabase (net8.0+).
- DateTimeOffset SQL literal: CONVERT_TZ at base; MySql80MappingSchema overrides to TIMESTAMP literal (MySqlMappingSchema.cs:123--137).

Hierarchy of 9 LockedMappingSchema subclasses, each chaining parent:
```
MySqlMappingSchema (base)
  +-- MySql57MappingSchema   -> MySqlData57MappingSchema / MySqlConnector57MappingSchema
  +-- MySql80MappingSchema   -> MySqlData80MappingSchema / MySqlConnector80MappingSchema
  +-- MariaDB10MappingSchema -> MySqlDataMariaDB10MappingSchema / MySqlConnectorMariaDB10MappingSchema
```
Each leaf also chains the adapter own MappingSchema (set during MySqlProviderAdapter construction) which registers provider-type scalar types (MySqlDecimal, MySqlDateTime).

### Bulk copy

MySqlBulkCopy (Source/LinqToDB/Internal/DataProvider/MySql/MySqlBulkCopy.cs:15) extends BasicBulkCopy.

- MaxParameters = 32767 -- MySQL multi-row INSERT limit.
- MaxSqlLength = 327670 -- conservative packet limit.
- **Provider-specific path** (MySqlConnector >= 0.67): ProviderSpecificCopy[Async] creates MySqlProviderAdapter.MySqlConnector.MySqlBulkCopy via Adapter.BulkCopy.Create(connection, transaction), configures column mappings (ordinal -> name), batches using EnumerableHelper.Batch to honor MaxBatchSize, calls WriteToServer / WriteToServerAsync. Supports RowsCopiedCallback via MySqlRowsCopied event (MySqlBulkCopy.cs:154).
- **Fallback** (MySql.Data or no BulkCopy adapter): MultipleRowsCopy1 / MultipleRowsCopy1Async from base.
- GetInsertInto: when ConflictAction.Ignore is set, emits INSERT IGNORE INTO instead of INSERT INTO for multi-row bulk inserts.

MySqlDataProvider.BulkCopy (MySqlDataProvider.cs:231--274) resolves effective BulkCopyType from MySqlOptions.Default when the caller passes BulkCopyType.Default, then delegates to MySqlBulkCopy.

### Schema provider

MySqlSchemaProvider (Source/LinqToDB/Internal/DataProvider/MySql/MySqlSchemaProvider.cs:15) extends SchemaProviderBase. Queries INFORMATION_SCHEMA directly (not GetSchema() API) due to known bugs in both connectors. Key overrides:

- GetProcedureSchemaExecutesProcedure = true -- MySql executes the procedure to retrieve result schema.
- GetTables: queries INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE(). The TableID is lowercase catalog + .. + name to compensate for case-folding differences in FK queries (MySqlSchemaProvider.cs:65).
- GetColumns: reads EXTRA column to detect auto_increment (IsIdentity) and VIRTUAL STORED/VIRTUAL GENERATED (SkipOnInsert/SkipOnUpdate). VECTOR column length is divided by 4 (bytes per float) (MySqlSchemaProvider.cs:139).
- GetProcedureParameters: avoids GetSchema('PROCEDURE PARAMETERS') which returns incorrect results in both connectors.
- GetProcedureSchema / GetProcedureResultColumns: filters out the fake @_cnet_param_ column rows that MySql.Data and MySqlConnector inject for output-parameter-only procedures (MySqlSchemaProvider.cs:372--373).
- GetDataType: tinyint(1) / tinyint with size 1 -> bool; bool -> bool; unsigned variants -> byte/ushort/uint/ulong; geometry types -> byte[]; vector -> float[].
- ForeignKeyColumnComparison: returns OrdinalIgnoreCase when the column name is all-lowercase (MySQL lowercases FK schema names in some versions).

### Member translator

MySqlMemberTranslator (Source/LinqToDB/Internal/DataProvider/MySql/Translation/MySqlMemberTranslator.cs:16) extends ProviderMemberTranslatorDefault. Shared base for every MySQL/MariaDB translator -- MySql57MemberTranslator, MySql80MemberTranslator (transitively MariaDBMemberTranslator) all derive from it. Overrides:

- DateFunctionsTranslator: date-part extraction uses EXTRACT(part FROM expr) or dedicated functions (DayOfYear, WeekDay). DateAdd uses DATE_ADD(expr, INTERVAL n unit). MakeDateTime builds STR_TO_DATE(...) from zero-padded string parts (MySqlMemberTranslator.cs:211). Milliseconds use MICROSECOND() DIV 1000.

  **Date/time 'now' variants** (PR #5467): four overrides in DateFunctionsTranslator cover all server-time access paths:
  - TranslateServerNow -- emits CURRENT_TIMESTAMP as a raw NotNullExpression (not a function call) returning DateTime (MySqlMemberTranslator.cs:224--228). Maps DateTime.Now to server-side local time.
  - TranslateNow -- returns null, deferring to the base which falls through to TranslateServerNow (MySqlMemberTranslator.cs:230--233).
  - TranslateUtcNow -- emits UTC_TIMESTAMP() returning DateTime (MySqlMemberTranslator.cs:235--240). Maps DateTime.UtcNow.
  - TranslateZonedUtcNow -- emits UTC_TIMESTAMP() returning the caller-supplied dbDataType (MySqlMemberTranslator.cs:242--246). Maps DateTimeOffset.UtcNow.

  **DateTime.Date truncation** (PR #5517): TranslateDateTimeTruncationToDate emits Date(expr), preserving the column original DbDataType (MySqlMemberTranslator.cs:216--221). This fixes incorrect truncation-cast behavior where the column DbType was erroneously carried into the cast result.

- MySqlStringMemberTranslator: String.Join -> GROUP_CONCAT(value SEPARATOR sep ORDER BY ... ) with ORDER BY, DISTINCT, and null/empty string handling (PR #5504: withoutSeparator path corrected -- uses HasSequenceIndex(0) and supplies factory.Value(valueType, string.Empty) as separator; SEPARATOR clause built via factory.Fragment). **NULLS ordering emulation in ORDER BY**: since MySQL/MariaDB sort NULL as smallest, GROUP_CONCAT ... ORDER BY uses sentinel boolean sort keys to emulate NULLS LAST / NULLS FIRST when the requested position does not match the natural order: (expr IS NULL) for NULLS LAST (null=1 sorts last), (expr IS NOT NULL) for NULLS FIRST (null=0 sorts first). Uses QueryHelper.MatchesNaturalNullsPosition to skip the sentinel when the natural order already satisfies the request (MySqlMemberTranslator.cs:322--328); the natural-order check now reads translationContext.ProviderFlags.DefaultNullsOrdering directly (delta -- was previously the hardcoded literal NullsDefaultOrdering.Smallest, same value today but no longer duplicated). TranslateTrimStart/TranslateTrimEnd return null when trimChars != null -- MySQL 5.7 has no regex replace and TRIM(LEADING ... FROM ...) is substring-not-charset semantics (PR #5515); MySql80MemberTranslator (below) overrides this via REGEXP_REPLACE.
- GuidMemberTranslator: Guid.ToString() -> LOWER(CAST(guid AS CHAR(36))).
- TranslateNewGuidMethod: -> Uuid() (non-pure function, side-effects tracked).
- SqlTypesTranslation: maps Sql.SqlTypes.Float/Real to DECIMAL(29,10) (because MySQL FLOAT is unsuitable for type functions), Bit -> BOOLEAN, TinyInt -> INT16.
- MySqlWindowFunctionsMemberTranslator (delta, new nested class, MySqlMemberTranslator.cs:417--431): extends WindowFunctionsMemberTranslator, wired via CreateWindowFunctionsMemberTranslator (MySqlMemberTranslator.cs:433--436). Disables IsFrameGroupsSupported, IsFrameExclusionSupported, IsPercentileContSupported, IsPercentileDiscSupported. Enables IsVarianceSupported and IsVarianceBareSupported, renaming the bare form to the documented sample-statistic function names StdDevFunctionName = STDDEV_SAMP / VarianceFunctionName = VAR_SAMP -- MySQL/MariaDB document bare STDDEV/VARIANCE as population synonyms for STDDEV_POP/VAR_POP, but Sql.Window.StdDev/Variance are sample statistics, so the translator maps to the sample-named function instead of reusing the bare (population) one. COVAR/CORR/REGR remain unsupported. This is the base window-function behavior; MySql57MemberTranslator and MariaDBMemberTranslator narrow/extend it below.

MySql57MemberTranslator (Source/LinqToDB/Internal/DataProvider/MySql/Translation/MySql57MemberTranslator.cs:5, delta -- new file) extends MySqlMemberTranslator directly. Its nested MySql57WindowFunctionsMemberTranslator (extends MySqlWindowFunctionsMemberTranslator) overrides IsWindowFunctionsSupported => false (MySql57MemberTranslator.cs:7--11) -- MySQL 5.7 has no window functions at all. This gates window-function translation at the member-translator level, independently of (and in addition to) the pre-existing SqlProviderFlags.IsWindowFunctionsSupported = false SQL-generation-level gate set in MySqlDataProvider's constructor for MySql57.

MySql80MemberTranslator (Source/LinqToDB/Internal/DataProvider/MySql/Translation/MySql80MemberTranslator.cs) extends MySqlMemberTranslator. Used for MySQL 8.0 (MariaDB 10+ now uses the dedicated MariaDBMemberTranslator subclass below, which derives from MySql80MemberTranslator rather than sharing the instance -- delta). Sole override: CreateStringMemberTranslator returns MySql80StringMemberTranslator.

MySql80StringMemberTranslator extends MySqlStringMemberTranslator:
- TranslateTrimStart(... trimChars != null) -- builds REGEXP_REPLACE(value, '^[chars]+', '') where chars is a character-class pattern with regex metacharacter escaping for backslash, close-bracket, caret, hyphen, open-bracket. Falls back to base (whitespace TRIM) when trimChars == null.
- TranslateTrimEnd(... trimChars != null) -- builds REGEXP_REPLACE(value, '[chars]+$', '').
- Both use the (?-i) inline flag prefix to force case-sensitive character matching regardless of column collation -- matching .NET semantics where TrimStart('a') removes only lowercase 'a', not 'A' (MySql80MemberTranslator.cs:59--61).

MariaDBMemberTranslator (Source/LinqToDB/Internal/DataProvider/MySql/Translation/MariaDBMemberTranslator.cs:12, delta -- new file) extends MySql80MemberTranslator -- inherits the REGEXP_REPLACE-based TrimStart/TrimEnd via MySql80StringMemberTranslator. Two additions:
- MariaDBWindowFunctionsMemberTranslator (nested, extends MySqlWindowFunctionsMemberTranslator): IsOrderedSetWindowedSupported => true and IsMedianSupported => true -- MariaDB 10.3.3+ supports windowed PERCENTILE_CONT/PERCENTILE_DISC (OVER required) and MEDIAN as window functions, unlike MySQL; the group-aggregate (no-OVER) percentile form stays unsupported. IsLeadLagDefaultSupported => false -- MariaDB's LEAD/LAG accept value + offset only and reject the 3rd default-value argument.
- TranslateNewGuid7Method override emits UUID_v7() (MariaDBMemberTranslator.cs:29--33), unconditionally for every MariaDB dialect. A code comment (MariaDBMemberTranslator.cs:9--11) acknowledges linq2db does not version-split MariaDB, so this is emitted even though UUID_v7() requires MariaDB 11.7+ -- see Known issues / debt.

### Public API surface

**MySqlVersion** (Source/LinqToDB/DataProvider/MySql/MySqlVersion.cs):
- AutoDetect (0), MySql57, MySql80, MariaDB10

**MySqlProvider** (Source/LinqToDB/DataProvider/MySql/MySqlProvider.cs):
- AutoDetect (0), MySqlData, MySqlConnector

**MySqlOptions** (Source/LinqToDB/DataProvider/MySql/MySqlOptions.cs): sealed record with single parameter BulkCopyType (default MultipleRows). Extends DataProviderOptions<MySqlOptions>. Used by MySqlDataProvider to resolve effective bulk copy mode.

**MySqlHints** (Source/LinqToDB/DataProvider/MySql/MySqlHints.cs + MySqlHints.generated.cs): static partial class. Exposes:
- MySqlHints.Table.* -- optimizer hint constants (join-order, table-level, index-level) and classic index hints (USE INDEX, IGNORE INDEX, FORCE INDEX + per-join/group-by variants).
- MySqlHints.Query.* -- query-level optimizer hint constants including SET_VAR, RESOURCE_GROUP, MaxExecutionTime(int).
- MySqlHints.SubQuery.* -- row-lock hints: FOR UPDATE, FOR SHARE, LOCK IN SHARE MODE, NOWAIT, SKIP LOCKED.
- Extension methods: TableHint, TablesInScopeHint, TableIndexHint, QueryHint, SubQueryHint, SubQueryTableHint, QueryBlockHint. Scope routing to Sql.QueryExtensionScope.* happens via [Sql.QueryExtension] attributes targeting ProviderName.MySql.
- SubQueryTableHintExtensionBuilder (MySqlHints.cs:628) -- implements the FOR SHARE MariaDB suppression: when the builder detects ProviderName.MariaDB10 in the mapping schema configuration list, it emits '-- ' (comment-out) before the hint. This makes FOR SHARE a no-op on MariaDB.
- Generated file adds strongly-typed per-hint methods (e.g. JoinFixedOrderHint, BkaHint, UseIndexHint, etc.) as three-way overloads (table, in-scope, query-block) per optimizer hint constant.

**MySqlExtensions** (Source/LinqToDB/DataProvider/MySql/MySqlExtensions.cs): full-text search via Match(ext, search, columns) and MatchRelevance(ext, modifier, search, columns) mapping to MATCH({columns, ', '}) AGAINST ({search}{modifier?}). Three modifiers: NaturalLanguage (default, no suffix), Boolean -> IN BOOLEAN MODE, WithQueryExpansion -> WITH QUERY EXPANSION.

**MySqlSpecificExtensions**: AsMySql<T>() wrapping ITable<T> / IQueryable<T> to IMySqlSpecificTable<T> / IMySqlSpecificQueryable<T>.

**IMySqlSpecificTable<T>, IMySqlSpecificQueryable<T>, IMySqlExtensions**: marker interfaces enabling MySQL-specific extension method dispatch.

### Parameter and type mapping quirks

MySqlDataProvider.SetParameter (MySqlDataProvider.cs:164--192):
- float[] (vector) on MySql.Data: converts to byte[] via Buffer.BlockCopy because MySql.Data does not accept float[] directly.
- MySqlDecimal parameter: unwraps to string and changes DataType to VarChar -- MySql.Data crashes on DataType.Decimal with large decimals.
- DateOnly without IsDateOnlySupported: converts to DateTime.

SetParameterType (MySqlDataProvider.cs:194--227):
- VarNumeric -> DbType.Decimal (MySql.Data trims fractional part otherwise).
- Date / DateTime2 -> DbType.DateTime (MySql.Data trims time part otherwise).
- BitArray -> DbType.UInt64.
- Vector32 -> provider-specific MySqlDbType.Vector enum value (different enum values for each connector: 242 for both but different enum types).

## Key types

| Type | File | Role |
|---|---|---|
| MySqlDataProvider | Internal/DataProvider/MySql/MySqlDataProvider.cs | Abstract base provider; 6 sealed subclasses |
| MySqlSqlBuilder | Internal/DataProvider/MySql/MySqlSqlBuilder.cs | Abstract SQL emitter; all shared MySQL SQL |
| MySql57SqlBuilder | Internal/DataProvider/MySql/MySql57SqlBuilder.cs | MySQL 5.7 overrides |
| MySql80SqlBuilder | Internal/DataProvider/MySql/MySql80SqlBuilder.cs | MySQL 8.0 overrides (LATERAL, VECTOR) |
| MariaDBSqlBuilder | Internal/DataProvider/MySql/MariaDBSqlBuilder.cs | MariaDB 10 overrides (VECTOR) |
| MySqlSqlOptimizer | Internal/DataProvider/MySql/MySqlSqlOptimizer.cs | Statement rewriting (UPDATE/DELETE fixups) |
| MySqlSqlExpressionConvertVisitor | Internal/DataProvider/MySql/MySqlSqlExpressionConvertVisitor.cs | Expression-level rewrites |
| MySqlProviderAdapter | Internal/DataProvider/MySql/MySqlProviderAdapter.cs | Runtime ADO.NET bridge (both clients) |
| MySqlProviderDetector | Internal/DataProvider/MySql/MySqlProviderDetector.cs | Auto-detect logic |
| MySqlMappingSchema | Internal/DataProvider/MySql/MySqlMappingSchema.cs | Type mapping (9 subclasses) |
| MySqlBulkCopy | Internal/DataProvider/MySql/MySqlBulkCopy.cs | Bulk copy strategy |
| MySqlSchemaProvider | Internal/DataProvider/MySql/MySqlSchemaProvider.cs | INFORMATION_SCHEMA queries |
| MySqlMemberTranslator | Internal/DataProvider/MySql/Translation/MySqlMemberTranslator.cs | LINQ member -> SQL function (shared base; hosts MySqlWindowFunctionsMemberTranslator) |
| MySql57MemberTranslator | Internal/DataProvider/MySql/Translation/MySql57MemberTranslator.cs | LINQ member -> SQL function (MySQL 5.7); disables window functions at translator level (delta) |
| MySql80MemberTranslator | Internal/DataProvider/MySql/Translation/MySql80MemberTranslator.cs | LINQ member -> SQL function (MySQL 8.0); adds REGEXP_REPLACE TrimStart/TrimEnd |
| MariaDBMemberTranslator | Internal/DataProvider/MySql/Translation/MariaDBMemberTranslator.cs | LINQ member -> SQL function (MariaDB 10+); extends MySql80MemberTranslator, adds windowed percentile/median + UUID_v7() (delta) |
| MySqlTools | DataProvider/MySql/MySqlTools.cs | Public factory / registration |
| MySqlVersion | DataProvider/MySql/MySqlVersion.cs | Product/version enum |
| MySqlProvider | DataProvider/MySql/MySqlProvider.cs | ADO.NET client enum |
| MySqlOptions | DataProvider/MySql/MySqlOptions.cs | Provider options (bulk copy type) |
| MySqlHints | DataProvider/MySql/MySqlHints.cs + .generated.cs | Hint constants + extension methods |
| MySqlExtensions | DataProvider/MySql/MySqlExtensions.cs | Full-text search (MATCH...AGAINST) |

## Files (Tier 1 / Tier 2)

### Tier 1 (11 files, all visited)

| File | Purpose |
|---|---|
| Internal/DataProvider/MySql/MySqlDataProvider.cs | Core provider base + 6 sealed subclasses |
| Internal/DataProvider/MySql/MySqlSqlBuilder.cs | Version-agnostic SQL emitter |
| Internal/DataProvider/MySql/MySqlSqlOptimizer.cs | Statement-level rewrites |
| DataProvider/MySql/MySqlTools.cs | Public registration entry point |
| DataProvider/MySql/MySqlVersion.cs | Product/version enum |
| DataProvider/MySql/MySqlProvider.cs | ADO.NET client enum |
| DataProvider/MySql/MySqlOptions.cs | Provider options |
| Internal/DataProvider/MySql/MySqlProviderAdapter.cs | ADO.NET adapter (both clients) |
| Internal/DataProvider/MySql/MySqlProviderDetector.cs | Auto-detect logic |
| Internal/DataProvider/MySql/MySqlMappingSchema.cs | Type mapping (9 subclasses) |
| Internal/DataProvider/MySql/MySqlBulkCopy.cs | Bulk copy strategy |

### Tier 2 (19 files, all visited)

| File | Purpose |
|---|---|
| Internal/DataProvider/MySql/MySql57SqlBuilder.cs | MySQL 5.7 builder (FROM DUAL, FLOAT cast) |
| Internal/DataProvider/MySql/MySql80SqlBuilder.cs | MySQL 8.0 builder (LATERAL, VECTOR) |
| Internal/DataProvider/MySql/MariaDBSqlBuilder.cs | MariaDB builder (VECTOR) |
| Internal/DataProvider/MySql/MySqlSchemaProvider.cs | Schema discovery via INFORMATION_SCHEMA |
| Internal/DataProvider/MySql/MySqlSqlExpressionConvertVisitor.cs | Expression rewrites |
| Internal/DataProvider/MySql/Translation/MySqlMemberTranslator.cs | Member -> SQL function (shared base for all MySQL/MariaDB translators) |
| Internal/DataProvider/MySql/Translation/MySql57MemberTranslator.cs | Member -> SQL function (MySQL 5.7); disables window functions (new file, delta) |
| Internal/DataProvider/MySql/Translation/MySql80MemberTranslator.cs | Member -> SQL function (MySQL 8.0) -- added PR #5515 |
| Internal/DataProvider/MySql/Translation/MariaDBMemberTranslator.cs | Member -> SQL function (MariaDB 10+); extends MySql80MemberTranslator, adds UUID_v7 + windowed percentile/median (new file, delta) |
| Internal/DataProvider/MySql/MySqlSpecificQueryable.cs | Wrapped queryable (internal impl) |
| Internal/DataProvider/MySql/MySqlSpecificTable.cs | Wrapped table (internal impl) |
| DataProvider/MySql/MySqlHints.cs | Hint constants + extension methods |
| DataProvider/MySql/MySqlHints.generated.cs | Generated per-hint typed methods |
| DataProvider/MySql/MySqlExtensions.cs | Full-text search extensions |
| DataProvider/MySql/MySqlSpecificExtensions.cs | AsMySql() adapter |
| DataProvider/MySql/IMySqlExtensions.cs | Marker interface |
| DataProvider/MySql/IMySqlSpecificQueryable.cs | Marker interface |
| DataProvider/MySql/IMySqlSpecificTable.cs | Marker interface |
| DataProvider/MySql/MySqlFactory.cs | XML-config factory |

### Tier 3

0 files (no generated/obj files under these paths).

## Known issues / debt

1. **FOR SHARE on MariaDB silently commented-out**: SubQueryTableHintExtensionBuilder detects MariaDB by checking the mapping schema configuration list and emits '-- ' before the hint text (MySqlHints.cs:635). This is a runtime behavior difference that is invisible to callers.

2. **MySql.Data decimal crash workaround**: SetParameter converts MySqlDecimal values to string and changes DataType to VarChar to avoid a crash in MySql.Data 8.x when large decimal values are passed as DataType.Decimal (MySqlDataProvider.cs:175--181). The comment links to the connector source: MySQL.Data/src/Types/MySqlDecimal.cs#L103.

3. **float[] parameter requires byte[] for MySql.Data**: vector parameters require Buffer.BlockCopy to produce byte[] for MySql.Data, silently converting in SetParameter (MySqlDataProvider.cs:167--172). MySqlConnector accepts float[] directly.

4. **No MERGE support**: BuildMergeStatement throws unconditionally (MySqlSqlBuilder.cs:507). MySQL has no MERGE statement. Related: IsUpsertWithMergeLoweringSupported = false (delta) surfaces a descriptive error (Error_Upsert_MergeLowering_NotSupported) earlier in the pipeline for Upsert shapes that would otherwise need MERGE lowering, rather than reaching this throw.

5. **MySqlConnector MySqlDecimal gated on assembly version**: GetMySqlDecimalMethodName is null for MySqlConnector < 2.0, so decimal precision read from the data reader silently falls back to double. The check is based on assembly version >= 2.0, not >= 2.1.0 (see comment in adapter, MySqlProviderAdapter.cs:302).

6. **RETURNING not supported** (MySQL): the OUTPUT / RETURNING clause is not mapped for MySQL/MySql80. MariaDB supports RETURNING but this is not surfaced in the builder hierarchy either -- BuildOutputSubclause in MySqlSqlBuilder.BuildInsertQuery calls the base without MariaDB-specific override.

7. **Correlated subquery depth**: SupportedCorrelatedSubqueriesLevel = 1 for MySql57 and MariaDB10 (unlimited only for MySql80) -- subqueries with multiple levels of parent reference are rewritten aggressively, which can degrade query readability.

8. **TrimStart/TrimEnd with chars falls back to client-side on MySQL 5.7**: when trimChars != null, MySqlStringMemberTranslator returns null -- the translation is not attempted and the operation executes client-side. MySQL 8.0+ and MariaDB 10+ use REGEXP_REPLACE via MySql80MemberTranslator (PR #5515).

9. **MariaDB 8.x/9.x version detection gap**: DetectServerVersion maps major >= 10 AND isMariaDB to MariaDB10, but MariaDB versions 8.x or 9.x (if they ever existed/exist) would fall through to MySql80 dialect instead of MariaDB10 (MySqlProviderDetector.cs:135--140). In practice MariaDB went 10.x after 5.x, so this is a theoretical risk for future MariaDB major-version changes.

10. **MariaDB UUID_v7() emitted without a version gate (delta)**: MariaDBMemberTranslator.TranslateNewGuid7Method unconditionally emits UUID_v7() for the single MariaDB10 dialect bucket, but the function requires MariaDB 11.7+ server-side (MariaDBMemberTranslator.cs:9--11, 29--33). linq2db does not version-split MariaDB beyond the one MariaDB10 enum value, so there is no capability flag to gate this -- calling the new-Guid-v7 translation against an older MariaDB 10.x server produces a runtime SQL error from the server, not a translation-time rejection. Same category of gap as item 9 (MariaDB's single version bucket hides sub-version capability differences).

## Inbound / outbound dependencies

### Inbound (consumers of this area)
- [TESTS-LINQ](../TESTS-LINQ/INDEX.md) -- per-provider MySQL and MariaDB test fixtures.

### Outbound (dependencies of this area)
- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) -- BasicSqlBuilder, BasicSqlOptimizer, SqlExpressionConvertVisitor, ISqlBuilder, ISqlOptimizer.
- [INTERNAL-API](../INTERNAL-API/INDEX.md) -- DynamicDataProviderBase, ProviderDetectorBase, BasicBulkCopy, BulkCopyReader, TypeMapper, SchemaProviderBase, MemberTranslatorBase (via ProviderMemberTranslatorDefault), WindowFunctionsMemberTranslator.
- [MAPPING](../MAPPING/INDEX.md) -- LockedMappingSchema, MappingSchema.
- [SQL-AST](../SQL-AST/INDEX.md) -- all SqlStatement, SelectQuery, SqlTable, SqlField, etc. node types consumed by builders and optimizer.

## See also

- [SQL-PROVIDER/INDEX.md](../SQL-PROVIDER/INDEX.md) -- BasicSqlBuilder base class documentation.
- [INTERNAL-API/INDEX.md](../INTERNAL-API/INDEX.md) -- DynamicDataProviderBase, ProviderDetectorBase, BasicBulkCopy, TypeMapper.
- [MAPPING/INDEX.md](../MAPPING/INDEX.md) -- LockedMappingSchema, mapping schema chain model.
- [PROV-SQLSERVER/INDEX.md](../PROV-SQLSERVER/INDEX.md) -- first PROV-* area; structural reference for pattern comparison.
- [PROV-POSTGRES/INDEX.md](../PROV-POSTGRES/INDEX.md) -- second PROV-* area; RETURNING, native bulk copy comparison.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 11 / 11
- Tier 2 (visited / total): 19 / 19 (100%)
- Tier 3 (skipped, logged): 0

Delta run 2026-05-11 (SHA 4a478ff1): re-read MySqlDataProvider.cs, MySqlMappingSchema.cs, MySqlMemberTranslator.cs. No structural changes in MySqlDataProvider.cs or MySqlMappingSchema.cs relative to prior coverage. MySqlMemberTranslator.cs: added TranslateDateTimeTruncationToDate (Date() function, PR #5517), TranslateServerNow (CURRENT_TIMESTAMP raw expression), TranslateNow (returns null), TranslateUtcNow (UTC_TIMESTAMP()), TranslateZonedUtcNow (UTC_TIMESTAMP() with caller dbDataType) -- all in DateFunctionsTranslator.

Read (this run -- delta):
- MySqlDataProvider.cs (SHA b3340aa9): no structural changes. CreateMemberTranslator, BulkCopy overloads, SetParameter, SetParameterType all confirmed unchanged vs prior coverage.
- MySqlOptions.cs (SHA b3340aa9): no structural changes. Single BulkCopyType parameter, CreateID returns builder unchanged, Equals/GetHashCode via ConfigurationID unchanged.
- MySqlProviderDetector.cs (SHA b3340aa9): clarified DetectServerVersion version-switch: major < 8 => MySql57, major >= 10 AND isMariaDB => MariaDB10, _ => MySql80. This means a hypothetical MariaDB 8.x/9.x would be misidentified as MySql80 (recorded as Known issue #9). ClickHouse guard confirmed at line 26. DetectProvider(options, provider) uses Common.Tools.IsProviderAssemblyPresent for disk probe (not direct DLL path search).
- Translation/MySqlMemberTranslator.cs (SHA b3340aa9): TranslateStringJoin ORDER BY block now emits NULLS ordering emulation via boolean sentinel sort keys: (expr IS NULL) for NULLS LAST, (expr IS NOT NULL) for NULLS FIRST. Uses QueryHelper.MatchesNaturalNullsPosition(NullsDefaultOrdering.Smallest, nulls, desc) to skip sentinel when natural order satisfies the request (lines 322--328). Added to MySqlStringMemberTranslator section under Member translator subsystem.

Delta run 2026-07-05 (SHA 36ee4f82f0): CreateMemberTranslator's dispatch went from a 2-way switch (MySql80MemberTranslator shared by MySql80 and MariaDB10 / MySqlMemberTranslator for MySql57) to a 4-way switch, one subclass per MySqlVersion. Two new Tier-2 files added to coverage: MySql57MemberTranslator.cs and MariaDBMemberTranslator.cs (both new since the last delta -- Tier-2 total 17/17 -> 19/19). MySqlMemberTranslator.cs gained a nested MySqlWindowFunctionsMemberTranslator (window-function capability gating + STDDEV_SAMP/VAR_SAMP naming) not present in the last-recorded coverage. MySqlDataProvider.cs also gained MaxColumnCount = 4096 and two new Upsert-routing SqlProviderFlags (IsInsertOrUpdateWithPredicateSupported, IsUpsertWithMergeLoweringSupported) alongside the translator-dispatch change -- confirmed via a git diff against the prior last_verified_sha (b3340aa9d) rather than inferred from body text alone, since these flags were not previously documented. Body updated in place (per delta procedure step 5) where the prior claim that MySQL 8.0 and MariaDB 10+ share MySql80MemberTranslator is now false -- MariaDBMemberTranslator is a distinct subclass (still deriving from MySql80MemberTranslator, so the REGEXP_REPLACE behavior is still inherited). See AUDIT-NOTE for this contradiction.

Read (this run -- delta):
- Internal/DataProvider/MySql/MySqlDataProvider.cs (SHA 36ee4f82f0): CreateMemberTranslator dispatch changed to 4-way (MariaDB10 -> MariaDBMemberTranslator, MySql80 -> MySql80MemberTranslator, MySql57 -> MySql57MemberTranslator, default -> MySqlMemberTranslator) at lines 110--119. Added SqlProviderFlags.MaxColumnCount = 4096 (line 60), SqlProviderFlags.IsInsertOrUpdateWithPredicateSupported = false (line 65) and SqlProviderFlags.IsUpsertWithMergeLoweringSupported = false (line 70), both with explanatory comments about Upsert routing given MySQL/MariaDB's lack of MERGE and of a WHERE clause on ON DUPLICATE KEY UPDATE. No other structural changes -- SqlProviderFlags block otherwise unchanged, CreateSqlBuilder, GetMappingSchema, SetParameter/SetParameterType, BulkCopy overloads all unchanged vs prior coverage (line numbers shifted by the insertion but logic identical).
- Internal/DataProvider/MySql/Translation/MySqlMemberTranslator.cs (SHA 36ee4f82f0): added nested MySqlWindowFunctionsMemberTranslator : WindowFunctionsMemberTranslator (lines 417--431) and CreateWindowFunctionsMemberTranslator override (lines 433--436). Disables frame-groups, frame-exclusion, PERCENTILE_CONT/DISC; enables variance support with bare STDDEV/VARIANCE renamed to STDDEV_SAMP/VAR_SAMP. Also: the GROUP_CONCAT NULLS-emulation natural-order check (TranslateStringJoin) now reads translationContext.ProviderFlags.DefaultNullsOrdering instead of the hardcoded literal NullsDefaultOrdering.Smallest (line 323) -- same effective value, no behavior change, removes a duplicated literal. TranslateNewGuidMethod trivially refactored (inlined return, no semantic change). All previously-documented members (DateFunctionsTranslator, MySqlStringMemberTranslator's other behaviors, GuidMemberTranslator, SqlTypesTranslation) unchanged.
- Internal/DataProvider/MySql/Translation/MySql57MemberTranslator.cs (SHA 36ee4f82f0, new file -- first coverage): extends MySqlMemberTranslator directly. Nested MySql57WindowFunctionsMemberTranslator overrides IsWindowFunctionsSupported => false (lines 7--11), gating window-function translation at the member-translator level for MySQL 5.7, in addition to the pre-existing SqlProviderFlags-level gate.
- Internal/DataProvider/MySql/Translation/MariaDBMemberTranslator.cs (SHA 36ee4f82f0, new file -- first coverage): extends MySql80MemberTranslator (not MySqlMemberTranslator directly), so it inherits REGEXP_REPLACE TrimStart/TrimEnd. Nested MariaDBWindowFunctionsMemberTranslator adds IsOrderedSetWindowedSupported and IsMedianSupported (MariaDB 10.3.3+ windowed PERCENTILE_CONT/PERCENTILE_DISC/MEDIAN) and disables IsLeadLagDefaultSupported (LEAD/LAG reject the 3rd default-value arg on MariaDB). Overrides TranslateNewGuid7Method to emit UUID_v7() (lines 29--33) -- unconditional per-dialect, no version gate despite the function requiring MariaDB 11.7+ (comment at lines 9--11 acknowledges linq2db does not version-split MariaDB) -- recorded as Known issue #10.
- Internal/DataProvider/MySql/Translation/MySql80MemberTranslator.cs: not in changedFiles; confirmed unchanged via git diff --stat against prior SHA (no entry) -- description in Key types / Member translator sections updated only to drop the now-incorrect used-for-MariaDB-10-too framing, not because the file itself changed.

</details>
