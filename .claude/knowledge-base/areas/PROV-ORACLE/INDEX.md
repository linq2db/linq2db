---
area: PROV-ORACLE
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
coverage_tier_1: 12/12
coverage_tier_2: 20/20
---

# PROV-ORACLE

Oracle Database provider for linq2db. Covers two SQL dialect versions (v11 and v12+), three ADO.NET clients (Native, Managed, Devart), and Oracle-specific SQL features including sequence-based identity, PL/SQL block emission, native MERGE, `ROWNUM`/`OFFSET...FETCH` pagination, XML tables, and four bulk-insert strategies.

## Subsystems

### Provider matrix

Six concrete `OracleDataProvider` subclasses are declared at the top of `OracleDataProvider.cs:21-27` and registered as singletons in `OracleProviderDetector.cs:15-26`:

| Concrete type | Client | Dialect |
|---|---|---|
| `OracleDataProviderNative11` | `Oracle.DataAccess` (ODP.NET) | v11 |
| `OracleDataProviderNative12` | `Oracle.DataAccess` (ODP.NET) | v12+ |
| `OracleDataProviderManaged11` | `Oracle.ManagedDataAccess[.Core]` | v11 |
| `OracleDataProviderManaged12` | `Oracle.ManagedDataAccess[.Core]` | v12+ |
| `OracleDataProviderDevart11` | `Devart.Data.Oracle` | v11 |
| `OracleDataProviderDevart12` | `Devart.Data.Oracle` | v12+ |

All extend `OracleDataProvider` which extends [`DynamicDataProviderBase<OracleProviderAdapter>`](../INTERNAL-API/INDEX.md). Provider and version are exposed as `OracleDataProvider.Provider` and `.Version` (`OracleDataProvider.cs:97-98`).

### ADO.NET adapter (`OracleProviderAdapter`)

`OracleProviderAdapter` implements [`IDynamicProviderAdapter`](../INTERNAL-API/INDEX.md) and is loaded once per client type (double-checked lock singletons at `OracleProviderAdapter.cs:28-33`). It wraps:

- **Oracle.DataAccess / Oracle.ManagedDataAccess** -- `CreateAdapter()` at `OracleProviderAdapter.cs:442` loads the assembly by name (`Oracle.DataAccess` or `Oracle.ManagedDataAccess`), reflects all Oracle-specific types (`OracleConnection`, `OracleDataReader`, `OracleBulkCopy`, etc.), and builds compiled lambdas for type-mapped operations (decimal precision loop at `:541-579`, `DateTimeOffset`-from-`OracleTimeStampTZ` at `:527-534`).
- **Devart.Data.Oracle** -- `CreateDevartAdapter()` at `OracleProviderAdapter.cs:646` follows the same pattern but uses `OracleLoader` for bulk copy and `ExecuteArray` for array-bound parameters.

Key capability flags:
- `BindingByNameEnabled`: `true` for Native + Devart, `false` for Managed. `OracleDataProvider.InitCommand` disables bind-by-name when there are no parameters to avoid `:NEW` token parse errors in triggers (`OracleDataProvider.cs:154-160`).
- `SetArrayBindCount` / `ExecuteArray`: present on Native and Devart, absent on Managed. Used by `AlternativeBulkCopy.InsertInto`.
- `BulkCopy` (nullable `IBulkCopyAdapter`): set when `OracleBulkCopy` (Oracle) or `OracleLoader` (Devart) type is found in the assembly.

Oracle-specific types exposed: `OracleBFileType`, `OracleBinaryType`, `OracleBlobType`, `OracleClobType`, `OracleDateType`, `OracleDecimalType`, `OracleIntervalDSType`, `OracleIntervalYMType`, `OracleStringType`, `OracleTimeStampType`, `OracleTimeStampLTZType` (nullable), `OracleTimeStampTZType` (nullable), `OracleXmlTypeType`, `OracleRefCursorType`.

### SQL builder hierarchy

```
BasicSqlBuilder<OracleOptions>
  +-- OracleSqlBuilderBase (abstract, partial)
        +-- OracleSqlBuilderBase.Merge.cs (partial -- MERGE support)
        +-- Oracle11SqlBuilder   -- v11 concrete
        +-- Oracle12SqlBuilder  -- v12+ concrete
```

