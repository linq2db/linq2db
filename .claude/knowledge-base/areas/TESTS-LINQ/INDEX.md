---
area: TESTS-LINQ
kind: area-index
sources: [code]
confidence: medium
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
coverage_tier_1: 4/4
coverage_tier_2: 545/611
---

# TESTS-LINQ

Integration and regression test suite under Tests/Linq/. The largest area in the KB: ~611 files across 23 subdirectories. The area's KB value is taxonomic -- knowing *what feature surface each subdirectory exercises* and *which production area it validates*, not the content of individual fixtures.

All test classes extend TestBase (from Tests.Tools). Provider selection uses [IncludeDataSources(...)] / [DataSources(...)] NUnit parameterized-fixture attributes; TestProvName.* constants identify provider sets. The single assembly [SetUpFixture] is TestsInitialization (root, no namespace).

## Assembly setup

Tests/Linq/TestsInitialization.cs -- NUnit [SetUpFixture], no namespace (intentional). [OneTimeSetUp] runs once per assembly:

- Pre-loads the SQLite native runtime (e_sqlite3) via NativeLibrary.SetDllImportResolver on .NET 8+, or forces a SQLiteConnection construction on .NET Framework to ensure SDS loads before other providers.
- **(updated, issue #5538)** On Linux (.NET 8+), registers a second SetDllImportResolver on IBM.Data.Db2.DB2Connection.Assembly that resolves libdb2.so from <baseDir>/clidriver/lib/libdb2.so -- fixing intermittent CI failures where LD_LIBRARY_PATH does not propagate reliably to the testhost subprocess on CI.
- Registers ActivityHierarchyFactory (debug) or ActivityStatistics.Factory (metrics) via ActivityService.
- Forces ClickHouseOptions.Default with UseStandardCompatibleAggregates = true -- required for test expectations.
- Registers SqlCE provider factory from a hardcoded path on non-NETFX.
- Sets OracleConfiguration.SqlNetAllowedLogonVersionClient = Version11 (enables Oracle 11 protocol with v23 client).
- On NETFX (non-Azure): installs AppDomain.AssemblyResolve handler to resolve IBM.Data.DB2 and IBM.Data.Informix from GAC.
- Calls TestNoopProvider.Init(), SQLiteMiniprofilerProvider.Init(), CustomizationSupport.Init().
- [OneTimeTearDown]: dumps ActivityStatistics.GetReport() and optionally writes metrics baselines via BaselinesWriter.WriteMetrics.

Tests/Linq/AssemblyInfo.TestProgress.cs -- **(new)** assembly-level [assembly: TestProgressReporter] attribute. Opt-in via the LINQ2DB_TEST_PROGRESS environment variable; provides a live heartbeat for long test runs. See .claude/docs/testing.md for monitoring details.
## Subsystems (subdirectory taxonomy)

| Subdirectory | File count | Purpose | Representative files | Production area(s) validated |
|---|---|---|---|---|
| Linq/ | ~179 | Per-LINQ-operator/feature fixtures. Core of the suite. | JoinTests.cs, GroupByTests.cs, CteTests.cs, EagerLoadingTests.cs, WindowFunctionsTests.cs | EXPR-TRANS, SQL-PROVIDER, SQL-AST |
| UserTests/ | ~262 | Issue-numbered regression repros (Issue<N>Tests.cs). | Issue2296Tests.cs, Issue5302Tests.cs, Issue3586Tests.cs | All areas (cross-cutting) |
| Update/ | ~45 | DML operations: Insert, Update, Delete, Merge, BulkCopy, CreateTable, TempTable, TruncateTable, MultiInsert, OutputWithRows. Merge family has 19 partial files (including new ComplexProperty partial). InsertWithOutput/DeleteWithOutput/UpdateWithOutput cover SQL Server, Firebird, PostgreSQL, SQLite, Ydb output-clause support. | MergeTests.cs (+18 partials), BulkCopyTests.cs, InsertTests.cs, UpdateTests.cs, UpdateWithOutputTests.cs | EXPR-TRANS, SQL-PROVIDER, PROV-* |
| DataProvider/ | ~33 | Provider-specific type-mapping and vendor feature tests. Root files cover Access, DB2, Informix, SqlCe, SqlServer, PostgreSQL (array + extensions), SQLite, Ydb. Types/ subfolder has 8 per-vendor type-framework files (DuckDBTypeTests.cs added). | SqlServerTests.cs, OracleTests.cs, PostgreSQLTests.cs, SqlServerVectorTypeTests.cs | PROV-SQLSERVER, PROV-ORACLE, PROV-POSTGRES, PROV-DUCKDB, etc. |
| Extensions/ | ~18 | Query hints, table aliases, vendor SQL extensions (SqlServer, MySQL, Oracle, PostgreSQL, SQLite, Access, ClickHouse). .generated.cs files are T4 output (see below). | QueryHintsTests.cs, SqlServerTests.cs, PostgreSQLTests.cs, DocExampleTests.cs | SQL-PROVIDER, PROV-* |
| Mapping/ | ~13 | Fluent mapping, attribute mapping, MappingSchema, value converters, enum mapping, dynamic columns, column aliases, expression methods, ConversionType. | FluentMappingTests.cs, MappingSchemaTests.cs, FluentMappingBuildTests.cs, FluentMappingExpressionMethodTests.cs | METADATA |
| Data/ | ~9 | DataConnection, transactions, retry policy, tracing, interceptors, stored procedures, QueryMultipleResult, MiniProfiler integration. | InterceptorsTests.cs, DataConnectionTests.cs, TransactionTests.cs, MiniProfilerTests.cs | INTERCEPTORS, INTERNAL-API |
| Common/ | ~13 | Utility/runtime helpers: ChangeType, Convert, Extensions, DefaultValue, ValueComparer, reserved words, EnumerableHelper, DataTools, ConnectionBuilder, SettingsReader, MemberInfoEqualityComparer. Also (new): AssemblyAvailabilityTests. | ConvertTests.cs, ValueComparerTests.cs, ReservedWordTest.cs | INTERNAL-API |
| Exceptions/ | ~9 | Validates that unsupported operations throw expected exception types. Mirrors Linq/ categories but tests error paths. Includes StackUseTests.cs for ExpressionVisitorBase / QueryElementVisitor stack-hop behavior. | CommonTests.cs, JoinTests.cs, AggregationTests.cs, StackUseTests.cs | EXPR-TRANS, SQL-AST |
| Infrastructure/ | ~6 | Test infrastructure self-tests: ActiveIssue attribute behavior, DataOptions builder, IdentifierBuilder, nullability context, Annotatable. | ActiveIssueConfigurationTests.cs, DataOptionsTests.cs, AnnotatableTests.cs, NullabilityContextTests.cs | TESTS-INFRA, INTERNAL-API |
| Metadata/ | ~3 | Attribute reader, XML reader, System.Data.Linq attribute reader (NETFX-only). | AttributeReaderTests.cs, XmlReaderTests.cs, SystemDataLinqAttributeReaderTests.cs | METADATA |
| SchemaProvider/ | ~3 | Schema introspection: SchemaProviderTests.cs, PostgreSQLSchemaProviderTests.cs, SqlServerTests.cs. | SchemaProviderTests.cs | SCAFFOLD |
| Scaffold/ | ~3 | Code generation name resolution and type-mapping: NameGenerationTests.cs, SchemaProviderTests.cs, TypeParserTests.cs. | NameGenerationTests.cs | SCAFFOLD, IN-TREE-TOOLS |
| TypeMapping/ | ~2 | Dynamic type-mapping wrapper tests (Oracle and generic). MappingTests.cs exercises ExpressionTypeMapper, delegate mapping, event wrapping. | OracleWrappingTests.cs, MappingTests.cs | PROV-ORACLE, INTERNAL-API |
| Reflection/ | ~2 | TypeAccessor and attribute reflection tests. AttributesTests.cs includes DynamicColumnInfo attribute API coverage. | TypeAccessorTests.cs, AttributesTests.cs | INTERNAL-API |
| Tools/ | ~5 | ComparerBuilder, DecimalHelper, ToDiagnosticString, entity-service identity map, mapper tests. MapperTests.cs exercises MapperBuilder<TFrom,TTo> and Map.GetMapper<T1,T2>(). | MapperTests.cs, IdentityMapTests.cs | IN-TREE-TOOLS |
| Samples/ | ~4 | Illustrative usage patterns: concurrency check, exception intercept, join operator, JSON conversion. JsonConvertTests.cs demonstrates MappingSchema-based JSON column converter via Newtonsoft.Json. | ConcurrencyCheckTests.cs, JsonConvertTests.cs | INTERNAL-API |
| OrmBattle/ | ~3 | Tests ported from the [ORMBattle.NET](http://ormbattle.net) benchmark suite (originally by Alexis Kochetov, 2009; T4-generated, updated 2015). Uses Northwind test model. Helper/ contains ExpressionUtils and GenericEqualityComparer. | OrmBattleTests.cs, Helper/ExpressionUtils.cs, Helper/GenericEqualityComparer.cs | EXPR-TRANS, SQL-PROVIDER |
| Microsoft/ | ~1 | OData query-composition integration test against Microsoft.AspNetCore.OData (net8+) and Microsoft.AspNet.OData (net462). | MicrosoftODataTests.cs | INTERNAL-API, EXPR-TRANS |
| ThirdParty/ | ~1 | Third-party LINQ extension compatibility (LinqKit.Core -- PredicateBuilder, AsExpandable()). | LinqKitTests.cs | EXPR-TRANS |
| AST/ | ~1 | SQL AST unit tests: SqlDataTypeTests.cs. Single test: SqlDataType.GetDataType(DataType.Boolean).SystemType. | SqlDataTypeTests.cs | SQL-AST |
| Create/ | ~1 | CreateData.cs -- utility class (no namespace, class a_CreateData) that populates test-database tables for all providers. Uses BulkCopy for seed rows and per-provider DbConnection action callbacks for binary/text data. [Order(-1)] ensures it runs first. | CreateData.cs | TESTS-INFRA |

## Delta since prior run (sha 7f972dbce -> 4a478ff14)

~70 files changed across six cross-cutting PRs:

### PR #5451 -- DuckDB provider tests

New file: DataProvider/Types/DuckDBTypeTests.cs -- sealed DuckDBTypeTests : TypeTestsBase tagged [TestFixture] under #if SUPPORTS_DATEONLY. Exercises the full DuckDB type surface via TypeTestsBase.TestType<TType,TNullableType>():

- **Boolean:** bool.
- **Integer:** sbyte/byte/short/ushort/int/uint/long/ulong, BigInteger (Int128 / UInt128 / variable-length HugeInt/BigNum). Provider bug with UHugeInt range commented out; BigNum (VARINT) uses literal-only mode (expectedParamCount: 0) due to provider read corruption.
- **Float/Double:** including NaN, PositiveInfinity, NegativeInfinity.
- **Decimal:** DECIMAL(18,3) default plus precision 1..38 with scale sweep.
- **String/Binary:** VARCHAR (string + char), BLOB (byte[] + System.Data.Linq.Binary), BITSTRING (BitArray + string + byte[] with DataType.BitArray; provider: expectedParamCount: 0, BulkCopy excluded).
- **UUID:** Guid.
- **Date/Time:** DATE (DateOnly + DateTime.Date + DuckDBDateOnly with infinity); TIME (TimeOnly + TimeSpan + precision 0/5/6; TIME_NS p=7/9 disabled due to provider ArgumentException); TIMETZ (TimeSpan + DateTimeOffset); INTERVAL (TimeSpan + DuckDBInterval; negative interval support missing); TIMESTAMP (DateTime x TIMESTAMP/TIMESTAMP_S/TIMESTAMP_MS/TIMESTAMP_NS precision ladder + DuckDBTimestamp native type); TIMESTAMPTZ (DateTimeOffset).
- **JSON:** string with DataType.Json.
- TestBulkCopyType local function skips BulkCopyType.ProviderSpecific for types the provider does not support in bulk copy.
- Known provider limitations documented in comments: UTINYINT range bug, \0/\x1 char parameter issues, TIME_NS type code 39 unknown, negative INTERVAL, UHugeInt range, BigNum read corruption.

DuckDB was also added to existing fixtures (sampled from changed file list): ConflictActionTests.cs adds TestProvName.AllDuckDB to [IncludeDataSources] for IgnoreConflictsTest / IgnoreConflictsTestAsync -- validates BulkCopyOptions { ConflictAction = ConflictAction.Ignore } with MultipleRows mode ignores PK conflicts and inserts non-conflicting rows. Similar DuckDB additions are present in BulkCopyTests.cs, DateTimeFunctionsTests.cs, DateTimeOffsetTests.cs, DataTypesTests.cs, MergeTests.*, and other fixtures throughout the delta set.

### PR #5495 -- AsQueryable parameterization (issue #5424)

New file: Linq/EnumerableSourceTests.AsQueryable.cs -- partial of EnumerableSourceTests. Covers the new three-argument IEnumerable<T>.AsQueryable(IDataContext, Action<IAsQueryableBuilder>) overload added in PR #5495. Tests:

- AsQueryable_Parameterize_AllParameters -- verifies SQL contains no inlined literals when .Parameterize() is configured.
- AsQueryable_Inline_AllInlined -- verifies SQL contains inlined literals when .Inline() is configured.
- AsQueryable_Parameterize_ExceptId_InlinesId / AsQueryable_Inline_ExceptData_ParameterisesData -- .Except(p => p.Id) / .Except(p => p.Data) flip individual member between inline/parameter mode.
- Cache stability: AsQueryable_Parameterize_CacheStable_AcrossDataChanges -- same-shape but different-data second query hits cache (no GetCacheMissCount() increase).
- Cache hit across NUnit [Values(1, 2)] iterations for Parameterize, Inline, and Parameterize().Except(p => p.Id) modes.
- Scalar int list, inline array, inline-array-in-SelectMany (expects LinqToDBException with 'AsQueryable configure' message).
- JOIN and CROSS APPLY patterns through parameterized enumerable source.
- Nested member (p.Address!.Zip) in .Except().
- Error cases: non-member selector (p.Id + 1), bare parameter (p => p), captured external member (other.Id) all throw LinqToDBException.
- CompiledQuery.Compile integration -- static _compiledConfiguredAsQueryable field reused across two invocations with different row counts/seeds.
- Provider exclusions: Access and ClickHouse excluded from most tests (join/apply patterns further restrict to SQL Server 2008+, PostgreSQL 9.3+, Oracle 12+, MySqlWithApply).

### PR #5467 -- DateTime.Now translation

DateTimeFunctionsTests.cs and DateTimeOffsetTests.cs updated. Changes are at the summary level (DuckDB provider additions and per-provider now-translation behavior assertions). The precise test changes verify that DateTime.Now / DateTimeOffset.Now / DateTime.UtcNow emit the correct provider-specific SQL for DuckDB (and may adjust expectations for other providers). See PROV-DUCKDB area for translation specifics.

### PR #5517 -- DateTime.Date DbType preservation

DateTimeFunctionsTests.cs updated. Issue #5309: DateTime.Date truncation cast was dropping the column's original DbType, causing incorrect type on the generated SQL parameter. Tests updated to verify the DbType is preserved after .Date access on a typed column. Involves SaveCommandInterceptor pattern to inspect DbCommand.Parameters (similar to Issue488Tests.cs pattern).

### PR #5503 -- Enum.HasFlag translation

ExpressionsTests.cs updated -- the Expressions.MapMember for Enum.HasFlag section receives additional coverage or provider-specific assertions. ClickHouse uses bitShiftLeft extension; SQL Server and others use bitwise AND. The test validates the SQL shape via ToSqlQuery().

### PR #5455 -- BulkCopy ConflictAction

ConflictActionTests.cs (new or substantially revised) -- validates BulkCopyOptions { BulkCopyType = BulkCopyType.MultipleRows, ConflictAction = ConflictAction.Ignore } against MySQL/PostgreSQL/SQLite/DuckDB. Sync and async variants. Verifies: conflicting PK rows (1, 2) retain original values; non-conflicting row (3) is inserted. Both table.BulkCopy(...) and table.BulkCopyAsync(...) paths exercised.

### Delta since prior run (sha 4a478ff14 -> 2e67bafc9) -- v6 string-concat / aggregate-nullability / trim

~23 files changed across several cross-cutting PRs (string concat, trim, aggregate nullability, AOT, issue regressions):

#### PR #5504 -- SqlConcatExpression / string concat overhaul

New file:  -- . Covers the new  translation path introduced in PR #5504. Tests span:

- **Basic concat forms:**  (scalar),  binary-add rewrite (the C# compiler emits ; the fixture confirms the registration-handler fix synthesizes a ),  four-arg, mixed numeric/string ().
- **Nullable semantics:**  with  wraps each null operand in  so the result is never null (covers issue #1916).  (non-null inputs) tested separately.
- **SELECT / ORDER BY positions:** concat in projection and in  clause.
- **Array form:** .
- **Aggregate (grouping) concat:**  emits provider-specific aggregate ( /  / ). Variants: nullable value filter (), DISTINCT + ordering,  over grouping, Guid/int column with per-element  rewriting.
- **:** whole-table and filtered forms, async variant.
- **Association subquery:**  through a  association.
- **Partial translation:**  where  is a -bound non-translatable helper -- exercises  partitioning.
- **String interpolation equivalence:**  vs  produce identical results; per-provider Oracle/Sybase empty-string semantics handled.
- **Provider exclusions / throws:**  covers , , , /.  skipped for nullable-operand tests.

**Updated (issue #5530):** New test  -- Sybase ASE  guard must only apply to actually-nullable operands; non-nullable columns and string literals must not get a redundant IS NULL guard. Verifies via  inspection.

#### PR #5515 -- TrimStart/TrimEnd with char sets

New file:  -- . Covers new  /  LINQ translation for issue #5515.

- **Whitespace trim (no-arg / empty-array / null-array):** , ,  -- all treated as whitespace trim; cross-provider.
- **Single-char trim** ():  / .
- **Multi-char set:** literal  and captured array  for VarChar, NVarChar, Char, NChar columns. Providers not supporting chars-trim ( constant: , , , , , ) expect .
- **Cache semantics:**  /  verify cache miss;  verifies re-execution hits cache; sorted-string cache key gives set semantics.
- **Legacy :** null-source null-propagation preserved through  rewrite.
- **Provider-specific SQL-shape assertions:** Oracle LTRIM/RTRIM, SQL Server NVarChar  literal, MySQL 8  with , ClickHouse  / .

#### PR #5557 -- Aggregate nullability (non-nullable Sum COALESCE wrap in subquery)

New file:  -- . Covers issue #5557 fix: non-nullable 00000     0 in subquery position must wrap with  to match LINQ semantics (empty sequence returns 0), while nullable 00000     0, and all //, must not wrap. Tests:

- **Decimal arithmetic minus subquery aggregate** (issue #5404 shape):  -- empty-inner group returns  unchanged.
- **Non-nullable Sum per numeric type** (, , , , ) in correlated subquery: empty group returns 0 (via COALESCE).
- **Nullable Sum** (, ) in subquery: SQL must NOT contain COALESCE; result must be  on empty group.
- **Min / Max / Average** in subquery: COALESCE must NOT appear; empty Max throws .
- All tests exclude  and .

#### PR #5552 -- AOT / MemberInfoEqualityComparer

New file:  -- pure unit test. Covers issue #5551: Native AOT emits  for lambda closures, whose  accessor throws . Tests , , . Production target: .

#### New UserTests fixtures (prior delta)

-  --  wrapping OrderBy in ; optimizer must not embed directive inside subquery column. PostgreSQL only.
-  --  +  on multi-level eager-loaded projection;  /  ordering in either direction. SQLite.
-  --  with  function () on -backed column. PostgreSQL 9.5+.

#### Modified fixtures (skim)

-  -- , ,  added.
-  --  added (string-aggregate parameter placement).
- , , , , , , ,  -- minor additions or provider-set expansions; no new fixture-level structures.
-  -- provider additions or MiniProfiler adapter refinements.
-  -- SQL Server-specific additions.
-  -- additional guard / async cancellation tests.
-  -- row-constructor update additions.
-  -- minor provider additions only.n
### Other changed files (cross-cutting)

The remaining ~55 changed files fall into these categories (not individually enumerated -- delta is additive, not structural):

- **Data/**: DataConnectionTests.cs, DataExtensionsTests.cs, TransactionTests.cs -- minor updates, likely DuckDB connection-lifecycle additions and/or async transaction coverage refinements.
- **DataProvider/**: FirebirdTests.cs, PostgreSQLTests.cs, SqlServerTests.cs -- provider-specific additions (DuckDB ripple or independent fixes).
- **Extensions/ClickHouseTests.cs**: ClickHouse-specific additions.
- **Infrastructure/DataOptionsTests.cs**: DataOptions builder coverage updates.
- **Linq/**: ~30 files -- primarily DuckDB provider additions to [IncludeDataSources] or [DataSources] attributes across AnalyticTests, CharTypesTests, CteTests, CteMaterializedTests, DataTypesTests, DefaultIfEmptyTests, EnumerableSourceTests, ExceptByMethodTests, GroupByExtensionsTests, IdlTests, InterfaceTests, IntersectByMethodTests, JoinTests, MinByMaxByMethodTests, ParameterTests, PredicateTests, SetOperatorTests, SqlExtensionTests, StringFunctionTests, StringFunctionsTests, SubQueryTests, TableOptionsTests, TypesTests, UnionByMethodTests, WhereTests. Also ConvertExpressionTests, ConvertTests -- expression/conversion coverage updates.
- **Mapping/**: ConversionTypeTests.cs, FluentMappingBuildTests.cs -- mapping coverage updates.
- **Update/**: BulkCopyTests.cs, DeleteTests.cs, DeleteWithOutputTests.cs, InsertTests.cs, InsertWithOutputTests.cs, MergeTests.* (10 files), UpdateFromTests.cs, UpdateTests.cs, OldMergeTests.cs -- DuckDB provider additions and possibly conflict-action coverage expansion.
- **UserTests/**: Issue1238Tests.cs, Issue269Tests.cs, Issue3432Tests.cs, Issue356Tests.cs, Issue445Tests.cs, Issue792Tests.cs, LetTests.cs -- regression additions or DuckDB provider additions.
### Delta since prior run (sha 2e67bafc9 -> b3340aa9d) -- YdbMemberNotFound removal, nested-mapping, assembly availability, NodaTime

~58 files changed across several PRs:
#### YdbMemberNotFoundAttribute removal (cross-cutting)

Tests/Linq/YdbToDoAttributes.cs -- YdbMemberNotFoundAttribute class removed. It was a YDB-specific ThrowsForProviderAttribute wrapping YdbException/InvalidOperationException with ErrorMessage matching the member-not-found message. The fix it gated is now implemented.
Across ~35 fixtures, [YdbMemberNotFound] replaced with [ThrowsRequiresCorrelatedSubquery(simple: true)]. Affected fixtures:

- Linq/AllAnyTests.cs, Linq/AssociationTests.cs, Linq/CommonTests.cs, Linq/CteTests.cs, Linq/ConcatUnionTests.cs, Linq/ContainsTests.cs, Linq/ConvertExpressionTests.cs, Linq/ConvertTests.cs, Linq/CountByMethodTests.cs, Linq/DistinctByMethodTests.cs, Linq/DistinctTests.cs, Linq/EnumMappingTests.cs, Linq/EnumerableSourceTests.cs, Linq/ExceptByMethodTests.cs, Linq/ExpressionsTests.cs, Linq/GuidTests.cs, Linq/IndexMethodTests.cs, Linq/IntersectByMethodTests.cs, Linq/IssueTests.cs, Linq/JoinTests.cs, Linq/MinByMaxByMethodTests.cs, Linq/OrderByTests.cs, Linq/ParameterTests.cs, Linq/PredicateTests.cs, Linq/ProjectionTests.cs, Linq/SelectTests.cs, Linq/SetOperatorComplexTests.cs, Linq/SetOperatorTests.cs, Linq/SetTests.cs, Linq/SqlRowTests.cs, Linq/StringFunctionsTests.cs, Linq/SubQueryTests.cs, Linq/UnionByMethodTests.cs, Linq/WhereTests.cs, Linq/CharTypesTests.cs
- Update/DeleteTests.cs (Delete3, Delete4, AlterDelete, DeleteMany1), Update/UpdateTests.cs (UpdateAssociation1Old through UpdateAssociation3, 6+ occurrences), Update/UpdateWithOutputTests.cs (Issue4193Test)
- UserTests/Issue269Tests.cs, UserTests/Issue825Tests.cs, UserTests/Issue2619Tests.cs, UserTests/Issue2816Tests.cs, UserTests/Issue3402Tests.cs, UserTests/SelectManyDeleteTests.cs, UserTests/UnnecessaryInnerJoinTests.cs

Pattern: tests previously expected YDB to throw 'Member not found'; now expect the standard correlated-subquery exception. Some fixtures also drop TestProvName.AllClickHouse from [DataSources] exclusion lists.
#### PR #5543 -- Nested member column mapping (MergeTests.ComplexProperty.cs)

New file: Update/MergeTests.ComplexProperty.cs -- partial of MergeTests (namespace Tests.xUpdate). Covers FluentMappingBuilder nested-member column mapping (column path crosses intermediate object, e.g. o => o.Nested.Field). Tests:

- ComplexProperty_UpdateWhenMatched -- [MergeDataContextSource]; UpdateWhenMatched() writes through nested path; unmatched row retains original nested value.
- ComplexProperty_InsertUpdate -- UpdateWhenMatched() + InsertWhenNotMatched() three-row scenario; both paths walk nested accessor.
- ComplexProperty_UpdateWithDelete -- Oracle-only ([IncludeDataSources(true, TestProvName.AllOracle)]); UpdateWhenMatchedAndThenDelete with nested column access.
- ExplicitInterfaceProperty_UpdateWhenMatched -- regression: explicit interface CLR name has dot (e.g. IExplicitComplexProperty.Field); dot-path splitter must NOT treat it as nested path. [MergeDataContextSource].
- ComplexProperty_NestedDiscriminator -- inheritance discriminator via nested member; OfType<NestedDiscriminatorDog>() must walk nested discriminator path. [DataSources].
- ComplexProperty_NestedPrimaryKey_OnTargetKey -- PK via [Column(MemberName = 'Key.Value')]; OnTargetKey() must dot-walk both sides of ON condition. [MergeDataContextSource].
#### TestsInitialization.cs update (issue #5538 -- Linux DB2/Informix native library resolver)

Added SetDllImportResolver for IBM.Data.Db2.DB2Connection.Assembly on Linux. Resolves libdb2.so from baseDir/clidriver/lib/libdb2.so. Guard: OperatingSystem.IsLinux() + File.Exists(path). Fixes intermittent CI failures where LD_LIBRARY_PATH does not propagate to testhost subprocess. Handle cached in IntPtr db2Handle closure.

#### Common/AssemblyAvailabilityTests.cs -- new fixture (issue #5538)

New class AssemblyAvailabilityTests (no TestBase inheritance; pure unit test). Tests LinqToDB.Internal.Common.Tools.IsProviderAssemblyPresent(name):

- ReturnsTrueForLoadedSelfAssembly -- the test assembly is loaded -> true.
- ReturnsTrueForLinqToDbCoreAssembly -- linq2db is referenced + loadable -> true.
- ReturnsFalseForNonexistentAssembly -- LinqToDB.Does.Not.Exist.ZZZ -> false; confirms internal Assembly.Load exception is swallowed.
- ReturnsTrueViaFileProbeForDeployedButUnloadableAssembly -- writes a non-PE .dll next to the linq2db assembly, verifies file-probe fallback returns true. Uses Shouldly for assertions.

#### Infrastructure/DataOptionsTests.cs update

Two new tests:

- WithDefaultNullsPositionTest -- pure unit; SqlOptions.WithDefaultNullsPosition(Sql.NullsPosition.Last) and DataOptions.UseDefaultNullsPosition(Sql.NullsPosition.First).
- ConfigurationSqlDefaultNullsPositionTest -- [NonParallelizable]; tests static Configuration.Sql.DefaultNullsPosition global; saves + restores prior value in finally.
#### DataProvider/OracleTests.cs update -- DateTimeOffset TIMESTAMP literal fix

TestDateTimeSQL updated: DateTimeOffset with positive UTC offset (Nepal +00:45) now emits local-time+offset form (e.g. TIMESTAMP '2020-01-03 04:05:06.789123 +00:45') rather than UTC-normalized form.
New test TestDateTimeOffsetToTimestampLiteral directly exercises db.MappingSchema.ValueToSqlConverter.TryConvert for zone-less TIMESTAMP column binding.

#### DataProvider/PostgreSQLTests.cs update -- NodaTime COALESCE (issue #5549)

New region Issue 5549: Issue5549Table entity (NodaTime.Instant nullable + non-nullable columns via DbType = timestamptz). Tests use UseConnectionFactory + NpgsqlDataSourceBuilder.UseNodaTime():

- Issue5549Test_BuiltInCoalesce -- (e.ClosedAt ?? e.CreatedAt) >= fromDate COALESCE via ?? operator; confirms two-row result.
- Additional variants test [Sql.Extension] builder and [Sql.Expression] raw-template approaches. Provider: TestProvName.AllPostgreSQL.

#### Update/BulkCopyTests.cs -- DateOnlyTable PK change

DateOnlyTable.Date changed from [PrimaryKey, Identity] to [PrimaryKey] (Identity removed). Reason: YDB BulkUpsert requires explicit PK; sole integer Identity PK becomes auto-SERIAL on YDB, conflicting with BulkUpsert.

#### Linq/StringConcatTests.cs update (issue #5530)

New test Concat_Sybase_NullGuardOnlyForNullableOperands -- verifies Sybase/SAP HANA CONCAT null guard emitted only for nullable operands. StringConcatNullEntity.ID decorated with [PrimaryKey].

#### UserTests/Issue5576Tests.cs -- new fixture (issue #5576)

New class Issue5576Tests : TestBase. Tests three-stage projection LEFT JOIN that previously produced spurious [item] column in VALUES clause (InvalidCastException: Failed to convert parameter value from T to Decimal). Shape: Campaign table LeftJoin with in-memory Counts[] (class, not struct) -> Stat -> WithRate (decimal arithmetic) -> Result re-mapping. [ActiveIssue(5611, Configuration = TestProvName.AllSQLite)] for SQLite integer-division issue.

#### AssemblyInfo.TestProgress.cs -- new assembly attribute

New file Tests/Linq/AssemblyInfo.TestProgress.cs. Single-line content: [assembly: TestProgressReporter]. Opt-in live test progress heartbeat via LINQ2DB_TEST_PROGRESS env var.
## Naming patterns

- **<Feature>Tests.cs** -- primary fixture style in Linq/, Exceptions/, Update/, Data/, Mapping/, Extensions/. One class per file; class name matches file name.
- **Issue<N>Tests.cs** -- UserTests/ pattern. N is the GitHub issue number. Issues start at 10 and run to 5576+ (as of HEAD). File-per-issue, one or few test methods. Some non-issue files in UserTests/ document a reproducer without a tracked issue (e.g. GroupBySubqueryTests.cs, SelectManyUpdateTests.cs).
- **Partial-class spreads** -- used when a fixture family is too large or has provider-specific branches:
  - Linq/WindowFunctionsTests.*.cs (13 files: Average, Cume, DenseRank, Frame, Max, Min, NTile, PercentRank, PercentileCont, Rank, RowNumber, Sum + root). **All 13 are excluded from the project via <Compile Remove> in Tests.csproj** -- they are work-in-progress or gated tests, not compiled into the assembly.
  - Linq/FullTextTests.*.cs -- three provider-specific partials (SqlServer, SQLite, MySql); [Category(TestCategory.FTS)] gates FTS tests.
  - Linq/ParameterTests.*.cs -- base + SqlServer + FSharp provider partials.
  - Linq/IsNullTests.SqlServer.cs -- SQL Server-specific IS NULL optimization tests.
  - Update/MergeTests.*.cs -- 19 partial files (root MergeTests.Issues.cs + 18 operation/sub-API files, including new MergeTests.ComplexProperty.cs). See **MergeTests family** below.
  - Update/UpdateFromTests.Row.cs -- row-constructor variant of update-from.
- **.generated.cs files** -- Extensions/ has T4-generated files: MySqlTests.generated.cs, PostgreSQLTests.generated.cs, OracleTests.generated.cs, SqlServerTests.generated.cs, SqlCeTests.generated.cs. Each is a partial class extending its sibling handwritten fixture.
## Notable per-fixture findings

**AST:**
- SqlDataTypeTests.cs -- minimal single test: verifies SqlDataType.GetDataType(DataType.Boolean).SystemType == typeof(bool). Validates the DataType-to-SystemType lookup table in SqlDataType.

**Common:**
- ConvertTests.cs -- exercises Convert<TFrom,TTo>, ConvertTo<T>, ConvertBuilder.GetConverter, MappingSchema.GetConverter<T1,T2>(), LinqToDBConvertException for ambiguous [MapValue]. Covers Convert<int,string>.Lambda / .Expression setters. Also tests nullable operator-parameter edge case in ConvertBuilder.GetConverter.
- ConnectionBuilderTests.cs -- tests DataOptions.UseLoggerFactory() / UseDefaultLogging() wiring; verifies QueryTraceOptions.WriteTrace is populated from ILoggerFactory. No database access except one SQL Server parameterized test.
- DataToolsTests.cs -- unit tests for DataTools.ConvertStringToSql (null-byte escaping, chr(N) emission for Access-style SQL) and DataTools.EscapeUnterminatedBracket (LIKE-pattern bracket escaping). No provider dependency.
- DefaultValueTests.cs -- tests DefaultValue<T>.Value get/set for all primitive types.
- DisposeTests.cs -- verifies double-dispose is safe for DataConnection, DataContext, and remote DataContext (both sync and async paths), with CloseAfterUse toggled.
- EnumerableHelperTest.cs -- tests EnumerableHelper.Batch<T>() (sync + async), including the invariant that a batch sub-sequence throws InvalidOperationException on second enumeration.
- ExtensionsTest.cs -- tests Type.GetMemberEx(MemberInfo) resolution for virtual and non-virtual properties on derived types. Uses MemberHelper.PropertyOf<T>.
- MemberInfoEqualityComparerTests.cs -- **(new, PR #5552)** pure unit tests for MemberInfoEqualityComparer.Default; covers AOT RuntimeSyntheticConstructorInfo path where MetadataToken throws. No database access. See Delta section above.
- AssemblyAvailabilityTests.cs -- **(new, issue #5538)** pure unit tests for LinqToDB.Internal.Common.Tools.IsProviderAssemblyPresent(name). Covers loaded/referenced/nonexistent/file-probe-fallback cases. No database access. See Delta section above.
- ReservedWordTest.cs -- tests ReservedWords.IsReserved(word, providerName) case-insensitivity for empty string, AllPostgreSQL, AllOracle provider names.
- SettingsReaderTests.cs -- **namespace anomaly**: class TestSettingsTests lives in Tests.Tools (not Tests.Common) despite being at path Tests/Linq/Common/SettingsReaderTests.cs. Tests SettingsReader.Deserialize(config, defaultJson, userJson) connection-merging logic with BasedOn inheritance chains.
- ValueComparerTests.cs -- tests ValueComparer.GetDefaultValueComparer<T>(true) null-handling for string, object, interface, and nullable value types.
**Create:**
- CreateData.cs -- class a_CreateData (no namespace). [Order(-1)] ensures runs first in test suite. Dispatches to per-provider SQL scripts under Database/Create Scripts/. Seeds LinqDataTypes2, Parent, Child, GrandChild, InheritanceParent2, InheritanceChild2 via BulkCopy. Per-provider DbConnection callbacks handle binary/BFILE columns (Oracle, SQLite, Informix, Access, Firebird). Oracle callback uses BindByNameOracleCommandInterceptor to avoid :NEW/:parameter confusion.

**Data:**
- DataExtensionsTests.cs -- exercises IDataContext.Query<T>(sql), Execute<T>, QueryMultiple-groupby, DataParameter -> DataParameter converter chain, CommandInfo.ClearObjectReaderCache(). Confirms [ScalarType(false)] on structs enables multi-column reads.
- MiniProfilerTests.cs -- large fixture (~36K tokens): wraps all supported providers behind StackExchange.Profiling.Data.ProfiledDbConnection. Validates that provider type-mapping (MappingSchema) still works correctly when DbCommand/DbDataReader are wrapped. Uses extern alias to disambiguate MySqlData vs MySqlConnector.
- ProcedureTests.cs -- tests QueryProc, ExecuteProc, QueryProcMultiple with SQL Server stored procs including output-parameter rebind after enumeration. Confirms [ResultSetIndex] attribute routing.
- QueryMultipleResultTests.cs -- tests QueryMultiple<T> and QueryProcMultiple<T> with [ResultSetIndex(N)] attribute routing for IEnumerable<T>, IList<T>, scalar, and array result types. Issue #4728 regression: empty result sets on multi-result stored procs.
- RetryPolicyTest.cs -- tests IRetryPolicy contract via custom Retry implementation; tests RetryPolicyBase subclass Issue3431RetryPolicy (overrides ShouldRetryOn) and exponential-base validation (ArgumentOutOfRangeException for expBase < 1.0). Uses SqlServerRetryPolicy integration test.
- TraceTests.cs -- comprehensive TraceInfo / TraceInfoStep coverage for LINQ queries, raw SQL, DML, transactions (BeginTransaction/Commit/Rollback all emit trace steps). Tests DataOptions.UseTracing(), .UseTraceLevel(), .UseTraceWith() and confirms TraceSwitch instance vs static priority.
- TransactionTests.cs -- tests async transaction lifecycle (BeginTransactionAsync, CommitTransactionAsync, RollbackTransactionAsync) for both DataContext and DataConnection; tests IsolationLevel overload; tests AttachToExistingTransaction for every provider via <Provider>Tools.GetDataProvider(connection, transaction). Issue #3863 PostgreSQL dispose-after-commit safety.
**Exceptions:**
- AggregationTests.cs -- confirms Min/Max/Average on empty non-nullable sequences throw InvalidOperationException.
- ConvertTests.cs -- confirms LinqToDBConvertException on duplicate [MapValue] values.
- DmlTests.cs -- confirms LinqToDBException on InsertOrUpdate with missing PK / missing PK in insert setter.
- ElementOperationTests.cs -- First / Single throw InvalidOperationException as expected.
- InheritanceTests.cs -- ParentInheritance2 without required discriminator mapping throws LinqToDBException.
- JoinTests.cs -- tests that join on new A() equals new B() with mismatched keys throws LinqToDBException; also contains positive multi-join regression tests.
- MappingTests.cs -- tests LinqToDBException on p.Name usage (not a mapped column) and LinqToDBConvertException on enum with inconsistent [MapValue].
- StackUseTests.cs -- tests ExpressionVisitorBase and QueryElementVisitor stack-hop safety under ThreadHopsScope. Verifies that deeply nested Expression.Call chains (30k+ nodes) trigger controlled InsufficientExecutionStackException (not raw stack overflow) with correct nesting depth matching hops count. Issue #5265: LoadWith chain on deeply associated entities executes within 200KB stack thread.

**Extensions:**
- AccessTests.cs -- AccessHints.Query.WithOwnerAccessOption hint propagation.
- ClickHouseTests.cs -- tests ClickHouse-specific JOIN modifiers (FINAL, SEMI, ANTI, ANY, GLOBAL, ALL), SETTINGS query hint (including Sql.TableName interpolation), and union interaction with hints.
- DocExampleTests.cs -- cross-provider doc-example test: exercises each provider hint API in a single fixture; DatabaseSpecificTest exercises all provider hint chains in one query.
- MySqlTests.cs + MySqlTests.generated.cs -- manual + T4-generated MySQL hint tests covering all MySqlHints.Table.* and MySqlHints.Query.* constants via parameterized [Values].
- OracleTests.cs + OracleTests.generated.cs -- Oracle hint tests covering OracleHints.Hint.* constants; generated file covers query-level hints.
- PostgreSQLTests.cs + PostgreSQLTests.generated.cs -- PostgreSQL locking hints (FOR UPDATE, FOR KEY SHARE, etc.) and sub-query hint scoping.
- QueryExtensionTests.cs -- tests self-join optimization with table hints (SelfJoinWithDifferentHintTest); verifies query.GetTableSource().Joins count.
- QueryNameTests.cs -- tests QueryName(name) emitting /* name */ (or /*+ QB_NAME(name) */ for Oracle) in SQL; cross-provider including Access fallback.
- SqlCeTests.cs + SqlCeTests.generated.cs -- SQL CE locking hints (WITH (NoLock) etc.), index hints, TablesInScopeHint.
- SQLiteTests.cs -- INDEXED BY <index> / NOT INDEXED hints.
- TableIDTests.cs -- tests TableID(pp) + Sql.TableAlias/TableName/TableSpec SQL ID resolution.
**Infrastructure:**
- ActiveIssueGenericTests.cs -- verifies [ActiveIssue] attribute variants (no details, details-only, URL+details, number-only). All tests are expected to skip in normal runs.
- IdentifierBuilderTests.cs -- tests IdentifierBuilder.Add() / CreateID() equality for null, bool, string, int, Delegate, lambda, object[], Type, and Expression.Constant. Note: two distinct lambda captures are NOT equal (false) because the C# compiler generates separate method instances.
- NullabilityContextTests.cs -- directly constructs SelectQuery / SqlTableSource / SqlJoinedTable with FULL/RIGHT/INNER join types; verifies NullabilityContext.CanBeNullSource() propagation rules. Exercises SqlExpressionOptimizerVisitor, AliasesContext, OptimizationContext, SqlSelectStatement.BuildSql.
- DataOptionsTests.cs -- **(updated)**: WithDefaultNullsPositionTest (pure unit; SqlOptions/DataOptions DefaultNullsPosition builder chain) and ConfigurationSqlDefaultNullsPositionTest ([NonParallelizable]; static Configuration.Sql.DefaultNullsPosition global). See Delta section above.

**Mapping:**
- CanBeNullTests.cs -- verifies Configuration.UseNullableTypesMetadata behavior: C# nullability annotations control CanBeNull on columns/associations when the flag is set.
- ConversionTypeTests.cs -- tests MappingSchema.SetConvertExpression<T1,T2>(., conversionType: ConversionType.FromDatabase / ToDatabase) asymmetric conversion; verifies DB stores trimmed value and read-back strips padding.
- DynamicStoreTests.cs -- tests [DynamicColumnsStore] / fluent DynamicColumnsStore() on Dictionary<string,object> columns; configuration-scoped stores (SQLite vs default).
- FluentDynamicMappingTests.cs -- tests FluentMappingBuilder.HasAttribute<T>(x => Sql.Property<int>(x, colName), attr) for adding attributes to dynamic columns.
- FluentMappingAliasTests.cs -- tests FluentMappingBuilder.Member(e => e.Alias).IsAlias(e => e.Real) -- column alias mapping through [ColumnAlias] and fluent builder.
- FluentMappingBuildTests.cs -- tests db.CreateTempTable(name, data, mb => mb.Property(.).IsPrimaryKey().) + InsertOrUpdate / Update on fluent-mapped temp tables.
- FluentMappingExpressionMethodTests.cs -- tests FluentMappingBuilder.Member(e => e.Computed).IsExpression(e => .) with and without materialization (true flag). Active issue #4987 marks several providers as skipped.
- MappingAmbiguityTests.cs -- tests that mixed [Column]/[NotColumn] on property+field with same name (different case) resolves correctly; verifies generated DDL column list.
- MappingSchemaTests.cs -- tests MappingSchema.SetDefaultValue, SetConvertExpression, converter chaining across parent/child schemas, XmlAttributeReader integration, and MetadataReader/AttributeReader fallback chain.
- MapValueTests.cs -- tests enum [MapValue] round-trip for string and char mapped enums via table INSERT + WHERE comparison.
- UseMappingSchemaTests.cs -- tests db.UseMappingSchema(schema) scoped override: within the using block column name maps to fluent-specified name; outside returns to original.
**Metadata:**
- AttributeReaderTests.cs -- tests AttributeReader.GetAttributes(type) and GetAttributes(type, member) for TableAttribute and ColumnAttribute lookup.
- SystemDataLinqAttributeReaderTests.cs -- NETFX-only. Tests System.Data.Linq.Mapping.* attributes (Linq2SQL) read by SystemDataLinqAttributeReader.
- XmlReaderTests.cs -- tests XmlAttributeReader parse, TableAttribute from <Table> element, ColumnAttribute from <ColumnAttribute> element (both short and fully-qualified names).

**OrmBattle/Helper:**
- ExpressionUtils.cs -- utility: ExtractMember(Expression) unwraps Lambda -> Convert -> MemberAccess. No tests; used by GenericEqualityComparer.
- GenericEqualityComparer.cs -- IEqualityComparer<T> driven by property-expression list. HashCodeBuilder.Hash uses prime-multiply. No tests; used by OrmBattleTests.

**Reflection:**
- AttributesTests.cs -- tests MemberInfo.HasAttribute<T>(inherit) for virtual property/event inheritance; tests DynamicColumnInfo attribute API coverage (all CustomAttributeExtensions overloads).
- TypeAccessorTests.cs -- tests TypeAccessor.GetAccessor<T>().CreateInstance(), member access via MemberAccessor, property/field read-write.

**Samples:**
- ConcurrencyCheckTests.cs -- illustrates intercepting UPDATE statements to append optimistic-concurrency WHERE clause by cloning SqlStatement and post-executing a SELECT verify. Uses LinqToDB.Internal.SqlQuery directly.
- ExceptionInterceptTests.cs -- illustrates IRetryPolicy wrapping exceptions with context (SQLiteException -> custom exception) and counting retries.
- JoinOperatorTests.cs -- simple join pattern examples against Northwind (Category x Product, multi-column join).
- JsonConvertTests.cs -- illustrates MappingSchema.SetConvertExpression<string,T> using Newtonsoft.Json.JsonConvert for JSON-column deserialization; dynamically generates converter expressions via Expression.Call.

**Scaffold:**
- SchemaProviderTests.cs -- issue #4444: PostgreSQL dblink extension schema-load does not crash LegacySchemaProvider. [ActiveIssue] so skipped in normal runs.
- TypeParserTests.cs -- tests IType / TypeParser (scaffold code-model type) parsing; TestType implements IType for unit testing.

**SchemaProvider:**
- PostgreSQLSchemaProviderTests.cs -- extensive expected schema for PostgreSQL TestTableFunctionSchema stored function: validates ProcedureSchema.Parameters, ResultTable.Columns including data types (int4, int8, numeric, etc.). Provider: NpgsqlTypes.
- SchemaProviderTests.cs -- TestApiImplemented confirms ISchemaProvider.GetSchema() runs without exception for all providers. Test validates table-name uniqueness and column-name uniqueness across the returned schema.
- SqlServerTests.cs (SchemaProvider/) -- SQL Server 2025+ JSON/vector column schema tests: validates jsonDataType -> typeof(string) (or SqlJson when provider-specific), vectorDataType -> float[] (or SqlVector<float>).
**Tools:**
- ComparerBuilderTests.cs -- tests ComparerBuilder.GetEqualityComparer<T>(selectors) including inherited member resolution for virtual overrides.
- DecimalHelperTests.cs -- tests DecimalHelper.GetFacets(decimal) returning (precision, scale) tuple for positive/negative and trailing-zero edge cases.
- IdentityMapTests.cs -- tests IdentityMap (from LinqToDB.Tools.EntityServices): identical-PK queries return same object reference; GetEntityEntries<T>() tracks DBCount / CacheCount.
- MapperTests.cs -- tests MapperBuilder<TFrom,TTo>, Map.GetMapper<T1,T2>(). Cross-references LinqToDB.Tools.Mapper.MapperBuilder in TOOLS area. Exercises GetMapperExpression/GetMapperExpressionEx, SetProcessCrossReferences, deep-copy semantics.
- ToDiagnosticStringTests.cs -- tests IEnumerable<T>.ToDiagnosticString() ASCII table formatter for primitive arrays and complex types.

**TypeMapping:**
- MappingTests.cs -- exercises ExpressionTypeMapper (dynamic type proxy): delegate wrapping (SimpleDelegate, ReturningDelegate with and without type mapping), event subscription/fire through mapped wrapper. Uses LinqToDB.Internal.Expressions.Types.
**Update:**

- BatchTests.cs -- BulkCopy inside/outside a DataConnection transaction; verifies commit semantics. Namespace Tests.xUpdate, [Order(10000)].
- CreateTableTests.cs -- db.CreateTable<T>() / db.DropTable<T>() round-trip with fluent mapping (PK, identity, length); async variant. Cross-provider.
- CreateTableTypesTests.cs -- comprehensive DDL type coverage: creates a table with int, long, double, bool, DateTime, enum, string, nullable variants, plus DataType.Json. Validates correct SQL type emission and round-trip for each .NET type.
- CreateTempTableTests.cs -- db.CreateTempTable(name, query, tableOptions: TableOptions.CheckExistence) API; verifies rows populate; cross-provider.
- DeleteTests.cs -- basic Delete / DeleteAsync / DeleteWithOutput API; Where-predicate DELETE.
- DeleteWithOutputTests.cs -- DeleteWithOutput / DeleteWithOutputInto across SQL Server, Firebird 5+, MariaDB, PostgreSQL, SQLite, Ydb. Feature-gated by FeatureDeleteOutputMultiple / FeatureDeleteOutputSingle / FeatureDeleteOutputInto constants.
- DropTableTests.cs -- table.Drop() / DropTable<T>(throwExceptionIfNotExists:) cross-provider; [Order(10000)].
- DynamicColumnsTests.cs -- Insert/Update/Delete using Sql.Property<T>(entity, colName) for dynamic column names.
- InsertIntoTests.cs -- query.Into(destTable).Insert() (SELECT INTO) for SQLite and ClickHouse.
- InsertWithOutputTests.cs -- InsertWithOutput / InsertWithOutputInto across SQL Server, Firebird, MariaDB, PostgreSQL, SQLite, Ydb.
- MergeTests family (19 files, partial class Tests.xUpdate.MergeTests):
  - MergeTests.Issues.cs -- root file; defines [TestFixture], test-model types, helpers (GetTarget, GetSource1/2, PrepareData), and issue-regression tests.
  - MergeTests.ApiParametersValidation.cs -- null-argument guard tests for all LinqExtensions.Merge overloads; exercises async cancellation path.
  - MergeTests.Caching.cs -- validates that enumerable-source merge queries hit the query cache on repeated calls.
  - MergeTests.CommandValidation.cs -- verifies providers that do not support MERGE throw LinqToDBException.
  - MergeTests.ComplexProperty.cs -- **(new, PR #5543)** nested-member column mapping in MERGE; FluentMappingBuilder Property(o => o.Nested.Field) paths. See Delta section above.
  - MergeTests.DynamicColumns.cs -- MERGE with [DynamicColumnsStore] source-side property reading.
  - MergeTests.EmptySource.cs -- MERGE with zero-row enumerable source.
  - MergeTests.Hints.cs -- SQL Server MERGE with table hints.
  - MergeTests.IQueryableSource.cs -- MERGE targeting an IQueryable; [ActiveIssue(2363)] pending.
  - MergeTests.OldApiMigratedTests.cs -- regressions converted from legacy MergeInto API.
  - MergeTests.TargetSourceOn.cs -- tests On(target, source, condition) and OnTargetKey() match-condition builder methods.
  - MergeTests.Types.cs -- MERGE with all numeric/date/string types in source rows.
  - MergeTests.WithOutput.cs -- MergeWithOutput / MergeWithOutputInto across SQL Server 2008+, PostgreSQL 17/18+, Firebird 3+.
  - MergeTests.Operations.Associations.cs -- MERGE with associations on target/source.
  - MergeTests.Operations.Combined.cs -- multi-operation MERGE (InsertWhenNotMatched + UpdateWhenMatched in single statement).
  - MergeTests.Operations.Delete.cs -- DeleteWhenMatched operation.
  - MergeTests.Operations.DeleteBySource.cs -- DeleteWhenNotMatchedBySource (SQL Server / Sybase extension).
  - MergeTests.Operations.IdentityInsert.cs -- InsertWhenNotMatched with identity column insert.
  - MergeTests.Operations.Insert.cs -- InsertWhenNotMatched variants.
  - MergeTests.Operations.LoadTests.cs -- large-batch MERGE performance/correctness.
  - MergeTests.Operations.Parameters.cs -- MERGE with parameterized source values.
  - MergeTests.Operations.Update.cs -- UpdateWhenMatched and UpdateWhenNotMatched variants.
  - MergeTests.Operations.UpdateBySource.cs -- UpdateWhenNotMatchedBySource (SQL Server / Sybase extension).
  - MergeTests.Operations.UpdateWithDelete.cs -- Oracle-only UPDATE ... DELETE clause within MERGE.
- MultiInsertTests.cs -- Oracle MULTI-TABLE INSERT (unconditional + conditional FIRST/ALL).
- OldMergeTests.cs -- tests for the deprecated MergeInto API; [Obsolete] on class.
- TempTableTests.cs -- db.CreateLocalTable<T>(seed) + insert-from-query across providers.
- TruncateTableTests.cs -- db.TruncateTable<T>() cross-provider; verifies row count drops to 0.
- UpdateFromTests.Row.cs -- partial of UpdateFromTests; tests UPDATE SET (col1, col2) = (SELECT .) row-constructor syntax for SQLite, Oracle, PostgreSQL, Informix, Firebird 5+.
- UpdateTests.cs -- comprehensive Update / UpdateAsync / Set(.) / InsertOrUpdate / UpdateFrom API coverage; cross-provider.
- UpdateWithOutputTests.cs -- UpdateWithOutput / UpdateWithOutputInto across SQL Server, Firebird, PostgreSQL 18+, SQLite, Ydb.
**DataProvider:**

- AccessProceduresTests.cs -- Access (OleDb + ODBC) stored procedure tests. Validates ProcedureSchema column metadata (OleDb vs ODBC schema differences). Uses ExecuteProc / QueryProc with OleDb/ODBC call syntax differences.
- AccessTests.cs -- Access (OleDb + ODBC) type mapping: all numeric, datetime, char, string, binary, GUID, XML, enum types. Tests AccessTools.CreateDatabase / DropDatabase (Jet and ACE versions).
- DB2Tests.cs -- IBM DB2 type mapping including provider-specific types (DB2Int64, DB2Real, DB2TimeStamp, DB2Clob, DB2Blob, DB2DateTime, DB2DecimalFloat, DB2Binary). High-precision timestamp (0--12 fractional digits via DB2TimeStamp). Module/package function calls.
- ExpressionTests.cs -- minimal: exercises IDataProvider.GetReaderExpression(reader, ordinal, drExpr, typeof(int)) to build a compiled reader delegate.
- InformixTests.cs -- Informix type mapping (bigint, int8, int, decimal, money, real, float, bool, char, varchar, nchar, nvarchar, lvarchar, text, date, datetime, interval, byte). BulkCopy with KeepIdentity for IDS provider.
- MySqlTestUtils.cs -- utility: EnableNativeBulk(db, context) sets SET GLOBAL local_infile=ON for MySqlConnector. Not a test fixture.
- OracleTests.cs -- **(updated)**: TestDateTimeSQL updated for DateTimeOffset local-time+offset form; new TestDateTimeOffsetToTimestampLiteral. See Delta section above.
- PostgreSQLArrayTests.cs -- PostgreSQL array-parameter caching: Sql.Ext.PostgreSQL().ValueIsEqualToAny(col, arr) parameterization with arrays of int, long, double, decimal, string, bool, short, float, Guid, DateTime.
- PostgreSQLExtensionsTests.cs -- PostgreSQL Unnest(array) / db.Unnest(col) table-valued function, PostgreSQLExtensions.ValueIsEqualToAny, array-column queries.
- PostgreSQLTests.cs -- **(updated)**: new region Issue 5549 with NodaTime.Instant COALESCE via ?? operator and [Sql.Extension] / [Sql.Expression] approaches. See Delta section above.
- SqlCeTests.cs -- SQL CE type mapping. BulkCopy. SqlCeTools.CreateDatabase / DropDatabase.
- SQLiteParameterTests.cs -- SQLite: DateTime stored as Int64 via custom MappingSchema converter. double/float parameter pass-through; float.MaxValue round-trip.
- SqlServerFunctionsTests.cs -- SQL Server SqlFn.* system functions: DbTS, LangID, Language, LockTimeout, MaxConnections, NestLevel, Options, RemServer, ServerName, ServiceName, Spid, TextSize, Version, and numerous date/string/math functions.
- SqlServerTestUtils.cs -- utility (non-fixture): provides TVPRecord class and GetSqlDataRecordsMS() / GetSqlDataRecords() helper enumerables for Table-Valued Parameter tests.
- SqlServerTypesTests.cs (root partial) -- SQL Server spatial and temporal types: SqlHierarchyId, SqlGeography, SqlGeometry; DateTimeOffset, DateTime2, TimeSpan.
- SqlServerTypesTests.TVP.cs (TVP partial) -- Table-Valued Parameter tests via DataTable, IEnumerable<SqlDataRecord>, and IEnumerable<SqlDataRecordMS> factories.
- SqlServerVectorTypeTests.cs -- SQL Server 2025 VECTOR type via SqlVector<float> and float[] column mappings. Tests SqlFn.VectorDistance(metric, v1, v2) with Cosine, Euclidean, Dot metrics.
- UniqueParametersNormalizerTests.cs -- unit tests for UniqueParametersNormalizer. Tests unique-string pass-through, duplicate renaming (test -> test_1 -> TEST_2), and case-insensitive dedup.
- YdbTests.cs -- Yandex DB (YDB) provider tests: schema introspection, DML (insert/update/delete), SelectQuery / QueryProc, BulkCopy. Uses YdbDataProvider internal class. Many tests tagged [YdbNotImplementedYet].
**DataProvider/Types:**

- TypeTestsBase.cs -- abstract base for per-vendor type tests. Provides TestType<TType,TNullableType>(context, DbDataType, value, nullableValue, ..) which exercises: CreateTable DDL, nullable/non-nullable insert, parameter queries, inline literal queries, all BulkCopy modes, filter-by-value SELECT.
- ClickHouseTypeTests.cs -- ClickHouse type coverage via TypeTestsBase. Notes unsupported types: LowCardinality, AggregateFunction, Nested, Tuple, Map, Array, Interval. Parameters disabled (TestParameters = false).
- DuckDBTypeTests.cs -- **(new, PR #5451)** DuckDB type coverage via TypeTestsBase. Covers Boolean, all integer widths (TINYINT--UBIGINT--HUGEINT--BIGNUM), FLOAT/DOUBLE, DECIMAL (precision/scale sweep), VARCHAR/BLOB/BITSTRING, UUID, DATE/TIME/TIMETZ/INTERVAL/TIMESTAMP/TIMESTAMPTZ/JSON. Known provider bugs documented inline. #if SUPPORTS_DATEONLY.
- MySqlTypeTests.cs -- MySQL/MariaDB VECTOR type (DataType.Vector32, float[]). MySQL 9+/MariaDB vector.
- PostgreSQLTypeTests.cs -- PostgreSQL JSON / JSONB types via DataType.Json / DataType.BinaryJson. Covers string, JsonDocument (MDS only).
- SapHanaTypeTests.cs -- SAP HANA SmallDecFloat (16-digit precision) and Decimal types via TypeTestsBase.
- SqlServerTypeTests.cs -- SQL Server 2025+ JSON type via DataType.Json. Covers string, JsonDocument (MDS v6+), SqlJson (MDS v6+).
- YdbTypeTests.cs -- YDB primitive types (bool, numerics, dates, strings, binary) via TypeTestsBase. Custom MakeListFilter using ListHas({1}, {0}) YDB SQL expression.
**Linq:**

- AbstractionTests.cs -- multi-class interface queries; ISample abstraction over two concrete types with Association; exercises eager-load through interface-typed associations.
- AggregationNullabilityTests.cs -- **(new, PR #5557)** subquery-aggregate nullability: non-nullable Sum wraps with COALESCE; nullable Sum, Min, Max, Average must not wrap. SQL-shape verified via ToSqlQuery().Sql.
- AggregationTests.cs -- aggregation over associations: Sum/Count/Average on IQueryable<ItemValue> association with null-value data. **(updated)**: SumByAssociationSubquery, LeftJoinToStringAggregate, ClosureList* aggregate tests added.
- AllAnyTests.cs -- Any/All LINQ operators: subquery Any, navigation Any, correlated All, combined predicates. **(updated)**: [YdbMemberNotFound] replaced with [ThrowsRequiresCorrelatedSubquery(simple: true)].
- AK107Tests.cs -- Oracle-only: sequence-backed identity insert ([SequenceName]) with cross-schema sequence (c##sequence_schema). Tests SkipOnUpdate in InsertOrUpdate path.
- ConflictActionTests.cs -- **(updated, PR #5455)** BulkCopyOptions { ConflictAction = ConflictAction.Ignore } with MultipleRows mode for MySQL/PostgreSQL/SQLite/DuckDB.
- EnumerableSourceTests.AsQueryable.cs -- **(new, PR #5495)** configured AsQueryable(db, builder) overload tests covering parameterize/inline modes, .Except(member) per-member flip, cache stability, scalar lists, inline arrays, error cases, CompiledQuery integration.
- StringConcatTests.cs -- **(new, PR #5504)** SqlConcatExpression coverage: basic concat forms, nullable semantics, SELECT/ORDER BY positions, array form, aggregate (grouping) concat, AggregateExecute, association subquery, partial translation, string interpolation equivalence.
- StringTrimTests.cs -- **(new, PR #5515)** TrimStart(char[]) / TrimEnd(char[]) translation coverage: whitespace trim, single-char trim, multi-char set, cache semantics, legacy TrimLeft/TrimRight, provider-specific SQL-shape assertions.
- AggregationNullabilityTests.cs -- **(new, PR #5557)** subquery-aggregate COALESCE wrap for non-nullable Sum; nullable Sum/Min/Max/Avg must not wrap. SQL-shape verified.
(All other Linq/ fixtures: see prior run entries -- no structural changes introduced by this delta.)
**UserTests (issues 10-1838 -- batch 5):**

[Same as prior run -- no structural changes to this batch]

**UserTests (issues 1869-2832 -- batch 6):**

[Same as prior run -- no structural changes to this batch]

**UserTests (issues 2856-4654 -- batch 7):**

[Same as prior run -- no structural changes to this batch]

**UserTests (issues 475-5458 + free-form -- batch 8):**

[Same as prior run -- no structural changes to this batch. Note: Issue269Tests.cs, Issue1238Tests.cs, Issue3432Tests.cs, Issue356Tests.cs, Issue445Tests.cs, Issue792Tests.cs, LetTests.cs received minor updates (likely DuckDB provider additions to [DataSources] sets or assertion refinements matching the PR #5451 provider expansion). No new test-class structures were introduced in these files.]

**UserTests (issues 5125-5505 -- delta additions, prior run):**

- Issue5125Tests.cs -- **(new)** IExpressionPreprocessor wrapping OrderBy in NULLS FIRST Sql.Expr; optimizer must not embed directive inside subquery column. PostgreSQL only.
- Issue5154Tests.cs -- **(new)** multi-level eager-loaded projection with Sql.Expr + [SqlQueryDependentParams]; ToSqlQuery / ToArray ordering in either direction must not throw. SQLite.
- Issue5505Tests.cs -- **(new)** UPDATE SET with ServerSideOnly function (jsonb_set) on a ValueConverter-backed column; translator must not double-wrap with converter expression. PostgreSQL 9.5+.
- Issue781Tests.cs -- minor updates only; no new test-class structures.

**UserTests (issue 5576 -- new this delta):**

- Issue5576Tests.cs -- **(new)** three-stage projection LEFT JOIN of DB table with in-memory Counts[] (class, not struct) -> Stat -> WithRate (decimal arithmetic) -> Result re-mapping. Regression: spurious [item] column in VALUES clause (InvalidCastException: Failed to convert parameter value from T to Decimal). [ActiveIssue(5611, Configuration = TestProvName.AllSQLite)] for SQLite integer-division. See Delta section above.
## Cross-area validation map

| Production area | Primary test subdirs |
|---|---|
| EXPR-TRANS | Linq/ (all), Exceptions/, UserTests/ (query shape regressions), OrmBattle/, ThirdParty/ |
| SQL-PROVIDER | Linq/ (SQL generation), Extensions/, Update/ |
| SQL-AST | AST/, Linq/InternalsTests.cs, Exceptions/CommonTests.cs, Exceptions/StackUseTests.cs, Infrastructure/NullabilityContextTests.cs |
| PROV-SQLSERVER | DataProvider/SqlServerTests.cs, DataProvider/SqlServerTypesTests.cs, DataProvider/SqlServerVectorTypeTests.cs, DataProvider/Types/SqlServerTypeTests.cs, Extensions/SqlServerTests.cs, SchemaProvider/SqlServerTests.cs |
| PROV-ORACLE | DataProvider/OracleTests.cs, Extensions/OracleTests.cs, TypeMapping/OracleWrappingTests.cs, Update/MultiInsertTests.cs |
| PROV-POSTGRES | DataProvider/PostgreSQLTests.cs, DataProvider/PostgreSQLArrayTests.cs, DataProvider/PostgreSQLExtensionsTests.cs, DataProvider/Types/PostgreSQLTypeTests.cs, Extensions/PostgreSQLTests.cs, SchemaProvider/PostgreSQLSchemaProviderTests.cs |
| PROV-MYSQL | DataProvider/MySqlTests.cs, DataProvider/Types/MySqlTypeTests.cs, Extensions/MySqlTests.cs |
| PROV-SQLITE | DataProvider/SQLiteTests.cs, DataProvider/SQLiteParameterTests.cs, Extensions/SQLiteTests.cs |
| PROV-DB2 | DataProvider/DB2Tests.cs |
| PROV-FIREBIRD | DataProvider/FirebirdTests.cs |
| PROV-SAPHANA | DataProvider/SapHanaTests.cs, DataProvider/Types/SapHanaTypeTests.cs |
| PROV-SYBASE | DataProvider/SybaseTests.cs |
| PROV-INFORMIX | DataProvider/InformixTests.cs |
| PROV-CLICKHOUSE | Extensions/ClickHouseTests.cs, DataProvider/Types/ClickHouseTypeTests.cs |
| PROV-ACCESS | DataProvider/AccessTests.cs, DataProvider/AccessProceduresTests.cs |
| PROV-YDB | DataProvider/YdbTests.cs, DataProvider/Types/YdbTypeTests.cs |
| PROV-DUCKDB | DataProvider/Types/DuckDBTypeTests.cs *(new)*, Linq/ConflictActionTests.cs, Update/BulkCopyTests.cs, Linq/DateTimeFunctionsTests.cs, Linq/DateTimeOffsetTests.cs, plus DuckDB entries in ~30 other Linq/ and Update/ fixtures |
| METADATA | Mapping/, Metadata/ |
| INTERCEPTORS | Data/InterceptorsTests.cs |
| SCAFFOLD | SchemaProvider/, Scaffold/ |
| IN-TREE-TOOLS | Tools/, Scaffold/ |
| INTERNAL-API | Common/, Infrastructure/, Reflection/, Samples/, Data/DataConnectionTests.cs |
| REMOTE-CLIENT | Linq/RemoteContextTests.cs |
## Files (Tier 1 / Tier 2)

**Tier 1 (read this run): 4/4**

| File | Role |
|---|---|
| Tests/Linq/TestsInitialization.cs | Assembly [SetUpFixture] -- provider registration, metrics, ClickHouse defaults, **(updated)** Linux DB2 native library resolver (issue #5538) |
| Tests/Linq/TestRetryPolicy.cs | No-op IRetryPolicy implementation used in tests |
| Tests/Linq/ExpectedExceptionAttribute.cs | NUnit IWrapTestMethod that replaces removed ExpectedExceptionAttribute |
| Tests/Linq/YdbToDoAttributes.cs | Yandex DB--specific ThrowsForProvider attribute variants; **(updated)** YdbMemberNotFoundAttribute removed |

**Tier 2: 545/611 sampled** (see deferred coverage block for the full un-visited list)

Representative reads (prior run): Linq/CteTests.cs, Linq/AnalyticTests.cs, Linq/EagerLoadingTests.cs, Linq/WindowFunctionsTests.cs, Linq/SubQueryTests.cs, Linq/FullTextTests.SqlServer.cs, Linq/ParameterTests.cs, Update/MergeTests.cs, Update/BulkCopyTests.cs, Update/UpdateFromTests.cs, DataProvider/SqlServerTests.cs, DataProvider/PostgreSQLTests.cs, DataProvider/OracleTests.cs, Extensions/QueryHintsTests.cs, Extensions/SqlServerTests.cs, Data/InterceptorsTests.cs, Data/DataConnectionTests.cs, Exceptions/CommonTests.cs, Infrastructure/ActiveIssueConfigurationTests.cs, Infrastructure/AnnotatableTests.cs, Mapping/FluentMappingTests.cs, Microsoft/MicrosoftODataTests.cs, OrmBattle/OrmBattleTests.cs, ThirdParty/LinqKitTests.cs, UserTests/Issue2296Tests.cs, Linq/JoinTests.cs, Linq/GroupByTests.cs, Linq/AssociationTests.cs, Linq/InheritanceTests.cs, DataProvider/MySqlTests.cs, DataProvider/SQLiteTests.cs, DataProvider/FirebirdTests.cs, DataProvider/SapHanaTests.cs, DataProvider/SybaseTests.cs, Update/InsertTests.cs, Update/DeleteTests.cs, Scaffold/NameGenerationTests.cs, TypeMapping/OracleWrappingTests.cs, Infrastructure/DataOptionsTests.cs.

Delta reads (this run, 2026-06-15): see Coverage block below.

Delta reads (this run, 2026-06-02): see Coverage block below.

Delta reads (this run, 2026-05-11): DataProvider/Types/DuckDBTypeTests.cs (new, full read), Linq/EnumerableSourceTests.AsQueryable.cs (new, full read), Linq/ConflictActionTests.cs (full read -- PR #5455 update).

Batch 1 reads (2026-05-07): see Coverage block below.

Batch 2 reads (2026-05-07): see Coverage block below.

Batch 3 reads (2026-05-07): see Coverage block below.

Batch 5 reads (2026-05-07): see Coverage block below.

Batch 6 reads (2026-05-07): see Coverage block below.

Batch 7 reads (2026-05-07): see Coverage block below.

Batch 8 reads (2026-05-07): see Coverage block below.
## Inbound / outbound dependencies

**Inbound:**
- TESTS-INFRA -- TestBase, TestConfiguration, CustomTestContext, NUnit fixtures, TestProvName, test model types from Tests.Model (Parent, Child, GrandChild, Person, Northwind). All fixtures in this area inherit TestBase.
- TESTS-MODEL -- POCO entities and mapping configurations (Person, Parent, Child, GrandChild, Northwind.*).

**Outbound (production areas exercised):**
- Nearly all LinqToDB.* namespaces. LinqToDB.Data, LinqToDB.Mapping, LinqToDB.DataProvider.*, LinqToDB.Internal.SqlQuery, LinqToDB.Internal.Linq, LinqToDB.Interceptors, LinqToDB.Tools, LinqToDB.Expressions are all directly imported by fixtures in this area.
- LinqToDB.Tools.Mapper.MapperBuilder (TOOLS area) -- cross-referenced by Tools/Mapper/MapperTests.cs.
- LinqToDB.Tools.EntityServices.IdentityMap (TOOLS area) -- cross-referenced by Tools/EntityServices/IdentityMapTests.cs.
- LinqToDB.Internal.Expressions.Types (dynamic type-mapping) -- cross-referenced by TypeMapping/MappingTests.cs.
- LinqToDB.Internal.Reflection.MemberInfoEqualityComparer -- cross-referenced by Common/MemberInfoEqualityComparerTests.cs (AOT regression, PR #5552).
- LinqToDB.Internal.Common.Tools.IsProviderAssemblyPresent -- cross-referenced by Common/AssemblyAvailabilityTests.cs (issue #5538).
## Known issues / debt

- WindowFunctionsTests family (13 files) is excluded via <Compile Remove> in Tests.csproj and is not compiled. These files exist on disk but have no active test coverage.
- OrmBattleTests.cs notes the file is generated from LinqTests.tt (T4). The .tt template is not in this directory; the generated output may be stale relative to the template.
- Provider-specific test files (DataProvider/, Extensions/) duplicate some logic with PROV-* area tests; delineation is: this area tests the *ORM layer* using provider features, while PROV-* areas document the provider-layer internals.
- Common/SettingsReaderTests.cs has class TestSettingsTests in namespace Tests.Tools (not Tests.Common) -- namespace/path mismatch.
- Update/MergeTests.Issues.cs is the root partial file for the MergeTests class (defines [TestFixture]), but it is named Issues rather than a neutral root name -- naming anomaly.
- DataProvider/SqlServerVectorTypeTests.cs requires SqlServerProviderAdapter.GetInstance(..).MappingSchema to be passed explicitly because out-of-box SqlVector<float> serialization is not yet registered by default.
- Linq/BatchTests.cs (in Linq/ subdirectory, not Update/) is wrapped in #if NETFRAMEWORK1 -- this condition is never true (no such symbol is defined), meaning the file is effectively dead code across all builds.
- Linq/CursorPagination.cs contains no [TestFixture] or [Test] attributes and is a utility class (Paginator) -- it contributes no test coverage, only a sample pagination implementation.
- Linq/DataServiceTests.cs is NETFX-only (#if NETFRAMEWORK); exercises System.Data.Services WCF stack that is not available on .NET Core/5+.
- DataProvider/Types/DuckDBTypeTests.cs documents extensive provider bugs inline (negative INTERVAL, UHugeInt range, BigNum read/write, TIME_NS type code 39, backslash-zero/backslash-x1 char parameters). These are DuckDB.NET provider limitations, not linq2db issues.
- Linq/StringTrimTests.cs TrimCharsUnsupported constant covers AllSqlServer2019Minus, SqlCe, AllSybase, AllAccess, AllFirebird, AllMySql57 -- on these providers chars-trim falls back to client-side. SQL shape is validated for CharColumnPaddingMismatch providers via separate assertions.
- YdbToDoAttributes.cs: YdbMemberNotFoundAttribute was removed in delta sha 2e67bafc9 -> b3340aa9d (replaced by [ThrowsRequiresCorrelatedSubquery(simple: true)] across ~35 fixtures). This KB entry documents the removal for future audit.
- UserTests/Issue5576Tests.cs: [ActiveIssue(5611, Configuration = TestProvName.AllSQLite)] -- SQLite integer-division causes decimal rate to be computed as integer, producing wrong result. Tracked in issue #5611.
## See also

- [TESTS-INFRA INDEX](../TESTS-INFRA/INDEX.md) -- TestBase, TestConfiguration, shared infrastructure.
- [TESTS-MODEL INDEX](../TESTS-MODEL/INDEX.md) -- shared POCO/entity models.
- [EXPR-TRANS INDEX](../EXPR-TRANS/INDEX.md) -- LINQ-to-SQL expression translation, exercised by Linq/.
- [SQL-PROVIDER INDEX](../SQL-PROVIDER/INDEX.md) -- SQL generation layer.
- [INTERCEPTORS INDEX](../INTERCEPTORS/INDEX.md) -- interceptor contracts tested in Data/InterceptorsTests.cs.
- [METADATA INDEX](../METADATA/INDEX.md) -- mapping/schema contracts tested in Mapping/, Metadata/.
<details><summary>Coverage</summary>

**Tier 1: 4/4 read**
- Tests/Linq/TestsInitialization.cs -- read (prior run); re-read this delta (Linux DB2 resolver added)
- Tests/Linq/TestRetryPolicy.cs -- read (prior run)
- Tests/Linq/ExpectedExceptionAttribute.cs -- read (prior run)
- Tests/Linq/YdbToDoAttributes.cs -- read (prior run); re-read this delta (YdbMemberNotFoundAttribute removed)

**Tier 2: 545/611 sampled**

Read (this run -- delta, 2026-06-15):
- Tests/Linq/TestsInitialization.cs -- re-read (Tier 1); Linux DB2 SetDllImportResolver added (issue #5538)
- Tests/Linq/YdbToDoAttributes.cs -- re-read (Tier 1); YdbMemberNotFoundAttribute class removed
- Tests/Linq/AssemblyInfo.TestProgress.cs -- new file; full read; [assembly: TestProgressReporter] opt-in progress heartbeat
- Tests/Linq/Common/AssemblyAvailabilityTests.cs -- new file; full read; IsProviderAssemblyPresent unit tests (issue #5538)
- Tests/Linq/Update/MergeTests.ComplexProperty.cs -- new file; full read; nested-member column mapping in MERGE (PR #5543)
- Tests/Linq/UserTests/Issue5576Tests.cs -- new file; full read; three-stage LEFT JOIN with decimal arithmetic regression (issue #5576)
- Tests/Linq/Infrastructure/DataOptionsTests.cs -- skimmed; WithDefaultNullsPositionTest and ConfigurationSqlDefaultNullsPositionTest added
- Tests/Linq/DataProvider/OracleTests.cs -- skimmed; TestDateTimeSQL updated for DateTimeOffset local-time+offset; TestDateTimeOffsetToTimestampLiteral added
- Tests/Linq/DataProvider/PostgreSQLTests.cs -- skimmed; Issue 5549 region added (NodaTime.Instant COALESCE via ??)
- Tests/Linq/Update/BulkCopyTests.cs -- skimmed; DateOnlyTable.Date PrimaryKey/Identity change (YDB BulkUpsert)
- Tests/Linq/Linq/StringConcatTests.cs -- skimmed; Concat_Sybase_NullGuardOnlyForNullableOperands added; StringConcatNullEntity.ID [PrimaryKey] added- Tests/Linq/Linq/AllAnyTests.cs -- skimmed; [YdbMemberNotFound] replaced with [ThrowsRequiresCorrelatedSubquery(simple: true)]
- Tests/Linq/Linq/AssociationTests.cs -- skimmed; [YdbMemberNotFound] replaced with [ThrowsRequiresCorrelatedSubquery(simple: true)]
- Tests/Linq/Linq/CommonTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/CteTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/ConcatUnionTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/ContainsTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/ConvertExpressionTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/ConvertTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/CountByMethodTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/DistinctByMethodTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/DistinctTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/EnumMappingTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/EnumerableSourceTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/ExceptByMethodTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/ExpressionsTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/GuidTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/IndexMethodTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/IntersectByMethodTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/IssueTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/JoinTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/MinByMaxByMethodTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/OrderByTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/ParameterTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/PredicateTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/ProjectionTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/SelectTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/SetOperatorComplexTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/SetOperatorTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/SetTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/SqlRowTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/StringFunctionsTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/SubQueryTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/UnionByMethodTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/WhereTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Linq/CharTypesTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/Update/DeleteTests.cs -- skimmed; [YdbMemberNotFound] replaced (Delete3, Delete4, AlterDelete, DeleteMany1)
- Tests/Linq/Update/UpdateTests.cs -- skimmed; [YdbMemberNotFound] replaced (UpdateAssociation1Old through UpdateAssociation3, 6+ occurrences)
- Tests/Linq/Update/UpdateWithOutputTests.cs -- skimmed; [YdbMemberNotFound] replaced (Issue4193Test)
- Tests/Linq/UserTests/Issue269Tests.cs -- skimmed; [YdbMemberNotFound] replaced; ProviderName.Ydb excluded from TestSkipDistinct/TestDistinctSkip/TestSkip
- Tests/Linq/UserTests/Issue825Tests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/UserTests/Issue2619Tests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/UserTests/Issue2816Tests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/UserTests/Issue3402Tests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/UserTests/SelectManyDeleteTests.cs -- skimmed; [YdbMemberNotFound] replaced
- Tests/Linq/UserTests/UnnecessaryInnerJoinTests.cs -- skimmed; [YdbMemberNotFound] replaced
Read (delta run, 2026-06-02):
- Tests/Linq/Linq/StringConcatTests.cs -- new file; full read; SqlConcatExpression / string.Concat / binary-add / aggregate-concat / partial-translation coverage (PR #5504)
- Tests/Linq/Linq/StringTrimTests.cs -- new file; full read; TrimStart/TrimEnd with char sets, cache semantics, provider-specific SQL shape (PR #5515)
- Tests/Linq/Linq/AggregationNullabilityTests.cs -- new file; full read; subquery-aggregate COALESCE wrapping for non-nullable Sum; nullable Sum/Min/Max/Avg must not wrap (PR #5557)
- Tests/Linq/Common/MemberInfoEqualityComparerTests.cs -- new file; full read; AOT RuntimeSyntheticConstructorInfo MetadataToken guard (PR #5552 / issue #5551)
- Tests/Linq/UserTests/Issue5125Tests.cs -- new file; full read; IExpressionPreprocessor NULLS FIRST subquery placement regression (issue #5125)
- Tests/Linq/UserTests/Issue5154Tests.cs -- new file; full read; SqlQueryDependentParams + multi-level eager-load ToSqlQuery/ToArray ordering (issue #5154)
- Tests/Linq/UserTests/Issue5505Tests.cs -- new file; full read; ServerSideOnly function on ValueConverter column UPDATE (issue #5505)
- Tests/Linq/Linq/AggregationTests.cs -- skimmed; added SumByAssociationSubquery, ClosureList* aggregate tests
- Tests/Linq/Linq/StringFunctionsTests.cs -- skimmed; added Issue5173_ParameterLocation
- Tests/Linq/Data/MiniProfilerTests.cs -- skimmed; provider additions or MiniProfiler adapter refinements; no new fixture structures
- Tests/Linq/DataProvider/SqlServerTests.cs -- skimmed; SQL Server-specific additions; no new fixture structures
- Tests/Linq/Update/MergeTests.ApiParametersValidation.cs -- skimmed; additional guard / async cancellation tests
- Tests/Linq/Update/UpdateFromTests.Row.cs -- skimmed; row-constructor update additions
- Tests/Linq/UserTests/Issue781Tests.cs -- skimmed; minor provider additions only

Read (delta run, 2026-05-11):
- Tests/Linq/DataProvider/Types/DuckDBTypeTests.cs -- new file; full read; DuckDB type matrix
- Tests/Linq/Linq/EnumerableSourceTests.AsQueryable.cs -- new file; full read; configured AsQueryable overload tests
- Tests/Linq/Linq/ConflictActionTests.cs -- full read; PR #5455 BulkCopy ConflictAction.Ignore

Sampled (prior runs, 2026-05-07): 521 files across all subdirectories -- see batch 1-8 entries above.

Skipped / deferred Tier-2 entries carry over from prior deferred-coverage state. The remaining un-visited Tier-2 files remain in the deferred queue (not enumerated here; maintained in state/deferred-coverage.json).

**Tier 3: 0 files** (no generated bin/obj under Tests/Linq/ in scope)

</details>
