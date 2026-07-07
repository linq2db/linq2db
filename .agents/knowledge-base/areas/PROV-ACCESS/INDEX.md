---
area: PROV-ACCESS
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-07-05
last_verified_sha: 36ee4f82f06eaf242b052ade8c87121d251a6165
coverage_tier_1: 11/11
coverage_tier_2: 16/16
---

# PROV-ACCESS

Microsoft Access provider. Wraps two distinct ADO.NET driver families -- OLE DB and ODBC -- and two engine generations (JET / ACE), yielding four concrete providers. Access is a file-based, single-user database engine with significant SQL dialect restrictions that drive most of the complexity in this area.

## Provider matrix

| `AccessProvider` | `AccessVersion` | Concrete class | `ProviderName` constant |
|---|---|---|---|
| `OleDb` | `Jet` | `AccessJetOleDbDataProvider` | `ProviderName.AccessJetOleDb` |
| `OleDb` | `Ace` | `AccessAceOleDbDataProvider` | `ProviderName.AccessAceOleDb` |
| `ODBC`  | `Jet` | `AccessJetODBCDataProvider`  | `ProviderName.AccessJetOdbc`  |
| `ODBC`  | `Ace` | `AccessAceODBCDataProvider`  | `ProviderName.AccessAceOdbc`  |

All four are `sealed` primary-constructor classes in `AccessDataProvider.cs:22-25` inheriting `AccessDataProvider`. The abstract base `AccessDataProvider` extends `DynamicDataProviderBase<AccessProviderAdapter>`.

## Public surface

**`AccessTools`** (`DataProvider/Access/AccessTools.cs`) is the registration entry point:
- `GetDataProvider(version, provider, connectionString?, ...)` -- returns an `IDataProvider` via `AccessProviderDetector`.
- `CreateDataConnection(...)` -- three overloads (connection string, `DbConnection`, `DbTransaction`).
- `CreateDatabase(databaseName, ...)` -- creates an `.mdb` / `.accdb` file via COM ADOX (`ADOX.Catalog`), requires OLE DB + ADOX to be installed. `AccessTools.cs:127-132`.
- `DropDatabase(databaseName)` -- delegates to `DataTools.DropFileDatabase`.

**`AccessVersion`** enum: `AutoDetect`, `Jet`, `Ace`. JET targets `.mdb` (Microsoft.Jet.OLEDB.4.0); ACE targets `.accdb` and `.mdb` (Microsoft.ACE.OLEDB.12.0+). `AccessVersion.cs`.

**`AccessProvider`** enum: `AutoDetect`, `OleDb`, `ODBC`. `AccessProvider.cs`.

**`AccessOptions`** record: single property `BulkCopyType` (default `BulkCopyType.MultipleRows`). Sealed record implementing `DataProviderOptions<AccessOptions>`. `AccessOptions.cs`.

**`IAccessSpecificQueryable<TSource>`**, **`IAccessSpecificTable<TSource>`** -- provider-specific query/table marker interfaces. Implemented internally by `AccessSpecificQueryable<T>` and `AccessSpecificTable<T>`.

**`AccessSpecificExtensions`** -- `AsAccess<T>(ITable<T>)` and `AsAccess<T>(IQueryable<T>)` to obtain provider-specific wrappers.

**`AccessHints`** -- single hint: `WITH OWNERACCESS OPTION` (emitted as a sub-query hint via `Sql.QueryExtensionScope.SubQueryHint`). No table hints, no join hints.

**`AccessFactory`** -- `DataProviderFactoryBase` subclass for configuration-driven instantiation. Reads `AssemblyName` and `Version` attributes, maps to `AccessProvider` / `AccessVersion`, then calls `AccessTools.GetDataProvider`.

## Key types

