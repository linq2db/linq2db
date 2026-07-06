---
area: PROV-SYBASE
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-07-05
last_verified_sha: 36ee4f82f06eaf242b052ade8c87121d251a6165
coverage_tier_1: 10/10
coverage_tier_2: 6/6
---

# PROV-SYBASE -- SAP ASE (Sybase Adaptive Server Enterprise)

SAP Adaptive Server Enterprise (formerly Sybase ASE). **Not** SQL Anywhere. Single dialect at the linq2db level -- no version axis in provider names. Historical T-SQL heritage shared with Microsoft SQL Server (see [PROV-SQLSERVER](../PROV-SQLSERVER/INDEX.md)), but with meaningful divergences in identifier limits, type system, and paging.

---

## Subsystems

### Public surface

 -- static facade for registration and connection creation. Delegates detection to . Exposes , ,  overloads (string / DbConnection / DbTransaction), and  /  for assembly-resolver injection.


 enum -- three values:
-  -- let the detector pick at runtime.
-  -- SAP SDK driver ().
-  -- open-source managed driver (, DataAction org on GitHub).


 -- single-field record (). Default is , **not** , because the SAP native driver has known bugs: wrong  inserted for the first-record BIT field and an exception when identity columns are present during bulk copy. The record comment documents this explicitly.


 --  subclass. Resolves assembly name from configuration attributes to one of the three  enum values and calls . Tagged  -- loaded by the configuration infrastructure via reflection.


### Data provider

 -- abstract, extends . Two concrete sealed subclasses in the same file:
-  (, )
-  (, )

 configuration set in the constructor:
-  --  is an inline literal, not a parameter.
-  -- no native OFFSET; no paging beyond TOP.
- 
- 
- 
-  -- TODO comment notes possible 16SP3 enablement.
- 
-  -- ASE sorts NULL as the smallest value (ascending => NULL first, descending => NULL last).
- 
- 
-  --  is valid.
-  -- BIT cannot be NULL (see below).
- 
- 
- 
- `SqlProviderFlags.IsInsertOrUpdateWithPredicateSupported = false` -- Sybase's `InsertOrUpdate` emits a single-statement `UPDATE` followed by `IF @@ROWCOUNT=0 INSERT`, which cannot honor an extra UPDATE predicate (`Upsert.Update.When`); predicated upserts on this provider fall back to the alternative UPDATE-then-INSERT emulation instead (`Source/LinqToDB/Internal/DataProvider/Sybase/SybaseDataProvider.cs:58-61`).


 overrides handle: , , , ,  (native client only), .


 -- native client () unwraps nullable types, maps ,  (because  rejects them).


Provider fields:  is read back as  (offset zero);  is read back as ; -typed columns go through  which strips the 1900-01-01 date part when present.


 -- supports temporary table prefix conventions ( = local,  = global), , .


### ADO.NET adapter

Loads one of two assemblies at runtime via reflection (). Both expose the same  type names under different namespaces:
- Native:  / namespace  / factory name .
- Managed:  / namespace  / no  name (null).

 enum (wrapped) -- 35 values including  (-10),  (-8),  (-9),  (-201),  (-203),  (-202),  (-4),  (-1). These are mapped in .


 -- wraps  and . **Only created for the native driver** (). The managed DataAction driver does not expose ;  property is  for managed instances.


Singletons protected by  (one per driver variant): , .


### Provider detection

 checks  string, then  for Managed/Native substrings, then falls back to probing the assembly directory for . If the file exists -> ; otherwise ->  (managed).


 always re-invokes  -- the  parameter is ignored for .

### SQL builder ()

Extends . No intermediate base class (unlike, e.g., PROV-ORACLE which has a ).

Key overrides:
-  -- returns . ASE 15.7+ supports ANSI  alongside T-SQL ; the builder emits  for consistency with other ANSI-pipe providers. Both operators share the same NULL-propagation semantics on ASE, so the  CASE WHEN wrap in  is unaffected.  remains  (inherited).
  
