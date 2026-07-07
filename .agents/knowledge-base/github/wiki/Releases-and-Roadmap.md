- [Release 6.3.0](#release-630)
- [Release 6.2.1](#release-621)
- [Release 6.2.0](#release-620)
- [Release 6.1.0](#release-610)
- [Release 6.0.0](#release-600)
- [Release 5.4.1.9](#release-5419)
- [Release 5.4.1](#release-541)
- [Release 5.4.0](#release-540)
- [Release 5.3.2](#release-532)
- [Release 5.3.1](#release-531)
- [Release 5.3.0](#release-530)
- [Release 5.2.2](#release-522)
- [Release 5.2.1](#release-521)
- [Release 5.2.0](#release-520)
- [Release 5.1.1](#release-511)
- [Release 5.1.0](#release-510)
- [Release 5.0.0](#release-500)
- [Release 5.0.0 RC 2](#release-500-rc-2)
- [Release 5.0.0 RC 1](#release-500-rc-1)
- [Release 5.0.0 Preview 2](#release-500-preview-2)
- [Release 5.0.0 Preview 1](#release-500-preview-1)
- [Release 4.4.1](#release-441)
- [Release 4.4.0](#release-440)
- [Release 4.3.0](#release-430)
- [Release 4.2.0](#release-420)
- [Release 4.1.1](#release-411)
- [Release 4.1.0](#release-410)
- [Release 4.0.1](#release-401)
- [Release 4.0.0](#release-400)
- [Release 4.0.0 Release Candidate 2](#release-400-release-candidate-2)
- [Release 4.0.0 Release Candidate 1](#release-400-release-candidate-1)
- [Release 4.0.0 Previews 2-10](#release-400-previews-2-10)
- [Release 4.0.0 Preview 1](#release-400-preview-1)
- [Releases 3.x](https://github.com/linq2db/linq2db/wiki/Release-Notes-3.0.0)
- [Older Versions](https://github.com/linq2db/linq2db/wiki/Archived-Release-Notes)

***

### Release 6.3.0

**LinqToDB**

- [#4325](https://github.com/linq2db/linq2db/pull/4325): [@darko1979](https://github.com/darko1979) added [support](https://github.com/linq2db/linq2db/pull/5395) for `IgnoreConflicts` option to `MultipleRows` mode of `BulkCopy` API for `MySql`, `MariaDB`, `PostgreSQL` and `SQLite`
- [#5302](https://github.com/linq2db/linq2db/pull/5302): fix query caching issue for `PostgreSQL` with array parameters. Array, `List<T>` and `IReadOnlyList<T>` types now recognized as scalars for `PostgreSQL`
- [#5323](https://github.com/linq2db/linq2db/pull/5323): [PostgreSQL, ClickHouse, DuckDB, SQLite] add explicit configuration API for `CTE` materialization using `AsCte` overload with configuration builder parameter. Examples:
  - `AsCte(c => c.IsMaterialized())`
  - `AsCte(c => c.IsMaterialized(false))`
  - `AsCte(c => c.IsMaterialized().HasName("custom_cte_name"))`
- [#5413](https://github.com/linq2db/linq2db/pull/5413): fix `UPDATE FROM` translation regressions
- [#5427](https://github.com/linq2db/linq2db/pull/5427): fix remaining issues with `null` handling by `ValueConverter` for non-nullable value type
- [#5429](https://github.com/linq2db/linq2db/pull/5429): fix `StackOverflowException` due to use of `AsQueryable` calls
- [#5434](https://github.com/linq2db/linq2db/pull/5434): fix value converters support in `Sql.Row`
- [#5443](https://github.com/linq2db/linq2db/pull/5443): [Oracle] improve `COALESCE` parameter type inference
- [#5444](https://github.com/linq2db/linq2db/pull/5444): [ClickHouse] add missing whitespace before table hint
- [#5445](https://github.com/linq2db/linq2db/pull/5445): fix predicate optimization for `NULL` checks
- [#5447](https://github.com/linq2db/linq2db/pull/5447): fix SET operators flattening
- [#5454](https://github.com/linq2db/linq2db/pull/5454): type conversion handling fix
- [#5457](https://github.com/linq2db/linq2db/pull/5457): fix too aggressive column removal for `CTE`
- [#5458](https://github.com/linq2db/linq2db/pull/5458): fix issue with member access translation for SET columns
- [#5463](https://github.com/linq2db/linq2db/pull/5463): several fixes to [Create/DropTable] APIs
  - [#798](https://github.com/linq2db/linq2db/pull/798): implement `DropTable(throwExceptionIfNotExists: false)` parameter to hide only non-existing table errors, not all errors. Thanks to [Carlos Salazar](https://github.com/xamtam54) for PR
  - [Oracle] fixed issue when drop of global temp table did nothing if table had data. Now it will truncate table data before drop
  - [SAP HANA] handle table not found errors on drop on SQL level for `throwExceptionIfNotExists: false`
  - [DB2][Firebird][SAP HANA][Oracle] properly escape sql statements in `EXECUTE` blocks, generated on table create/drop operations
- [#5480](https://github.com/linq2db/linq2db/pull/5480): fix query caching issues with `FromSql` queries

**LinqToDB for EntityFramework**

- [#5439](https://github.com/linq2db/linq2db/issues/5439): [SqlServer] detect fields, defined using `UseSequence` API, as identity fields

**LinqToDB F# Support**

- [#5430](https://github.com/linq2db/linq2db/pull/5430):
  - [#5428](https://github.com/linq2db/linq2db/pull/5428): fix compatibility with `FSharp.Core` 10.1.x
  - fix record mapping issues for members with case-only name distinctions

***

### Release 6.2.1

**LinqToDB**

- [#5284](https://github.com/linq2db/linq2db/pull/5284): multiple fixes to generated SQL
  - [#5283](https://github.com/linq2db/linq2db/pull/5283): fix translation issues for nested associations
  - fix issues with `UPDATE` queries with sub-queries in setters
  - fix `COALESCE` translation
  - improve joins optimization and unnesting, detect more cases when `LEFT JOIN` could be promoted to `INNER JOIN`
- [#5411](https://github.com/linq2db/linq2db/pull/5411): fix behavior of `CompareNulls.LikeSqlExceptParameters` option
- [#5416](https://github.com/linq2db/linq2db/pull/5416): fix `null` conversion to mapped enum
- [#5417](https://github.com/linq2db/linq2db/pull/5417): ignore table filters for `Drop` and `Truncate` table operations
- [#5420](https://github.com/linq2db/linq2db/pull/5420): fix invalid SQL generation for `INSERT` with sub-query setters in some cases
- [#5423](https://github.com/linq2db/linq2db/pull/5423): fix cases where `ORDER BY` generated in subqueries, that doesn't support it

**LinqToDB LINQPad Driver**

- [#5421](https://github.com/linq2db/linq2db/pull/5421): re-fix issue with settings dialog crashing on opening

***

### Release 6.2.0

**LinqToDB**

- [#5227](https://github.com/linq2db/linq2db/pull/5227): [ClickHouse] fix compatibility with `ClickHouse.Driver` `0.9.0`
- [#5256](https://github.com/linq2db/linq2db/pull/5256): fix invalid SQL generation for nested aggregations
- [#5258](https://github.com/linq2db/linq2db/pull/5258): binary/unary operators mapping improvements
  - [#5254](https://github.com/linq2db/linq2db/pull/5254), [#5259](https://github.com/linq2db/linq2db/pull/5259): fix regressions in handling of custom operators in mapping
  - add/fix support for custom operators mapping. Now you can map them using `Sql.ExpressionAttribute`, `MethodExpressionAttribute` or `IMemberTranslator` (you can see mapping examples in [tests](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/Linq/OperatorsTests.cs)). Note that in general you don't need to map custom operators if their logic follow logic of mapped binary or unary operation. But if it implements non-standard behavior, which you want to reproduce n SQL, then you must provde corresponding mapping for `LINQ to DB`
- [#5259](https://github.com/linq2db/linq2db/pull/5259): adding conversion to `DataParameter` to mapping schema for struct type will automatically add it also for nullable struct type if such conversion is not registered already
- [#5262](https://github.com/linq2db/linq2db/pull/5262): some internal optimizations
- [#5263](https://github.com/linq2db/linq2db/pull/5263): fix `Sql.CurrentTimestamp2` translation regression
- [#5264](https://github.com/linq2db/linq2db/pull/5264):
  - fix `ToString` implementation for string-based enums
  - obsolete `bool Configuration.UseEnumValueNameForStringColumns` option as it doesn't have any effect
  - improve performance of enum mapping
  - [Oracle] map `DataType.NVarChar` to ``NVARCHAR2` type and fix discovered issues with `NVARCHAR2` handling
- [#5266](https://github.com/linq2db/linq2db/pull/5266): fix mapping of composite properties with overlapping names
- [#5268](https://github.com/linq2db/linq2db/pull/5268): remove unnecessary subquery nesting for `IN` predicate query
- [#5269](https://github.com/linq2db/linq2db/pull/5269): don't force translated methods conversion to SQL if they could be evaluated on client as parameters or literals
- [#5272](https://github.com/linq2db/linq2db/pull/5272): [DB2][Firebird] add decimal type facets population from value
- [#5274](https://github.com/linq2db/linq2db/pull/5274): fix support for inhertance mapping of hierarchies wth 16+ types
- [#5285](https://github.com/linq2db/linq2db/pull/5285): fix scalars support by `FromSql` API
- [#5289](https://github.com/linq2db/linq2db/pull/5289): fix `InsertOrUpdate` API use for tables with query filter
- [#5291](https://github.com/linq2db/linq2db/pull/5291):
  - [#5286](https://github.com/linq2db/linq2db/pull/5286): implement bitwise not (`~`) translation for all providers
  - improve parameter database type inference for unmapped types with mapped underlying type (e.g. enums or nullable structs)
  - [Access][MySQL] fix bitwise OR (`|`) operator precedence
  - fix/improve some binary operation optimizations
- [#5297](https://github.com/linq2db/linq2db/pull/5297): `Sql.DateAdd` will not accept `DayOfYear` and `WeekDay` date part argument values anymore. Before they were treated in same way as `Day` date part value
- [#5301](https://github.com/linq2db/linq2db/pull/5301): fix `ArgumentException : must be reducible node` error
- [#5304](https://github.com/linq2db/linq2db/pull/5304): fix handling of entites that implement `Enity : IEnumerable<Entity>` interface
- [#5307](https://github.com/linq2db/linq2db/pull/5307): improvements to SQL generation for complex queries
  - [#5305](https://github.com/linq2db/linq2db/pull/5305): fix issues with unused columns detection and removal
  - [#5103](https://github.com/linq2db/linq2db/pull/5103), [#5327](https://github.com/linq2db/linq2db/pull/5327): fix issues with missing `ORDER BY` clause for some queries with `GROUP BY/DISTINCT` clauses
- [#5310](https://github.com/linq2db/linq2db/pull/5310): fix column mappings discovery for `OUTPUT/RETURNING` special tables
- [#5314](https://github.com/linq2db/linq2db/pull/5314): fix merge if `IgnoreFilters` calls
- [#5317](https://github.com/linq2db/linq2db/pull/5317): fix association handling regression
- [#5320](https://github.com/linq2db/linq2db/pull/5320): [SQL Server] fix non-ASCII descriptions load by Schema API
- [#5321](https://github.com/linq2db/linq2db/pull/5321): improve/fix translation of `Convert.To*/Sql.Convert` APIs to SQL
  - [#5298](https://github.com/linq2db/linq2db/pull/5298): fix `Convert.To*` calls with dynamic properties
- [#5328](https://github.com/linq2db/linq2db/pull/5328): fix nullability calculation for aggregate in subqueries
- [#5332](https://github.com/linq2db/linq2db/pull/5332):
  - [#5325](https://github.com/linq2db/linq2db/pull/5325): add `DateTime.UtcNow` property mapping
  - fix convert operation translation issues
- [#5336](https://github.com/linq2db/linq2db/pull/5336): fix `Cast<T>` operator translation
- [#5340](https://github.com/linq2db/linq2db/pull/5340): fix issue with subqueries handling in `UPDATE`
- [#5341](https://github.com/linq2db/linq2db/pull/5341): fix nullability of join conditions for optional associations sequence
- [#5344](https://github.com/linq2db/linq2db/pull/5344): fix found issues with complex CTE translations, improve CTE aliases generation for `ClickHouse`
- [#5350](https://github.com/linq2db/linq2db/pull/5350): correct return value database type for some date diff operations, fix typo in SQL for diff in months for `PostgreSQL`
- [#5351](https://github.com/linq2db/linq2db/pull/5351): fix `ValueConverter` support in `UPDATE` queries, improve associations/subqueries translation for `UPDATE`
- [#5356](https://github.com/linq2db/linq2db/pull/5356): fix unused joins detection issues for nested joins
- [#5357](https://github.com/linq2db/linq2db/pull/5357): [PostgreSQL] add timeout option support for native bulk copy
- [#5361](https://github.com/linq2db/linq2db/pull/5361): fix threading issue for mapping types with non-public mapped members, including `Storage` members
- [#5362](https://github.com/linq2db/linq2db/pull/5362): fix `GroupBy` translation
- [#5366](https://github.com/linq2db/linq2db/pull/5366): fix SQL generation for queries with `DISTINCT` and `ORDER BY` over expression
- [#5368](https://github.com/linq2db/linq2db/pull/5368): support aggregation sub-queries inside non-table queries
- [#5369](https://github.com/linq2db/linq2db/pull/5369): add support for translation of following `IEnumerable/IQueryable` methods
  - `CountBy`
  - `Index`
  - `MaxBy/MinBy` (overload with comparer not supported)
  - `ExceptBy/UnionBy/IntersectBy` (overload with comparer not supported)
- [#5371](https://github.com/linq2db/linq2db/pull/5371): fix issue with name of parameter ignored when provided by `DataParameter`
- [#5373](https://github.com/linq2db/linq2db/pull/5373): fix translation of aggregation function calls with `AsEnumerable/AsQueryable` calls in chain
- [#5379](https://github.com/linq2db/linq2db/pull/5379): [SQLite] don't generate `CAST` to guid as it mess with SQLite affinity inference
- [#5381](https://github.com/linq2db/linq2db/pull/5381): don't remove subqueries with aggregates if outer query has complex clauses that could affect aggregation
- [#5382](https://github.com/linq2db/linq2db/pull/5382): fix `CTE` support for `DELETE` and `UPDATE` queries
- [#5383](https://github.com/linq2db/linq2db/pull/5383): fix hint API use with `ToSqlQuery` helper
- [#5392](https://github.com/linq2db/linq2db/pull/5392):
  - [#5390](https://github.com/linq2db/linq2db/pull/5390): don't generate precision/scale for `DateTime.Date` type
  - [Access][SQL Server 2005] don't generate precision/scale for `DateTime` type
- [#5398](https://github.com/linq2db/linq2db/pull/5398): fix handling or conversion of one enum type value to value of other enum type
- [#5401](https://github.com/linq2db/linq2db/pull/5401): [ClickHouse] add support for `ClickHouse.Driver` 1.x provider versions. We recommend to enable `ReadStringsAsByteArrays=true` option, especially if you work with binary data to avoid data loss on `binary->utf8->binary` translations

#### YDB provider updates

- implement `string.Join` translation
- fix `ORDER BY` over aliased column
- fix translation of some string-related APIs: padding, replace, indexof, substring

#### Introduce several improvements around stack use

Related issues: [#5261](https://github.com/linq2db/linq2db/pull/5261), [#5265](https://github.com/linq2db/linq2db/pull/5265)

- [#5273](https://github.com/linq2db/linq2db/pull/5273): significantly reduce stack use by expression and query visitors
- [#5270](https://github.com/linq2db/linq2db/pull/5270): support switching to additional thread(s) by visitors if there is not enough stack on current thread.

Number of additional threads to use configured using `int LinqToDB.Common.Configuration.TranslationThreadMaxHopCount` option:
- default value is set to `5`
- negative values (e.g. `-1`) will disable feature and `StackOverflowException` will be generated in case of stack end
- `0` will not use additional threads, but still test remaining stack space and generate `InsufficientExecutionStackException`, which could be intercepted
- values greater than `0` will use up to N threads before throwing `InsufficientExecutionStackException` exception if this amount is still not enough

We don't recommend to set this option to bigger values as it could lead to performance issues and thread pool starvation if you hit some bug with infinite recursion.

**LinqToDB for EntityFramework**

- [#4668](https://github.com/linq2db/linq2db/issues/4668): fix too aggressive inheritance detection
- [#5318](https://github.com/linq2db/linq2db/issues/5318): fix mapping of `PostgreSQL` enums when nullable enum type used (`SomeEnum?`). Thanks to [@denis-tsv](https://github.com/denis-tsv) for PR
- [#5388](https://github.com/linq2db/linq2db/issues/5388): fix value converters use for constant values

**Scaffold T4**

- [#5252](https://github.com/linq2db/linq2db/pull/5252): [SQL Server 2025] `JSON` and `VECTOR` types support
- [#5255](https://github.com/linq2db/linq2db/pull/5255): fix issues when `NotifyPropertyChanged` template used with scaffold

**Scaffold CLI**

- [#5281](https://github.com/linq2db/linq2db/pull/5281): [SQL Server 2025] `JSON` and `VECTOR` types support


***

### Release 6.1.0

**LinqToDB**

- [#4816](https://github.com/linq2db/linq2db/pull/4816): [SQL Server] fix separator parameter type for `STRING_AGG` to be based on type of first argument
- [#5233](https://github.com/linq2db/linq2db/pull/5233): add support for .NET 10 `LeftJoin` and `RightJoin` operators
- [#5236](https://github.com/linq2db/linq2db/pull/5236): fix exception converting null value of `Nullable<T>` type to another type using non-nullable converter
- [#5237](https://github.com/linq2db/linq2db/pull/5237): fix nuget explorer warings for packages (non-functional change)
- [#5239](https://github.com/linq2db/linq2db/pull/5239): fix incorrect update translation with `string.Join` setter expression
- [#5240](https://github.com/linq2db/linq2db/pull/5240): [SQL Server < 2025] fix v5 compatibility by mapping `DataType.Json` to `NVARCHAR(MAX)`
- [#5244](https://github.com/linq2db/linq2db/pull/5244): fix support for non-array collection parameters

**LinqToDB LINQPad Driver**

- [#5237](https://github.com/linq2db/linq2db/pull/5237): fix issue with settings dialog crashing on opening
- [#5247](https://github.com/linq2db/linq2db/pull/5247): re-enable DB2 iSeries support

***

### Release 6.0.0

<a name='release-600-preview1'></a>
<a name='release-600-preview2'></a>
<a name='release-600-preview3'></a>
<a name='release-600-preview4'></a>
<a name='release-600-rc1'></a>
<a name='release-600-rc2'></a>
<a name='release-600-rc3'></a>

Fore release notes and migration notes see [page](https://github.com/linq2db/linq2db/wiki/Linq-To-DB-6).

***

### Release 5.4.1.9

This [#5161](https://github.com/linq2db/linq2db/pull/5161) maintenance release contains **no functional changes, improvements, or bug fixes**.
Only third-party NuGet dependencies have been updated to their latest compatible versions.

- Updated third-party dependencies to improve long-term compatibility and security.
- Most Microsoft packages have been upgraded to **version 9.0.10**.
- No modifications were made to LINQ to DB core libraries or behavior.
- Intended for users who continue to rely on the stable 5.x branch in production environments.

***

### Release 5.4.1

**LinqToDB**

- [#4445](https://github.com/linq2db/linq2db/pull/4445): [PostgreSQL] improve performance of native `BulkCopy`. Thanks to [@AndreyShipunov](https://github.com/AndreyShipunov) for PR
- [#4456](https://github.com/linq2db/linq2db/pull/4456): [SQLite] Add optional `extension` parameter to `SQLiteTools.CreateDatabase` API to specify extension of created database file instead of hardcoded `.sqlite`. Thanks to [@alexey-leonovich](https://github.com/alexey-leonovich) for PR
- [#4457](https://github.com/linq2db/linq2db/pull/4457): Fix connection leak issue with `DataContext` transactions/eager load when context used with auto-released connection without context disposal

***

### Release 5.4.0

**LinqToDB**

- [#4005](https://github.com/linq2db/linq2db/pull/4005): adds metrics collection API
  - [#3405](https://github.com/linq2db/linq2db/issues/3405): see `OpenTelemetry` integration [example](https://github.com/linq2db/linq2db/tree/master/Examples/Metrics/OpenTelemetry) in our samples
  - add `Common.Configuration.TraceMaterializationActivity = false` option to enable materialization metrics collection
- [#4217](https://github.com/linq2db/linq2db/issues/4217): fixed `SQL` generation of unnecessary `IS NOT NULL` checks for some of nullable boolean comparisons
- [#4252](https://github.com/linq2db/linq2db/issues/4252): add new setting `UseEnableConstantExpressionInOrderBy(bool)` to allow constants in `ORDER BY` clause to sort using ordinals. Note that we don't recommend to use it as we don't give guaranty over column order in generated query `SQL`
- [#4327](https://github.com/linq2db/linq2db/issues/4327): fix issues with `null` handling in `IN (sub-query)/EXISTS` clauses
- [#4337](https://github.com/linq2db/linq2db/issues/4337): fix 5.3.0 regression `LinqException: ...TransparentIdentifier... cannot be converted to SQL.`
- [#4341](https://github.com/linq2db/linq2db/issues/4341): mark configuration API methods with `[Pure]` attribute to highlight that they doesn't modify options object. Thanks to [@curllog](https://github.com/curllog) for PR
- [#4368](https://github.com/linq2db/linq2db/issues/4368): add support for one-way conversions registration using `mappingSchema.SetConvertExpression.SetConverter` methods using new `conversionType = ConversionType.[From/To]Database` parameter
- [#4371](https://github.com/linq2db/linq2db/issues/4371): fix multiple issues where `CurrentCulture` used instead of `Invariant` culture
  - [#3783](https://github.com/linq2db/linq2db/issues/3783): fix issue with non-SQL compatible negative literals generated for some cultures
- [#4378](https://github.com/linq2db/linq2db/issues/4378): fix `BulkCopyOptions` copy constructor
- [#4383](https://github.com/linq2db/linq2db/issues/4383): fix inherited classes support in eager load
- [#4385](https://github.com/linq2db/linq2db/issues/4385): [Oracle] fix `DateOnly` support in array-bound bulk copy mode
- [#4387](https://github.com/linq2db/linq2db/issues/4387): [NativeAOT] fixed couple of NativeAOT issues. Note that we still doesn't support NativeAOT officially
- [#4389](https://github.com/linq2db/linq2db/issues/4389): [ClickHouse] fix bulk copy support with `ClickHouse.Client` 6.8.0+ provider
- [#4398](https://github.com/linq2db/linq2db/issues/4398): fix rare race conditions in connection options creation logic
- [#4403](https://github.com/linq2db/linq2db/issues/4403): [Oracle] fix use of fixed-size buffer in multiple-rows bulk copy mode
- [#4415](https://github.com/linq2db/linq2db/issues/4415): fix regression in nullable aggregated scalar sub-queries handling
- [#4426](https://github.com/linq2db/linq2db/issues/4426): fix `NullReferenceException` due to race conditions in context logging when user change trace levels

**LinqToDB Configuration**

- [#4326](https://github.com/linq2db/linq2db/issues/4326): fix incorrect registration logic for `AddLinqToDBContext<TContext, TContextImplementation>()` overload

**Scaffold T4**

- [#4375](https://github.com/linq2db/linq2db/issues/4375): emit procedures and functions ordered by name

**Scaffold CLI**

- [#4373](https://github.com/linq2db/linq2db/pull/4373): fixed incorrect regular expression examples in help. Thanks to [Michel Bretschneider](https://github.com/embix) for PR
- [#4428](https://github.com/linq2db/linq2db/pull/4428): add .NET 8 tool build

***

### Release 5.3.2

**LinqToDB**

- [#4313](https://github.com/linq2db/linq2db/issues/4313): fix regression in Npgsql 7- support, introduced by 5.3.1 release

***

### Release 5.3.1

**LinqToDB**

- [#4309](https://github.com/linq2db/linq2db/issues/4309): fix compatibility with Npgsql 8

***

### Release 5.3.0

**LinqToDB**

- [#3273](https://github.com/linq2db/linq2db/issues/3273): [Oracle] Prefer single `COALESCE` function over multiple nested `NVL` for calls with 3 or more parameters. Thanks to [@ddas09](https://github.com/ddas09) for PR
- [#4122](https://github.com/linq2db/linq2db/issues/4122): log elements of collection-typed query parameters (e.g. arrays). By default only first 8 elements logged. Use `Configuration.MaxArrayParameterLengthLogging` setting to change this limit
- [#4168](https://github.com/linq2db/linq2db/issues/4168), [#4174](https://github.com/linq2db/linq2db/issues/4174): fixed issue with linked server name being ignored by multiple APIs when only server name passed to API as input parameter
- [#4172](https://github.com/linq2db/linq2db/issues/4172): [Oracle] fix SQL, generated empty string comparison. Thanks to [Divyansh Bhatia](https://github.com/divyanshbhatia1) for PR
- [#4175](https://github.com/linq2db/linq2db/issues/4175): fix issue with conditional expressions parsing in output/returning clause. Thanks to [Kasper Fabæch Brandt](https://github.com/poizan42) for PR
- [#4176](https://github.com/linq2db/linq2db/issues/4176): [Access] removed wrong identifier escaping logic, when identifier with dot in name was split into multi-component identifier
- [#4182](https://github.com/linq2db/linq2db/issues/4182): fix table attributes support for `MergeWithOutputInto` output table. Thanks to [Kasper Fabæch Brandt](https://github.com/poizan42) for PR
- [#4196](https://github.com/linq2db/linq2db/issues/4196): fixed `null` values support for `value IN (subquery)` syntax. Added new option `PreferExistsForScalar` to configure generated SQL for scalar subquery conditions. This feature is useful for databases (e.g. ClickHouse), which doesn't support correlated sub-queries by converting it to non-correlated one.
  - `PreferExistsForScalar = true`: `EXISTS (SELECT * FROM sequence WHERE sequence.key = value)`
  - `PreferExistsForScalar = false` (default): `value IN (SELECT sequence.key FROM sequence)`
- [#4201](https://github.com/linq2db/linq2db/issues/4201): support associations with different key types on both sides
- [#4202](https://github.com/linq2db/linq2db/issues/4202): fix issues with sub-query hints in SET queries
- [#4203](https://github.com/linq2db/linq2db/issues/4203): fix issue with query hints in subqueries
- [#4204](https://github.com/linq2db/linq2db/issues/4204): fix issue with optimization of subquery with grouping
- [#4210](https://github.com/linq2db/linq2db/issues/4210): fix nullability tracing for ternary expressions
- [#4219](https://github.com/linq2db/linq2db/issues/4219): [Oracle] limit generated query parameter length to 30 characters for Oracle 12+ temporary while we don't have explicit support for 12.2+ Oracle dialects
- [#4228](https://github.com/linq2db/linq2db/issues/4228), [#4254](https://github.com/linq2db/linq2db/issues/4254): fix regression in parameter names generation
- [#4229](https://github.com/linq2db/linq2db/issues/4229): fixed issue, when cached query could hold reference to initial data context object, preventing it from garbage collection. This could lead to excessive memory use by query cache (sometimes huge)
- [#4256](https://github.com/linq2db/linq2db/issues/4256): fix issue with non-nullable constant values in sub-query projection to nullable projection field/property
- [#4257](https://github.com/linq2db/linq2db/issues/4257): enable support for `IDataContext.CloseAfterUse = true` context flag in `BulkCopy` API
- [#4261](https://github.com/linq2db/linq2db/issues/4261): fixed issue with embedding of enumerable collections with long path to collection
- [#4263](https://github.com/linq2db/linq2db/issues/4263): [NativeAOT] fixed issue with `Expression.DebugView` trimmed away
- [#4267](https://github.com/linq2db/linq2db/issues/4267): [NativeAOT] implemented workaround for `NativeAOT` issue with `GetInterfaceMap` API
- [#4268](https://github.com/linq2db/linq2db/issues/4268): changed `DataConnection.WriteTraceLineConnection` setter accessibility to protected to allow direct assignment from child classes. Thanks to [@Metadorius](https://github.com/Metadorius) for PR
- [#4275](https://github.com/linq2db/linq2db/issues/4275): some internal memory/performance optimizations to entity model build
- [#4280](https://github.com/linq2db/linq2db/issues/4280): fixed exception when `Update<TBase>(derivedInstance)` API called for entity with inheritance mapping
- [#4285](https://github.com/linq2db/linq2db/issues/4285): [PostgreSQL] fixed issues in identifier quotation logic
- [#4299](https://github.com/linq2db/linq2db/issues/4299): fix potential issue with default struct constructors
- [#4302](https://github.com/linq2db/linq2db/issues/4302): [Access] implement missing conversion to SQL `bool`

**Scaffold**
- [#4131](https://github.com/linq2db/linq2db/issues/4131):
  - [T4] fix Oracle templates, broken by 5.0 release
  - [#4058](https://github.com/linq2db/linq2db/issues/4058): [T4] fix issues with identifiers generation process steps, when C# keyword check applied before final name generated for some identifier types
- [#4134](https://github.com/linq2db/linq2db/issues/4134): [CLI] add `add-init-context` option to manage generation of `InitDataContext` partial method on context
- [#4255](https://github.com/linq2db/linq2db/issues/4255): [CLI] fixed multiple issues with parsing of generic types by `ITypeParser` implementation
- [#4278](https://github.com/linq2db/linq2db/issues/4278): [T4] we decided to cancel obsoletion of T4 nugets (scaffold [cli utility](https://www.nuget.org/packages/linq2db.cli) is still recomended way to scaffold database model)

***

### Release 5.2.2

**LinqToDB**

- [#4043](https://github.com/linq2db/linq2db/issues/4043): add missing support for column converters (`IValueConverter`) by `Execute[Async](string sql)` API
- [#4146](https://github.com/linq2db/linq2db/issues/4146):
  - refactor query parameter names generation to avoid issues like [#3902](https://github.com/linq2db/linq2db/issues/3902) and [#4144](https://github.com/linq2db/linq2db/issues/4144)
  - [PostgreSQL] fixed missing identifier quotation when identifier starts from non-letter and non-underscore character

**Scaffold CLI**

- [#4127](https://github.com/linq2db/linq2db/issues/4127): fix binding of output parameters for synchronous mappings of stored procedures


***

### Release 5.2.1

**LinqToDB**

- [#4025](https://github.com/linq2db/linq2db/issues/4025): fix issue with `object->DataParameter` conversion being ignored
- [#4074](https://github.com/linq2db/linq2db/issues/4074): improve discard of invalid `ORDER BY` columns from joined subqueries
- [#4090](https://github.com/linq2db/linq2db/issues/4090): fix nullability tracking for `OUTER APPLY` columns
- [#4098](https://github.com/linq2db/linq2db/issues/4098): fix issues with missing columns for queries with join to subquery
- [#4107](https://github.com/linq2db/linq2db/issues/4107): fix merge keys selection into CTE for merge-into-cte queries
- [#4113](https://github.com/linq2db/linq2db/issues/4113): fix regression in handling properties, which hide interface implementation with `new` keyword
- [#4124](https://github.com/linq2db/linq2db/issues/4124): fix database provider detection for `ProviderName.MariaDB` name

**Scaffold CLI**

- [#4111](https://github.com/linq2db/linq2db/issues/4111): fix scaffold of table functions in separate schema class

***

### Release 5.2.0

We started our own Discord server. Invite could be found [here](https://github.com/linq2db/linq2db#linq-to-db).


**LinqToDB**

- [#3034](https://github.com/linq2db/linq2db/issues/3034): fix mapping of inherited interfaces for interface mapping
- [#3776](https://github.com/linq2db/linq2db/issues/3776), [#3895](https://github.com/linq2db/linq2db/issues/3895), [EF#213](https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/213), [EF#316](https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/316): [PostgreSQL] fix issue with `DateTime` values `Kind` normalization, when it was converted to `Unspecified` for column of `timestamp with time zone` type. If you still have this issue, check that your column has type specified.
- [#4031](https://github.com/linq2db/linq2db/issues/4031): Improve detection of interface property implementation for mapping
- [#4045](https://github.com/linq2db/linq2db/issues/4045): [Oracle][PostgreSQL] fix issue with connection string parameter ignored by `PostgreSQLTools`/`OracleTools` in some methods
- [#4046](https://github.com/linq2db/linq2db/issues/4046): Apply `DataParameter` conversions to parameters in more cases
- [#4055](https://github.com/linq2db/linq2db/issues/4055): [MySQL] fix trailing hint generation for `INSERT` queries (e.g. `INSERT ... SELECT ... FOR UPDATE`)
- [#4065](https://github.com/linq2db/linq2db/issues/4065): [Firebird] fix incorrect SQL generation for parameter-less stored procedure, called as table function
- [#4066](https://github.com/linq2db/linq2db/issues/4066): [ClickHouse] add helpers to specify join hints, `FINAL` modifier and `SETTINGS` clause (see [tests](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/Extensions/ClickHouseTests.cs) for usage examples)
- [#4070](https://github.com/linq2db/linq2db/issues/4070): [ClickHouse] fixed creation of temporary table with primary key in ClickHouse 23.3+
- [#4072](https://github.com/linq2db/linq2db/issues/4072): add static `DataConnection.DefaultOnTraceConnection` property to set default application-wide trace handler
- [#4079](https://github.com/linq2db/linq2db/issues/4079): fix `InvalidCastException` generated for some queries with `Sql.Property` API
- [#4082](https://github.com/linq2db/linq2db/issues/4082): fix materialization of entity, queried using implemented interface, where interface has read-only property and entity class implements it with setter
- [#4086](https://github.com/linq2db/linq2db/issues/4086): fix multiple issues with `DataContext`:
  - [#4057](https://github.com/linq2db/linq2db/issues/4057): async eager load operation on `DataContext` could fail with exception `InvalidOperationException: There is already an open DataReader associated with this Connection which must be closed first.`
  - `DataContext` with `KeepConnectionAlive = false` setting closes implicit eager load transaction after first query
  - `DataContext` queries caching doesn't work correctly (could affect only applications with multiple configurations for same database type)

**Scaffold CLI**

- [#4061](https://github.com/linq2db/linq2db/issues/4061): Fix regression in association names generation for composite foreign keys

***

### Release 5.1.1

**LinqToDB**

- [#4037](https://github.com/linq2db/linq2db/issues/4037): fixed issue with incorrect use of connection string parameter by some of `[Oracle|PostgreSQL|SqlServer]Tools` APIs

***

### Release 5.1.0

**LinqToDB**

- [#3966](https://github.com/linq2db/linq2db/issues/3966):
  - prefer `field IN (subquery)` SQL generation instead of `EXISTS(subquery with field filter)` for scalar subqueries
  - improve `boolean` type compatibility for `ClickHouse` for `Octonica` and `MySql` providers
- [#3997](https://github.com/linq2db/linq2db/issues/3997): don't throw exception from provider dialect detector when dialect already specified in context options and connection string is not set
- [#4001](https://github.com/linq2db/linq2db/issues/4001):
  - added new extension point for query hints/extensions generation: `QueryExtensionScope.TableNameHint`
  - [SQL Server] add temporal table extensions to filter by `FOR SYSTEM_TIME` clause: `TemporalTableHint`, `TemporalTableAll`, `TemporalTableAsOf`, `TemporalTableFromTo`, `TemporalTableBetween`, `TemporalTableContainedIn`
- [#4006](https://github.com/linq2db/linq2db/issues/4006): add `BigInteger` type support for PostgreSQL
- [#4010](https://github.com/linq2db/linq2db/issues/4010): fix potential issues with hint extensions
- [#4011](https://github.com/linq2db/linq2db/issues/4011): fix nullability tracking for deep-nested optional associations
- [#4014](https://github.com/linq2db/linq2db/issues/4014): fix overloads conflict for `UsePostgreSQL` configuration extensions
- [#4015](https://github.com/linq2db/linq2db/issues/4015): add `WhereKeyOptimistic` extension to apply query filter over primary key and lock field for specific record
- [#4016](https://github.com/linq2db/linq2db/issues/4016): disable unused left join optimization for joins with hints
- [#4022](https://github.com/linq2db/linq2db/issues/4022): fix work with inherited attributes on properties
- [#4027](https://github.com/linq2db/linq2db/issues/4027): fix regression in `DataConnection.DefaultSettings` assignment handling

**Scaffold CLI**

- [#4021](https://github.com/linq2db/linq2db/issues/4021):
  - [#945](https://github.com/linq2db/linq2db/issues/945): add support for fluent metadata generation: new `--metadata` option with values `none`, `attributes` (default), `fluent`
  - automatically pass generated mapping schema to context in generated context constructors (for fluent mapping and `PostgreSQL` tuples mapping)
  - [PostgreSQL] fix error when scaffold includes `NpgsqlTypes.NpgsqlInterval` type
  - [SQL CE] fix duplicate foreign key columns
  - fixed equality generation for nullable `System.Data.SqlTypes.Sql*` struct types and `SqlHierarchyId`
  - fixed equality generation for DB2 custom struct types (e.g. `DB2Int32`) including nullable types
  - fixed defaults rendering in `--find-methods` command help, add `none` option to disable generation of `Find*` methods

***

### Release 5.0.0

Also check [V5 migration notes](Migration-to-v5).

**LinqToDB**

- [#3975](https://github.com/linq2db/linq2db/issues/3975): fix support for `AssociationSetterExpression` for cases when `Storage` use different type compared to mapped association
- [#3981](https://github.com/linq2db/linq2db/issues/3981): simplify and clarify `IMetadataReader.GetAttributes` API contract:
  - remove use of generic arguments and set return type to `MappingAttribute[]` from `T[] where T: MappingAttribute`
  - document that implementation should return all mapping attributes for requested member/type
- [#3983](https://github.com/linq2db/linq2db/issues/3983): fix `InvalidCastException` from `InsertWithOutput*` APIs when target and output types doesn't match
- [#3986](https://github.com/linq2db/linq2db/issues/3986): fix `Contains` to `IN` conversion for collections with `null` values

***

### Release 5.0.0 RC 2

<a name='release-500-rc2'></a>

**LinqToDB**

- [#3959](https://github.com/linq2db/linq2db/issues/3959): fix issue with reverted default column order for mapping classes with inheritance. Thanks to [Guillaume Lecomte](https://github.com/guillaume86) for PR
- [#3961](https://github.com/linq2db/linq2db/issues/3961): add support for custom association setter expression, used on association load using `LoadWith`/`ThenWith` APIs (Thanks to [Guillaume Lecomte](https://github.com/guillaume86) for PR):
  - `AssociationAttribute.AssociationSetterExpressionMethod`
  - `AssociationAttribute.AssociationSetterExpression`
- [#3962](https://github.com/linq2db/linq2db/issues/3962): add additional overloads to `CreateTempTable` API to support anonymous classes
- [#3967](https://github.com/linq2db/linq2db/issues/3967): fixed incorrect equality operators implementations for `DataOptions`
- [#3969](https://github.com/linq2db/linq2db/issues/3969): ([#1592](https://github.com/linq2db/linq2db/issues/1592), [#1822](https://github.com/linq2db/linq2db/issues/1822)) replace `MappingSchema.EntityDescriptorCreatedCallback` instance delegate with
  - application-wide `MappingSchema.EntityDescriptorCreatedCallback` static delegate
  - context-specific delegate, set using `WithOnEntityDescriptorCreated`/`UseOnEntityDescriptorCreated` configuration extensions
- [#3970](https://github.com/linq2db/linq2db/issues/3970): add additional overloads to `Use<DB_NAME>` options configuration extensions without connection string parameter

***

### Release 5.0.0 RC 1

<a name='release-500-rc1'></a>

**LinqToDB**

- [#3643](https://github.com/linq2db/linq2db/issues/3643): added source table support to output/returning clause to Merge API:
  - `MergeWithOutput[Async]` (SQL Server 2008+, Firebird 3.0+)
  - `MergeWithOutputInto[Async]` (SQL Server 2008+)
- [#3840](https://github.com/linq2db/linq2db/issues/3840): fix type convert issues in mapping generation
- [#3858](https://github.com/linq2db/linq2db/issues/3858):
  - improve detection of cases when we can generate single query with `APPLY` instead of multiple queries
  - [#3799](https://github.com/linq2db/linq2db/issues/3799): fix `NotImplementedException` from eager-load queries with single-record subqueries (e.g. using `FirstOrDefault`)
- [#3952](https://github.com/linq2db/linq2db/issues/3952): due to compatibility issues between `MySql.Data` provider and recent versions on `MariaDB` we drop support for `MySql.Data` provider use with `MariaDB`. You can still use it with old `MariaDB` versions, but we don't accept issues for this provider/database combination and recommend to use `MySqlConnector` provider (for `MySql` too)

**linq2db.AspNet**

- [#3953](https://github.com/linq2db/linq2db/issues/3953): downgrade required `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Logging.Abstractions` dependencies to version 6 from 7

***

### Release 5.0.0 Preview 2

<a name='release-500-preview2'></a>

**LinqToDB**

- [#553](https://github.com/linq2db/linq2db/issues/553): added API for record delete/update with optimistic lock. See [notes](#optimistic-lock-extensions) below
- [#2690](https://github.com/linq2db/linq2db/issues/2690): add support for C# Nullable Reference Types annotations to infer nullability of columns and single-record associations. To enable it, set `Configuration.UseNullableTypesMetadata = true;` option. Note that curently this is application-wide option and cannot be configured per-context
- [#3905](https://github.com/linq2db/linq2db/issues/3905): [MySql][MariaDB] added extensions to work with 'FOR UPDATE/SHARE' hints (see examples in [tests](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/Extensions/MySqlTests.cs))
- [#3900](https://github.com/linq2db/linq2db/issues/3900): performance-related optimizations and refactorings
  - mapping attributes refactoring (see [below](#mapping-attributes-refactoring))
  - breaking changes to fluent mapping (see [below](#fluent-mapping-changes))
  - [**BREAKING CHANGE**] `Configuration.Linq.EnableAutoFluentMapping` was renamed to `EnableContextSchemaEdit` to better reflect affected behavior and set it to `false` by [default](#default-configuration-changes)
  - [**BREAKING CHANGE**] disable support for mapping attributes from `System.ComponentModel.DataAnnotations.Schema` and `System.Data.Linq.Mapping` namespaces by [default](#default-configuration-changes)
  - connection factory delegate accepts `DataOptions` instance now (`Func<DbConnection>` -> `Func<DataOptions, DbConnection>`). It allows user to access connection options, e.g. connection string: `options => new SqlConnection(options.ConnectionOptions.ConnectionString)`
  - [**BREAKING CHANGE**] custom `IMetadataReader` implementations should support requests for base attribute types. See more details [here](#metadata-reader-changes)
- [#3906](https://github.com/linq2db/linq2db/issues/3906): fixed `InvalidOperationException` in eager load
- [#3910](https://github.com/linq2db/linq2db/issues/3910): fixed C# NRT annotations on `LoadWith`/`ThenLoad` APIs
- [#3921](https://github.com/linq2db/linq2db/issues/3921): fixed several issues with asociations:
  - [#3658](https://github.com/linq2db/linq2db/issues/3658): respect `CanBeNull` for associations with custom predicate expression
  - prevent "unused" inner join removal by sql optimizer if it was added by association
  - fix generation of joins for associations for `Count` scalar queries
- [#3926](https://github.com/linq2db/linq2db/issues/3926): fixed issue in eager load
- [#3933](https://github.com/linq2db/linq2db/issues/3933): fix context mapping schema pollution in temporary table APIs with custom entity configuration delegate parameter
- [#3939](https://github.com/linq2db/linq2db/issues/3939): add new connection extensions to simplify database connection setup without need to define interceptor class: `UseBeforeConnectionOpened`/`UseAfterConnectionOpened`. Both extensions accept sync delegate with connection instance parameter and optional async delegate for use from async code if you need to call blocking code from setup delegate. Some examples where it could be useful:
  - Sql Server connection authentication setup using [`SqlCredential`](https://github.com/linq2db/linq2db/issues/2137) or [`AccessToken`](https://github.com/linq2db/linq2db/issues/1604)
  - SQLite database encryption setup using `PRAGMA *KEY` [directives](https://www.sqlite.org/see/doc/trunk/www/readme.wiki)
- [#3943](https://github.com/linq2db/linq2db/issues/3943): improve performance of `System.Data.Linq.Binary` .NET Core compatibility class. Thanks to [Guillaume Lecomte](https://github.com/guillaume86) for PR

**Scaffold CLI**

- [#3728](https://github.com/linq2db/linq2db/issues/3728): fix one-to-one association name generation regression
- [#3896](https://github.com/linq2db/linq2db/issues/3896): fix `PE image doesn't contain managed metadata.` error when use T4 template for interceptors (thanks to [@AndyBan](https://github.com/AndyBan) for PR)
- [#3938](https://github.com/linq2db/linq2db/issues/3938): fix SQL Server support regression in `preview-1`

#### Optimistic Lock Extensions

Namespace: `LinqToDB.Concurrency`.

There are two new extensions to leverage record update and delete operations using optimistic locks:
- `UpdateOptimistic[Async]`
- `DeleteOptimistic[Async]`

Those extensions:
- accept record object you need to update or delete with version field value set and add version check to your query;
- return number of affected records which you should check to see if operation executed successfully or nothing was updated/deleted due to version change in database or because record is not found.

To use those extensions you need to tell `Linq To DB` which field should be used as version field and how to generate new version value on optimistic update. For that you can:
- annotate it with `OptimisticLockPropertyAttribute` which provides several standard version generation strategies;
- implement your own one using `OptimisticLockPropertyBaseAttribute` as base attribute class and specify update expression in `GetNextValue` method.

Built-in strategies:
- `VersionBehavior.Auto`: use database-generated value. E.g. SQL Server `rowversion` field type or `UPDATE TRIGGER`
- `VersionBehavior.AutoIncrement`: use autoincrement strategy (by +1) on numeric field
- `VersionBehavior.Guid`: use `Guid.NewGuid()` client side generation. Could be applied to fields of `Guid`, `string` (using `guid.ToString()`) or `byte[]` (using `guid.ToByteArray()`) types

Example:
```cs
[Table]
public class MyTable
{
    [PrimaryKey, Identity]
    public int Id { get;set; }

    [Column]
    [OptimisticLockProperty(VersionBehavior.Guid)]
    public Guid Version { get;set; }

    [Column]
    public string Name { get;set; }

    // ... other fields
}

using var db = new MyContext();

var record1 = new MyTable()
{
    // initial version
    Version = Guid.NewGuid(),
    Name = "My Name"
};

// add record to database
db.Insert(record1);

record.Name = "New Name";
if (db.UpdateOptimistic(record1) == 0)
{
    // update failed - sombody managed to remove our record or change it's version
    throw new InvalidOperationException(
        $"Update failed, record with id={record1.Id} and"
            + $" version={record1.Version} wasn't found in database");
}
else
{
    // add additional condition to query
    if (db.GetTable<MyTable>().Where(r => r.Name.Contains("My")).DeleteOptimistic(record1) == 0)
    {
        // use only primary key + version as delete condition
        if (db.DeleteOptimistic(record1) == 0)
        {
            throw new InvalidOperationException(
                $"Delete failed, record with id={record1.Id} and"
                    + $" version={record1.Version} wasn't found in database");
        }
    }
}
```

#### Mapping attributes refactoring

This release introduce major refactoring to mapping attributes which includes:
- all mapping attributes now inherited from `MappingAttribute` base class, which means all of them now have `string? Configuration` property to specify configuration-specific mapping. Previously it wasn't possible to specify configuration-specific mappings using some mapping attributes;
- `interface IMetadataReader` now has generic constrain `TAttribute: MappingAttribute` which will require changes from you, if you use custom metadata readers
  - you need to add generic constrains to your implementation
  - you cannot return non-mapping attributes from reader (which already was useless, as `linq2db` doesn't query such attributes from readers anyways)
- attribute getter methods in `MappingSchema` class now also have `TAttribute: MappingAttribute` constrain now. This shouldn't affect most of users as those methods designed for use by `linq2db` itself

#### Fluent mapping changes

With this release we introduce breaking change to fluent mapping configuration, which will require changes from all users of fluent mapping.

Previously to configure mappings using fluent builder all you need to do is to create builder using `GetFluentMappingBuilder()` method on mapping schema or database context and add mappings using builder method which immediatly reflected in mapping schema.

We change this behavior to postpone configured mappings registration in mapping schema till explicit call to new `Build()` method on fluent mappings builder and remove `GetFluentMappingBuilder` from `MappingSchema` and `DataContext` and `DataConnection` classes to make this breaking change explicit.

E.g. you have following code which worked before:
```cs
// create new data context
using var db = new MyDataContext();

// create new mapping schema for fluent mappings
// and add it to context
var fluentMappings = new MappingSchema();
db.AddMappingSchema(fluentMappings);

// get mappings builder from mapping schema
var builder = fluentMappings.GetFluentMappingBuilder();
// or using context mapping schema directly
// var builder = db.MappingSchema.GetFluentMappingBuilder();
// or
// var builder = db.GetFluentMappingBuilder();

// configure mappings
builder.Entity<MyEntity>().Property(e => e.Id).IsPrimaryKey();

// all done, you can use your mappings already

// delete entity by primary key value from Id property
db.Delete(entity);
```

While it worked before, that example contains a lot of issues with performance and will not work in new version for at least two reasons:
- there is no `Build()` method call yet, so context don't know anything about new mappings;
- `AddMappingSchema(..)` method called before mappings configured - this will also not work anymore, because fluent mappings not set to mapping schema yet by `Build()` call;
- if you used `GetFluentMappingBuilder` method on context/context schema - it will also will not work as we changed library defaults and context mapping schema is not editable by default anymore.

Except those obvious breaking changes this example also has big performance issue: if you need to configure mappings you need to do it once and then use pre-configured mapping schema with all context instances otherwise you will have big performance penalty as `linq2db` will be unable to reuse cached mapping information.

Proper configuration of (fluent) mappings:
```cs
// setup mappings once, e.g. in application startup or MyDataContext static constructor:

private readonly MappingSchema _mappings;

static MyDataContext()
{
    // create shared mapping schema instance with all context mappings
    _mappings = new MappingSchema();

    // configure fluent mappings
    // create builder instance explicitly
    new FluentMappingBuilder(_mappings)
        .Entity<MyEntity>()
            .Property(e => e.Id)
                .IsPrimaryKey()
        // (!) commit mappings to mapping schema
        Build();

    // also we can configure additional mappings
    _mappings.SetConvertExpression(...);
}

// pass mapping schema to context base contructor (e.g. using DataOptions)
public MyDataContext(DataOptions options)
    : base(options.UseMappingSchema(_mappings))
{
}
```

#### Default configuration changes

We are changing some library defaults to provide better performance by default. It could break some users.

##### Data context mapping schema (`MappingSchema IDataContext.MappingSchema` property) from now is read-only by default.

It means you cannot edit it from context instance:

```cs
var db = new MyContext();

// add fluent mappings to context mapping schema
db.MappingSchema.GetFluentMappingBuilder();
// register new type conversion
db.MappingSchema.SetConvertExpression(...);
```

You can restore old behavior using following code:
```cs
// application-wide switch
Configuration.Linq.EnableContextSchemaEdit = true;

// context-wide option
var db = new MyContext(new DataOptions().UseEnableContextSchemaEdit(true));
```

But we wont recommend doing it as it will affect performance. Recommended approach is to create single mapping schema instance, configure it and use with all context instances. You can see example how to do it in [fluent mappings section](#fluent-mapping-changes).

##### disable support for mapping attributes from non-linq2db namespaces

In previous releases Linq To DB was able to consume some mapping attributes from `System.ComponentModel.DataAnnotations.Schema` and `System.Data.Linq.Mapping` namespaces.

Starting from this release support for those attributes is not enabled by default. Those mappings were used by minor part of users but calls to corresponding metadata providers are done for all users.

If you used them, you need to enable corresponding metadata readers:

```cs
// for System.Data.Linq.Mapping support
var reader = SystemDataLinqAttributeReader();
// for System.ComponentModel.DataAnnotations.Schema
var reader = SystemComponentModelDataAnnotationsSchemaAttributeReader();

// enable globally
MappingSchema.Default.AddMetadataReader(reader);

// enable for specific mapping schema (don't forget to setup
// schema once per-application to benefit from mappings caching)
mappingSchema.AddMetadataReader(reader);
```

#### Metadata Reader Changes

**_Obsoletion note: in final release we removed generics from `GetAttributes` and require from them to return all attributes always_**

If you implemented custom metadata reader in your application (`interface IMetadataReader`), you will need to make several changes to your implementation:

1. add `where T : MappingAttribute` generic constrain to `GetAttributes` methods implementation
2. change `GetAttributes` logic to support calls where `T = MappingAttribute`

From this release linq2db could call `GetAttributes` methods using base attribute type instead of concrete type. E.g. `GetAttributes<MappingAttribute>` instead of `GetAttributes<TableAttribute>`. To support this new behavior you will need to make two changes to your implementation.

Replace `T` check conditions to use `IsAssignableFrom`:

```cs

// old code
if (typeof(T) == typeof(TableAttribute))
{
   // create and return TableAttribute instance
}

// new check. takes inheritance into account
if (typeof(T).IsAssignableFrom(typeof(TableAttribute)))
{
   // create and return TableAttribute instance
}

```

Return all applicable attributes if all of them derived from `T`:

```cs

// old code
if (typeof(T) == typeof(ColumnAttribute))
{
   // create and return ColumnAttribute instance
}
else if (typeof(T) == typeof(Sql.ExpressionAttribute))
{
   // create and return Sql.ExpressionAttribute instance
}

// new code
// because T could represent base class, both supported attributes should be returned
var result = new List<T>();
if (typeof(T).IsAssignableFrom(typeof(ColumnAttribute)))
{
   // create ColumnAttribute instance
   result.Add(columnAttr);
}

if (typeof(T).IsAssignableFrom(typeof(Sql.ExpressionAttribute)))
{
   // create Sql.ExpressionAttribute instance
   result.Add(expressionAttr);
}

// return all applicable attributes
return result.ToArray();
```

***

### Release 5.0.0 Preview 1

<a name='release-500-preview1'></a>

- [#3530](https://github.com/linq2db/linq2db/issues/3530):
  - [#471](https://github.com/linq2db/linq2db/issues/471): fix issue when changing some application-wide options form `Configuration.Linq` could affect existing connections/cached queries, see more details below on options rework
  - [#472](https://github.com/linq2db/linq2db/issues/472): introduced connection-wide provider specific options instead of existing application-wide options
  - add `void RemoveInterceptor(IInterceptor interceptor)` method to contexts
  - add `DataConnection.OnRemoveInterceptor` event
  - improved SQL dialect detection logic for SQL Server, PostgreSQL and Oracle to cache detection results per-connection string
  - context options `DataOptions` instance accessible using `IDataContext.Options` property
  - [T4] added new T4 setting `string GetDataOptionsMethod` to specify name of custom static method with `DataOptions` return type, called by generated constructors
  - [t4] renamed T4 option `GenerateLinqToDBConnectionOptionsConstructors`  to `GenerateDataOptionsConstructors`
  - [SQL Server] default SQL Server dialect bumped to SQL Server 2012 from 2008
  - added support for automatic detection of SQL Server provider (`SqlServerProvider.AutoDetect`)
- [#3898](https://github.com/linq2db/linq2db/issues/3898): removed obsoleted APIs
- [#3902](https://github.com/linq2db/linq2db/issues/3902): [Sybase] fix issue with too long parameter name trimming

#### Provider Dialect Detection Changes

`AutoDetect` logic enabled by default for SQL Server, PostgreSQL and Oracle providers if user doesn't specify required dialect version explicitly:
- SQL Server provider will use `AutoDetect` logic to detect SQL dialect instread of `SQL Server 2008` (`2012` starting from this release) dialect by default. To disable it set `SqlServerTools.AutoDetectProvider` to `false`
- PostgreSQL provider will use `AutoDetect` logic to detect SQL dialect instread of `PostgreSQL 9.2` dialect by default. To disable it set `PostgreSQLTools.AutoDetectProvider` to `false`
- Oracle provider will use `AutoDetect` logic to detect SQL dialect instread of `Oracle 12` dialect by default. To disable it set `OracleTools.AutoDetectProvider` to `false`

#### Options Rework

[PR](https://github.com/linq2db/linq2db/pull/3530)

This PR introduce major options overhaul.

##### Required changes to configuration code

`LinqToDBConnectionOptionsBuilder` options builder class alongside with `LinqToDBConnectionOptions` and `LinqToDBConnectionOptions<T>` classes
replaced with `DataOptions[<T>]` classes. This will require from you to change configuration logic a bit:

```cs
// OLD CONFIGURATION APPROACH

// 1. create options builder
var optionsBuilder = new LinqToDBConnectionOptionsBuilder();
// 2. set options to builder
optionsBuilder.UseSqlServer(connectionString);
// 3. generate options object using Build() method and pass it to context or register in DI container
new DataContext(optionsBuilder.Build());

// NEW CONFIGURATION APPROACH

// 1. create immutable options object
var options = new DataOptions();
// 2. "mutate" it using same configuration methods as before
// IMPORTANT: because options object is immutable you should chaing configuration methods calls
// or save returned options object to variable
options = options.UseSqlServer(connectionString);
// 3. pass options to context/DI container
new DataContext(options);
```

As you can notice main changes are:
- two old configuration classes (builder and options) replaced with single options class
- because options are immutable (compared to old builder), you must not forget to use results of `Use*` configuration methods
- configuration build extensions not changed in general, several old extensions that were not used `Use*` naming pattern were renamed. See full list of renames below
- `AspNet` configuration extensions `AddLinqToDB`\`AddLinqToDBContext` now require that you return options instance from `configure` delegate parameter

##### Fix to application-wide options scope

Changes to application-wide configuration options (see list of options below) doesn't affect already created contexts and cached queries

- `Configuration.Linq.PreloadGroups`
- `Configuration.Linq.IgnoreEmptyUpdate`
- `Configuration.Linq.GenerateExpressionTest`
- `Configuration.Linq.TraceMapperExpression`
- `Configuration.Linq.DoNotClearOrderBys`
- `Configuration.Linq.OptimizeJoins`
- `Configuration.Linq.CompareNullsAsValues`
- `Configuration.Linq.GuardGrouping`
- `Configuration.Linq.DisableQueryCache`
- `Configuration.Linq.CacheSlidingExpiration`
- `Configuration.Linq.PreferApply`
- `Configuration.Linq.KeepDistinctOrdered`
- `Configuration.Linq.ParameterizeTakeSkip`
- `Configuration.Linq.EnableAutoFluentMapping`

Same logic applied to retry policy objects and application-wide options for them:

- `Configuration.RetryPolicy.DefaultRandomFactor`
- `Configuration.RetryPolicy.DefaultExponentialBase`
- `Configuration.RetryPolicy.DefaultCoefficient`

Also all those options now could be configured per-context using `Use<OptionName>` configuration extensions.

For `Configuration.Linq` options:
- `UsePreloadGroups`
- `UseIgnoreEmptyUpdate`
- `UseGenerateExpressionTest`
- `UseTraceMapperExpression`
- `UseDoNotClearOrderBys`
- `UseOptimizeJoins`
- `UseCompareNullsAsValues`
- `UseGuardGrouping`
- `UseDisableQueryCache`
- `UseCacheSlidingExpiration`
- `UsePreferApply`
- `UseKeepDistinctOrdered`
- `UseParameterizeTakeSkip`
- `UseEnableAutoFluentMapping`

For `Configuration.RetryPolicy` options:
- `UseRetryPolicy`
- `UseDefaultRetryPolicyFactory`
- `UseFactory`
- `UseMaxRetryCount`
- `UseMaxDelay`
- `UseRandomFactor`
- `UseExponentialBase`
- `UseCoefficient`

Added `UseBulkCopy<option_name>()` helpers to configure connection-wide bulk copy defaults.

Also for following option objects we added `With<option_name>` configuration extensions:
- `BulkCopyOptions`
- `RetryPolicyOptions`
- `QueryTraceOptions`
- `ConnectionOptions`
- `LinqOptions`

##### Provider-specific options

Introduced support for provider-specific options instead of application-wide settings, usually located in `<DB>Tools` provider classes.

Current release contains options for all providers (see list below). They could be configured globally using `<DB>Options.Default` options set or using `Use<DB>` configuration extension.

List of available options (old options were marked with `Obsolete` attribute):

- [ALL] `<DB>Tools.DefaultBulkCopyType` -> `<DB>Options.BulkCopyType`
- [MSSQL] `SqlServerConfiguration.GenerateScopeIdentity` -> `SqlServerOptions.GenerateScopeIdentity`
- [SQL CE] `SqlCeConfiguration.InlineFunctionParameters` -> `SqlCeOptions.InlineFunctionParameters`
- [SQLite] `SQLiteTools.AlwaysCheckDbNull` -> `SQLiteOptions.AlwaysCheckDbNull`
- [PostgreSQL] `PostgreSQLTools.NormalizeTimestampData` -> `PostgreSQLOptions.NormalizeTimestampData`
- [PostgreSQL] `PostgreSQLSqlBuilder.IdentifierQuoteMode` -> `PostgreSQLOptions.IdentifierQuoteMode`
- [Oracle] `OracleTools.DontEscapeLowercaseIdentifiers` -> `OracleOptions.DontEscapeLowercaseIdentifiers`
- [Informix] `InformixConfiguration.ExplicitFractionalSecondsSeparator` -> `InformixOptions.ExplicitFractionalSecondsSeparator`
- [Firebird] `FirebirdConfiguration.IdentifierQuoteMode` -> `FirebirdOptions.IdentifierQuoteMode`
- [Firebird] `FirebirdConfiguration.IsLiteralEncodingSupported` -> `FirebirdOptions.IsLiteralEncodingSupported`
- [DB2] `DB2SqlBuilderBase.IdentifierQuoteMode` -> `DB2Options.IdentifierQuoteMode`
- [ClickHouse] `ClickHouseConfiguration.UseStandardCompatibleAggregates` -> `ClickHouseOptions.UseStandardCompatibleAggregates`

#### T4 changes

T4 templates updated to:
- generate new options constructors
- added new T4 setting `string GetDataOptionsMethod` to specify name of custom static method with `DataOptions` return type, called by generated constructors
- renamed T4 option `GenerateLinqToDBConnectionOptionsConstructors`  to `GenerateDataOptionsConstructors`

#### Public API Changes/Renames

- `BulkCopyOption` changed from `class` to `class record`. Because class records are immutable, you should use `with` statement or new `With<BulkOption>()` helpers
- added `DataConnection` constructors with `Func<DataOptions,DataOptions> optionsSetter` parameter to build options using `DataConnection.DefaultDataOptions` as input
- method rename: `UseAccessODBC` -> `UseAccessOdbc`
- method rename: `WithInterceptor` -> `UseInterceptor`
- method rename: `WithTracing` -> `UseTracing`
- method rename: `WithTraceLevel` -> `UseTraceLevel`
- method rename: `WriteTraceWith` -> `UseTraceWith`
- property rename: `SqlServerTools.Provider` -> `SqlServerTools.DefaultProvider`
- property rename: `GrpcDataContext.Options` -> `GrpcDataContext.ChannelOptions`
- internal infrastructure support class `TypeExtensions` with `Type` extension methods (`UnwrapNullableType`, `IsNullableType`, `IsInteger`, `IsNumericType`, `IsSignedInteger`, `IsSignedType`) removed from public surface
- added `OracleVersion.AutoDetect`, `PostgreSQLVersion.AutoDetect`, `SqlServerVersion.AutoDetect` enum values to explicitly specify that SQL dialect should be detected by `LinqToDB` based on server version
- `LinqToDB.Common.Internal.ValueComparer[<T>]` classes removed from public surface

***

### Release 4.4.1

- [#3946](https://github.com/linq2db/linq2db/issues/3946):
  - [DB2][Firebird][Oracle] remove unnecessary wrapper subquery around `SELECT` with `CTE` for `INSERT FROM SELECT` queries
  - [DB2] fixed position of `CTE` clause in `INSERT FROM SELECT` queries
  - [#3945](https://github.com/linq2db/linq2db/issues/3945): fixed generation of addtional unnecessary columns in select sub-query with composite columns
  - [PostgreSQL][Firebird][MySql][MariaDb] don't add `RECURSIVE` keyword to simple `CTE` clauses, defined by `GetCte` API
***

### Release 4.4.0

- [#3218](https://github.com/linq2db/linq2db/issues/3218): [T4][SQLite] fixed issue with schema load where it could fail due to outdated sqlite runtime version used
- [#3579](https://github.com/linq2db/linq2db/issues/3579): corrected nullability annotations on `Sql.Collate` function
- [#3712](https://github.com/linq2db/linq2db/issues/3712): add `InsertWithOutput` extensions for `IValueInsertable` interface
- [#3751](https://github.com/linq2db/linq2db/issues/3751): treat non-nullable columns from left joins as nullable during SQL generation
- [#3760](https://github.com/linq2db/linq2db/issues/3760), [#3834](https://github.com/linq2db/linq2db/issues/3834): fixed issue where `InsertWithOutput` API cannot handle columns when column's name and property's name are different
- [#3761](https://github.com/linq2db/linq2db/issues/3761): improve mapping of `Nullable<T>.GetValueOrDefault()` to generate query parameter
- [#3762](https://github.com/linq2db/linq2db/issues/3762): [Scaffold] fix SQL Server scalar function mappings to always include schema name as required by SQL Server
- [#3777](https://github.com/linq2db/linq2db/issues/3777): [Scaffold] fixed issue where scaffold could generate classes/properties in different order between generations. From now generated code will have mappings for tables and functions ordered by name in database and columns by column ordinal
- [#3788](https://github.com/linq2db/linq2db/issues/3788), [#3872](https://github.com/linq2db/linq2db/issues/3872): improve `GroupBy(constant)` optimization to exclude unnecessary `GROUP BY` generation
- [#3791](https://github.com/linq2db/linq2db/issues/3791): fix `InvalidOperationException: No coercion operator is defined...` error when custom conversion specified between types with `TypeConverter` support
- [#3797](https://github.com/linq2db/linq2db/issues/3797): [ClickHouse] enable support for `EXCEPT ALL`/`INTERCEPT ALL` set operators
- [#3803](https://github.com/linq2db/linq2db/issues/3803): Scaffold framework improvements:
  - add `AfterSourceCodeGenerated(FinalDataModel model)` interceptor to provide access to data model used for code generation with actual indentifiers, used for generated code, set
  - add `string? ForeignKeyName` property to `AssociationModel` class
- [#3809](https://github.com/linq2db/linq2db/issues/3809): fix issue in conditional expressions handling for associated tables
- [#3810](https://github.com/linq2db/linq2db/issues/3810): [Scaffold] fixed composite foreign key association generation to use all key columns instead of first key column only 
- [#3811](https://github.com/linq2db/linq2db/issues/3811): Fix equality implementation for `System.Data.Linq.Binary` type for .net (core) runtimes
- [#3820](https://github.com/linq2db/linq2db/issues/3820): [Scaffold] add native support for `net6.0` and `net7.0` to scaffold cli tool
- [#3825](https://github.com/linq2db/linq2db/issues/3825): Fixed support for some mapping attributes in fluent mapping (`MapValueAttribute` and some more)
- [#3826](https://github.com/linq2db/linq2db/issues/3826): Improve error message for `CREATE TABLE` command when column database type cannot be determined automatically to include column member's .NET type name
- [#3829](https://github.com/linq2db/linq2db/issues/3829): [ClickHouse] add support for `ClickHouseDecimal` type from `ClickHouse.Client` provider
- [#3837](https://github.com/linq2db/linq2db/issues/3837): [Informix] Improve SQL generation for `Nvl` function
- [#3843](https://github.com/linq2db/linq2db/issues/3843): expose constructor of `DataReaderWrapper` class to unblock custom `DataReader` extensions implementation
- [#3854](https://github.com/linq2db/linq2db/issues/3854): [SQL Server] temporal tables support in schema/scaffold:
  - mark from/to validity range columns as read-only
  - mark temporal history table as read-only
  - add option to ignore history tables on schema load (`mssql-ignore-temporal-history-tables` flag in `linq2db.cli` and `GetSchemaOptions.IgnoreSystemHistoryTables` in schema API)
- [#3855](https://github.com/linq2db/linq2db/issues/3855): implement `IAsyncDisposable` on `DataContextTransaction`
- [#3863](https://github.com/linq2db/linq2db/issues/3863): disposal of transaction without explicit call to `Rollback` method will not call `Rollback` explicitly on transaction object anymore (transaction still be rolled back by database provider if needed)
  - **BREAKING** such transactions will not generate `RollbackTransaction` trace event anymore. Also we introduce new `DisposeTransaction` trace event as replacement
- [#3865](https://github.com/linq2db/linq2db/issues/3865): fixed `ValueConverter` support in remote context

***

### Release 4.3.0

- [#3462](https://github.com/linq2db/linq2db/issues/3462): support single query generation for queries with multiple `cross apply` clauses
- [#3759](https://github.com/linq2db/linq2db/issues/3759):
  - Update SQL Server 2022 support to RC.0 level (`chars` parameter support for `string.TrimEnd(string value, char[] chars)` and `string.TrimStart(string value, char[] chars)` mappings)
  - add `chars` parameter support for `string.TrimEnd(string value, char[] chars)` and `string.TrimStart(string value, char[] chars)` mappings for following databases:
    - SQLite
    - SAP HANA
    - PostgreSQL
    - Oracle
    - Informix
    - DB2
    - ClickHouse
    - SQL Server 2022
- [#3774](https://github.com/linq2db/linq2db/issues/3774): fix 4.2.0 regression with handling of `IN` clauses in columns

***

### Release 4.2.0

- [#1796](https://github.com/linq2db/linq2db/issues/1796): new `ClickHouse` database provider. See [details](#clickhouse) below
- [#3584](https://github.com/linq2db/linq2db/issues/3584): [PostgreSQL] Add `MERGE` queries support to `PostgreSQL` provider (requires PostgreSQL 15)
- [#3595](https://github.com/linq2db/linq2db/issues/3595): Add new command interceptor events `BeforeReaderDispose` and `BeforeReaderDisposeAsync` invoked before `DbDataReader` disposal
- [#3649](https://github.com/linq2db/linq2db/issues/3649): [PostgreSQL][SQLite] Improve `UPDATE ... FROM` queries support for PostgreSQL and SQLite
- [#3659](https://github.com/linq2db/linq2db/issues/3659): [SQL Server] Add initial support for SQL Server 2022:
  - new dialect `SqlServerVersion.v2022`
  - support for [INGORE NULLS](https://docs.microsoft.com/en-us/sql/t-sql/functions/first-value-transact-sql?view=sql-server-ver15) qualifier for FIRST_VALUE/LAST_VALUE window functions (requires CTP 2.0)
  - support for [IS [NOT] DISTINCT FROM](https://docs.microsoft.com/en-us/sql/t-sql/queries/is-distinct-from-transact-sql?view=sql-server-ver15) operator (requires CTP 2.1)
- [#3664](https://github.com/linq2db/linq2db/issues/3664): Fix parameters caching in `LoadWith` filters
- [#3667](https://github.com/linq2db/linq2db/issues/3667): Fix compatibility with `Npgsql` 7
- [#3668](https://github.com/linq2db/linq2db/issues/3668): Fixed `An item with the same key has already been added` error for `GroupBy` queries with eager load inroduced by 4.1.1 release
- [#3669](https://github.com/linq2db/linq2db/issues/3669): Fix duplicate columns detection for columns with conditional expressions
- [#3673](https://github.com/linq2db/linq2db/issues/3673): [Scaffold CLI] added new schema option to specify default database schema(s) explicitly (`default-schemas: string[]`)
- [#3676](https://github.com/linq2db/linq2db/issues/3676): [Scaffold CLI] fixed `transformation` option support from command line or `json` config
  - `none` value is recognized
  - help mentions proper name for `association` value (instead of non-existing `t4`)
- [#3679](https://github.com/linq2db/linq2db/issues/3679): [Scaffold] Add by-name filtering options for stored procedures, scalar and aggregate functions (table functions already covered):
  - `include-stored-procedures` / `exclude-stored-procedures`
  - `include-scalar-functions` / `exclude-scalar-functions`
  - `include-aggregate-functions` / `exclude-aggregate-functions`
- [#3683](https://github.com/linq2db/linq2db/issues/3683):
  - made changes to `Linq To DB` to support databases without transactions (e.g. `ClickHouse`)
  - added `TransientRetryPolicy` and `DbExceptionTransientExceptionDetector` classes to support new property `DbException.IsTransient` by retry policy mechanism. Available for `net6.0` tfm
- [#3684](https://github.com/linq2db/linq2db/issues/3684): fix `Argument types do not match` exception when `ValueConverter` with `HandlesNulls = true` option used
- [#3685](https://github.com/linq2db/linq2db/issues/3685): fix `null` values handling in enumerable sources to generate `NULL alias` instead of `NULLalias`, which could lead to sql errors for some databases
- [#3687](https://github.com/linq2db/linq2db/issues/3687): fix argument nullability tracking for `CAST` and some other functions to avoid null checks generation for non-nullable arguments
- [#3689](https://github.com/linq2db/linq2db/issues/3689): Fix date/time strings generation i queries to generate fixed-length components (e.g. `01:05` for time instead of `1:5`)
- [#3690](https://github.com/linq2db/linq2db/issues/3690):
  - obsolete `SqlDataType.GetDataType` method (`MappingSchema.GetDataType` should be used)
   - **[BREAKING]**: Conversion functions that doesn't specify exact type (`System.Convert.ToDecimal`, `LinqToDB.Common.Convert`) change behavior for decimal type. They will generate cast to decimal type definition, specified in mapping schema (usually `decimal` without precision and scale set). Previously they used hardcoded `decimal(29, 10)` type.
     - Workarounds:
       - register suitable decimal type mapping in mapping schema (not recommended): `mappingSchema.AddScalarType(typeof(decimal), new SqlDataType(new DbDataType(typeof(decimal), DataType.Decimal, null, null, <precision>, <scale>)));`
       - use better-typed convert API, e.g. `Sql.Convert(Sql.Types.Decimal(<precision>,<scale>), convertedValue)`
     - List of affected providers:
       - DB2: `decimal(29, 10)` -> `decimal`
       - Firebird: `decimal(18, 10)` -> `decimal`
       - SQL Sever CE: `decimal(29, 10)` -> `decimal`
       - SQL Sever: `decimal(29, 10)` -> `decimal`
       - MySQL/MariaDB: `decimal(29, 10)` -> `decimal`
       - SAP/Sybase ASE: `decimal(29, 10)` -> `decimal`
       - Oracle: `decimal(29, 10)` -> `decimal`
- [#3697](https://github.com/linq2db/linq2db/issues/3697): Fix issue when `UpdateWithOutput` could generate `NULL` instead of column in outputs for complex queries
- [#3701](https://github.com/linq2db/linq2db/issues/3701): add support for sequence name configuration for column using fluent mapping (`UseSequence` or `HasAttribute(SequenceNameAttribute)` methods)
- [#3703](https://github.com/linq2db/linq2db/issues/3703): [PostgreSQL] add support for `NpgsqlInterval` type from `Npgsql` 6
- [#3705](https://github.com/linq2db/linq2db/issues/3705):
  - [PostgreSQL] add type hint to `bytea` and `uuid` literals (`::bytea`, `::uuid`)
  - [PostgreSQL] add new SQL dialect `PostgreSQLVersion.v15`. Enables `MERGE` query generation for `InsertOrReplace/Update` APIs
- [#3709](https://github.com/linq2db/linq2db/issues/3709): [CLI] fix `ColumnType not provided by schema for table` error for `Access` scaffolding using `ODBC` provider for `decimal` columns
- [#3728](https://github.com/linq2db/linq2db/issues/3728): [Scaffold] Fix generation of association name for one-to-one relation over primary keys in CLI tool
- [#3729](https://github.com/linq2db/linq2db/issues/3729): fix `Merge` into table with query filters defined
- [#3731](https://github.com/linq2db/linq2db/issues/3731): [Oracle] reset `ArrayBindCount` property on `OracleCommand` to prevent exception on command reuse after `BulkCopy` in `AlternativeBulkCopy.InsertInto` mode
- [#3738](https://github.com/linq2db/linq2db/issues/3738): remove incorrect database type inferring logic for binary expressions
- [#3747](https://github.com/linq2db/linq2db/issues/3747): [Scaffold] CLI tool improvements
  - tolerate allow trailing commas in JSON config
  - properly detect and report conflicting options in JSON config

#### ClickHouse

This release adds initial support for `ClickHouse` database.

Following providers/protocols supported:

- MySQL interface using [MySqlConnector](https://github.com/mysql-net/MySqlConnector)
- HTTP(S) interface using [ClickHouse.Client](https://github.com/DarkWanderer/ClickHouse.Client)
- TCP (binary) interface using [Octonica.ClickHouseClient](https://github.com/Octonica/ClickHouseClient)

To see supported data type mappings and related issues with providers see [this](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/DataProvider/ClickHouse/README.md) document.

##### Additional features

- added new bulk copy options `BulkCopyOptions.MaxDegreeOfParallelism` and `BulkCopyOptions.WithoutSession` for `ClickHouse.Client` HTTP provider
- CLI scaffold: implemented
- T4 scaffold: not planned
- query parameters: not supported/not planned
- LinqPAD support: will be added in future releases
- Custom query hints/extensions: will be added in future releases (feedback on which extensions needed is welcome)

***

### Release 4.1.1

- [#3629](https://github.com/linq2db/linq2db/issues/3629): fix `OUTPUT`/`RETURNING` into temporary table
- [#3630](https://github.com/linq2db/linq2db/issues/3630): [SQL Server] fix type load exception for self-extracting applications
- [#3633](https://github.com/linq2db/linq2db/issues/3633): fix `AddLinqToDBContext` DI helper to support context constructors with additional parameters
- [#3636](https://github.com/linq2db/linq2db/issues/3636): fix `GROUP BY` query caching
- [#3637](https://github.com/linq2db/linq2db/issues/3637): improve typing of `CASE` expression

***

### Release 4.1.0

- [#861](https://github.com/linq2db/linq2db/issues/861): fix tracing for misconfigured/failed connection
- [#3557](https://github.com/linq2db/linq2db/issues/3557): Fix cases where optional association could produce `INNER JOIN` instead of `LEFT JOIN`
- [#3585](https://github.com/linq2db/linq2db/issues/3585): [Oracle] Add support for `Devart.Data.Oracle` provider
  - [#3610](https://github.com/linq2db/linq2db/issues/3610): fix scaffolding exception with Oracle 19+
- [#3591](https://github.com/linq2db/linq2db/issues/3591): fix exception from `DataContext.ctor(IDataProvider, string)` contructor
- [#3594](https://github.com/linq2db/linq2db/issues/3594): Improve typing of `COALESCE`
- [#3596](https://github.com/linq2db/linq2db/issues/3596): [SQL Server] Add `OPENJSON` function mapping (`LinqToDB.DataProvider.SqlServer.SqlFn.OpenJson(...)`)
- [#3601](https://github.com/linq2db/linq2db/issues/3601): [Oracle] Specify `DataType.Cursor` for `ref cursor` columns in scaffold
- [#3604](https://github.com/linq2db/linq2db/issues/3604): [Scaffold] Fixed include/exclude object filter parsing when both `schema` and `name`/`regex` filters specified
- [#3611](https://github.com/linq2db/linq2db/issues/3611): [MySQL] Support `ColumnAttribute.Length` values in [256, 65535] range for `VarChar` type for `CreateTable` APIs
- [#3612](https://github.com/linq2db/linq2db/issues/3612): [Scaffold] Fix exception from `help scaffold` command from some environments
- [#3615](https://github.com/linq2db/linq2db/issues/3615): [Scaffold] Update naming options configuration approach. Now you don't need to specify all naming option properties from scratch if you want to change only one property (e.g. name casing) - values for unspecified properties will be taken from default option value

***

### Release 4.0.1

- [#1878](https://github.com/linq2db/linq2db/issues/1878): fix `Guid` compatibility issue with `Microsoft.Data.SQLite` 3+. Now it is possible to map `Guid` to text by using `Column(DbType="TEXT")` or `Column(DataType=DataType.ONE_OF_TEXT_TYPES_HERE)`. Default `Guid` mapping is still remains `binary`
- [#3563](https://github.com/linq2db/linq2db/issues/3563): add missing dependencies to `linq2db.PostgreSQL` T4 nuget
- [#3564](https://github.com/linq2db/linq2db/issues/3564): fixed issues with pre-generated scaffolding template (missing namespace and NRT warnings). Thanks to [Alexey Zagoskin](https://github.com/Powerz) for fixing it
- [#3567](https://github.com/linq2db/linq2db/issues/3567): improve scaffold tool help to include default switch values for T4 compat mode
- [#3568](https://github.com/linq2db/linq2db/issues/3568): fixed names of generated files by scaffold tool to have same name as generated class even after class renamed by scaffold interceptor
- [#3569](https://github.com/linq2db/linq2db/issues/3569): add new switch `--generated-suffix` to scaffold tool to add `.generated` suffix to generated file names
- [#3570](https://github.com/linq2db/linq2db/issues/3570): [SQL Server] fixed handling of date/time types (`DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `SqlDateTime`) to respect precision and generate typed literals in queries
- [#3575](https://github.com/linq2db/linq2db/issues/3575): fixed `linq2db.AspNet` nuget to reference proper version of `Microsoft.Extensions.DependencyInjection`
- [#3578](https://github.com/linq2db/linq2db/issues/3578): [CLI] removed database name from name of table/view/procedure, returned by schema by default. Use new option `--database-in-name` to re-enable it

***

### Release 4.0.0

No changes since RC2.

***

### Release 4.0.0 Release Candidate 2

<a name='release-400-rc2'></a>

- [#3502](https://github.com/linq2db/linq2db/issues/3502): `DateOnly` type support (all databases)
- [#3506](https://github.com/linq2db/linq2db/issues/3506): fix missing `Nullable<T>.Value` handling in some places
- [#3534](https://github.com/linq2db/linq2db/issues/3534): Scaffold tool improvements
  - adds extension points to customize generated entities, procedures, functions and associations
  - adds `--equatable-entities` option to implement functionality of `Equatable.ttinclude` T4 template in scaffold tool (generation of `IEquality<T>` interface implementation on entity classes)
- [#3536](https://github.com/linq2db/linq2db/issues/3536): add support for [database packages](#packages-support)
  - add new `Package` property in `Sql.TableFunctionAttribute`
  - automatically wrap table function call in `TABLE(...)` wrapper for `DB2 LUW` and `Oracle 11` dialects
  - [Oracle][Firebird][DB2] add support for functions and packages to Schema API
  - add support for package procedures and functions to scaffold by `T4` and [`linq2db.cli`](https://www.nuget.org/packages/linq2db.cli)
- [#3541](https://github.com/linq2db/linq2db/issues/3541): improve value nullability tracking, annotate functions in `Sql` class for nullability where it was missing
- [#3542](https://github.com/linq2db/linq2db/issues/3542): fix support for members, marked with `Sql.ExpressionAttribute`, in association queries
- [#3543](https://github.com/linq2db/linq2db/issues/3543): fix rare `IndexOutOfRangeException` in queries with functions
- [#3547](https://github.com/linq2db/linq2db/issues/3547): add `Sql.NullIf` function, mapped to `NULLIF` sql function (with emulation for databases which doesn't support it)
- [#3548](https://github.com/linq2db/linq2db/issues/3548): fixed regression in `PostgreSQL` enum parameters support

#### Packages Support

Database package is a named group of procedures and functions (plus types and variables in some DB), supported by following databases:

- Oracle Database
- DB2 LUW (under `module` name)
- Firebird 3+
- SAP HANA (under `library` name)
- MariaDB

By support of packages in `Linq To DB` we mean following:

- support of package table functions mapping using `Sql.TableFunctionAttribute` by adding `Sql.TableFunctionAttribute.Package` property to specify package name for function
- support of package functions and procedures in `Schema API`
- support for package functions and procedures in database scaffolding (using both `T4` templates and [`linq2db.cli`](https://www.nuget.org/packages/linq2db.cli) utility)

Known issues and limitations:

- [MariaDB] package stored procedures cannot be called using stored procedure API (`QueryProc`, `ExecuteProc`) with `MySql.Data` provider (provider bug). User should use raw sql calls like `CALL package.proc(...)` or even better - switch to [`MySqlConnector`](https://mysqlconnector.net/) provider
- [SAP HANA] library functions/procedures cannot be called using native unmanaged provider due to provider bug. `ODBC` provider should be used
- [SAP HANA][MariaDB] packages not supported by Schema API (and scaffolding) because those databases don't expose required metadata

***

### Release 4.0.0 Release Candidate 1

<a name='release-400-rc1'></a>

Also contains all changes from [Release 3.7.0](#release-370).

For migration notes check [this document](Version-4-Migration).

- [#2452](https://github.com/linq2db/linq2db/issues/2452): Query Extensions API to extend queries with custom SQL at specific points. See [details](#query-extensions-api) below.
  - [SQL Server] adds new SQL Server dialect levels: `SqlServerVersion.v2014` and `SqlServerVersion.v2019` enum values and `ProviderName.SqlServer2019` identifier
  - improved SQL optimization to remove unnecessary nesting for SQL like `SELECT * FROM (sub-query)`
- [#2582](https://github.com/linq2db/linq2db/issues/2582): Improved handling of changes to mapping schema to avoid issues when changes ignored due to cached queries
- [#2619](https://github.com/linq2db/linq2db/issues/2619): Improved support for set (`UNION`-like) queries with sorting
- [#3097](https://github.com/linq2db/linq2db/issues/3097): Default value for `Configuration.ContinueOnCapturedContext` setting changed to `false` 
(as it should be)
- [#3098](https://github.com/linq2db/linq2db/issues/3098): New database scaffold dotnet tool implemented to replace old T4 templates. See [more details](#scaffold-tool) below
- [#3410](https://github.com/linq2db/linq2db/issues/3410): Remote context refactoring. See [more details](#remote-context) below
- [#3411](https://github.com/linq2db/linq2db/issues/3411): [SQL Server] Added pre-generated mappings for `sys` and `INFORMATION_SCHEMA` schemas for SQL Server:
  - `LinqToDB.DataProvider.SqlServer.SqlType`: type name generators
  - `LinqToDB.DataProvider.SqlServer.SqlFn`: functions and variables
  - `LinqToDB.Tools.DataProvider.SqlServer.Schemas.SystemDB` (`linq2db.Tools` nuget package): system schemas data context
- [#3441](https://github.com/linq2db/linq2db/issues/3441): Removed `netcoreapp2.1` TFM. Linq To DB still available for .NET Core 2.1 using `netstandard2.0` TFM.
- [#3496](https://github.com/linq2db/linq2db/issues/3496): [**Breaking**] `DataConnection.GetTable` instance methods removed as they duplicate existing data context extension methods
- [#3499](https://github.com/linq2db/linq2db/issues/3499): Enable CTE support for SQL Server 2005 dialect
- ~[#3502](https://github.com/linq2db/linq2db/issues/3502): `DateOnly` type support (all databases)~ due to packaging error, feature will be shipped in RC2
- [#3508](https://github.com/linq2db/linq2db/issues/3508): Support `IReadOnlyDictionary.ContainsKey` method in queries
- [#3514](https://github.com/linq2db/linq2db/issues/3514): fixed SQL generation for `[NOT] IN (...)` operator in `CompareNullsAsValues = true` mode to use value semantics for `null`
- [#3515](https://github.com/linq2db/linq2db/issues/3515): [**Breaking**] `Sql.<type_name>` database type name generation properties moved to `Sql.Types.` "namespace" to avoid naming conflicts with `using static Sql` code
- [**BREAKING**] Default SQL Server provider changed from `System.Data.SqlClient` to `Microsoft.Data.SqlClient` (you can use `SqlServerTools.Provider` property to change it back)
- [**BREAKING**] We are marking most of database provider classes (e.g. `SqlServerDataProvider`) as abstract. There are many reasons why you shouldn't create provider instance manually. Proper way to get provider instance is to use `<dbname>Tools.GetDataProvider(...)` method or `DataConnection.GetDataProvider(...)` methods. Only situation when you should create provider instance manually is when you sub-class provider to override some of it's methods. And even is such case it is better to register your provider implementation in `DataConnection` with `AddDataProvider` method.

#### Obsoletions

With this release we obsolete some code:
- several properties on `AssociationAttribute` (`KeyName`, `BackReferenceName`, `IsBackReference`, `Relationship`) and `Relationship` enum. It was never used by `Linq To DB`. T4 templates and scaffolding tool will not generate them anymore.
- `DataConnection.OnTrace` property. `LinqToDBConnectionOptions.OnTrace` should be used instead

#### Scaffold Tool

For detailed documentation check [tool nuget readme](https://www.nuget.org/packages/linq2db.cli#readme-tab).

For feedback please use this [discussion](https://github.com/linq2db/linq2db/discussions/3531).

With RC1 we release initial version of new database scaffolding tool, which will replace T4 templates.

We still plan to ship T4 templates for `Linq To DB` 4 but there is no plans to introduce new T4 features and we plan to obsolete them completely in future (`Linq To DB` 5 probably?).

##### Reasoning behind new tool development

T4 templates were introduced in times when Visual Studio and .NET Framework were only development platforms and doesn't reflect current situation on .NET world anymore:
- Visual Studio not available for non-Windows platforms and other IDEs lack support for T4 preprocessing features, required for them to work (e.g. MS Build directives support)
- Visual Studio runs T4 templates using .NET Framework as host (x86 for VS 2017- and x64 process for VS 2022). Other IDEs could use .NET Core runtime for T4 templates. This creates two usability issues:
  - T4 templates use .NET Framework versions of database providers which will not work in .NET Core hosts
  - some database providers use unmanaged code and require specific process architecture, but Visual Studio doesn't give users such choice

Those are main issues, that cannot be solved with current T4 templates architecture.

##### What is implemented for current release

Current release (4.0.0-rc.1) ships base scaffolding functionality and initial support for scaffolding process customization. Currently customization includes hooks for database schema load process. Later releases will add remaining hooks.

List of additional changes:
- new implementation has full control over generated code which allows it to generate valid code where T4 templates could fail:
  - automatic detection and fix of naming conflicts within generated code including method overloads handling and support for conflicting names from external code (requires from user to provide list of conflicting identifiers). E.g. see T4 issues [#1586](https://github.com/linq2db/linq2db/issues/1586), [#2168](https://github.com/linq2db/linq2db/issues/2168)
  - proper literals generation (e.g. escaping of characters in generated strings)
  - language-agnostic model allows generation of data model code in different laanguages/language versions (current release ships only C# support, but at least we plan to introduce F# support in future)
- [#1825](https://github.com/linq2db/linq2db/issues/1825): file-per-class code generation support
- [#1897](https://github.com/linq2db/linq2db/issues/1897): new option to enable/disable `@return` parameter scaffolding for SQL Server stored procedures
- [#2793](https://github.com/linq2db/linq2db/issues/2793): new options to generate various entity `Find` and `FinqQuery` extension methods including async versions of `Find` method
- [Access] support for mixed scaffold mode, which use two database providers to load database schema: `OLE DB` and `ODBC` providers. This feature is needed, because both `OLE DB` and `ODBC` providers return incorrect/incomplete database schema, but those errors are specific only to one provider. When we merge schemas from both providers - we receive correct database schema for scaffolding.
- support for generation of async stored procedure mappings
- more flexible identifier generation options
- extensibility hook to override type mapping of database type to .NET type
- added some more database types mappings to .net type in scaffolding (previously default type `object` used):
  - [Sybase]: `usmallint`, `uint`, `ubigint`, `bigdatetime`, `date`, `time`, `bigtime`, `unitext`, `unichar`, `univarchar`
  - [Oracle]: `BINARY_INTEGER`, `INTERVAL DAY TO ...`, `INTERVAL YEAR TO MONTH`
  - [PostgreSQL]: `regproc` (as string), custom enum (as string, in future we can add C# enum generation), default multi-/range types (except custom range types)
  - scaffold tool will generate error message to console when unknown database type found. If this is standard type - it is recommended to report it as issue so we can add support for it. For custom types it is better to use extensibility hook to specify proper .net type for it.

#### Remote Context

Thanks to [Vyacheslav Avdeev](https://github.com/lsoft) for [PR](https://github.com/linq2db/linq2db/pull/3410).

Remote data context allows `Linq To DB` to work with database indirectly by sending LINQ queries to server application over network. This feature were introduced long time ago mostly to facilitate database access for runtimes with limited functionality (e.g. currently dead Silverlight or mono-based mobile frameworks).

Initial implementation (pre-4 versions) supported only .NET Framework and two transports:
- WCF
- ASMX WebServices

With 4.0 release we refresh this feature to support modern frameworks and transports. List of changes:
- ASMX WebServices transport support removed (this is quite old technology nobody really should use for at least recent 10 years)
- GRPC transport added
- .NET (Core) support added (GRPC-only, WCF implementation currently limited to .NET Framework)
- WCF an GRPC transport implemntations support async transport APIs for async queries (previously all network requests used blocking API)
- Core functionality made public to allow new transport implementations by users
- Existing transports implementations moved to separate nugets: [linq2db.Remote.Grpc](https://www.nuget.org/packages/linq2db.Remote.Grpc) and [linq2db.Remote.Wcf](https://www.nuget.org/packages/linq2db.Remote.Wcf)

We can add more transports in future on request.

For use examples, check examples [here](https://github.com/linq2db/linq2db/tree/master/Examples) (Remote folder).

#### Interceptors (Changes since previous releases)

You can also read about interceptors in `Linq To DB` [here](https://linq2db.github.io/articles/general/interceptors.html).

This release brings some changes to interceptors feature, introduced in earlier releases:
- added new `IUnwrapDataObjectInterceptor` interceptor to work with data connection wrappers, e.g. [MiniProfiler](https://miniprofiler.com/dotnet/)
- moved `EntityCreated` interceptor event from `IDataContextInterceptor` to separate `IEntityServiceInterceptor`
- renamed `ConnectionOpenedEventData` and `ConnectionOpeningEventData` structs to `ConnectionEventData`
- added `AfterExecuteReader` interceptor to `ICommandInterceptor` interface

##### `ICommandInterceptor.AfterExecuteReader`

This interceptor is triggered after `DbCommand.ExecuteReader(CommandBehavior)` or `DbCommand.ExecuteReaderAsync(CommandBehavior, CancellationToken)` calls before reader enumeration. E.g. it could be used to modify reader options before data enumeration (Oracle provider could have such options).

```cs
void AfterExecuteReader(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, DbDataReader dataReader);
```

##### `IUnwrapDataObjectInterceptor`

This interceptor replaces old approach, where you need to register unwrap expressions in `MappingSchema`.

Example of interceptor implementation for `MiniProfiler`:
```cs
public class UnwrapProfilerInterceptor : UnwrapDataObjectInterceptor
{
    // as interceptor is thread-safe, we will create
    // and use single instance of it
    public static readonly IInterceptor Instance = new UnwrapProfilerInterceptor();

	public override DbConnection UnwrapConnection(IDataContext dataContext, DbConnection connection)
	{
		return connection is ProfiledDbConnection c ? c.WrappedConnection : connection;
	}

	public override DbTransaction UnwrapTransaction(IDataContext dataContext, DbTransaction transaction)
	{
		return transaction is ProfiledDbTransaction t ? t.WrappedTransaction : transaction;
	}

	public override DbCommand UnwrapCommand(IDataContext dataContext, DbCommand command)
	{
		return command is ProfiledDbCommand c ? c.InternalCommand : command;
	}

	public override DbDataReader UnwrapDataReader(IDataContext dataContext, DbDataReader dataReader)
	{
		return dataReader is ProfiledDbDataReader dr ? dr.WrappedReader : dataReader;
	}
}
```


#### Query Extensions API

For additional examples and documentation check [this article](https://linq2db.github.io/articles/sql/Query-Extensions.html).

This new feature allows user to attach custom SQL to LINQ queries at several extension points. This allows user to use most of database-specific SQL statement extensions without downgrading to raw SQL queries. E.g.:
- specify database-specific query hints
- annotate queries or subqueries with custom comments
- other non-hint code

In previous versions of Linq To DB we had several API with for limited support for such extensions:
- `With` table extension to add MSSQL-style table hint `WITH(...)`
- `WithTableExpression` table extension to add custom table hint
- `DataConnection.QueryHints` and `DataConnection.NextQueryHints` to add custom SQL as suffix or prefix to next query or queries.
Those API are still available and supported. E.g. `QueryHints` and `NextQueryHints` could be useful with non-linq APIs, where new extensions API is not available.

For list of extension points check [this table](https://linq2db.github.io/articles/sql/Query-Extensions.html#common-hint-extensions). More extension points could be added on user request in future.

Except extension API itself we added a lot of ready-to-use extensions (mostly hints) for supported databases. If you still cannot find some hint you need - create feature request. Available extensions:
- `LinqToDB.DataProvider.<DB_NAME>.<DB_NAME>Hints` extension classes with db-specific hints for tables, joins, (sub-)queries
- `As<DB_NAME>()` extension method in `LinqToDB.DataProvider.<DB_NAME>` namespace which could be used to specify extensions for use with specific database type. This is useful for applications that should work with multiple database types (e.g. `Oracle` and `PostgreSQL`) and want to use same linq query code for them.
- `QueryName` extension to set a name to (sub-)query which will be used as reference to query from query-specific hint (for databases with query naming support)
- `TableId`, `Sql.TableName`, `Sql.TableSpec`, `Sql.TableAlias` methods to assign identifier to table for reference from hint. It is similar to `QueryName` but used for tables instead of (sub-)queries
- hint methods `TableHint`, `TablesInScopeHint`, `IndexHint`, `JoinHint`, `SubQueryHint` and `QueryHint`

To create your own extension you can use new `Sql.QueryExtensionAttribute` attribute. Check existing extensions for examples.

***

### Release 4.0.0 Previews 2-10

No new v4-specific features, only changes from underlying v3 releases:
- <a name='release-400-preview10'>preview 10</a>: updates from [3.6.0](#release-360)
- <a name='release-400-preview9'>preview 9</a>: updates from [3.5.2](#release-352)
- <a name='release-400-preview8'>preview 8</a>: updates from [3.5.1](#release-351)
- <a name='release-400-preview7'>preview 7</a>: updates from [3.5.0](#release-350)
- <a name='release-400-preview6'>preview 6</a>: updates from [3.4.5](#release-345)
- <a name='release-400-preview5'>preview 5</a>: updates from [3.4.4](#release-344)
- <a name='release-400-preview4'>preview 4</a>: updates from [3.4.3](#release-343)
- <a name='release-400-preview3'>preview 3</a>: updates from [3.4.2](#release-342)
- <a name='release-400-preview2'>preview 2</a>: updates from [3.4.1](#release-341)

***

### Release 4.0.0 Preview 1

<a name='release-400-preview1'></a>

With this release we start to publish previews of Linq To DB v4 (planned for release later this year). Previews will include all features and fixes from current v3 release and new features, fixes and refactorings for v4.
This will allow users to use new features that require major version bump due to breaking changes without waiting for next major release.

Based on: v3.4.0. For migration notes check [this document](Version-4-Migration)

- [#2728](https://github.com/linq2db/linq2db/issues/2728):
  - [#2643](https://github.com/linq2db/linq2db/issues/2643): MARS-like queries support. This feature will unblock execution of queries inside of enumeration of another open query without force materialization (e.g. using `ToList()/ToAray()` methods):
    - [SQL Server]: `MultipeActiveResutSets=true` connection option required
    - [MySQL/MariaDB][PostgreSQL]: feature is not supported due to provider or database protocol limitation
  - [**BREAKING**] Access to current command data for data connection changed to support multiple active commands:
    - `DataConnection.LastParameters` property removed
    - `DataConnection.Command` property removed
    - procedures, generated using T4 templates should be regenerated, as they were using removed properties
    - to access `Command` (on handle other events/extension points in future) we introduce interceptors support, similar to [EF.Core interceptors](https://docs.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors) with some distinctions (see below)
    - following `DataConnection` events replaced with interceptors: `OnBeforeConnectionOpen`, `OnBeforeConnectionOpenAsync`, `OnConnectionOpened`, `OnConnectionOpenedAsync`
  - [Sybase] Native bulk copy against temp table will automatically downgrade to SQL-based bulk copy implementation as native bulk copy doesn't support temp tables
- [#2812](https://github.com/linq2db/linq2db/issues/2812): SQL Server 2000 support was removed (min. supported SQL Server version is 2005 from now). If you still need to support SQL Server 2000 you can use Linq To DB v3 or request support restore from us.
- [#2826](https://github.com/linq2db/linq2db/issues/2826): Adds `Guid` type support for Firebird provider. Thanks to [Eugine Savin](https://github.com/jack128) for contribution. By default `Guid` is mapped to Firebird `UUID` type (`CHAR(16) CHARACTER SET OCTETS`). Specifying `DataType = DataType.Char` or `DataType = DataType.NChar` in mapping will map it to `CHAR(38)`
  - [#2823](https://github.com/linq2db/linq2db/issues/2823): properly generate type name for `Guid` (e.g. for `CreateTable` API or `CAST` expression) based used `DataType`
  - [#2833](https://github.com/linq2db/linq2db/issues/2833): support literal generation for `Guid` based used `DataType`
  - note that in previous versions of `Linq To DB` `Guid` support was limited to `CHAR(38)` string literal generation for parameters and if you want to preserve mapping to string for your model, you should annotate it with `DataType.Char`
- [#2929](https://github.com/linq2db/linq2db/issues/2929): replace se of ADO.NET interfaces with corresponding base classes everywhere. Following changes done:
  - `IDataRecord`/`IDataReader` -> `DbDataReader`
  - `IDbCommand` -> `DbCommand`
  - `IDbDataParameter` -> `DbParameter`
  - `IDbConnection` -> `DbConnection`
  - `IDbTransaction` -> `IDbTransaction`
  - [**BREAKING**]: if you had custom mappings that use those interfaces, they should be updated to use classes, e.g.:
    - `MiniProfiler` unwrap mappings
    - custom data reader expressions
- [#2930](https://github.com/linq2db/linq2db/issues/2930): remove functionality, marked in v3 with `[Obsolete]` attribute
- [#2941](https://github.com/linq2db/linq2db/issues/2941):
  - [#2934](https://github.com/linq2db/linq2db/issues/2934): add support for `LinqToDbConnectionOptions` to `DataContext`
  - [#2927](https://github.com/linq2db/linq2db/issues/2927): migrate more `DataConnection` and `DataContext` events to interceptors (see details [here](https://github.com/linq2db/linq2db/wiki/Releases-and-Roadmap#interceptors))
  - [SQLite] use `temp` schema for temporary tables explicitly to avoid naming conflicts with `main` schema

#### Interceptors

With this release we are starting migration from events to interceptors and introduce first interceptor events. For initial release we concentrating on migration of already existed functionality (e.g. events) to interceptors. New events without prior implementation will be added later or on request.

To see which APIs were replaced with interceptors check [migration notes](Version-4-Migration)

###### `ICommandInterceptor`

This interceptor provides access to events and operations associated with database command.

```cs
// triggered after command initialization but before execution
// it provides access to prepared SQL command and parameters
DbCommand CommandInitialized(CommandEventData eventData, DbCommand command);

// triggered before `ExecuteScalar/ExecuteScalarAsync` call on command
// and could replace actual call by returning results from interceptor
Option<object?>       ExecuteScalar     (
                                         CommandEventData eventData,
                                         DbCommand command,
                                         Option<object?> result);
Task<Option<object?>> ExecuteScalarAsync(
                                         CommandEventData eventData,
                                         DbCommand command,
                                         Option<object?> result,
                                         CancellationToken cancellationToken);

// triggered before `ExecuteNonQuery/ExecuteNonQueryAsync` call on command
// and could replace actual call by returning results from interceptor
Option<int>       ExecuteNonQuery     (CommandEventData eventData, DbCommand command, Option<int> result);
Task<Option<int>> ExecuteNonQueryAsync(
                                       CommandEventData eventData,
                                       DbCommand command,
                                       Option<int> result,
                                       CancellationToken cancellationToken);

// triggered before `ExecuteReader/ExecuteReaderAsync` call on command
// and could replace actual call by returning results from interceptor
Option<DbDataReader>       ExecuteReader     (
                                              CommandEventData eventData,
                                              DbCommand command,
                                              CommandBehavior commandBehavior,
                                              Option<DbDataReader> result);
Task<Option<DbDataReader>> ExecuteReaderAsync(
                                              CommandEventData eventData,
                                              DbCommand command,
                                              CommandBehavior commandBehavior,
                                              Option<DbDataReader> result,
                                              CancellationToken cancellationToken);

struct CommandEventData
{
    public DataConnection DataConnection { get; }
}

// convinience base class for custom interceptor implementation
public abstract class CommandInterceptor : ICommandInterceptor
{
    // interceptor implementation as no-op virtual methods
}
```

###### `IDataContextInterceptor`

This interceptor provides access to events and operations associated with database context (built-in class that implements `IDataContext`, e.g. `DataConnection` or `DataContext`).

```cs
// triggered when new entity created during query materialization
// (except queries with explicit constructor call)
object EntityCreated(DataContextEventData eventData, object entity);

// triggered before data context instance `Close/CloseAsync` method execution
void OnClosing(DataContextEventData eventData);
Task OnClosingAsync(DataContextEventData eventData);

// triggered after data context instance `Close/CloseAsync` method execution
void OnClosed(DataContextEventData eventData);
Task OnClosedAsync(DataContextEventData eventData);

struct DataContextEventData
{
    public IDataContext Context { get; }
}

// convinience base class for custom interceptor implementation
public abstract class DataContextInterceptor : IDataContextInterceptor
{
    // interceptor implementation as no-op virtual methods
}
```

###### `IConnectionInterceptor`

This interceptor provides access to events and operations associated with database connection.

```cs
// triggered before data connection `Open/OpenAsync` method execution
void ConnectionOpening(ConnectionOpeningEventData eventData, DbConnection connection);
Task ConnectionOpeningAsync(
                            ConnectionOpeningEventData eventData,
                            DbConnection connection,
                            CancellationToken cancellationToken);

// triggered after data connection `Open/OpenAsync` method execution
void ConnectionOpened(ConnectionOpenedEventData eventData, DbConnection connection);
Task ConnectionOpenedAsync(
                           ConnectionOpenedEventData eventData,
                           DbConnection connection,
                           CancellationToken cancellationToken);

struct ConnectionOpenedEventData
{
    public DataConnection DataConnection { get; }
}

// convinience base class for custom interceptor implementation
public abstract class ConnectionInterceptor : IConnectionInterceptor
{
    // interceptor implementation as no-op virtual methods
}
```

##### Configuration

To register interceptor you can use:
- `AddInterceptor` method on data context (e.g. `DataConnection` or `DataContext`)
- add interceptor to `LinqToDbConnectionOptions` using `LinqToDbConnectionOptionsBuilder.AddInterceptor(interceptor)` API
- for one-time execution of `ICommandInterceptor.CommandInitialized` event you can use `db.OnNextCommandInitialized(interceptor delegate)` method on `DataConnection` or `DataContext`

```cs
// registration in DataContext
using (var ctx = new DataContext(...))
{
    ctx.AddInterceptor(interceptor);

    // one-time command prepared interceptor
    ctx.OnNextCommandInitialized((args, cmd) =>
    {
        // save next command parameters to external variable
        parameters = cmd.Parameters.Cast<DbParameter>().ToArray();
	return cmd;
    });
}

// registration in DataConnection
using (var ctx = new DataConnection(...))
{
    ctx.AddInterceptor(interceptor);

    // one-time command prepared interceptor
    ctx.OnNextCommandInitialized((args, cmd) =>
    {
        // set oracle-specific command option for next command
        ((OracleCommand)command).BindByName = false;
    });
}

// registration in DataConnection using fluent configuration
var builder = new LinqToDbConnectionOptionsBuilder()
    .UseSqlServer(connectionString)
    .WithInterceptor(interceptor);
var dc = new DataConnection(builder.Build());
```