| Type | File | Role |
|---|---|---|
| `AccessDataProvider` | `Internal/DataProvider/Access/AccessDataProvider.cs` | Abstract provider base; 4 concrete subclasses; `SqlProviderFlags` incl. Upsert-merge-lowering gate; dispatch to OleDb/ODBC SQL builders and schema providers |
| `AccessSqlBuilderBase` | `Internal/DataProvider/Access/AccessSqlBuilderBase.cs` | Abstract SQL builder; TOP syntax, IIF-based conditionals, IDENTITY reset via `ALTER COLUMN COUNTER` |
| `AccessOleDbSqlBuilder` | `...AccessOleDbSqlBuilder.cs` | Concrete OLE DB builder; passes through to base, adds `GetProviderTypeName` via OLE DB type enum |
| `AccessODBCSqlBuilder` | `...AccessODBCSqlBuilder.cs` | Concrete ODBC builder; overrides `Convert` to emit `?` placeholders; forces GUID values to parameters |
| `AccessSqlOptimizer` | `...AccessSqlOptimizer.cs` | Statement rewriter: multi-table query correction, inner-join normalization, EXISTS/IN rewrite, parameter wrapping |
| `AccessSqlExpressionConvertVisitor` | `...AccessSqlExpressionConvertVisitor.cs` | Expression-level rewrites: function names, COALESCE->IIF chain, LIKE escaping, bitwise ops, type casts; `SupportsNullIf = false` |
| `AccessMappingSchema` | `...AccessMappingSchema.cs` | Type mappings; date literals as `#yyyy-MM-dd#`; string concatenation via `+`; decimal/float as `AnsiString`; string->numeric and string->bool parsers scoped to `ConversionType.FromDatabase` |
| `AccessProviderAdapter` | `...AccessProviderAdapter.cs` | Thin wrapper unifying OleDb/ODBC adapter instances; exposes `GetOleDbSchemaTable` for schema introspection |
| `AccessProviderDetector` | `...AccessProviderDetector.cs` | Auto-detect from connection string tokens, `ProviderName`, config string, or fallback to file probing |
| `AccessBulkCopy` | `...AccessBulkCopy.cs` | `BasicBulkCopy` subclass; caps at 767 parameters and 64 000-character SQL |
| `AccessDmlService` | `...AccessDmlService.cs` | `DmlServiceBase` subclass; `IsTableNotFoundException` matching for OleDb `DB_E_NOTABLE` (0x80040E37) and ODBC SQLSTATE 42S02 |
| `AccessSchemaProviderBase` | `...AccessSchemaProviderBase.cs` | Shared data-type mapping, database-name from file path, `GetSystemType` text-length->char specialization |
| `AccessOleDbSchemaProvider` | `...AccessOleDbSchemaProvider.cs` | OLE DB schema: uses `GetOleDbSchemaTable` for FKs, separate connection workaround for provider bug; procedures from `GetSchema("Procedures")` |
| `AccessODBCSchemaProvider` | `...AccessODBCSchemaProvider.cs` | ODBC schema: no FKs (runtime bug), no PKs (runtime bug); views merged with tables via `TABLE_TYPE` |
| `AccessMemberTranslator` | `...Translation/AccessMemberTranslator.cs` | ACE/base translator; date functions (`DatePart`/`DateAdd`/`DateSerial`/`Now`), math (`Round`/`Int`/`^`), string (`Mid`, `String`, `InStr`, whitespace-only `TrimStart`/`TrimEnd`, `IsNullOrWhiteSpace`, `Join`), GUID (`IIF` null-guard + `CStr`+`Mid`+`LCase`), nested window-translator stub |
| `AccessJetMemberTranslator` | `...Translation/AccessJetMemberTranslator.cs` | JET override; `TranslateReplace` returns `null` (JET has no `REPLACE` function) |

## SqlProviderFlags highlights

Set in `AccessDataProvider.cs:37-67`:

- `IsParameterOrderDependent = true` -- forced for both drivers (OLE DB has complex-query parameter order bugs; `AccessDataProvider.cs:52-55`).
- `IsSkipSupported = false`, `IsSubQuerySkipSupported = false` -- no `OFFSET` in Access.
- `TakeHintsSupported = TakeHints.Percent` -- `TOP {n} PERCENT` is the only limit form.
- `IsCrossJoinSupported = false`, `IsNestedJoinSupported = false` (builder), `IsMultiTablesSupportsJoins = false`.
- `IsInsertOrUpdateSupported = false` -- no MERGE or `INSERT OR REPLACE`.
- `IsUpsertWithMergeLoweringSupported = false` -- Access has no MERGE statement; `Upsert` shapes that need two-branch MERGE lowering (bulk source, non-PK match, conditional Insert, or SkipInsert) are rejected up front by `UpsertBuilder` (`Internal/Linq/Builder/UpsertBuilder.cs:262`) via `ErrorHelper.Error_Upsert_MergeLowering_NotSupported` rather than reaching `BuildMergeStatement`. `AccessDataProvider.cs:43-45`. Cross-provider flag -- also `false` on MySQL, PostgreSQL, SAP HANA, SQLite, SqlCe, SQL Server.
- `IsWindowFunctionsSupported = false`.
- `SupportedCorrelatedSubqueriesLevel = 1`.
- `IsOuterJoinSupportsInnerJoin = false`, `WrapJoinCondition = true` -- parenthesized join conditions always emitted.
- `IsAccessBuggyLeftJoinConstantNullability = true` -- provider-specific flag for a known Access LEFT JOIN NULL propagation bug.
- `IsSimpleCoalesceSupported = false` -- COALESCE is rewritten to `IIF(x IS NULL, ...)` chains in `AccessSqlExpressionConvertVisitor`.
- `IsSubqueryExpressionInsidePredicateSupported = false`, `IsSubqueryJoinOnOuterReferenceSupported = false`.
- `IsSubQueryOrderBySupported = false` -- subquery ORDER BY not permitted.
- `IsUnionAllOrderBySupported = true`.
- `DefaultNullsOrdering = NullsDefaultOrdering.Smallest` -- Access sorts NULL as the smallest value. `AccessDataProvider.cs:39`.
- `IsDistinctSetOperationsSupported = false`.
- `IsOrderByAggregateSubquerySupported = false`.
- `DefaultMultiQueryIsolationLevel = IsolationLevel.Unspecified`.
- `SupportsPredicatesComparison = true`.
- `IsUpdateFromSupported = false`.
- `AcceptsTakeAsParameter = false`.
- `IsSupportsJoinWithoutCondition = false`.