`OracleSqlBuilderBase` (`OracleSqlBuilderBase.cs`) overrides:
- **`BuildSelectClause`** -- injects `FROM SYS.DUAL` when no FROM tables (`OracleSqlBuilderBase.cs:31-42`).
- **`BuildGetIdentity`** -- emits `RETURNING <field> INTO :IDENTITY_PARAMETER` (`OracleSqlBuilderBase.cs:44-55`).
- **`GetIdentityExpression`** -- returns `<sequence>.nextval` from `[SequenceName]` attribute (`OracleSqlBuilderBase.cs:57-74`). Oracle 11g has no native `IDENTITY` columns; v12+ has them, but linq2db still defaults to sequences for compatibility.
- **`BuildSetOperation`** -- maps `Except` -> `MINUS` / `ExceptAll` -> `MINUS ALL` (`OracleSqlBuilderBase.cs:85-94`).
- **`BuildDataTypeFromDataType`** -- Oracle-specific DDL types: `date`, `timestamp`, `Number(N)`, `Raw(16)` for GUID, `BLOB` for binary, `NClob`/`Clob`, etc. (`OracleSqlBuilderBase.cs:96-152`).
- **`BuildDropTableStatement`** / **`BuildCommand`** -- wraps DROP in `BEGIN ... EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;` PL/SQL blocks and creates/drops sequences + triggers for identity columns (`OracleSqlBuilderBase.cs:308-532`).
- **`BuildCreateTableStatement`** / **`BuildEndCreateTableStatement`** -- wraps `CREATE TABLE` in `EXECUTE IMMEDIATE` inside `BEGIN ... END;` for `IF NOT EXISTS` semantics (ORA-955 swallowed); appends `ON COMMIT DELETE/PRESERVE ROWS` for global temporary tables (`OracleSqlBuilderBase.cs:632-686`).
- **`BuildMultiInsertQuery`** / **`BuildMultiInsertClause`** -- Oracle `INSERT ALL` / `INSERT FIRST` multi-table insert (`OracleSqlBuilderBase.cs:690-735`).
- **Hints** -- `StartStatementQueryExtensions` initialises a `HintBuilder`; `FinalizeBuildQuery` inserts the `/*+ ... */` block at `_hintPosition` in the SQL string (`OracleSqlBuilderBase.cs:743-789`). `QB_NAME` query-block naming is supported.
- **`BuildInsertOrUpdateQuery`** -- delegates to `BuildInsertOrUpdateQueryAsMerge(..., "FROM SYS.DUAL")` (`OracleSqlBuilderBase.cs:269-272`).
- **`BuildIsDistinctPredicate`** -- uses `DECODE(a, b, 0, 1) = 1` because Oracle has no `IS DISTINCT FROM` until 23ai (`OracleSqlBuilderBase.cs:258-266`).
- **`BuildValue`** / **`TryConvertParameterToSql`** -- large strings (> 4000 bytes UTF-8) and large byte arrays (> 2000 bytes) are forced to parameters to avoid ORA-01704 (`OracleSqlBuilderBase.cs:797-836`).
- **`GetReserveSequenceValuesSql`** -- `SELECT <seq>.nextval FROM DUAL CONNECT BY level <= N` (`OracleSqlBuilderBase.cs:274-277`).

**`Oracle11SqlBuilder`** (`Oracle11SqlBuilder.cs`) only overrides `GetPhysicalTableName` to wrap table-valued functions as `TABLE(<name>)`.