-  -- returns . No OFFSET clause.
-  -- emits  after insert.
-  -- ASE  (like ) does **not** propagate NULL. When  and there are multiple operands, wraps in ; otherwise delegates to base. The AST stays a plain  -- the NULL-guard is injected at text-emit time, not in the convert visitor. (PR #5504)
  
-  -- returns ; ASE allows column aliases inside  source expressions.
  
-  -- , , , ,  capped at 5461 (ASE page-size limit),  uses explicit  syntax.
-  -- suppresses NULL/NOT NULL for BIT fields (BIT cannot be nullable in ASE).
-  -- emits  form.
-  -- correctly targets the update table when joined.
-  -- delegates to  (no native MERGE upsert idiom for this path).
-  -- emits  keyword.
-  -- emits .
-  /  /  -- two-command truncate:  followed by  to reset the identity seed.
-  -- uses  guard (no  syntax).
-  /  -- for non-temporary tables with : wraps the DDL in .
-  -- falls back to the fallback implementation.


:
- Parameter names (, , ): truncated to **26 characters** (hard limit in ), prefixed with .
- Identifiers (, , , , , , ): quoted in  unless , length > 28, already bracketed, or is a temp-table (-prefixed) name.


 flag -- when building a SELECT subbuilder, column aliases are suppressed to avoid Sybase alias-resolution quirks in nested contexts.


 -- forces explicit type cast for the first row of a VALUES table when the value is , , , , , , or . Complemented by  which corrects DECIMAL facets for typed expressions.


Temporary table name handling --  prefixes names with  (local) or  (global) based on . Database qualifier is stripped for temp tables.


**Merge partial** ():
-  -- ASE MERGE does not support the  source syntax.
-  /  -- wraps identity-insert-enabled targets with  around the MERGE.


### SQL optimizer

Extends .  instantiates .

:
- Rejects  and  (raises ).
- Rejects .
- For UPDATE: calls  -- if the query is incompatible (contains subqueries or self-join of the update table) it promotes to ; otherwise tries  or falls back to .


 applies  before calling the base.

### Expression convert visitor

Extends .

 -- . The  bracket negation character is included (absent in most other providers).

:
- 
- 
-  (3-arg form) ->  -- emulates positional search.
-  (empty replacement string) ->  -- workaround for ASE Stuff semantics.


 -- wraps EXISTS subqueries that have set operators (/etc.) in an outer  to make them valid.

 -- adds mandatory  for untyped numeric literals and parameters (, , , , , ) in column position to prevent type inference errors.


### Mapping schema

Base  keyed on . Two sub-schemas:
-  -- keyed on .
-  -- keyed on .

Converters registered:
-  -- concatenation-based string literal builder using  for non-ASCII.
-  -- same pattern.
-  -- inline as  literal (or ticks for Int64 targets).
-  /  --  hex prefix.
-  -- formatted as  via .
-  --  extracted and formatted identically to  via .


Default type mappings:
- 
-  (note: ASE server default is  but linq2db uses 10-digit scale by default for .NET )
-  default value ->  (ASE  lower bound).


### Parameters normalizer

Extends  with .

The  base strips non-ASCII non-alphanumeric-non-underscore characters, enforces first-character as ASCII letter, deduplicates via  suffix, and truncates to . For Sybase, parameter names are capped at 26 characters before the  prefix is added by . The normalizer truncates the internal name to 26;  also independently truncates its value to 26 before prepending  -- ensuring the final  form stays at or under 27 characters.



No reserved-word override is present ( stays at the base  implementation). Identifier quoting via  in the SQL builder is the defense against reserved words in field/table names.

### Bulk copy

Extends . Limits: ,  (conservative, from SAP documentation).

Strategy:
-  uses  (via ) **only** when  (i.e., native driver) **and** the target table is not a temp table. The comment cites an ASE bug where native bulk copy fails on temp tables with a syntax error.
- Falls back to  ->  (multi-row  batches with empty separator) when the conditions are not met.
- The  path does **not** escape table/column names passed to  and column mappings, as ASE bulk copy chokes on bracketed identifiers.


Transaction support: if a transaction is active, the provider transaction is extracted. A comment notes a Stack Overflow reference about  creating a temp table during bulk copy -- the transaction must be passed through to avoid issues.


### Schema provider

Uses ASE system tables directly -- not INFORMATION_SCHEMA:
-  -- tables/views.
-  +  -- column info, including  for nullability,  for identity.
-  +  -- primary key detection via  (unique) and  (PK).
-  +  -- foreign keys; UNION-based loop over up to 16 key columns.


Stored procedures: uses  system procedure. Parameters: uses . Procedure schema inspection uses ; the managed DataAction driver requires a different code path (workaround for  issue #189).


Unicode size correction:  and  are read once via  and used to convert byte-lengths to character-lengths for / and / columns respectively.


 returns a manually constructed list (both native and managed) -- the native provider's  returns incomplete information. Includes ASE-specific extended types: , , , , , , , .


 -- cannot be called inside a transaction; will throw  with a message directing the caller to disable  or remove the transaction.

### Member translator

Extends . Sub-translators:
-  -- maps  and  SQL types to .
-  -- , . Now-translation (PR #5467) uses the five-virtual split:
  -  ->  (emits server local time as ).
  -  -> returns  (falls back to client-side evaluation; no server-side  equivalent on ASE).
  -  ->  (returns ).
  -  ->  (same function,  result type passed through from caller).
  -  -- not overridden; inherits base behavior (no translation / null).
   constructs via string concatenation with  padding and a final .  uses  -- the result type is taken from the source expression existing  via  (PR #5517: preserves column  rather than forcing a fixed date type).
-  --  emulated via  pattern (no native  or ).  branch (PR #5504): calls  directly, bypassing the SUBSTRING-emulation path.  and  (PR #5515): return  when  -- ASE has no native trim-with-chars support; only the no-arg (whitespace-only) form delegates to the base.
  
-  --  overloads force explicit  precision when none provided.
-  --  -> .
-  ->  (non-pure function).
- `CreateWindowFunctionsMemberTranslator` override returns `SybaseWindowFunctionsMemberTranslator`, a `WindowFunctionsMemberTranslator` subclass with `IsWindowFunctionsSupported => false` (PR #5468, the `Sql.Window` API) -- reinforces the constructor-level `SqlProviderFlags.IsWindowFunctionsSupported = false` flag so `Sql.Window.*` LINQ calls fall back to client-side evaluation consistently (`Source/LinqToDB/Internal/DataProvider/Sybase/Translation/SybaseMemberTranslator.cs:299-307`).


---

## ASE-specific SQL quirks (summary)

| Quirk | Detail | Source |
|---|---|---|
| Paging |  only; no OFFSET/FETCH |  |
| IDENTITY retrieval |  after INSERT |  |
| BIT nullability | BIT column cannot be NULL in ASE; NULL attribute suppressed |  |
| NULL ordering |  -- NULL sorts first ascending, last descending |  |
| Parameter length | Max 26 chars (before  prefix) | ,  |
| Identifier length | Max 28 chars before quoting is skipped |  |
| NVarChar max | 5461 chars (page limit) |  |
| CreateIfNotExists | Via IF OBJECT_ID guard + EXECUTE wrapper |  |
| LIKE escape | , , , ,  |  |
| MERGE identity |  wrapping |  |
| Bulk copy native limit | Only non-temp tables; unescaped names | ,  |
| System tables | , , ,  |  |
| Truncate + identity | Two commands:  +  |  |
| DateTime lower bound | 1753-01-01 |  |
| DateTime.Now | Returns null (client-side only; no ASE server-local equivalent) |  |
| DateTimeOffset literal | Offset stripped; stored/emitted as  | ,  |
| String concat style |  (ANSI pipe concat); ASE 15.7+; NULL-propagation guard still needed |  |
| String concat NULL | pipe does not propagate NULL;  wrapped in  |  |
| TrimStart/TrimEnd chars | Custom trim-chars unsupported; returns null (no translation) |  |
| InsertOrUpdate predicate | `Upsert.Update.When` unsupported in the single-statement UPDATE+INSERT emulation; falls back to UPDATE-then-INSERT emulation | `Source/LinqToDB/Internal/DataProvider/Sybase/SybaseDataProvider.cs:61` |

---

## Key types

| Type | File | Role |
|---|---|---|
| SybaseDataProvider | Internal/.../SybaseDataProvider.cs | Abstract base; SybaseDataProviderNative / SybaseDataProviderManaged are the concrete singletons; IsInsertOrUpdateWithPredicateSupported=false forces UPDATE-then-INSERT emulation for predicated upserts |
| SybaseSqlBuilder | Internal/.../SybaseSqlBuilder.cs (+.Merge.cs) | SQL text generation; T-SQL/ASE dialect; ConcatBuildStyle.Pipes; SqlConcatExpression NULL-propagation guard (PR #5504) |
| SybaseSqlOptimizer | Internal/.../SybaseSqlOptimizer.cs | Statement rewrites (UPDATE compatibility, CorrectMultiTableQueries) |
| SybaseSqlExpressionConvertVisitor | Internal/.../SybaseSqlExpressionConvertVisitor.cs | Function name mapping, LIKE escapes, EXISTS wrapping, column type casts |
| SybaseMappingSchema | Internal/.../SybaseMappingSchema.cs | Type defaults, literal converters (string/char/TimeSpan/binary/DateTime/DateTimeOffset); sub-schemas for native/managed |
| SybaseProviderAdapter | Internal/.../SybaseProviderAdapter.cs | Runtime ADO.NET type loading; AseDbType enum; BulkCopyAdapter (native only) |
| SybaseProviderDetector | Internal/.../SybaseProviderDetector.cs | Auto-detect native vs managed driver |
| SybaseBulkCopy | Internal/.../SybaseBulkCopy.cs | AseBulkCopy (native) or MultipleRowsCopy2 fallback |
| SybaseSchemaProvider | Internal/.../SybaseSchemaProvider.cs | ASE system-table queries, procedure discovery |
| SybaseParametersNormalizer | Internal/.../SybaseParametersNormalizer.cs | 26-char parameter name truncation |
| SybaseMemberTranslator | Internal/.../Translation/SybaseMemberTranslator.cs | .NET member -> ASE SQL function translation; Now-split into 5 virtuals (PR #5467); TrimStart/TrimEnd char-guard (PR #5515); String.Join withoutSeparator (PR #5504); window functions disabled via SybaseWindowFunctionsMemberTranslator (PR #5468) |
| SybaseTools | DataProvider/Sybase/SybaseTools.cs | Public registration/connection API |
| SybaseProvider | DataProvider/Sybase/SybaseProvider.cs | ADO.NET client enum (AutoDetect, Unmanaged, DataAction) |
| SybaseOptions | DataProvider/Sybase/SybaseOptions.cs | BulkCopyType option (default MultipleRows due to native driver bugs) |
| SybaseFactory | DataProvider/Sybase/SybaseFactory.cs | Configuration-system factory, loaded via reflection |


---

## Files (Tier 1 / Tier 2)

**Tier 1** (10 files, all read in full):

| File | Notes |
|---|---|
| Internal/DataProvider/Sybase/SybaseDataProvider.cs | Provider flags, parameter handling, bulk copy dispatch; DefaultNullsOrdering = NullsDefaultOrdering.Smallest; IsInsertOrUpdateWithPredicateSupported=false (predicated upsert -> UPDATE-then-INSERT emulation) |
| Internal/DataProvider/Sybase/SybaseSqlBuilder.cs | SQL text builder, identifier/parameter quoting; ConcatBuildStyle.Pipes; BuildSqlConcatExpression NULL-guard (PR #5504); SupportsColumnAliasesInSource (PR #5504) |
| Internal/DataProvider/Sybase/SybaseSqlOptimizer.cs | Statement rewrite, UPDATE/DELETE guard |
| Internal/DataProvider/Sybase/SybaseMappingSchema.cs | Type defaults and literal converters |
| Internal/DataProvider/Sybase/SybaseProviderAdapter.cs | ADO.NET wrapper, AseDbType, bulk copy adapter |
| Internal/DataProvider/Sybase/SybaseProviderDetector.cs | Auto-detect native vs managed |
| Internal/DataProvider/Sybase/SybaseBulkCopy.cs | Bulk copy strategy |
| DataProvider/Sybase/SybaseTools.cs | Public entry point |
| DataProvider/Sybase/SybaseProvider.cs | ADO.NET client enum |
| DataProvider/Sybase/SybaseOptions.cs | Provider options |

**Tier 2** (6 files, all read in full):

| File | Notes |
|---|---|
| Internal/DataProvider/Sybase/SybaseSqlBuilder.Merge.cs | MERGE partial -- identity insert wrapping, no VALUES syntax |
| Internal/DataProvider/Sybase/SybaseSqlExpressionConvertVisitor.cs | Function renames, EXISTS wrapping, column type casts, LIKE chars |
| Internal/DataProvider/Sybase/Translation/SybaseMemberTranslator.cs | Date/string/math/Guid LINQ translation; Now-split (PR #5467); Date truncation DbType preservation (PR #5517); TrimStart/TrimEnd char-guard (PR #5515); String.Join withoutSeparator (PR #5504); window-functions disabled via SybaseWindowFunctionsMemberTranslator (PR #5468) |
| Internal/DataProvider/Sybase/SybaseSchemaProvider.cs | System-table schema queries |
| Internal/DataProvider/Sybase/SybaseParametersNormalizer.cs | MaxLength=26 override |
| DataProvider/Sybase/SybaseFactory.cs | Configuration factory |

Tier 3: none identified.

---

## Inbound / outbound dependencies

**Inbound:**
- LinqToDB.DataProvider.Sybase namespace types are part of the public API (SybaseTools, SybaseProvider, SybaseOptions).
- SybaseFactory is loaded via the linq2db configuration infrastructure reflectively.

**Outbound:**
- DynamicDataProviderBase<SybaseProviderAdapter> -- [INTERNAL-API](../INTERNAL-API/INDEX.md)
- BasicSqlBuilder, BasicSqlOptimizer, SqlExpressionConvertVisitor -- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md)
- LockedMappingSchema, SqlDataType -- [MAPPING](../MAPPING/INDEX.md)
- SchemaProviderBase -- [METADATA](../METADATA/INDEX.md)
- ProviderMemberTranslatorDefault, DateFunctionsTranslatorBase, StringMemberTranslatorBase, MathMemberTranslatorBase -- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md)
- SqlConcatExpression (PR #5504) -- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md); SybaseSqlBuilder.BuildSqlConcatExpression consumes the new AST node directly.
- WindowFunctionsMemberTranslator (PR #5468's Sql.Window API) -- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md); SybaseWindowFunctionsMemberTranslator subclasses it with IsWindowFunctionsSupported => false, keeping Sql.Window.* LINQ calls client-side-only, consistent with the SqlProviderFlags-level flag.
- T-SQL dialect heritage shared with [PROV-SQLSERVER](../PROV-SQLSERVER/INDEX.md): TOP, IDENTITY, @@IDENTITY, CONVERT, DatePart, DateAdd, temp-table #/## prefixes, OBJECT_ID() for existence checks, SET IDENTITY_INSERT ON/OFF.


---

## Known issues / debt

- IsDistinctSetOperationsSupported = false and IsWindowFunctionsSupported = false carry a TODO noting potential enablement at ASE 16SP3; no version detection is implemented. The `Sql.Window` API added in PR #5468 is disabled at the translator level too, via `SybaseWindowFunctionsMemberTranslator.IsWindowFunctionsSupported => false`, keeping window-function LINQ calls client-side-only consistent with the `SqlProviderFlags`-level flag.
- SybaseSqlExpressionConvertVisitor.cs:13 has a commented-out SupportsDistinctAsExistsIntersect property guarded by the same SP03 caveat.
- Native driver bulk copy has known bugs with BIT and IDENTITY fields; SybaseOptions.BulkCopyType defaults to MultipleRows as a permanent defensive workaround rather than a version-gated fix.
- GetProcedureParameters throws when called inside a transaction -- a hard limitation of sp_oledb_getprocedurecolumns. No workaround path exists; callers must disable GetSchemaOptions.GetProcedures.
- Managed DataAction driver does not support AseBulkCopy; BulkCopy is null on that adapter, causing silent fallback to multi-row INSERT. Users expecting provider-specific bulk performance must use the native driver.
- MultipleRowsCopy2 uses empty string as the record separator -- intent is inherited from BasicBulkCopy but the comment is absent; may warrant documentation.
- The 26-character parameter-name limit is enforced in two independent places (SybaseParametersNormalizer.MaxLength and SybaseSqlBuilder.Convert). The former is the primary enforcement point; the latter is a belt-and-suspenders guard.
- TranslateNow returns null -- DateTime.Now has no ASE server-side equivalent and falls back to client-side evaluation. This is by design but may surprise callers who expect a server timestamp.
- DateTimeOffset is stored as DateTime (offset stripped) at both the mapping-schema level (literal emission) and the SQL-types-translation level. Round-trip fidelity for non-UTC offsets is lost.
- TranslateTrimStart / TranslateTrimEnd return null for the trimChars != null case -- ASE has no native trim-with-characters function. The LINQ expression will fall back to client-side evaluation. No server-side workaround is provided. (PR #5515)
- `SqlProviderFlags.IsInsertOrUpdateWithPredicateSupported = false` -- ASE's single-statement `UPDATE` + `IF @@ROWCOUNT=0 INSERT` idiom for `InsertOrUpdate` can't honor an `Upsert.Update.When` predicate; predicated upserts fall back to the UPDATE-then-INSERT emulation path instead of the native single-statement form, which costs an extra round trip / statement compared to the un-predicated case.

---

## See also

- [SQL-PROVIDER/INDEX.md](../SQL-PROVIDER/INDEX.md) -- BasicSqlBuilder, BasicSqlOptimizer, SQL AST.
- [INTERNAL-API/INDEX.md](../INTERNAL-API/INDEX.md) -- DynamicDataProviderBase, IDataProvider, IDynamicProviderAdapter.
- [MAPPING/INDEX.md](../MAPPING/INDEX.md) -- LockedMappingSchema, MappingSchema.
- [METADATA/INDEX.md](../METADATA/INDEX.md) -- SchemaProviderBase, ISchemaProvider.
- [PROV-SQLSERVER/INDEX.md](../PROV-SQLSERVER/INDEX.md) -- T-SQL dialect origin; compare TOP, temp tables, IDENTITY, @@IDENTITY, OBJECT_ID checks.

<details><summary>Coverage</summary>

**Tier 1 -- read in full (10/10):**
- Source/LinqToDB/Internal/DataProvider/Sybase/SybaseDataProvider.cs
- Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSqlBuilder.cs
- Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSqlOptimizer.cs
- Source/LinqToDB/Internal/DataProvider/Sybase/SybaseMappingSchema.cs
- Source/LinqToDB/Internal/DataProvider/Sybase/SybaseProviderAdapter.cs
- Source/LinqToDB/Internal/DataProvider/Sybase/SybaseProviderDetector.cs
- Source/LinqToDB/Internal/DataProvider/Sybase/SybaseBulkCopy.cs
- Source/LinqToDB/DataProvider/Sybase/SybaseTools.cs
- Source/LinqToDB/DataProvider/Sybase/SybaseProvider.cs
- Source/LinqToDB/DataProvider/Sybase/SybaseOptions.cs

**Tier 2 -- read in full (6/6):**
- Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSqlBuilder.Merge.cs
- Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSqlExpressionConvertVisitor.cs
- Source/LinqToDB/Internal/DataProvider/Sybase/Translation/SybaseMemberTranslator.cs
- Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSchemaProvider.cs
- Source/LinqToDB/Internal/DataProvider/Sybase/SybaseParametersNormalizer.cs
- Source/LinqToDB/DataProvider/Sybase/SybaseFactory.cs

**Read (this run -- delta, sha 2e67bafc9bfc8ae8ba573b93bde8671d9920c95d):**
- Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSqlBuilder.cs -- new BuildSqlConcatExpression override: emits CASE WHEN guard for PreserveNull=true multi-operand concat; new SupportsColumnAliasesInSource = true override. (PR #5504)
- Source/LinqToDB/Internal/DataProvider/Sybase/Translation/SybaseMemberTranslator.cs -- TranslateTrimStart / TranslateTrimEnd return null when trimChars != null (PR #5515); TranslateStringJoin withoutSeparator=true branch calls ConfigureConcat directly (PR #5504).

**Read (this run -- delta, sha b3340aa9ded15ffc626983fd202e6399daa081ca):**
- Source/LinqToDB/DataProvider/Sybase/SybaseOptions.cs -- no functional change; IEquatable impl and CreateID unchanged.
- Source/LinqToDB/Internal/DataProvider/Sybase/SybaseDataProvider.cs -- new DefaultNullsOrdering = NullsDefaultOrdering.Smallest flag (line 48): ASE sorts NULL as the smallest value (ascending => NULL first, descending => NULL last).
- Source/LinqToDB/Internal/DataProvider/Sybase/SybaseProviderDetector.cs -- no material change vs prior content; GetDataProvider always re-detects, confirmed.
- Source/LinqToDB/Internal/DataProvider/Sybase/SybaseSqlBuilder.cs -- new ConcatStyle => ConcatBuildStyle.Pipes override (line 45): ASE 15.7+ supports ANSI pipe concat alongside T-SQL +; builder now emits pipe for consistency with ANSI providers. NULL-propagation semantics unchanged; PreserveNull CASE WHEN guard in BuildSqlConcatExpression still applies.
- Source/LinqToDB/Internal/DataProvider/Sybase/Translation/SybaseMemberTranslator.cs -- no material change vs prior content; all sub-translators confirmed.

**Read (this run -- delta, sha 36ee4f82f06eaf242b052ade8c87121d251a6165):**
- Source/LinqToDB/Internal/DataProvider/Sybase/SybaseDataProvider.cs -- new `SqlProviderFlags.IsInsertOrUpdateWithPredicateSupported = false` (line 61): Sybase's single-statement `UPDATE` + `IF @@ROWCOUNT=0 INSERT` `InsertOrUpdate` idiom can't honor an `Upsert.Update.When` predicate; predicated upserts route through the alternative UPDATE-then-INSERT emulation.
- Source/LinqToDB/Internal/DataProvider/Sybase/Translation/SybaseMemberTranslator.cs -- new `SybaseWindowFunctionsMemberTranslator : WindowFunctionsMemberTranslator` with `IsWindowFunctionsSupported => false`, wired via a new `CreateWindowFunctionsMemberTranslator` override (lines 299-307, PR #5468); `TranslateNewGuidMethod` also had a no-op refactor (inlined local variable into the return statement, no behavior change).

**Tier 3:** none.

</details>