## SQL dialect specifics

### Identifier quoting
Square brackets `[name]` for all identifiers (fields, tables, aliases, databases). `AccessSqlBuilderBase.cs:141-175`. No schema component emitted -- `BuildObjectName` strips schema, supports only database + name.

### Parameters
- OLE DB: `@name` placeholders, named parameters (inherits `BasicSqlBuilder` default). `AccessSqlBuilderBase.cs:145-147`.
- ODBC: positional `?` placeholders, all parameter names collapsed. `AccessODBCSqlBuilder.cs:30-37`. `GetQueryParameterNormalizer` returns `NoopQueryParametersNormalizer` for ODBC to suppress name-based normalization. `AccessDataProvider.cs:123-126`.

### Date literals
`#yyyy-MM-dd#` for date-only values; `#yyyy-MM-dd HH:mm:ss#` for datetime. `AccessMappingSchema.cs:15-30`. Sub-second precision not representable as literals -- `AccessSqlBuilderBase.cs:278-297` forces such values to parameters.

### Conditional expressions
`CASE` is absent; `SqlCaseExpression` is converted to `ConvertCaseToConditions` chains (reduces to nested `IIF`). `AccessSqlBuilderBase.cs:120-128`. `SqlConditionExpression` -> `IIF(cond, t, f)`. `SupportsNullIf = false` override in `AccessSqlExpressionConvertVisitor` prevents NULLIF emission. `AccessSqlExpressionConvertVisitor.cs:23`.

### NULL column typization
`IIF(False, <typed-default>, NULL)` for NULL literals in SELECT, ensuring UNION column type resolution. `AccessSqlBuilderBase.cs:71-95`.

### IS DISTINCT FROM
Emitted as `IIF(e1 = e2 OR e1 IS NULL AND e2 IS NULL, 0, 1) = 0/1`. `AccessSqlBuilderBase.cs:97-111`.

### MERGE / CTE / window functions
All unsupported. `BuildMergeStatement` throws `LinqToDBException`. `AccessSqlBuilderBase.cs:207-210`. No CTE support in Access JET/ACE engine. Most non-trivial `Upsert` call shapes never reach this throw -- `SqlProviderFlags.IsUpsertWithMergeLoweringSupported = false` (see SqlProviderFlags highlights) makes `UpsertBuilder` fail fast with a descriptive error before SQL generation; the builder-level throw here only covers a direct `Merge()`-API statement.

### Comments
SQL comments stripped entirely -- `BuildSqlComment` returns without appending. `AccessSqlBuilderBase.cs:212-215`.

### IDENTITY reset after TRUNCATE
`CommandCount` returns `count + 1` for `SqlTruncateTableStatement` with identity fields. `BuildCommand` emits `ALTER TABLE [t] ALTER COLUMN [f] COUNTER(1, 1)` for each identity field, and `SELECT @@IDENTITY` for INSERT identity retrieval. `AccessSqlBuilderBase.cs:25-52`.

### UPDATE syntax
`BuildUpdateClause` builds `FROM` clause then rewrites prefix to `UPDATE` (Access uses `UPDATE t ... FROM t ...` style). `AccessSqlBuilderBase.cs:112-118`.

### Bitwise operations
`&` -> `BAND`, `|` -> `BOR`, bitwise NOT `~x` -> `-1 - x`. `AccessSqlExpressionConvertVisitor.cs:224-233`, `:183-186`.

### Function remapping (`AccessSqlExpressionConvertVisitor`)
| linq2db pseudo | Access |
|---|---|
| `TO_LOWER` | `LCase` |
| `TO_UPPER` | `UCase` |
| `LENGTH`   | `Len`  |
| `CharIndex(p,s)` | `InStr(1, s, p, 1)` |
| `%` modulo | `MOD` |

### Type casts (`ConvertConversion`)
`CStr`, `CBool`, `CDate`, `DateValue`, `TimeValue` -- all wrapped in `IIF(x IS NOT NULL, CFunc(x), NULL)` to preserve nullability. `AccessSqlExpressionConvertVisitor.cs:189-222`.