**`Oracle12SqlBuilder`** (`Oracle12SqlBuilder.cs`) overrides:
- `LimitFormat` -> `FETCH NEXT {0} ROWS ONLY`
- `OffsetFormat` -> `OFFSET {0} ROWS`
- `OffsetFirst` -> `true` (OFFSET precedes FETCH)
- `ShouldBuildWhere` -- skips v11's ROWNUM injection for SELECT queries; v12+ uses native pagination.
- `CanSkipRootAliases` -- false when TAKE/SKIP present (issue #2785).

**`OracleSqlBuilderBase.Merge.cs`** configures Oracle MERGE specifics: `FakeTable = "dual"`, `FakeTableSchema = "sys"`, `IsValuesSyntaxSupported = false`, `SupportsColumnAliasesInSource = false`; overrides `BuildMergeInto`, `BuildMergeOperationInsert`, `BuildMergeOperationUpdate`, `BuildMergeOperationUpdateWithDelete` with Oracle `WHEN MATCHED` / `WHEN NOT MATCHED` syntax.

### SQL optimizer hierarchy

```
BasicSqlOptimizer
  +-- Oracle11SqlOptimizer (uses OracleSqlExpressionConvertVisitor)
        +-- Oracle12SqlOptimizer (uses Oracle12SqlExpressionConvertVisitor)
```

**`Oracle11SqlOptimizer`** (`Oracle11SqlOptimizer.cs`):
- `TransformStatement` -- calls `GetAlternativeDelete`, `GetAlternativeUpdate`, then `ReplaceTakeSkipWithRowNum` for all statement types.
- `ReplaceTakeSkipWithRowNum` -- wraps queries in nested subqueries with `ROWNUM <= take` and/or `RN > skip` predicates using `QueryHelper.WrapQuery`; moves ORDER BY to outermost query (`Oracle11SqlOptimizer.cs:89-168`).
- `IsParameterDependedElement` -- detects text-type operands in `ExprExpr` predicates that may compare against empty-string parameters (needed because Oracle treats `''` as NULL -- `Oracle11SqlOptimizer.cs:38-65`).
- `FixSetOperationValues` -- promotes inconsistent CHAR/NCHAR columns in UNION branches via `HasInconsistentCharset` / `FixCharset` (`Oracle11SqlOptimizer.cs:170-193`).

**`Oracle12SqlOptimizer`** (`Oracle12SqlOptimizer.cs`):
- `TransformStatement` -- calls `CorrectOutputTables`, then `GetAlternativeDelete`/`Update`, then `ReplaceTakeSkipWithRowNum` only for DML (not SELECT -- v12+ uses `OFFSET ... FETCH`).
- `CreateConvertVisitor` -- returns `Oracle12SqlExpressionConvertVisitor`.

### Expression-convert visitor hierarchy

```
SqlExpressionConvertVisitor
  +-- OracleSqlExpressionConvertVisitor (v11 and base)
        +-- Oracle12SqlExpressionConvertVisitor
```

**`OracleSqlExpressionConvertVisitor`** (`OracleSqlExpressionConvertVisitor.cs`):
- `ConvertExprExprPredicate` -- rewrites comparisons to empty string `""` as `IS NULL` / `IS NOT NULL` since Oracle treats `''` as NULL (`OracleSqlExpressionConvertVisitor.cs:29-108`).
- `ConvertSqlBinaryExpression` -- maps `%` -> `MOD`, `&` -> `BITAND`, `|` -> `a + b - BITAND(a,b)`, `^` -> XOR equivalent, `+` on strings -> `||`.
- `ConvertSqlUnaryExpression` -- bitwise NOT -> `-1 - expr`.
- `ConvertConversion` -- date/time cast routing: `Trunc(x,'DD')` for date, `TO_DATE`, `TO_TIMESTAMP`, `TO_TIMESTAMP_TZ`, `To_Char` with format masks.
- `ConvertCoalesce` / `ConvertSqlCondition` / `ConvertSqlCaseExpression` / `VisitSqlValuesTable` -- fix mixed CHAR/NCHAR charsets via `FixCharset` (wraps with `To_NChar` or `CAST ... AS NCHAR`).

**`Oracle12SqlExpressionConvertVisitor`** (`Oracle12SqlExpressionConvertVisitor.cs`) adds:
- `PseudoFunctions.TRY_CONVERT` -> `CAST(x AS t DEFAULT NULL ON CONVERSION ERROR)` (12c+).
- `PseudoFunctions.TRY_CONVERT_OR_DEFAULT` -> `CAST(x AS t DEFAULT y ON CONVERSION ERROR)`.

### Parameter name normalization

Two normalizers extend `UniqueParametersNormalizer` (from [INTERNAL-API](../INTERNAL-API/INDEX.md)):

| Class | Max identifier length | File |
|---|---|---|
| `Oracle122ParametersNormalizer` | 128 (base class default) | `Oracle122ParametersNormalizer.cs` |
| `Oracle11ParametersNormalizer` | 30 | `Oracle11ParametersNormalizer.cs:5` |

Oracle 11g limits unquoted identifiers to 30 characters. Oracle 12.2+ raised the limit to 128 characters. `Oracle11ParametersNormalizer.MaxLength` overrides to 30.

`OracleDataProvider.GetQueryParameterNormalizer` always returns `Oracle11ParametersNormalizer` (`OracleDataProvider.cs:193-200`). A TODO comment acknowledges that `Oracle122ParametersNormalizer` cannot yet be enabled because the version enum has no `v122` value -- see issue [#4219](https://github.com/linq2db/linq2db/issues/4219).

Both normalizers also check `ReservedWords.IsReserved(name, ProviderName.Oracle)` to avoid collisions.

### Bulk copy strategies

`OracleBulkCopy` extends `BasicBulkCopy`. Strategy selected by `BulkCopyType` and `AlternativeBulkCopy`:

| `BulkCopyType` | Mechanism |
|---|---|
| `ProviderSpecific` | `OracleBulkCopy` (ADO.NET, ODP.NET-only). Falls back to `MultipleRows` if adapter has no bulk copy or columns need escaping (`OracleBulkCopy.cs:51-134`). |
| `MultipleRows` + `InsertAll` | `INSERT ALL ... INTO t VALUES(...) ... SELECT * FROM dual` (`OracleBulkCopy.cs:188-232`). |
| `MultipleRows` + `InsertInto` | `INSERT INTO t(cols) VALUES(:p1, ...)` with `SetArrayBindCount` / `ExecuteArray` oracle extension (`OracleBulkCopy.cs:234-431`). |
| `MultipleRows` + `InsertDual` | `INSERT INTO t ... SELECT ... FROM DUAL UNION ALL SELECT ...` (`OracleBulkCopy.cs:432-484`). |

`ProviderSpecificCopyAsync` for `IEnumerable<T>` (`:137-141`) calls `Task.FromResult(ProviderSpecificCopy(...))` because ODP.NET `BulkCopy.WriteToServer` has no async overload. The `IAsyncEnumerable<T>` overload (`:144-153`) materialises the async enumerator via `EnumerableHelper.AsyncToSyncEnumerable` then delegates to the same synchronous path.

`MultipleRowsConvertToParameter` (`OracleBulkCopy.cs:194-199`) is a static lambda that forces a value to a parameter when `BulkCopyOptions.UseParameters` is set or when the column data type is `Text`, `NText`, `Binary`, or `VarBinary` -- ensuring large/blob-typed values are always parameterised rather than inlined into the SQL string.

Known limitation: ODP.NET bulk copy fails when any column name requires quoting. The check at `OracleBulkCopy.cs:67-77` detects this and falls back to `MultipleRows`.

### Mapping schema

`OracleMappingSchema` (`OracleMappingSchema.cs`) extends `LockedMappingSchema`. Six concrete subclasses cover the 3 providers x 2 versions matrix (e.g. `Native11MappingSchema`, `ManagedMappingSchema`). Key mappings:
- `Guid` -> `DataType.Guid` -> `Raw(16)` in DDL, serialized as `HEXTORAW('...')`.
- `DateTime` -> `TO_DATE(..., 'YYYY-MM-DD HH24:MI:SS')` literal.
- `DateTime2` -> `TIMESTAMP 'yyyy-MM-dd HH:mm:ss[.fff...]'` with precision 0--7.
- `DateTimeOffset` -> `TIMESTAMP 'yyyy...' +00:00'`.
- `float` / `double` NaN/Infinity -> `BINARY_FLOAT_NAN`, `BINARY_FLOAT_INFINITY`, etc.
- `decimal` -> `Number(28, 10)` default.
- `string` default -> `VarChar(255)`.
- Mixed char/nchar charset disambiguation via `HasInconsistentCharset` / `FixCharset` (`OracleExtensions.cs:11-43`).

### Schema provider

`OracleSchemaProvider` queries Oracle data dictionary views:
- `ALL_TABLES` / `USER_TABLES` + `ALL_VIEWS` / `USER_VIEWS` + `ALL_MVIEWS` -- table and view list (`OracleSchemaProvider.cs:72`). The `ALL_*` path is used only when schema filters are set (markedly slower); the `USER_*` path is the default fast path.
- `ALL_CONS_COLUMNS` + `ALL_CONSTRAINTS` -- primary keys and foreign keys (`OracleSchemaProvider.cs:142`).
- `ALL_TAB_COLUMNS` / `USER_TAB_COLUMNS` + `ALL_COL_COMMENTS` -- column definitions with `CHAR_LENGTH` vs `DATA_LENGTH` disambiguation for string types (`OracleSchemaProvider.cs:188-271`).
- `IDENTITY_COLUMN` field present only on 12+: the provider calls `GetMajorVersion` via `PRODUCT_COMPONENT_VERSION` and selects different `IsIdentity` SQL accordingly (`OracleSchemaProvider.cs:173-196`).
- Procedures: `GetProcedureSchemaExecutesProcedure = true` -- both Managed and Native execute the procedure to get result metadata.

### Member translator

`OracleMemberTranslator` (`Translation/OracleMemberTranslator.cs`) extends `ProviderMemberTranslatorDefault`. Full translator content read in delta run (PR #5467 "Improve 'now' translation", PR #5517 "Fix DateTime.Date truncation"):

**`DateFunctionsTranslator`** -- extends `DateFunctionsTranslatorBase`:
- `TranslateDateTimeDatePart` -- `YEAR`/`MONTH`/`DAY`/`HOUR`/`MINUTE`/`SECOND` via `EXTRACT(<part> FROM {0})`; `Quarter` via `TO_NUMBER(TO_CHAR(dt,'Q'))`; `DayOfYear` via `TO_NUMBER(TO_CHAR(dt,'DDD'))`; `Week` via `TO_NUMBER(TO_CHAR(dt,'WW'))`; `Millisecond` via `TO_NUMBER(TO_CHAR(dt,'FF')) / 1000`; `WeekDay` via `TRUNC`/`IW` arithmetic (`OracleMemberTranslator.cs:60-118`).
- `TranslateDateTimeOffsetDatePart` -- delegates to `TranslateDateTimeDatePart` (`OracleMemberTranslator.cs:121-124`).
- `TranslateDateTimeDateAdd` -- uses `INTERVAL '1' YEAR|MONTH|DAY|HOUR|MINUTE|SECOND` multiplication (`OracleMemberTranslator.cs:126-153`).
- `TranslateMakeDateTime` -- builds `TO_TIMESTAMP(concat_expr, 'YYYY-MM-DD HH24:MI:SS.FF3')` via `LPad`-padded string parts (`OracleMemberTranslator.cs:155-209`).
- `TranslateDateTimeTruncationToTime` -- `TO_CHAR(dt, 'HH24:MI:SS')` (`OracleMemberTranslator.cs:212-221`).
- `TranslateDateTimeTruncationToDate` -- `TRUNC(dt)` (no format mask; truncates to day boundary) (`OracleMemberTranslator.cs:224-231`). This is the fix from PR #5517: the prior `ConvertConversion` path used `Trunc(x,'DD')` with an explicit format; the translator now uses bare `TRUNC` without a format argument.
- `TranslateNow` -- emits `LOCALTIMESTAMP` (session time zone, `DateTime` typed) (`OracleMemberTranslator.cs:240-245`).
- `TranslateUtcNow` -- emits `SYS_EXTRACT_UTC(SYSTIMESTAMP)` (`DateTime` typed) (`OracleMemberTranslator.cs:233-238`). Note: `SYSTIMESTAMP` is passed as a fragment, not a function call.
- `TranslateZonedNow` -- emits `CURRENT_TIMESTAMP` (`DateTimeOffset` typed) (`OracleMemberTranslator.cs:247-250`).
- `TranslateZonedUtcNow` -- emits `SYSTIMESTAMP AT TIME ZONE 'UTC'` (`DateTimeOffset` typed) (`OracleMemberTranslator.cs:252-255`).

**`OracleMathMemberTranslator`** -- extends `MathMemberTranslatorBase`:
- `Math.Max` -> `GREATEST(x, y)` (`OracleMemberTranslator.cs:260-267`).
- `Math.Min` -> `LEAST(x, y)` (`OracleMemberTranslator.cs:269-276`).

**`GuidMemberTranslator`** -- extends `GuidMemberTranslatorBase`:
- `Guid.ToString()` -> `LOWER(SUBSTR(RAWTOHEX(g),7,2)||SUBSTR(...,5,2)||...)` -- 8-group UUID format via byte-order-reversed hex slices, wrapped in null-check `CASE` (`OracleMemberTranslator.cs:290-327`).

**`OracleStringMemberTranslator`** -- extends `StringMemberTranslatorBase`:
- `String.Join` -> `LISTAGG(value, separator) WITHIN GROUP (ORDER BY value)` aggregate. Handles `DISTINCT` modifier, filter conditions (via CASE for non-GROUP BY contexts), NVARCHAR->VARCHAR cast (LISTAGG does not accept NVARCHAR) (`OracleMemberTranslator.cs:330-401`).

**`TranslateNewGuidMethod`** (on `OracleMemberTranslator` directly): `Guid.NewGuid()` -> `Sys_Guid()` (non-pure function) (`OracleMemberTranslator.cs:279-284`).

**`SqlTypesTranslation`**:
- `Sql.Money` -> `Decimal(19,4)` (`OracleMemberTranslator.cs:43-44`).
- `Sql.SmallMoney` -> `Decimal(10,4)` (`OracleMemberTranslator.cs:46-47`).
- `Sql.NVarChar(n)` -> `VarChar2(n)` (`OracleMemberTranslator.cs:49-55`).

## Key types

### Public surface (`Source/LinqToDB/DataProvider/Oracle/`)

| Type | Kind | Role |
|---|---|---|
| `OracleTools` | `static partial class` | Registration entry point: `GetDataProvider`, `CreateDataConnection`, `DefaultVersion`, `AutoDetectProvider` |
| `OracleTools` (partial `OracleXmlTable.cs`) | `static partial class` | `OracleXmlTable<T>` extension -- passes an in-memory collection or XML string as a virtual relational table via `XMLType(...) COLUMNS ...` |
| `OracleVersion` | `enum` | `AutoDetect`, `v11 = 11`, `v12 = 12` |
| `OracleProvider` | `enum` | `AutoDetect`, `Managed`, `Native`, `Devart` |
| `OracleOptions` | `sealed record` | `BulkCopyType`, `AlternativeBulkCopy`, `DontEscapeLowercaseIdentifiers` |
| `AlternativeBulkCopy` | `enum` | `InsertAll` (default), `InsertInto`, `InsertDual` |
| `OracleHints.Hint` | `static class` | String constants + parameterized helpers for all Oracle optimizer hints |
| `OracleHints` (`.generated.cs`) | `static partial class` | Generated typed extension methods (e.g. `AllRowsHint`, `FullHint`, `IndexHint`) on `IOracleSpecificQueryable<T>` / `IOracleSpecificTable<T>` |
| `OracleSpecificExtensions` | `static class` | `AsOracle<T>()` cast to `IOracleSpecificQueryable` / `IOracleSpecificTable` |
| `IOracleSpecificQueryable<T>` | `interface` | Marker for Oracle-scoped query extensions |
| `IOracleSpecificTable<T>` | `interface` | Marker for Oracle-scoped table hints |
| `OracleFactory` | `sealed class` (internal) | `IDataProviderFactory` -- reads `version` + assembly name from config attributes |

### Internal (`Source/LinqToDB/Internal/DataProvider/Oracle/`)

| Type | Role |
|---|---|
| `OracleDataProvider` | Abstract base; 6 concrete sealed subclasses above |
| `OracleSqlBuilderBase` + `.Merge.cs` | SQL builder base (abstract, partial) |
| `Oracle11SqlBuilder` / `Oracle12SqlBuilder` | Dialect-specific builders |
| `Oracle11SqlOptimizer` / `Oracle12SqlOptimizer` | Statement transformation + take/skip rewriting |
| `OracleSqlExpressionConvertVisitor` / `Oracle12SqlExpressionConvertVisitor` | Expression-level rewrites |
| `Oracle11ParametersNormalizer` / `Oracle122ParametersNormalizer` | Identifier length enforcement |
| `OracleProviderAdapter` | ADO.NET type-mapping wrapper (3 static singletons) |
| `OracleProviderDetector` | Auto-detect client + version |
| `OracleMappingSchema` + 6 sub-schemas | Type/literal mapping |
| `OracleBulkCopy` | 4-strategy bulk insert |
| `OracleSchemaProvider` | Dictionary-view-based schema discovery |
| `OracleExtensions` (internal static) | `HasInconsistentCharset` / `FixCharset` helpers |
| `OracleMemberTranslator` | LINQ member -> Oracle SQL function translation |
| `OracleSpecificTable<T>` / `OracleSpecificQueryable<T>` | Internal implementations of public interfaces |

## Oracle-specific features

### Identity / sequences

Oracle 11g has no `IDENTITY` columns. `OracleSqlBuilderBase` models identity via:
1. `GetIdentityExpression` emits `<schema>.<sequence>.nextval`.
2. On `CREATE TABLE` with an identity field, `CommandCount` returns 3 and `BuildCommand` emits two extra commands: `CREATE SEQUENCE SIDENTITY_<table>` and `CREATE OR REPLACE TRIGGER TIDENTITY_<table> BEFORE INSERT ... SELECT seq.NEXTVAL INTO :NEW.<col> FROM dual` (`OracleSqlBuilderBase.cs:492-531`).
3. After insert, `BuildGetIdentity` emits `RETURNING <field> INTO :IDENTITY_PARAMETER`.

Oracle 12c+ supports `GENERATED ALWAYS AS IDENTITY`. The schema provider reads `IDENTITY_COLUMN = 'YES'` for v12+.

### Pagination

- v11: `ROWNUM`-based triple-nesting via `ReplaceTakeSkipWithRowNum`. The innermost query fetches data + adds `ROWNUM <= skip+take`; a middle query aliases ROWNUM as `RN`; the outer filters `RN > skip`.
- v12+: native `OFFSET n ROWS FETCH NEXT m ROWS ONLY` (builder `LimitFormat`/`OffsetFormat`).

### Empty string = NULL

Oracle treats `''` as NULL. `OracleSqlExpressionConvertVisitor.ConvertExprExprPredicate` rewrites any `= ''` or `<> ''` comparison to `IS NULL` / `IS NOT NULL`. `Oracle11SqlOptimizer.IsParameterDependedElement` marks text-type parameters as query-plan-dependent when comparing to empty-string values.

### MERGE

Oracle invented the MERGE statement (SQL:2003 origin). `OracleSqlBuilderBase.Merge.cs` emits standard `MERGE INTO ... USING ... ON ... WHEN MATCHED THEN UPDATE ... WHEN NOT MATCHED THEN INSERT ...` with optional `DELETE WHERE` clause. `INSERT OR UPDATE` maps to MERGE via `BuildInsertOrUpdateQueryAsMerge(..., "FROM SYS.DUAL")`.

### Multi-table INSERT

Oracle supports `INSERT ALL ... SELECT * FROM dual` and `INSERT FIRST ... SELECT * FROM dual`. `BuildMultiInsertQuery` / `BuildMultiInsertClause` support both `MultiInsertType.Unconditional` (INSERT ALL) and `MultiInsertType.First` (INSERT FIRST) with `WHEN ... THEN INTO ... ELSE INTO ...` conditional variants.

### XML table

`OracleTools.OracleXmlTable<T>` serialises a .NET `IEnumerable<T>` to an XML string in the format `<t><r><c0>...</c0>...</r></t>` and passes it via a parameter. The `OracleXmlTableAttribute` (a `Sql.TableExpressionAttribute`) rewrites the table expression to `XmlTable('/t/r' PASSING XmlType({param}) COLUMNS col1 TYPE path 'c0', ...)` at query-building time (`OracleTools.OracleXmlTable.cs:114-179`).

### Global temporary tables

`BuildCreateTableCommand` maps all temp-table `TableOptions` combinations to `CREATE GLOBAL TEMPORARY TABLE`. `BuildEndCreateTableStatement` appends `ON COMMIT DELETE ROWS` (transaction-scoped) or `ON COMMIT PRESERVE ROWS` (session-scoped, default). `BuildDropTableStatement` wraps `TRUNCATE TABLE` in a `BEGIN ... EXCEPTION ... END` block before DROP to clear session rows and avoid ORA-14452 (`OracleSqlBuilderBase.cs:322-343`).

### Char/NChar charset mixing

Oracle raises errors when mixing CHAR/VARCHAR2 and NCHAR/NVARCHAR2 in set operations, CASE expressions, COALESCE, and VALUES. `OracleExtensions.HasInconsistentCharset` detects this (`OracleExtensions.cs:11`); `OracleExtensions.FixCharset` wraps the CHAR side with `TO_NCHAR(...)` or `CAST(... AS NCHAR)` (`OracleExtensions.cs:31-43`). Called from `ConvertCoalesce`, `ConvertSqlCondition`, `ConvertSqlCaseExpression`, `VisitSqlValuesTable`, and `FixSetOperationValues`.

### Provider-specific parameter handling

- `BFile` -> `value = null` (not supported for input, `OracleDataProvider.cs:238-241`).
- `Boolean` -> `BYTE` / `NUMBER(1)`.
- `Guid` -> `byte[]` / `Raw(16)`.
- `DateTimeOffset` -> constructed `OracleTimeStampTZ` with explicit offset string.
- `DateTime` precision truncated: `DateTime` -> 0 decimals, `DateTime2` -> `Precision ?? 6`.
- Long strings (>= 4000 chars) -> `NText` data type to route to `NClob`.

### `SqlProviderFlags` notable settings

(`OracleDataProvider.cs:37-60`)
- `IsUpdateFromSupported = false` -- Oracle has no `UPDATE ... FROM`.
- `SupportedCorrelatedSubqueriesLevel = 1` -- one level of correlation allowed.
- `DoesProviderTreatsEmptyStringAsNull = true`.
- `SupportsBooleanType = false` (TODO: retest for Oracle 23ai).
- `MaxInListValuesCount = 1000` -- Oracle IN-list limit.
- `IsApplyJoinSupported = true` only for v12+.
- `IsColumnSubqueryShouldNotContainParentIsNotNull` and `IsColumnSubqueryWithParentReferenceAndTakeSupported` differ by version (`OracleDataProvider.cs:47-48`).

## Files (Tier 1 / Tier 2)

### Tier 1 (12 files -- all read)

| File | Role |
|---|---|
| `Source/LinqToDB/Internal/DataProvider/Oracle/OracleDataProvider.cs` | Abstract data provider + 6 concrete subclasses |
| `Source/LinqToDB/Internal/DataProvider/Oracle/OracleSqlBuilderBase.cs` | SQL builder base |
| `Source/LinqToDB/Internal/DataProvider/Oracle/OracleSqlBuilderBase.Merge.cs` | MERGE partial |
| `Source/LinqToDB/Internal/DataProvider/Oracle/Oracle11SqlBuilder.cs` | v11 builder |
| `Source/LinqToDB/Internal/DataProvider/Oracle/Oracle12SqlBuilder.cs` | v12+ builder |
| `Source/LinqToDB/Internal/DataProvider/Oracle/Oracle11SqlOptimizer.cs` | v11 optimizer + ROWNUM rewrite |
| `Source/LinqToDB/Internal/DataProvider/Oracle/Oracle12SqlOptimizer.cs` | v12+ optimizer |
| `Source/LinqToDB/Internal/DataProvider/Oracle/OracleProviderAdapter.cs` | ADO.NET dynamic wrapper |
| `Source/LinqToDB/Internal/DataProvider/Oracle/OracleProviderDetector.cs` | Auto-detect client + version |
| `Source/LinqToDB/Internal/DataProvider/Oracle/OracleMappingSchema.cs` | Type/literal mapping (6 subclasses) |
| `Source/LinqToDB/Internal/DataProvider/Oracle/OracleBulkCopy.cs` | Bulk copy (4 strategies) |
| `Source/LinqToDB/DataProvider/Oracle/OracleTools.cs` | Public registration entry point |

### Tier 2 (20/20 visited)

| File | Status |
|---|---|
| `Source/LinqToDB/DataProvider/Oracle/OracleVersion.cs` | Read -- version enum |
| `Source/LinqToDB/DataProvider/Oracle/OracleProvider.cs` | Read -- client enum |
| `Source/LinqToDB/DataProvider/Oracle/OracleOptions.cs` | Read -- provider options record |
| `Source/LinqToDB/DataProvider/Oracle/AlternativeBulkCopy.cs` | Read -- bulk copy mode enum |
| `Source/LinqToDB/DataProvider/Oracle/OracleTools.OracleXmlTable.cs` | Read -- XML table feature |
| `Source/LinqToDB/DataProvider/Oracle/OracleHints.cs` | Read -- hint constant catalog |
| `Source/LinqToDB/DataProvider/Oracle/OracleHints.generated.cs` | Read (sampled) -- generated hint extension methods |
| `Source/LinqToDB/DataProvider/Oracle/OracleSpecificExtensions.cs` | Read -- `AsOracle<T>()` cast |
| `Source/LinqToDB/DataProvider/Oracle/IOracleSpecificQueryable.cs` | Read -- marker interface |
| `Source/LinqToDB/DataProvider/Oracle/IOracleSpecificTable.cs` | Read -- marker interface |
| `Source/LinqToDB/DataProvider/Oracle/OracleFactory.cs` | Read -- config-based factory |
| `Source/LinqToDB/Internal/DataProvider/Oracle/Oracle11ParametersNormalizer.cs` | Read -- 30-char limit |
| `Source/LinqToDB/Internal/DataProvider/Oracle/Oracle122ParametersNormalizer.cs` | Read -- 128-char limit |
| `Source/LinqToDB/Internal/DataProvider/Oracle/Oracle12SqlExpressionConvertVisitor.cs` | Read -- TRY_CONVERT for 12c+ |
| `Source/LinqToDB/Internal/DataProvider/Oracle/OracleSqlExpressionConvertVisitor.cs` | Read -- empty-string/bitwise/cast rewrites |
| `Source/LinqToDB/Internal/DataProvider/Oracle/OracleSchemaProvider.cs` | Read (partial -- first 300 lines) |
| `Source/LinqToDB/Internal/DataProvider/Oracle/OracleExtensions.cs` | Read -- charset helpers |
| `Source/LinqToDB/Internal/DataProvider/Oracle/Translation/OracleMemberTranslator.cs` | Read (full -- delta run; all sub-translators inspected) |
| `Source/LinqToDB/Internal/DataProvider/Oracle/OracleSpecificQueryable.cs` | Read -- sealed impl |
| `Source/LinqToDB/Internal/DataProvider/Oracle/OracleSpecificTable.cs` | Read -- sealed impl |

### Tier 3 (1 file -- skipped)

- `Source/LinqToDB/DataProvider/Oracle/OracleHints.tt` -- T4 template that generates `OracleHints.generated.cs`

## Known issues / debt

1. **`Oracle11ParametersNormalizer` always used regardless of version.** `OracleDataProvider.GetQueryParameterNormalizer` hardcodes `Oracle11ParametersNormalizer` (30-char limit). `Oracle122ParametersNormalizer` exists but is never activated. See issue [#4219](https://github.com/linq2db/linq2db/issues/4219) and TODO at `OracleDataProvider.cs:195`.

2. **`SupportsBooleanType = false` awaiting Oracle 23ai retest.** `OracleDataProvider.cs:52` has a TODO -- Oracle 23ai introduced a native `BOOLEAN` type; the flag has not been updated.

3. **Reserved words list is a static 11g merge.** `OracleSqlBuilderBase.IsReserved` notes the list is a merge of 11g SQL + PL/SQL reserved words and should be version-gated (`OracleSqlBuilderBase.cs:167-172`).

4. **`OracleBulkCopy` provider-specific path is synchronous-only.** Both `ProviderSpecificCopyAsync` overloads (`:137-141` for `IEnumerable<T>`, `:144-153` for `IAsyncEnumerable<T>`) delegate to the synchronous path because ODP.NET has no async `WriteToServer`.

5. **ODP.NET bulk copy column-escaping limitation.** Provider-specific bulk copy falls back to multi-row INSERT when any column name requires quoting (`OracleBulkCopy.cs:66-77`). A TODO notes ordinal-based column mapping as the fix.

6. **`BFile` parameter setting unsupported.** `OracleDataProvider.SetParameter` sets value to `null` for `DataType.BFile` (`OracleDataProvider.cs:238-241`) with a TODO.

7. **`function with ref_cursor return type returns object`** -- noted in `OracleSchemaProvider.cs:15` as a missing schema feature.

## Inbound / outbound dependencies

**Inbound:** `OracleTools` is the public entry point. `OracleFactory` is loaded by `DataProviderFactoryBase` via assembly scanning.

**Outbound:**
- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md): `BasicSqlBuilder<OracleOptions>`, `BasicSqlOptimizer`, `SqlExpressionConvertVisitor`.
- [INTERNAL-API](../INTERNAL-API/INDEX.md): `DynamicDataProviderBase<OracleProviderAdapter>`, `IDynamicProviderAdapter`, `BasicBulkCopy`, `ProviderDetectorBase<OracleProvider,OracleVersion>`, `UniqueParametersNormalizer`, `TypeMapper`, `SchemaProviderBase`, `MemberTranslatorBase`.
- [MAPPING](../MAPPING/INDEX.md): `LockedMappingSchema`, `MappingSchema`.

## See also

- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) -- `BasicSqlBuilder`, `BasicSqlOptimizer`, `SqlExpressionConvertVisitor`
- [INTERNAL-API](../INTERNAL-API/INDEX.md) -- `DynamicDataProviderBase`, `BasicBulkCopy`, `ProviderDetectorBase`, `TypeMapper`
- [MAPPING](../MAPPING/INDEX.md) -- `LockedMappingSchema`
- [PROV-SQLSERVER](../PROV-SQLSERVER/INDEX.md), [PROV-POSTGRES](../PROV-POSTGRES/INDEX.md), [PROV-MYSQL](../PROV-MYSQL/INDEX.md) -- parallel provider implementations for cross-reference

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 12 / 12
- Tier 2 (visited / total): 20 / 20 (100%)
- Tier 3 (skipped, logged): 1 (`OracleHints.tt`)

Delta run (this run -- sha 4a478ff14):
- Read: `Source/LinqToDB/Internal/DataProvider/Oracle/OracleBulkCopy.cs` (full -- includes `MultipleRowsConvertToParameter` static lambda forcing Text/NText/Binary/VarBinary values to parameters)
- Read: `Source/LinqToDB/Internal/DataProvider/Oracle/Translation/OracleMemberTranslator.cs` (full -- previously partial; PR #5467 now-virtual split and PR #5517 `TranslateDateTimeTruncationToDate` use bare `TRUNC` instead of `Trunc(x,'DD')`)

</details>