### COALESCE -> IIF rewrite
`ConvertCoalesce` in `AccessSqlExpressionConvertVisitor` first calls `RemoveNullValues` to strip NULL-literal operands before folding to nested `IIF` chains -- prevents `Coalesce(x, NULL)` from generating `IIF(x IS NULL, NULL, x)` (issue #5531). `AccessSqlExpressionConvertVisitor.cs:132-164`.

### LIKE escaping
Access LIKE metacharacters: `_`, `?`, `*`, `%`, `#`, `-`, `!`. Escape via bracket notation `[c]`. No `ESCAPE` clause support. `AccessSqlExpressionConvertVisitor.cs:17-44`.
`EscapeLikeCharacters` (for dynamic patterns with `REPLACE`) throws `LinqToDBException` -- Access ACE lacks `REPLACE` in this code path; `TODO` exists for ACE. `:50-52`.

### Case-sensitive string search
`InStr(1, expr, pattern, 0)` with `Compare = 0` (binary) used for case-sensitive `StartsWith`/`EndsWith`/`Contains`. `AccessSqlExpressionConvertVisitor.cs:53-130`.

### Decimal / VarNumeric parameters
Passed as `DbType.AnsiString` (OLE DB) or `DbType.AnsiString` (ODBC) to avoid culture-aware decimal separator bugs. `AccessDataProvider.cs:201-202`, `237-240`. Corresponding `SetConvertExpression` calls in `AccessMappingSchema.cs:57-63` parse strings back using culture-default `Parse` (no `IFormatProvider`) and are scoped to `ConversionType.FromDatabase` -- see PR #5520 / issue #5519.

### String-to-numeric and string-to-bool read parsers (PR #5520 / issue #5519)
`AccessMappingSchema` registers four `SetConvertExpression` converters for read-back of culture-specific decimal string forms:
- `(string v) => decimal.Parse(v)` -- `AccessMappingSchema.cs:57`
- `(string v) => float.Parse(v)` -- `AccessMappingSchema.cs:58`
- `(string v) => double.Parse(v)` -- `AccessMappingSchema.cs:59`
- `(string v) => v == "-1"` (bool) -- `AccessMappingSchema.cs:63`

All four carry `conversionType: ConversionType.FromDatabase`. Without this constraint, a bool-typed column with a `ValueConverter` could pick up the `string->bool` parser during write-side parameter prep (`ParametersContext.PrepareParameterCacheEntry`), routing a closure-captured storage-form string through bool and back and losing the value. `ConversionType.FromDatabase` restricts these converters to the read path only; the write-side `Common` lookup is unaffected. `AccessMappingSchema.cs:44-63`.

### GUID literals
- OLE DB: `{guid {B-format}}` with NETFX/NETCORE difference. `AccessMappingSchema.cs:98-103`.
- ODBC: forced to parameter always (`{}` conflicts with ODBC escape syntax). `AccessODBCSqlBuilder.cs:52-77`.

### Parameter type casting (`BuildParameter`)
When `NeedsCast` is set, wraps in `CSng(...)` for `DataType.Single`, otherwise `CVar(...)`. `AccessSqlBuilderBase.cs:251-275`.

## Optimizer transforms (`AccessSqlOptimizer`)

`TransformStatement` pipeline (`AccessSqlOptimizer.cs:22-35`):
1. `base.TransformStatement` -- standard rewrite.
2. `CorrectMultiTableQueries` -- inherited (re-aliases tables for multi-table queries).
3. `CorrectInnerJoins` -- promotes INNER JOINs with mis-bound or unbound conditions to old-style comma-join + WHERE. `AccessSqlOptimizer.cs:88-151`. This handles Access's unusual parenthesized-join ordering requirements.
4. `CorrectExistsAndIn` -- rewrites `EXISTS`/`IN` subqueries (from a `SELECT <condition>` form) to `SELECT COUNT(*) > 0` / `= 0` because Access has limited subquery predicate support. `AccessSqlOptimizer.cs:154-222`.
5. `GetAlternativeDelete` / `CorrectAccessUpdate` -- for `DELETE`/`UPDATE` statements.

`CorrectAccessUpdate`: strips `ORDER BY`, calls `CorrectUpdateTable` with `leaveUpdateTableInQuery: true`. Throws if the update has `LIMIT`/`TOP`. `AccessSqlOptimizer.cs:75-87`.

`Finalize` wraps all NULL/parameter top-level SELECT columns in `CVar(...)` via `WrapParameters` to avoid an Access ODBC driver type-reporting bug (returns 0 for type of NULL/parameter columns). `AccessSqlOptimizer.cs:36-73`.

## Mapping schema hierarchy

`AccessMappingSchema` (base) -> `AccessOleDbMappingSchema` (GUID format override) -> four leaf schemas one per `(version, provider)` combination. `AccessMappingSchema.cs:107-114`. Date literals, string concatenation via `+` with `chr(N)` for non-printable chars, binary as `0x...` hex.

## Bulk copy strategy

`AccessBulkCopy` extends `BasicBulkCopy` with Access-specific limits:
- Max parameters per batch: 767 (Access limit per query). `AccessBulkCopy.cs:9`.
- Max SQL length: 64 000 characters (Access specification). `AccessBulkCopy.cs:13`.
- Strategy: multi-row `INSERT` only (`BulkCopyType.MultipleRows` default). No native bulk-copy API.

## Schema provider divergence (OLE DB vs ODBC)

| Feature | OLE DB (`AccessOleDbSchemaProvider`) | ODBC (`AccessODBCSchemaProvider`) |
|---|---|---|
| Foreign keys | Via `GetOleDbSchemaTable(Foreign_Keys)` | Not available (runtime bug, returns `[]`) |
| Primary keys | Via `GetSchema("Indexes")` filtered by `PRIMARY_KEY` | Not available (runtime bug, returns `[]`) |
| Views | In `GetSchema("Tables")` as `TABLE_TYPE=VIEW` | Separate `GetSchema("Views")` table |
| Procedures | `GetSchema("Procedures")` with definition text; params parsed from regex on `PROCEDURE_DEFINITION` | `GetSchema("Procedures")` + `GetSchema("ProcedureParameters")` |
| Identity | Always `false` (OLE DB reports INT NOT NULL as identity -- issue #3149) | Detected by `TYPE_NAME = "COUNTER"` |
| Separate connection | Opens new `DataConnection` per `GetSchema` call to workaround provider bug | Uses current connection |
| Procedure schema | `ExecuteReader(CommandBehavior.KeyInfo)` -> `GetSchemaTable()` | `GetSchema("ProcedureColumns", [null, null, name])` |

## DML service (`AccessDmlService`)

`AccessDmlService` overrides only `IsTableNotFoundExceptionCore`. It covers both driver paths:
- OLE DB: `OleDbException` with HResult `0x80040E37` (`DB_E_NOTABLE`) or message containing "does not exist". `AccessDmlService.cs:13-17`.
- ODBC: `OdbcException` with SQLSTATE `42S02` or message containing "does not exist". `AccessDmlService.cs:20-23`.

This is not a multi-statement splitter -- Access commands execute one statement at a time at the ADO.NET level; the DML service here only concerns table-existence detection for create-table-if-not-exists patterns.

## Member translation (`AccessMemberTranslator` / `AccessJetMemberTranslator`)

`AccessMemberTranslator` (used for ACE and as base for JET):

### Date/time

- **DatePart**: `DatePart("x", e)` for all parts except millisecond (returns `null`). `AccessMemberTranslator.cs:50-75`.
- **DateAdd**: `DateAdd("x", n, e)` for year/quarter/month/day/week/hour/minute/second. `AccessMemberTranslator.cs:82-105`.
- **MakeDateTime (date-only)**: `DateSerial(y, m, d)`. `AccessMemberTranslator.cs:122-124`.
- **MakeDateTime (with time, no millisecond)**: string-concat of zero-padded parts cast to `DateTime`. Millisecond must evaluate to 0 at compile time or the call returns `null`. `AccessMemberTranslator.cs:126-179`.
- **DateTime.Date truncation**: `CAST(expr AS Date)` -- emits a direct SQL `CAST` to the `Date` data type. `AccessMemberTranslator.cs:182-188`. Added in PR #5517.
- **DateTime.TimeOfDay**: `TimeValue(expr)`. `AccessMemberTranslator.cs:190-195`.
- **Now / DateTime.Now**: `TranslateNow` and `TranslateServerNow` both emit the no-arg `Now` function. `AccessMemberTranslator.cs:198-208`. `TranslateServerNow` delegates directly to `TranslateNow` -- no separate server-clock override.
- **UTC now / DateTimeOffset.UtcNow**: `TranslateZonedNow` also emits `Now` (Access has no UTC clock function; `DateTimeOffset` "zoned now" is silently local time). `AccessMemberTranslator.cs:210-213`. Added/clarified in PR #5467.

### Math

- **Round (banker's, precision 0)**: `TranslateRoundToEven` builds `IIF(Abs(v*10 Mod 10) = 5 And Int(v) Mod 2 = 0, Int(v), Round(v))` per its source comment (`AccessMemberTranslator.cs:226-231`), but the generated `isEven` predicate at `AccessMemberTranslator.cs:239` literally compares `Int(v) Mod 2` to the value `2`, not `0` -- see Known issues / debt.
- **Round (away-from-zero, precision 0)**: `IIF(v >= 0, Int(v + 0.5), Int(v - 0.5))`. `AccessMemberTranslator.cs:266-283`.
- **Round (away-from-zero, non-zero precision)**: `Int(v * 10^p + IIF(v >= 0, 0.5, -0.5)) / 10^p`. `AccessMemberTranslator.cs:285-315`.
- **Pow**: `x ^ y` binary expression; `decimal` base is cast to `double` first. `AccessMemberTranslator.cs:318-346`.

### String

- **LPad**: `String(n, char) + value`. `AccessMemberTranslator.cs:368-383`.
- **Join**: `Mid`-based concat-with-separator emulation via `AggregateFunctionBuilder` + `ConfigureConcatWsEmulation` when a separator is present; the no-separator overload (e.g. `string.Concat`-style Join) instead configures plain `ConfigureConcat(wrapByCoalesce: true)`. `AccessMemberTranslator.cs:385-407`.
- **TrimStart / TrimEnd (whitespace-only)**: delegates to `StringMemberTranslatorBase`; returns `null` when a `trimChars` argument is present (custom char-set trim is unsupported). `AccessMemberTranslator.cs:352-366`. Added in PR #5515.
- **IsNullOrWhiteSpace**: `{value} IS NULL OR LTRIM({value}) = ''` -- uses Access `LTRIM` (space-only trim; full Unicode whitespace handling is not available without REPLACE chains). `AccessMemberTranslator.cs:412-421`.

### GUID

- **ToString()**: `IIF(IsNull(g), NULL, LCase(Mid(CStr(g), 2, 36)))`. Explicit null-guard via `IIF` because Access `CStr(NULL)` throws Invalid use of Null at the ODBC layer rather than propagating NULL. Jet/ACE SQL `IIF` short-circuits (only evaluates the selected branch), so the false branch `CStr(g)` is skipped when `g IS NULL`. `AccessMemberTranslator.cs:426-449`.

### Aggregates

- `COUNT DISTINCT` and aggregation `DISTINCT` both unsupported (`IsCountDistinctSupported = false`, `IsAggregationDistinctSupported = false`). `AccessMemberTranslator.cs:452-456`.

### Window functions

`CreateWindowFunctionsMemberTranslator` returns a nested `AccessWindowFunctionsMemberTranslator : WindowFunctionsMemberTranslator` overriding `IsWindowFunctionsSupported => false`, consistent with `SqlProviderFlags.IsWindowFunctionsSupported = false` set in the provider constructor. `AccessMemberTranslator.cs:458-466`.

`AccessJetMemberTranslator` overrides string translator: `TranslateReplace` returns `null` (JET has no `REPLACE`). `AccessJetMemberTranslator.cs:17-21`.

## Inbound / outbound dependencies

**Inbound** (callers depend on this area):
- `AccessTools` is the public entry point for consumer code and for `AccessFactory` (configuration-driven resolution).
- Test infrastructure (`Tests/Linq/`) uses `AccessTools.CreateDataConnection`.

**Outbound** (this area depends on):
- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) -- `BasicSqlBuilder`, `BasicSqlOptimizer`, `SqlExpressionConvertVisitor`, `WrapParametersVisitor`.
- [INTERNAL-API](../INTERNAL-API/INDEX.md) -- `DynamicDataProviderBase`, `ProviderDetectorBase`, `BasicBulkCopy`, `DmlServiceBase`, `MemberTranslatorBase`, `ProviderMemberTranslatorDefault`, `UpsertBuilder` (Upsert-with-merge-lowering gating via `IsUpsertWithMergeLoweringSupported`).
- [MAPPING](../MAPPING/INDEX.md) -- `LockedMappingSchema`, `MappingSchema`.
- [METADATA](../METADATA/INDEX.md) -- `SchemaProviderBase`.
- Framework adapters: `OleDbProviderAdapter`, `OdbcProviderAdapter` -- both defined in `Internal/DataProvider/` root (INTERNAL-API area).

## Known issues / debt

1. **OLE DB identity reporting bug (issue #3149)**: `AccessOleDbSchemaProvider.GetColumns` always sets `IsIdentity = false` because the OLE DB provider incorrectly flags all `INT NOT NULL` columns as identity. `AccessOleDbSchemaProvider.cs:138`.

2. **ODBC FK and PK gaps**: `AccessODBCSchemaProvider.GetForeignKeys` and `GetPrimaryKeys` return empty collections due to a .NET runtime bug (`dotnet/runtime#35442`). Schema scaffolding produces no FK/PK metadata over ODBC. `AccessODBCSchemaProvider.cs:26-29`, `:70-73`.

3. **`EscapeLikeCharacters` throws for dynamic patterns**: A `TODO` comment in `AccessSqlExpressionConvertVisitor.cs:49` notes ACE does have `REPLACE`, so dynamic LIKE escaping could be supported for ACE but is not yet implemented.

4. **`DeleteIfExists` overload on `CreateDatabase` deprecated**: The `string provider` overload is marked `[Obsolete]` for removal in v7. `AccessTools.cs:102`.

5. **`IsParameterOrderDependent = true` applies to both drivers**: The comment at `AccessDataProvider.cs:52-55` acknowledges OLE DB has complex-query parameter order bugs; a stricter per-driver flag (`should be: provider == ODBC`) was traded for a blanket `true`.

6. **OLE DB `GetOleDbSchemaTable` hard-crash risk**: The comment at `AccessOleDbSchemaProvider.cs:52-55` notes this call can trigger an unhandled native AV (issue #23 in linq2db.LINQPad). No mitigation is possible in managed code.

7. **UTC now silently returns local time**: `TranslateZonedNow` in `AccessMemberTranslator.cs:210-213` emits `Now` for `DateTimeOffset.UtcNow`/zoned-now queries because Access has no UTC clock function. Callers expecting UTC semantics get local system time instead. No warning or fallback is emitted. Added in PR #5467.

8. **`TranslateRoundToEven`'s `isEven` predicate compares against the wrong literal**: the source comment at `AccessMemberTranslator.cs:226-230` documents the tie-break as `Int(v) Mod 2 = 0`, but the generated predicate at `AccessMemberTranslator.cs:239` is `factory.Equal(factory.Mod(intCast, factory.Value(2)), factory.Value(2))` -- comparing to the literal `2`, a value `Mod 2` never produces. The `Int(v)` tie-break branch is therefore never selected; execution always falls through to the `Round(v)` false-branch. Likely benign in practice -- Access/Jet's native `Round()` already implements round-half-to-even -- but the custom tie-break code path is dead. Not confirmed against a failing test; flagged for `kb-issue-detector` triage.

## See also

- [SQL-PROVIDER index](../SQL-PROVIDER/INDEX.md) -- `BasicSqlBuilder`, optimizer base.
- [INTERNAL-API index](../INTERNAL-API/INDEX.md) -- `DynamicDataProviderBase`, `ProviderDetectorBase`, `DmlServiceBase`.
- [MAPPING index](../MAPPING/INDEX.md) -- `LockedMappingSchema`.
- [METADATA index](../METADATA/INDEX.md) -- `SchemaProviderBase`.

## Files (Tier 1 / Tier 2)

### Tier 1 (11 files)

| File | Role |
|---|---|
| `Internal/DataProvider/Access/AccessDataProvider.cs` | Abstract provider base; 4 concrete subclasses; `SqlProviderFlags` incl. Upsert-merge-lowering gate; dispatch |
| `Internal/DataProvider/Access/AccessSqlBuilderBase.cs` | SQL generation base; TOP/IIF/IDENTITY/UPDATE/JOIN/parameter quirks |
| `Internal/DataProvider/Access/AccessSqlOptimizer.cs` | Statement rewriting pipeline |
| `DataProvider/Access/AccessTools.cs` | Public entry point; `CreateDatabase` via ADOX |
| `DataProvider/Access/AccessVersion.cs` | Engine version enum |
| `DataProvider/Access/AccessProvider.cs` | ADO.NET driver family enum |
| `DataProvider/Access/AccessOptions.cs` | Provider options record |
| `Internal/DataProvider/Access/AccessProviderAdapter.cs` | OleDb/ODBC adapter unifier |
| `Internal/DataProvider/Access/AccessProviderDetector.cs` | Auto-detection from connection string / config |
| `Internal/DataProvider/Access/AccessMappingSchema.cs` | Date/string/decimal/GUID literal converters; FromDatabase-scoped string->numeric/bool parsers |
| `Internal/DataProvider/Access/AccessBulkCopy.cs` | Multi-row INSERT with Access parameter/length caps |

### Tier 2 (16 files, all visited)

| File | Role |
|---|---|
| `Internal/DataProvider/Access/AccessOleDbSqlBuilder.cs` | OLE DB concrete builder |
| `Internal/DataProvider/Access/AccessODBCSqlBuilder.cs` | ODBC concrete builder; `?` parameters; GUID->parameter |
| `Internal/DataProvider/Access/AccessSqlExpressionConvertVisitor.cs` | Expression rewrites (functions, LIKE, COALESCE->IIF with NULL-strip, casts, bitwise); `SupportsNullIf = false` |
| `Internal/DataProvider/Access/AccessDmlService.cs` | Table-not-found exception detection |
| `Internal/DataProvider/Access/AccessSchemaProviderBase.cs` | Shared schema provider base; data-type map |
| `Internal/DataProvider/Access/AccessOleDbSchemaProvider.cs` | OLE DB schema introspection |
| `Internal/DataProvider/Access/AccessODBCSchemaProvider.cs` | ODBC schema introspection |
| `Internal/DataProvider/Access/Translation/AccessMemberTranslator.cs` | ACE/base member translations; whitespace-only TrimStart/TrimEnd (PR #5515); IsNullOrWhiteSpace via LTRIM; GUID null-guard IIF; Join no-separator branch; nested window-translator override |
| `Internal/DataProvider/Access/Translation/AccessJetMemberTranslator.cs` | JET override (no REPLACE) |
| `Internal/DataProvider/Access/AccessSpecificQueryable.cs` | Internal implementation of `IAccessSpecificQueryable<T>` |
| `Internal/DataProvider/Access/AccessSpecificTable.cs` | Internal implementation of `IAccessSpecificTable<T>` |
| `DataProvider/Access/AccessHints.cs` | `WITH OWNERACCESS OPTION` hint |
| `DataProvider/Access/AccessSpecificExtensions.cs` | `AsAccess<T>` extension methods |
| `DataProvider/Access/IAccessSpecificQueryable.cs` | Public marker interface |
| `DataProvider/Access/IAccessSpecificTable.cs` | Public marker interface |
| `DataProvider/Access/AccessFactory.cs` | Configuration-driven factory |

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 11 / 11
- Tier 2 (visited / total): 16 / 16 (100%)
- Tier 3 (skipped, logged): 0

Read (prior run -- delta sha 4a478ff14):
- `AccessDataProvider.cs` -- no structural changes
- `AccessMappingSchema.cs` -- no structural changes
- `Translation/AccessMemberTranslator.cs` -- PR #5467 explicit `TranslateNow`/`TranslateServerNow`/`TranslateZonedNow` (all emit `Now`); PR #5517 `TranslateDateTimeTruncationToDate` emits `CAST(expr AS Date)`; non-zero precision `TranslateRoundAwayFromZero` implementation visible.

Read (this run -- delta sha 2e67bafc9):
- `AccessMappingSchema.cs` -- PR #5520 / issue #5519: four `SetConvertExpression` string-parser converters (`decimal.Parse`, `float.Parse`, `double.Parse`, `v == "-1"`) now carry `conversionType: ConversionType.FromDatabase`; added explanatory comment block (lines 44-53) documenting the write-side parameter-prep hazard that motivated the change; pragma block extended to include `RS0030` for the banned `Parse` overloads.
- `Translation/AccessMemberTranslator.cs` -- PR #5515: `AccessStringMemberTranslator` gained `TranslateTrimStart` and `TranslateTrimEnd` overrides (lines 352-366); both return `null` when `trimChars != null` (custom char-set trim unsupported on Access); whitespace-only trim delegates to `StringMemberTranslatorBase`. No other structural changes.

Read (this run -- delta sha b3340aa9):
- `AccessOptions.cs` -- no structural changes; same single `BulkCopyType` property and `DataProviderOptions<AccessOptions>` base.
- `AccessDataProvider.cs` -- additional `SqlProviderFlags` now documented: `IsSubQueryOrderBySupported = false`, `IsUnionAllOrderBySupported = true`, `DefaultNullsOrdering = NullsDefaultOrdering.Smallest`, `IsDistinctSetOperationsSupported = false`, `IsOrderByAggregateSubquerySupported = false`, `CalculateSupportedCorrelatedLevelWithAggregateQueries = true`, `DefaultMultiQueryIsolationLevel = IsolationLevel.Unspecified`, `SupportsPredicatesComparison = true`, `IsUpdateFromSupported = false`, `AcceptsTakeAsParameter = false`, `IsSupportsJoinWithoutCondition = false`; all added to SqlProviderFlags section.
- `AccessProviderDetector.cs` -- no structural changes; detection logic unchanged.
- `AccessSqlExpressionConvertVisitor.cs` -- `SupportsNullIf = false` override added (prevents NULLIF emission); `ConvertCoalesce` now calls `RemoveNullValues` before IIF folding (issue #5531 fix, prevents `Coalesce(x, NULL)` -> `IIF(x IS NULL, NULL, x)` no-op round-trip); `ConvertSearchStringPredicate` uses `SqlSearchCondition` with `canBeUnknown: null`; `ConvertConversion` uses `ParametersNullabilityType.NotNullable` for type-cast function calls.
- `Translation/AccessMemberTranslator.cs` -- `GuidMemberTranslator.TranslateGuildToString` now wraps result in explicit `IIF(IsNull(g), NULL, LCase(Mid(CStr(g), 2, 36)))` null-guard (Access `CStr(NULL)` throws at ODBC layer; Jet/ACE SQL IIF short-circuits so false branch is skipped when predicate is true); `AccessStringMemberTranslator.TranslateIsNullOrWhiteSpace` added (emits `{v} IS NULL OR LTRIM({v}) = ''` using Access space-only LTRIM); `TranslateDateTimeTruncationToTime` delegates to `TimeValue(expr)` (confirms existing INDEX.md claim).

Read (this run -- delta sha 36ee4f82f):
- `AccessDataProvider.cs` -- new `SqlProviderFlags.IsUpsertWithMergeLoweringSupported = false` (`AccessDataProvider.cs:43-45`), part of a cross-provider Upsert-with-merge-lowering capability (same flag also `false` on MySQL, PostgreSQL, SAP HANA, SQLite, SqlCe, SQL Server; gated in `UpsertBuilder.cs:262` via `ErrorHelper.Error_Upsert_MergeLowering_NotSupported`); this insertion shifted the constructor's `SqlProviderFlags` block to lines 37-67, the `IsParameterOrderDependent` comment to lines 52-55, and the OLE DB/ODBC `Decimal`/`VarNumeric` -> `AnsiString` branches in `SetParameterType` to lines 201-202 / 237-240 -- citations updated accordingly. No other structural changes.
- `Translation/AccessMemberTranslator.cs` -- `TranslateStringJoin` now branches on `withoutSeparator`: plain `ConfigureConcat(wrapByCoalesce: true)` when there's no separator, `ConfigureConcatWsEmulation` (unchanged Mid-based emulation) otherwise -- previously only the separator path was documented. Confirmed nested `AccessWindowFunctionsMemberTranslator : WindowFunctionsMemberTranslator` (`IsWindowFunctionsSupported => false`, lines 458-466), not previously called out. While verifying `TranslateRoundToEven` (unchanged logic, re-read in full per Tier-2 scan), found the `isEven` predicate at line 239 compares against the literal `2` instead of `0`, contradicting both the adjacent source comment and this file's prior "Round (banker's)" claim -- claim corrected in place, discrepancy logged as Known issues / debt item 8 and flagged via AUDIT-NOTE. Also corrected stale citations discovered during the full re-read: `LPad` (`352-367` -> `368-383`) and `Aggregates`/`AccessAggregateFunctionsMemberTranslator` (`405-409` -> `452-456`), both drifted from earlier additive PRs (#5515 etc.) that were never reflected in these two citations.

</details>
