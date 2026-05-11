## About This Release

In Linq To DB 6 we concentrated on refactoring of existing query parser, which architecture had no significant changes for more than 10 years, leading to multiple issues when you need to add new functionality or fix existing one. With this refactoring we plan to reach following goals:

- make code more clean and easy to understand;
- get rid of a lot of shortcuts and workarounds;
- fix performance issues with old parser implementation;
- fix long standing issues in query parsing and generation;
- prepare grounds for future improvements.

Doing such refactorings could introduce regressions, so we urge you to try it early and report any issues you discovered.

- [About This Release](#about-this-release)
- [Changes Since Preview 1 Release](#changes-since-preview-1-release)
  - [Preview 2](#preview-2)
    - [Fixed Preview 1 Regressions](#fixed-preview-1-regressions)
    - [Entity Framework Core Integration](#entity-framework-core-integration)
  - [Preview 3](#preview-3)
  - [Preview 4](#preview-4)
  - [Release Candidate 1](#release-candidate-1)
  - [Release Candidate 2](#release-candidate-2)
  - [Release Candidate 3](#release-candidate-3)
  - [Final Release](#final-release)
- [Important Migration Notes](#important-migration-notes)
  - [`Sql.Expression` Parameterization](#sqlexpression-parameterization)
  - [Changes to `MappingSchema.SetConvertExpression<?, DataParameter>` calls](#changes-to-mappingschemasetconvertexpression-dataparameter-calls)
  - [Changes to `query.ToString()` Behavior (including EF.Core Integration)](#changes-to-querytostring-behavior-including-efcore-integration)
  - [Changes to Scalar Types Detection](#changes-to-scalar-types-detection)
- [API Removals/Changes](#api-removalschanges)
  - [Changes to GroupBy extensions](#changes-to-groupby-extensions)
  - [linq2db.AspNet NuGet Rename](#linq2dbaspnet-nuget-rename)
  - [Preview 2 Obsoletions](#preview-2-obsoletions)
- [Target Frameworks Review](#target-frameworks-review)
- [Performance Improvements](#performance-improvements)
- [New features](#new-features)
  - [More `IAsyncEnumerable` Support](#more-iasyncenumerable-support)
    - [New SQL-based IAsyncEnumerable APIs](#new-sql-based-iasyncenumerable-apis)
    - [New \*OutputAsync APIs](#new-outputasync-apis)
  - [Helper For Table Functions and Expressions Mapping](#helper-for-table-functions-and-expressions-mapping)
  - [Ordinal Generation API](#ordinal-generation-api)
  - [Support Select With Indexer Overload](#support-select-with-indexer-overload)
  - [Mapping Changes](#mapping-changes)
  - [New Query Parser](#new-query-parser)
    - [Composite properties tracking improvemens](#composite-properties-tracking-improvemens)
    - [Projection handling improvements](#projection-handling-improvements)
    - [Aggregation functions improvements](#aggregation-functions-improvements)
    - [Improvements to DML queries with OUTPUT/RETURNING clause](#improvements-to-dml-queries-with-outputreturning-clause)
    - [Improvements to MERGE queries](#improvements-to-merge-queries)
    - [Improvements to CTE (Common Table Expression) clause](#improvements-to-cte-common-table-expression-clause)
    - [Improvements to SET operators translation](#improvements-to-set-operators-translation)
    - [Better non-SQL `GroupBy` handling](#better-non-sql-groupby-handling)
    - [Column nullability tracing](#column-nullability-tracing)
    - [Imrovements to sub-queries and JOINs](#imrovements-to-sub-queries-and-joins)
      - [Ineffective ORDER BY removal](#ineffective-order-by-removal)
      - [Better JOINs optimization](#better-joins-optimization)
      - [Wider LATERAL/APPLY JOINs use](#wider-lateralapply-joins-use)
    - [Parameters translation changes](#parameters-translation-changes)
    - [INSERT improvements](#insert-improvements)
    - [UPDATE improvements](#update-improvements)
    - [DELETE improvements](#delete-improvements)
    - [Other  parser improvements and fixes](#other--parser-improvements-and-fixes)
  - [Member Translators](#member-translators)
  - [F# Support Changes](#f-support-changes)
  - [`IExceptionInterceptor` exception interceptor](#iexceptioninterceptor-exception-interceptor)
  - [Improve `COALESCE` function optimizations](#improve-coalesce-function-optimizations)
  - [Unification of Database Configuration API](#unification-of-database-configuration-api)
    - [Changes to `DataOptions` configuration API](#changes-to-dataoptions-configuration-api)
    - [Changes to database/provider version configuration](#changes-to-databaseprovider-version-configuration)
    - [Changes to `DBTools` classes](#changes-to-dbtools-classes)
      - [Obsoleted members removal](#obsoleted-members-removal)
      - [Unification of `*Tools` API](#unification-of-tools-api)
    - [Other Mapping Changes](#other-mapping-changes)
- [Async Code Changes](#async-code-changes)
  - [Obsolete `ContinueOnCapturedContext` option](#obsolete-continueoncapturedcontext-option)
  - [Other changes](#other-changes)
- [Null Comparison Improvements](#null-comparison-improvements)
- [Better Assertion of Database Features](#better-assertion-of-database-features)
- [Context API Review](#context-api-review)
- [Other Improvements](#other-improvements)
  - [Enhance telemetry events with command and connection details in tags](#enhance-telemetry-events-with-command-and-connection-details-in-tags)
  - [Apply custom type conversion mappings to `In`/`Contains` client-side collections](#apply-custom-type-conversion-mappings-to-incontains-client-side-collections)
  - [Associations over interfaces](#associations-over-interfaces)
  - [Miscellaneous minor fixes/changes](#miscellaneous-minor-fixeschanges)
- [Scaffold Changes](#scaffold-changes)
  - [Simplify table function mappings](#simplify-table-function-mappings)
  - [T4 Nugets Refactoring and New Scaffold Nuget](#t4-nugets-refactoring-and-new-scaffold-nuget)
    - [T4 Nuget Changes](#t4-nuget-changes)
  - [Other Changes](#other-changes-1)
  - [Provider-specific Scaffold Changes](#provider-specific-scaffold-changes)
- [Database-specific Changes](#database-specific-changes)
  - [Access](#access)
    - [Access Preview 2 changes](#access-preview-2-changes)
    - [Access Preview 4 changes](#access-preview-4-changes)
  - [ClickHouse](#clickhouse)
    - [ClickHouse Preview 2 changes](#clickhouse-preview-2-changes)
    - [ClickHouse Preview 3 changes](#clickhouse-preview-3-changes)
    - [ClickHouse Final Release changes](#clickhouse-final-release-changes)
  - [DB2](#db2)
    - [DB2 Preview 2 changes](#db2-preview-2-changes)
    - [DB2 Preview 4 changes](#db2-preview-4-changes)
  - [Firebird](#firebird)
    - [Firebird Dialect selection](#firebird-dialect-selection)
    - [Firebird dialect-specific SQL changes](#firebird-dialect-specific-sql-changes)
    - [Other Firebird support changes](#other-firebird-support-changes)
    - [Firebird Preview 2 changes](#firebird-preview-2-changes)
    - [Firebird RC 1 changes](#firebird-rc-1-changes)
    - [Firebird Final Release changes](#firebird-final-release-changes)
  - [Informix](#informix)
    - [Informix Preview 2 changes](#informix-preview-2-changes)
  - [MySQL/MariaDB](#mysqlmariadb)
    - [MySQL Dialect selection](#mysql-dialect-selection)
    - [MySQL dialect-specific SQL changes](#mysql-dialect-specific-sql-changes)
    - [MySQL/MariaDB Preview 2 changes](#mysqlmariadb-preview-2-changes)
    - [MySQL/MariaDB Final Release changes](#mysqlmariadb-final-release-changes)
  - [Oracle](#oracle)
    - [Oracle Preview 2 changes](#oracle-preview-2-changes)
    - [Oracle Preview 3 changes](#oracle-preview-3-changes)
    - [Oracle Final Release changes](#oracle-final-release-changes)
  - [PostgreSQL](#postgresql)
    - [PostgreSQL Preview 2 changes](#postgresql-preview-2-changes)
    - [PostgreSQL RC1 changes](#postgresql-rc1-changes)
    - [PostgreSQL Final Release changes](#postgresql-final-release-changes)
  - [SAP HANA](#sap-hana)
    - [SAP HANA Preview 2 changes](#sap-hana-preview-2-changes)
    - [SAP HANA Preview 4 changes](#sap-hana-preview-4-changes)
  - [SQL CE](#sql-ce)
    - [SQL CE Preview 2 changes](#sql-ce-preview-2-changes)
    - [SQL CE Preview 4 changes](#sql-ce-preview-4-changes)
  - [SQLite](#sqlite)
    - [SQLite Preview 2 changes](#sqlite-preview-2-changes)
    - [SQLite Final Release changes](#sqlite-final-release-changes)
  - [SQL Server](#sql-server)
    - [SQL Server Preview 2 changes](#sql-server-preview-2-changes)
    - [SQL Server Preview 4 changes](#sql-server-preview-4-changes)
    - [SQL Server RC1 changes](#sql-server-rc1-changes)
    - [SQL Server Final Release changes](#sql-server-final-release-changes)
  - [SAP/Sybase ASE](#sapsybase-ase)
    - [SAP/Sybase ASE Preview 2 changes](#sapsybase-ase-preview-2-changes)
    - [SAP/Sybase ASE Preview 4 changes](#sapsybase-ase-preview-4-changes)
    - [SAP/Sybase ASE RC 1 changes](#sapsybase-ase-rc-1-changes)

## Changes Since Preview 1 Release

### Preview 2

- [#940](https://github.com/linq2db/linq2db/issues/940) : fix translation of equality for composite properties
- [#2787](https://github.com/linq2db/linq2db/issues/2787) : fixes to ternary expressions translation
- [#2897](https://github.com/linq2db/linq2db/issues/2897) : cleanup generated SQL from unnecessary brackets in predicates
- [#2943](https://github.com/linq2db/linq2db/issues/2943) : fixed discovered issues in translation of queries with various combinations of ordering, distinct and paging options
- [#3311](https://github.com/linq2db/linq2db/issues/3311) : improvements to conversion of subqueries to APPLY/LATERAL joins
- [#3599](https://github.com/linq2db/linq2db/issues/3599) : [PostgreSQL] support partitioned tables in Schema API and scaffold
- [#3663](https://github.com/linq2db/linq2db/issues/3663) : fix `ObjectDisposedException` from trace logging
- [#3665](https://github.com/linq2db/linq2db/issues/3665) : fix `Enum.GetValues` translation
- [#3807](https://github.com/linq2db/linq2db/issues/3807) : fix issue with tracking of intermediate projection members in complex queries
- [#4060](https://github.com/linq2db/linq2db/issues/4060) : fix `ArgumentOutOfRangeException` when eager load used in SET queries
- [#4163](https://github.com/linq2db/linq2db/issues/4163) : [Oracle] fix nullability support for empty string as enum value
- [#4253](https://github.com/linq2db/linq2db/issues/4253) : `UpdateWithOutput` query generates empty `RETURNING` clause
- [#4322](https://github.com/linq2db/linq2db/issues/4322) : fix `ArgumentException` from test-case generator when query expression use single-character identifiers
- [#4347](https://github.com/linq2db/linq2db/issues/4347) : better support for complex queries translation
- [#4349](https://github.com/linq2db/linq2db/issues/4349) : fixed `ORDER BY` generation over constant values
- [#4431](https://github.com/linq2db/linq2db/issues/4431), [#4729](https://github.com/linq2db/linq2db/issues/4729) : fix `CloseAfterUse` context flag support by `Query*` APIs
- [#4487](https://github.com/linq2db/linq2db/issues/4487) : [PostgreSQL] fixed issues with enum array parameters
- [#4528](https://github.com/linq2db/linq2db/issues/4528) : add `Sql.Power(decimal?, decimal?)` overload for `POW` function/operation with decimal arguments. Thanks to [Riktim Mondal](https://github.com/riktimmondal) for PR
- [#4534](https://github.com/linq2db/linq2db/issues/4534) : add missing `CancellationToken` parameter to `SelectAsync` scalar query API
- [#4542](https://github.com/linq2db/linq2db/issues/4542) : improve performance of query builders dispatching
- [#4554](https://github.com/linq2db/linq2db/issues/4554) : sql generation improvements for constant conditions
- [#4556](https://github.com/linq2db/linq2db/issues/4556) : [PostgreSQL] `JSON`/`JSONB` values support in client-side `Merge` sources
- [#4586](https://github.com/linq2db/linq2db/issues/4586) : fix issue with ordering disappering from query with paging for old dialects: `Oracle 11` and `SQL Server 2005/2008`
- [#4602](https://github.com/linq2db/linq2db/issues/4602) : suppport reference to dynamic columns from association mappings. Thanks to [Bogi Napoleon Wennerstrøm](https://github.com/boginw) for PR
- [#4631](https://github.com/linq2db/linq2db/issues/4631) : [SQL Server] fix schema load error for table functions and procedures with `(n)varchar(max)` parameters. Thanks to [@cal-tlabwest](https://github.com/cal-tlabwest) for PR
- [#4654](https://github.com/linq2db/linq2db/issues/4654) : fix `InvalidOperationException: No coercion operator is defined between ...` exception
- [#4689](https://github.com/linq2db/linq2db/issues/4689) : add `ToLookupAsync` `IQueryable<T>` extensions
- [#4723](https://github.com/linq2db/linq2db/issues/4723) : fix query-based association for `string`-typed property
- [#4728](https://github.com/linq2db/linq2db/issues/4728) : fix `RuntimeBinderException` from `QueryMultipleAsync` API
- [#4776](https://github.com/linq2db/linq2db/issues/4776) : [cli] add support for property setter access modifiers. Thanks to [@johnjuuljensen](https://github.com/johnjuuljensen) for PR
- Fixed issue with `MySql` provider dialect detection logic being ignored in some setups. `MySql` dialects support was introduced in preview 1
- [Fix value conversion for `In`/`Contains` client-side collection values](#apply-custom-type-conversion-mappings-to-incontains-client-side-collections)
- [Minor breaking change to `MappingSchema.SetConvertExpression<?, DataParameter>` conversion](#changes-to-mappingschemasetconvertexpression-dataparameter-calls)
- [`IExceptionInterceptor` exception interceptor](#iexceptioninterceptor-exception-interceptor)
- [Improve `COALESCE` function optimizations](#improve-coalesce-function-optimizations)
- [Support Select With Indexer Overload](#support-select-with-indexer-overload)
- [Access Preview 2 changes](#access-preview-2-changes)
- [ClickHouse Preview 2 changes](#clickhouse-preview-2-changes)
- [DB2 Preview 2 changes](#db2-preview-2-changes)
- [Firebird Preview 2 changes](#firebird-preview-2-changes)
- [Informix Preview 2 changes](#informix-preview-2-changes)
- [MySQL/MariaDB Preview 2 changes](#mysqlmariadb-preview-2-changes)
- [Oracle Preview 2 changes](#oracle-preview-2-changes)
- [PosgtreSQL Preview 2 changes](#postgresql-preview-2-changes)
- [SAP HANA Preview 2 changes](#sap-hana-preview-2-changes)
- [SQL CE Preview 2 changes](#sql-ce-preview-2-changes)
- [SQLite Preview 2 changes](#sqlite-preview-2-changes)
- [SQL Server Preview 2 changes](#sql-server-preview-2-changes)
- [Enhance telemetry events with command and connection details in tags](#enhance-telemetry-events-with-command-and-connection-details-in-tags)
- [Async Code Changes](#async-code-changes)
- [Null Comparison Improvements](#null-comparison-improvements)
- [Changes to `query.ToString()` Behavior (including EF.Core Integration)](#changes-to-querytostring-behavior-including-efcore-integration)
- [Better Assertion of Database Features](#better-assertion-of-database-features)
- Improve stack use by queries with a lot of AND/OR conditions in query expression
- Add `WithCommandTimeout`/`UseCommandTimeout` configuration extensions to simplify setup of command timeout for context
- [.NET 9 support](https://github.com/linq2db/linq2db/pull/4693)
- [Preview 2 Obsoletions](#preview-2-obsoletions)
- [T4 Nugets Refactoring and New Scaffold Nuget](#t4-nugets-refactoring-and-new-scaffold-nuget)
- Improvements to boolean type handling in predicates for databases with `BOOLEAN` type
- Improvements to constant parameters detection, e.g. for values, taken from `readonly` fields

#### Fixed Preview 1 Regressions

- [#4520](https://github.com/linq2db/linq2db/issues/4520) : fix ternary expressions translation regression
- [#4533](https://github.com/linq2db/linq2db/issues/4533) : improve SQL generation for conditional/ternary expressions (partial regression in Preview 1)
- [#4539](https://github.com/linq2db/linq2db/issues/4539) : fix regression in custom types mapping
- [#4596](https://github.com/linq2db/linq2db/issues/4596) : fix regression in eager load support for expression-based associations
- [#4607](https://github.com/linq2db/linq2db/issues/4607) : fix regression in mapping of interface members
- [#4613](https://github.com/linq2db/linq2db/issues/4613) : fix exception translating `ExpressionMethod` call
- [#4626](https://github.com/linq2db/linq2db/issues/4626) : fix issues with unexpected client-side function evaluation
- [#4714](https://github.com/linq2db/linq2db/issues/4714), [#4717](https://github.com/linq2db/linq2db/issues/4717) : query with `CROSS JOIN` could produce invalid SQL

#### Entity Framework Core Integration

As part of task to move all projects under same repository and release them syncronously, we are moving [linq2db.EntityFrameworkCore](https://github.com/linq2db/linq2db.EntityFrameworkCore) project to main repository in preview 2. Old repository will be archived after version 6 release.

Fixes:

- [#4570](https://github.com/linq2db/linq2db/issues/4570) : fix issue with EF.Core context replacement in complex queries
- [#4629](https://github.com/linq2db/linq2db/issues/4629): `ORDER BY` clause lost for complex query
- [#4630](https://github.com/linq2db/linq2db/issues/4630): bad subquery optimization for sub-query with analytic function column
- [#4638](https://github.com/linq2db/linq2db/issues/4638): ineffective eager load query for client-side `GroupBy`
- [#4642](https://github.com/linq2db/linq2db/issues/4642): stack overflow in `MergeWithOutput` query
- [#4657](https://github.com/linq2db/linq2db/issues/4657): fix incorrect column names for `MERGE OUTPUT`
- [#4667](https://github.com/linq2db/linq2db/issues/4667) : [PostgreSQL] `JSON`/`JSONB` values support in client-side `Merge` sources
- [#4669](https://github.com/linq2db/linq2db/issues/4669) : fix `UnreachableException` for Pomelo MySql provider

Other changes:

- Default dialect for `SQL Server` changed to `AutoDetect` from `2008`
- Default dialect for `PostgreSQL` changed to `AutoDetect` from `9.3`

### Preview 3

*Linq To DB*

- [#4789](https://github.com/linq2db/linq2db/pull/4789) : fixed Preview 2 regression with interface member projection throwing exception
- [#4813](https://github.com/linq2db/linq2db/pull/4813) : fixed regression since version 5, when selection of joined subquery in projection could return wrong result

*Linq To DB CLI Scaffold*

- [#4786](https://github.com/linq2db/linq2db/pull/4786) : add new scaffold option `context-modifier` to change generated data context class access modifier. Thanks to [@johnjuuljensen](https://github.com/johnjuuljensen) for PR

### Preview 4

*Linq To DB*

- [#3940](https://github.com/linq2db/linq2db/issues/3940) : obsolete `DataConnection` and `DataContext` constructors except those with `.ctor()`, `.ctor(string configuration)` and `.ctor(DataOptions)` signatures. We recommend to use constructor with `DataOptions` parameter for connection configuration
- [#4317](https://github.com/linq2db/linq2db/issues/4317) : fix issue where multiple client-side conditions in `||` or `&&` expression could lead to errors due to out-of-order evaluation
- [#4439](https://github.com/linq2db/linq2db/issues/4439) : [MySQL] Use `DROP TEMPORARY TABLE` instead of `DROP TABLE` for temp tables
- [#4449](https://github.com/linq2db/linq2db/issues/4449) : [SQLite] Don't mark column with type token `timespan` as read-only column in schema
- [#4785](https://github.com/linq2db/linq2db/issues/4785) : fix issue with query filter not disabled on associations by `IgnoreFilters` calls
- [#4791](https://github.com/linq2db/linq2db/pull/4791) : [SQL Server] JSON type support (expected in SQL Server 2025)
- [#4798](https://github.com/linq2db/linq2db/pull/4798) : fix issue with handling of custom-typed parameters
- [#4800](https://github.com/linq2db/linq2db/issues/4800) : fix wrong table reference in generated SQL for LEFT JOIN with aggregate in condition
- [#4803](https://github.com/linq2db/linq2db/pull/4803) : fix preview 2 regression with too aggressive optimization of unused columns in sub-query with `GROUP BY`
- [#4827](https://github.com/linq2db/linq2db/pull/4827) : add missing explicit decimal precision/scale defaults for DB2, SQL Server, SQL CE, Access and ASE providers (set to `DECIMAL(18, 10)` instead of `DECIMAL`)
- [#4835](https://github.com/linq2db/linq2db/pull/4835) : fix support for custom reference types as scalars
- [#4839](https://github.com/linq2db/linq2db/pull/4839) : fixed regression in support of sub-queries by `InsertOrUpdate` queries
- [#4848](https://github.com/linq2db/linq2db/pull/4848) : fix issue with `DISTINCT` column list containing extra columns when selected record comes from left join
- [#4852](https://github.com/linq2db/linq2db/pull/4852) : fix issue with ordered limited subquery loosing `ORDER BY` clause when ordered by aggregate
- [#4788](https://github.com/linq2db/linq2db/issues/4788), [#4855](https://github.com/linq2db/linq2db/pull/4855) : fix regression in INSERT operation for temporary tables
- [#4790](https://github.com/linq2db/linq2db/issues/4790) : fix issue with duplicate joins generation when same association used in query using different owner types (e.g. interfaces)
- [#4859](https://github.com/linq2db/linq2db/pull/4859) : set `AllowMultiple=true` for mapping attributes that was missing it: `EnumAttribute`, `ValueConverterAttribute`, `QueryFilterAttribute`, `NotNullAttribute`, `DynamicColumnsStoreAttribute`, `AssociationAttribute` and `OptimisticLockProperty*Attribute`
- [#4860](https://github.com/linq2db/linq2db/pull/4860) : fix issue with remote context client initialization when it could call server from contructor while client initialization is not completed
- [#4864](https://github.com/linq2db/linq2db/pull/4864) : fix collation name validation to allow `-` character in `Sql.Collation` API
- [#4870](https://github.com/linq2db/linq2db/issues/4870) : fix parameter index remapping for `Sql.ExpressionAttribute` with `ArgIndices` specified
- [#4884](https://github.com/linq2db/linq2db/pull/4884) : fix regression in parameters deduplication, when Linq To DB could incorrectly merge parameters with same value from different sources into single parameter
- [#4886](https://github.com/linq2db/linq2db/issues/4886) : [SAP HANA] add support for `Sap.Data.Hana.Net.v6.0.dll` and `Sap.Data.Hana.Net.v8.0.dll` providers
- [#4887](https://github.com/linq2db/linq2db/pull/4887) : [SAP HANA] improve support for `DECIMAL`/`SMALLDECIMAL`/`DECIMAL(P,S)` types; add `HanaDecimal` client type support; try to add `REAL_VECTOR` type support (untested due to type availability only for cloud HANA version)
- [#4889](https://github.com/linq2db/linq2db/pull/4889) : fix regression in group-by subquery used as source for DML queries
- [#4890](https://github.com/linq2db/linq2db/pull/4890) : fix regression in client method call conversion to query parameter
- [#4891](https://github.com/linq2db/linq2db/issues/4891) : fix issues with mapped properties, accessed through overrides/generics

*Linq To DB CLI Scaffold*

- [#4792](https://github.com/linq2db/linq2db/pull/4792) : add new scaffold boolean option `add-static-init-context` to generate `StaticInitDataContext` partial method on context, called from last line of static constructor (generated always when option enabled). Thanks to [@johnjuuljensen](https://github.com/johnjuuljensen) for PR
- [#4794](https://github.com/linq2db/linq2db/pull/4794) : add new scaffold option `fluent-entity-type-helpers` accepting list of extension methods (by name) that will be called on each fluent entity mapping builder to allow user setup additional mappings for entity. Thanks to [@johnjuuljensen](https://github.com/johnjuuljensen) for PR

*Linq To DB T4 Scaffold*

- [#4840](https://github.com/linq2db/linq2db/pull/4840) : remove `linq2db` nuget dependency from `linq2db.t4models` package

*linq2db.EntityFrameworkCore*

- [#274](https://github.com/linq2db/linq2db.EntityFrameworkCore/pull/274) : fix translation of `PropertySaveBehavior` EF property mapping as `Linq To DB` `SkipOnInsert/SkipOnUpdate` mappings. Thanks to [@CryptKat](https://github.com/CryptKat) for PR
- [#4847](https://github.com/linq2db/linq2db/pull/4847) : fix EF 8 and 9 nuget packaging issues

*Linq To DB Compat*

- [#4821](https://github.com/linq2db/linq2db/pull/4821) : add new nuget package `linq2db.Compat` to provide support for code migration from .NET Framework

*Linq To DB for F#*

- [#4853](https://github.com/linq2db/linq2db/pull/4853) : fix use of `UseFSharp` extension from F# code. Thanks to [@gubser](https://github.com/gubser) for PR

### Release Candidate 1

*Linq To DB*

- [#1294](https://github.com/linq2db/linq2db/issues/1294) : fix parameter rendering options for table function mappings
- [#1767](https://github.com/linq2db/linq2db/issues/1767) : fix duplicate `IS [NOT] NULL` checks generation in some cases
- [#2779](https://github.com/linq2db/linq2db/issues/2779) : improve `FromSqlScalar` API. Now it assigns name `value` to query column for databases that support `alias(column)` syntax. For other databases used should still define alias with `value` name on column manually (MS Access, MySQL less 8 and MariaDB, SQL CE, SQLite, ClickHouse, SAP HANA and Oracle require column alias).
- [#3503](https://github.com/linq2db/linq2db/issues/3503) : async LINQ extensions moved from `LinqToDB` to `LinqToDB.Async` to avoid conflicts with similar extensions in other libraries (including upcoming .NET 10 async extensions)
- [#4320](https://github.com/linq2db/linq2db/issues/4320) : add missing context disposed checks to `DataConnection`, `DataContext` and `RemoteDataContextBase` APIs to prevent accidental context use after disposal
- [#4748](https://github.com/linq2db/linq2db/pull/4748) : multiple improvements and fixes in predicates generation
  - fix recognition of `Sql.Expression(IsPredicate=true)` as predicate
  - simplify SQL generation for databases with `BOOLEAN` type support
  - simplify SQL generation for databases with predicates comparison support
  - improve `IS [NOT] NULL` check argument reduction
  - fix issues with `string.Compare` translation
  - properly eliminate `UNKNOWN` values from nested predicates if they are not expected
  - many other small fixes to predicates
- [#4754](https://github.com/linq2db/linq2db/pull/4754) : introduce `LinqToDB.Internal` namespace for code, not intented for use by users to avoid situations when internal APIs used by users unintentionally. Public code is this namespace mostly intended for use by external database provider implementations, so if your code need anything from this namespace - probably you are doing something wrong or you need to use some advanced functiononality and know what are you doing.
- [#4775](https://github.com/linq2db/linq2db/pull/4775) :
  - add ASP.NET Core SignalR remote context transport support using new packages `linq2db.Remote.SignalR.Client` and `linq2db.Remote.SignalR.Server`
  - add HTTP remote context transport support using new packages `linq2db.Remote.HttpClient.Client` and `linq2db.Remote.HttpClient.Server` (server implementation required .NET8+)
- [#4797](https://github.com/linq2db/linq2db/issues/4797) : fix regression in support of nested `LoadWith` eager load calls inside `LoadWith`
- [#4828](https://github.com/linq2db/linq2db/pull/4828) : map `Guid.ToString()` to SQL for all databases
- [#4842](https://github.com/linq2db/linq2db/pull/4842) :
  - fix fluent mapping compatibility with temporary tables
  - add support for `RowByRow` bulk copy to remote context
  - add `UseAdditionalMappingSchema` `DataOptions` configuration API to allow user specify multiple mapping schemas during context configuration
- [#4858](https://github.com/linq2db/linq2db/pull/4858) : fix `Equals` method mapping for enums
- [#4861](https://github.com/linq2db/linq2db/pull/4861) : introduce new API to manage existing context options:
  - `IDisposable IDataContext.Use*Options(...)` : this API applies specified changes to context options and reverts them on dispose of returned disposable object. Note that some fundamental options like provider or connection string options cannot be changed here and will throw exception if you try to modify them
  - add `IDataContext.SetMappingSchema(MappingSchema)` API to replace (instead of add) of context's `MappingSchema`. It's mostly intended for use by `LinqToDB` itself and we don't recommend it for users, because you shouldn't forget to restore provider-specific mappings when use this call
  - add `IDisposable IDataContext.UseMappingSchema(MappingSchema)` API with same as `SetMappingSchema` behavior, but it could be reverted to old schema by disposing returning disposable object
- [#4868](https://github.com/linq2db/linq2db/pull/4868) : improve/fix translation of `string.PadLeft` and `string.Length` to SQL for all providers
- [#4875](https://github.com/linq2db/linq2db/pull/4875) : multiple fixes and improvements to nullability tracking and predicates optmization
- [#4897](https://github.com/linq2db/linq2db/issues/4897) : fix parameters generation for `FromSql` APIs
- [#4899](https://github.com/linq2db/linq2db/issues/4899) : fix invalid optimization of constant `ORDER BY`
- [#4900](https://github.com/linq2db/linq2db/pull/4900) : multiple fixes to context public APIs. See [here](#context-api-review) for more details
- [#4907](https://github.com/linq2db/linq2db/pull/4907) : add `Sql.Parameter` and `Sql.Constant` APIs to explicitly convert argument to query parameter or literal
- [#4911](https://github.com/linq2db/linq2db/pull/4911) : fix `Sql.FunctionAttribute` mapping nullability calculation when user don't specify nullability explicitly
- [#4912](https://github.com/linq2db/linq2db/pull/4912) : fix `Insert` for entity with entity filter
- [#4918](https://github.com/linq2db/linq2db/pull/4918) : mark `WithIgnoreEmptyUpdate` and `UseIgnoreEmptyUpdate` context options configuration methods as extensions. Thanks to [@rameel](https://github.com/rameel) for PR
- [#4923](https://github.com/linq2db/linq2db/issues/4923) : fix exception when enumerable source in `MERGE` used to lookup data from joined tables
- [#4933](https://github.com/linq2db/linq2db/pull/4933) : fix `IsPredicate` property support for `Sql.Function` and `Sql.Extension` attributes
- [#4935](https://github.com/linq2db/linq2db/pull/4935) : fix `ArgumentOutOfRangeException` error during optimization of complex predicates
- [#4937](https://github.com/linq2db/linq2db/pull/4937) : drop support for .NET 6 and EF.Core 6
- [#4943](https://github.com/linq2db/linq2db/pull/4943) : improve support for client-side (enumereable) tables
- [#4558](https://github.com/linq2db/linq2db/pull/4558) : improve generation of joins with complex conditions
- [#4963](https://github.com/linq2db/linq2db/pull/4963) : fix issue, when parameter with smaller range than original value could be created (e.g. `tinyint` parameter for 'int' client value) leading to overflow exceptions
- [#4964](https://github.com/linq2db/linq2db/pull/4964) :
  - [Firebird] Apply parameter type hints in binary operations
  - [SAP HANA] Map `uint` parameters to `DbType.Int64`
- [#4968](https://github.com/linq2db/linq2db/issues/4968) : fix issues with tuples handling in CTE
- [#4971](https://github.com/linq2db/linq2db/issues/4971) : fix issue when `MERGE` query could generate unbound source alias duplicate
- [#4977](https://github.com/linq2db/linq2db/issues/4977) : fix issue with support of interface member implementation mapped to expression
- [#5000](https://github.com/linq2db/linq2db/pull/5000) : add SQL Server 2025 dialect
  - improve `JSON` type support
  - add `VECTOR` type support
- [#5001](https://github.com/linq2db/linq2db/pull/5001) : add PostgreSQL 18 dialect
  - support `OLD` and `NEW` tables in `RETURNING` clause for `INSERT`, `UPDATE`, `DELETE` and `MERGE`
- [#5008](https://github.com/linq2db/linq2db/pull/5008) : avoid throw-and-catch type load in `SystemDataSqlServerAttributeReader`
- [#5004](https://github.com/linq2db/linq2db/issues/5004) : convert `LEFT JOIN`s with hard non-nullable filters to `INNER`

*linq2db.EntityFrameworkCore*

- [#4940](https://github.com/linq2db/linq2db/pull/4940) : [PostgreSQL] support enum mappings, defined in EF model. Thanks to [Denis](https://github.com/denis-tsv) for PR
- [#4917](https://github.com/linq2db/linq2db/pull/4917) : use existing connection to detect database dialect version. Thanks to [Denis](https://github.com/denis-tsv) for PR
- [#4957](https://github.com/linq2db/linq2db/pull/4957) : support custom `QueryCompiler`s. Thanks to [Florian Friedrich](https://github.com/ffried) for PR

*Linq To DB Tools*

- [#4949](https://github.com/linq2db/linq2db/pull/4949) : [SQL Server] refresh SQL Server system schema mappings

*Linq To DB Scaffold*

- [#4974](https://github.com/linq2db/linq2db/pull/4974) : fix `linq2db.Tools` dependency version for `linq2db.Scaffold` nuget package

### Release Candidate 2

*Linq To DB*

- [#4913](https://github.com/linq2db/linq2db/issues/4913) : fix `RetryPolicy`s `ExponentialBase` parameter validation to not allow values in `[0, 1)` range
- [#4991](https://github.com/linq2db/linq2db/pull/4991): improve numeric literals and parameters typing in columns:
  - [#4955](https://github.com/linq2db/linq2db/issues/4955) : fix incorrect mapping expression generation due to wrong numeric type assigned to column from literal expression
  - improve numeric constants handling as colum values (for `uint`, `long`, `ulong`, `decimal`, `float` and `double` types)
  - [MySQL] avoid use of casts to `FLOAT` type as it has artifically lowered precision
  - [SAP HANA] fix `ulong` type support in parameters
  - [Access] fix precision loss in casts to `Single` type
  - fix issue where decimal parameters with explicit typing could have wrong precision and/or scale
- [#5026](https://github.com/linq2db/linq2db/issues/5026) : restore debug property `DebugView` on query, mistakenly removed in previous release
- [#4572](https://github.com/linq2db/linq2db/issues/4572), [#5027](https://github.com/linq2db/linq2db/pull/5027) : cleanup `LinqToDB.Internal.*` code
  - make sure implementation classes for data providers available and could be inherited
  - mark as private some internal implementation code, not intended for use by library consumers
- [#5031](https://github.com/linq2db/linq2db/issues/5031) : obsolete mappings, exposed on `Expressions` class as they never intended for use by users
- [#5035](https://github.com/linq2db/linq2db/issues/5035) : handle missing `string.Format` overload and ensure that nulls in string interpolation translated as empty strings
- [#5036](https://github.com/linq2db/linq2db/issues/5036) : fix cases where nested `LoadWith` could be ignored
- [#5039](https://github.com/linq2db/linq2db/issues/5039) : `BulkCopyUseConnectionCommandTimeout` option used connection timeout instead of command timeout
- [#5040](https://github.com/linq2db/linq2db/issues/5040) : fix error in interface property mapping to implementation
- [#5041](https://github.com/linq2db/linq2db/issues/5041) : fix `ObjectDisposedException` on double dispose of `DataConnection`
- [#5042](https://github.com/linq2db/linq2db/issues/5042) : fix usings in T4 templates, broken by RC1 changes
- [#5045](https://github.com/linq2db/linq2db/pull/5045):
  - fix regression with SAP HANA procedure schema load
  - fix obsoletion error messages during T4 scaffold template run for Informix and Oracle
- [#5049](https://github.com/linq2db/linq2db/issues/5049) : fix regression in eager load handling of aggregated sub-queries
- [#5052](https://github.com/linq2db/linq2db/issues/5052) : fix regression in eager load projection mapping

### Release Candidate 3

*Linq To DB*

- [#5012](https://github.com/linq2db/linq2db/issues/5012): fix support for enum value conversion to `System.Enum` in mapper expression
- [#5057](https://github.com/linq2db/linq2db/issues/5057): fix regression in mapping of collection of custom-typed values
- [#5058](https://github.com/linq2db/linq2db/pull/5058): don't try to translate `string.Length` in final projection, if it is not required for SQL generation
- [#5065](https://github.com/linq2db/linq2db/pull/5065): change default value of `Configuration.IsStructIsScalarType` option to `false`. Read more [here](#changes-to-scalar-types-detection)
  - [#5056](https://github.com/linq2db/linq2db/issues/5056): fix discovered issues in mapping of custom scalar and non-scalar types
- [#5066](https://github.com/linq2db/linq2db/pull/5066): fix client collections support in grouped queries
- [#5069](https://github.com/linq2db/linq2db/issues/5069): fix discovery issue for association on interface member implementation
- [#5070](https://github.com/linq2db/linq2db/issues/5070): fix issue with duplicate column generation for group by query over constant column from nested query
- [#5071](https://github.com/linq2db/linq2db/pull/5071): [ClickHouse] drop support for [`ClickHouse.Client`](https://github.com/DarkWanderer/ClickHouse.Client) provider and switch to it's fork [`ClickHouse.Driver`](https://github.com/ClickHouse/clickhouse-cs)
- [#5074](https://github.com/linq2db/linq2db/pull/5074): temporary disabled one of predicate optimizations due to incorrect implementation
- [#5075](https://github.com/linq2db/linq2db/issues/5075): fix `IValueConverter` use with nullable struct values
- [#5077](https://github.com/linq2db/linq2db/pull/5077): [PostgreSQL] fix read of `+/-infinity` `timestamptz` values into `DateTimeOffset`
- [#5080](https://github.com/linq2db/linq2db/pull/5080): fix `RemoteContext.HttpClient` not working properly
- [#5087](https://github.com/linq2db/linq2db/pull/5087): fix stack overflow exception optimizing nested `NOT(conditional)` predicate and in SQLLite predicate with `DateTime` subquery
- [#5090](https://github.com/linq2db/linq2db/issues/5090): fix `COALESCE` translation for boolean type for databases without native boolean type
- [#5091](https://github.com/linq2db/linq2db/issues/5091): [Oracle] correct `bool` mapping for `CreateTable` to use `NUMBER(1)` instead of `CHAR(1)` by default (as it works everywhere else)

*linq2db.EntityFrameworkCore*

- [#4625](https://github.com/linq2db/linq2db/issues/4625): fix use of converters with nullable struct values

### Final Release

*Linq To DB*

- [#4132](https://github.com/linq2db/linq2db/issues/4132): fix F# support for `INSERT/UPDATE` API with expressions
- [#4998](https://github.com/linq2db/linq2db/issues/4998): add overloads with `Create[Temp]TableOptions` parameter to `CreateTable[Async]/CreateTempTable[Async]/IntoTempTable[Async]`. This will allow user to specify custom header/footer clauses for `CREATE TABLE` statements for temporary tables. Thanks to [Aiden Fuller](https://github.com/AidenFuller) for PR
- [#5014](https://github.com/linq2db/linq2db/issues/5014), [#5116](https://github.com/linq2db/linq2db/issues/5116): [SQLite] fix support for constrains information in schema for tables with `WITHOUT ROWID` option. Thanks to [Joonatan Uusväli](https://github.com/Seramis) for PR
- [#5097](https://github.com/linq2db/linq2db/issues/5097): avoid expanding `LambdaExpression` when it is not a part of query
- [#5104](https://github.com/linq2db/linq2db/pull/5104): obsolete setter for `DataContext.OnTraceConnection` property
- [#5106](https://github.com/linq2db/linq2db/pull/5106): remove internal blocking APIs from remote contexts
  - [#4618](https://github.com/linq2db/linq2db/issues/4618): avoid blocking network calls from `WASM` client
- [#5107](https://github.com/linq2db/linq2db/pull/5107): refactoring of `DataConnection` extensions
  - [#1987](https://github.com/linq2db/linq2db/issues/1987), [#2904](https://github.com/linq2db/linq2db/issues/2904): update `DataConnection` extensions to work with `DataContext` too (by extending `this IDataContext` interface)
  - [#3171](https://github.com/linq2db/linq2db/issues/3171): add missing async overloads for `Query` and `ExecuteReader` APIs
  - respect `DataContext.KeepConnectionAlive` flag in `BulkCopy` and other places where it could have been handled incorrectly
  - add `DataContext` support to `RetrieveIdentity` API and add `RetrieveIdentityAsync` overload
  - [**BREAKING**] switch order of `object? parameters` and `CancellationToken cancellationToken` parameters to have token as last parameter for some APIs: `QueryToArrayAsync`, `QueryProcMultipleAsync`, `QueryToListAsync`, `ExecuteAsync`, `ExecuteProcAsync`
  - add `ExecuteReaderProc[Async]` extension APIs
  - update SQLite FTS extensions to work with `DataContext` too and add async overloads
- [#5109](https://github.com/linq2db/linq2db/pull/5109):
  - rename `DoNotClearOrderBys` option to `ConcatenateOrderBy` to better reflect its functionality
  - [#5108](https://github.com/linq2db/linq2db/issues/5108): fix regression in handing of `RemoveOrderBy` API and `ConcatenateOrderBy` option
  - [#5111](https://github.com/linq2db/linq2db/issues/5111): fix `InvalidOperationException` error when `ConcatenateOrderBy` option enabled
- [#5114](https://github.com/linq2db/linq2db/issues/5114): fix translation of `Sql.NullIf` helpers to SQL
- [#5122](https://github.com/linq2db/linq2db/pull/5122): [SQLite] rewrite SQLite database schema provider to SQL queries. Thanks to [Joonatan Uusväli](https://github.com/Seramis) for PR
  - [#3330](https://github.com/linq2db/linq2db/issues/3330), [#4117](https://github.com/linq2db/linq2db/issues/4117): implement database schema provider for `Microsoft.Data.Sqlite` provider
  - [#1269](https://github.com/linq2db/linq2db/issues/1269): support non-default (`temp`) schemas by schema provider
  - [#2099](https://github.com/linq2db/linq2db/issues/2099): generate `INTEGER` type for `DataType.Int64`/`long` mappings instead of `BigInt` to support `rowid` primary keys creation
  - [#934](https://github.com/linq2db/linq2db/issues/934): fix detection of `rowid`-based primary keys as identity fields
  - [#4736](https://github.com/linq2db/linq2db/issues/4736): fix composite foreign keys detection as single key instead of separate FK for each key column
- [#5125](https://github.com/linq2db/linq2db/issues/5125): fix string interpolation handling regression in calculated expressions
- [#5130](https://github.com/linq2db/linq2db/pull/5130): improve constant expressions detection for well-known types
- [#5131](https://github.com/linq2db/linq2db/pull/5131): fixed join optimization to not apply to joins between non-table sources
- [#5132](https://github.com/linq2db/linq2db/pull/5132): fixed issue with composite properties equality translation when table has primary key defined on main class
- [#5133](https://github.com/linq2db/linq2db/pull/5133):
  - add support for `System.Data.SQLite` 2
  - [SQLite] use more standard functions, available everywhere, instead of extended functions available only with `System.Data.SQLite` v1.x (e.g. `PadR`, `Replicate`, `CharIndex`, `Left`, `Right`, `Space`, `Cot`, `Log` have new translation)
  - fix compatibility with `Npgsql` 10, remove support for obsoleted `NpgsqlCidr` type in scaffold and use `IPNetwork` type
  - [MySQL/MariaDB] add support for `VECTOR` type
- [#5134](https://github.com/linq2db/linq2db/pull/5134): fix CTE generation regression
- [#5135](https://github.com/linq2db/linq2db/pull/5135): fix issue with unused columns optimization regression removing all columns from `SET` queries if they are not used
- [#5138](https://github.com/linq2db/linq2db/pull/5138): [SQL Server 2025] fix issue with `VECTOR` parameters configuration
- [#5139](https://github.com/linq2db/linq2db/pull/5139): add additional field `SameAsFirstOrSecondParameter` to parameters nullability configuration enum for functions and expressions mappings
- [#5140](https://github.com/linq2db/linq2db/pull/5140): remove unnecessary generation of `RECURSIVE` CTE keyword for some regular CTEs
- [#5143](https://github.com/linq2db/linq2db/pull/5143): fix type configuration for binary (blob) parameters
- [#5144](https://github.com/linq2db/linq2db/pull/5144): fix issue with client-side `as` expression translation
- [#5146](https://github.com/linq2db/linq2db/pull/5146): make sure aliases, generated for query, doesn't match the name of any table, used in query (case-insensitive) to not confuse some databases
- [#5147](https://github.com/linq2db/linq2db/issues/5147): more aggresively generate type casts for `NULL` columns in table-less queries. Thanks to [Aiden Fuller](https://github.com/AidenFuller) for PR
- [#5151](https://github.com/linq2db/linq2db/issues/5151): .NET 10 support. Fixes [#5164](https://github.com/linq2db/linq2db/issues/5164) and [#5186](https://github.com/linq2db/linq2db/issues/5186)
- [#5152](https://github.com/linq2db/linq2db/issues/5152): fix some cases when `CAST` generated for conversion between same database types
- [#5155](https://github.com/linq2db/linq2db/pull/5155): [ClickHouse] Fix generation of `FINAL` hint
- [#5156](https://github.com/linq2db/linq2db/pull/5156): eager load API interface `ILoadWithQueryable` doesn't inherit `IAsyncEnumerable<T>` anymore
- [#5158](https://github.com/linq2db/linq2db/pull/5158): fix provider-specific configuration issues for remote context use in WASM applications
- [#5166](https://github.com/linq2db/linq2db/issues/5166): fix detection of `Pure` expression with type cast
- [#5168](https://github.com/linq2db/linq2db/pull/5168): improvements to aggregates translation
 - [#3816](https://github.com/linq2db/linq2db/issues/3816): `DistinctBy` translation
 - [#5173](https://github.com/linq2db/linq2db/issues/5173): incorrect separator parameter handling for `Sql.StringAggregate` call
 - translate `string.Join` overloads to SQL
 - improve `Sql.ConcatStrings` and `Sql.StringAggregate` translation to SQL
 - improve translation of aggregates with filter
 - [PostgreSQL] use `FILTER` clause for filtered aggregates
 - [Firebird] fix incorrect use of `FLOAT` type instead of `DOUBLE PRECISION` for casts to `double`
- [#5176](https://github.com/linq2db/linq2db/issues/5176): fix issue with referencing field of `FromSql<T>` subquery
- [#5177](https://github.com/linq2db/linq2db/issues/5177): fix issue with use of struct converters with nullable struct values
- [#5181](https://github.com/linq2db/linq2db/issues/5181): fix `MERGE` SQL generation for queries without source columns used and for empty client-side source
- [#5188](https://github.com/linq2db/linq2db/issues/5188): fix issues when using `Linq To DB` with some cultures
- [#5189](https://github.com/linq2db/linq2db/issues/5189): [PostgreSQL 13+] add support for translation of `Sql.NewGuid` and `Guid.NewGuid`
- [#5191](https://github.com/linq2db/linq2db/issues/5191): fix issue with use of joined tables by extensions
- [#5192](https://github.com/linq2db/linq2db/issues/5192): fix nullability of window functions
- [#5193](https://github.com/linq2db/linq2db/issues/5193): fix regression in v6 preview releases in `CROSS/OUTER APPLY` queries optimization
- [#5194](https://github.com/linq2db/linq2db/issues/5194): fix regression in v6 preview releases failing to translate `Tuple<>` in output clauses
- [#5196](https://github.com/linq2db/linq2db/pull/5196): fix `IDataContext` handling in queryable associations
- [#5200](https://github.com/linq2db/linq2db/pull/5200): [Oracle] improve LOB types handing
 - [#4661](https://github.com/linq2db/linq2db/issues/4661), [#5010](https://github.com/linq2db/linq2db/issues/5010): fix `ORA-01704 string literal too long` errors when generating string literals for LOB types (e.g. in `MultiRow` bulk copy mode and `Merge`). Now literals with more that 4000 bytes will be replaced with parameters
 - explicitly type `(N)CLOB` string literals to avoid typing errors
 - use `NULL` literal instead of parameter for `N(CLOB)` null values in `MultiRow` bulk copy
- [#5215](https://github.com/linq2db/linq2db/pull/5215): [SQL Server 20225] improve `VECTOR` type support:
 - support `float[]` mappings (and `Half[]` in future, requires `SqlClient` support)
 - add vector functions mappings in `SqlFn.*` class (`*VectorDistance`, `VectorNorm*`)
- [#5216](https://github.com/linq2db/linq2db/pull/5216): fix issue with column type detection in complex queries with nested projection subqueries
- [#5218](https://github.com/linq2db/linq2db/pull/5218): early preview version of new database provider for YDB database. It is still in development and implementation will be updated in next releases. For now it doesn't include Schema API and scaffold functionality and could miss some other query functionality.
- [#5225](https://github.com/linq2db/linq2db/issues/5225): fix provider detection using connection with open transaction
- [#5228](https://github.com/linq2db/linq2db/pull/5228): improve provider detection logic

*linq2db.EntityFrameworkCore*

- [#5184](https://github.com/linq2db/linq2db/issues/5184): EF.Core 10 support

*Linq To DB Scaffold*

- switch CLI tool to use `Microsoft.Data.Sqlite` provider for SQLite. This change will add support for more platforms and improve scaffold for views with expression columns
- [#5107](https://github.com/linq2db/linq2db/pull/5107): update stored procedure mappings generation to support `DataContext`-based contexts too

*Linq To DB LINQPad Driver*

- [#5102](https://github.com/linq2db/linq2db/issues/5102): `LINQPad` driver project moved to main repository
- add support for `Microsoft.Data.Sqlite` provider
- add Firebird versioning
- add SQL Server 2025 dialect
- add PostgreSQL 13 and 18 dialects
- [LINQPad 5] add notes to SQL Server provider how to resolve issues with types from `Microsoft.SqlServer.Types`assembly

## Important Migration Notes

### `Sql.Expression` Parameterization

This release improves parameterization of custom extensions which could result in parameter generation in extension instead of literal generated before. Usually it doesn't matter, but in come cases it could result in error if database doesn't support parameter is specific context. You should review your custom extensions if you have any and mark such parameters with `[ExprParameter(DoNotParameterize = true)]` attribute to tell `Linq To DB` that it should always generate parameter value as SQL literal.

Same also applies to `Sql.ExpressionAttribute`-related attributes:

- `Sql.TableExpressionAttribute`
- `Sql.FunctionAttribute`
- `Sql.TableFunctionAttribute`
- `Sql.ExtensionAttribute`

```cs
[Sql.Expression("CAST({0} AS DECIMAL(38, {1}))", ServerSideOnly = true)]
public static decimal MyCustomExtension(decimal value, [ExprParameter(DoNotParameterize = true)] byte scale)
{
    throw new InvalidOperationException("This method cannot be called from C# code");
}
```

Example below will will produce following SQL if there is no `ExprParameter` set

```sql
CAST(@p1 AS DECIMAL(38, @p2))
```

which is not valid, because SQL Server doesn't support parameters in type specification.

With attribute applied to second parameter, it will be always generated as literal as it should:

```sql
CAST(@p1 AS DECIMAL(38, 6))
```

### Changes to `MappingSchema.SetConvertExpression<?, DataParameter>` calls

(**Preview 2**)

Mapping schema API `SetConvertExpression` now ignores `addNullCheck` parameter for convertsion to `DataParameter` type and never wraps configured conversion expression in null check. Note that it doesn't apply to `SetConverter` API as it never had such functionality.

```cs
ms.SetConvertExpression<string, DataParameter>(value => new DataParameter(value.Length));

// final expreesion
// old behavior:
value => value == null ? null : new DataParameter(value.Length)
// new behavior:
value => new DataParameter(value.Length) // as-is
```

It means that your conversion expression for `DataParameter` should support `null` values handling as input for nullable types.

```cs
ms.SetConvertExpression<string, DataParameter>(value => new DataParameter(value == null ? null : value.Length));
```

Note that this is minor breaking change as `addNullCheck: true` never worked properly with `DataParameter` conversions anyways and crashed with `NullReferenceException` for `null` values.

### Changes to `query.ToString()` Behavior (including EF.Core Integration)

(**Preview 2**) [[PR #4734](https://github.com/linq2db/linq2db/pull/4734)]

Previously it was possible to generate query SQL text with parameters (declared using `DECLARE xxx` SQL Server syntax) using `ToString()` call on query.

This approach had several downsides:

- query generation is relatively expensive operation, but it could be called a lot by IDE during debugging to visualize variables using `ToString` calls
- current implementation was implemented for debugging purposes and it wasn't easy to use it with parameters for non-SQL Server databases
- not all types of LINQ queries vere covered (e.g. `Merge` and Oracle-specific `InsertAll` APIs didn't implemented this functionality)

In new version we are removing SQL generation from `ToString` calls and introduce new API:

```cs
// QueryType is one of:
// - IQueryable<T> for SELECT queries
// - IUpdatable<T> for UPDATE queries
// - IValueInsertable<T> for INSERT queries
// - ISelectInsertable<TSource, TTarget> for INSERT queries
// - IMultiInsertInto<T> for INSERT ALL queries
// - IMultiInsertElse<T> for INSERT ALL queries
// - IMergeable<TSource, TTarget> for MERGE queries
public static QuerySql ToSqlQuery<...>(this [QueryType] query, SqlGenerationOptions? options = null);

public sealed class QuerySql(string sql, IReadOnlyList<DataParameter> parameters)
{
    // command SQL
    public string                       Sql        => sql;
    // parameters with values
    public IReadOnlyList<DataParameter> Parameters => parameters;
}

public sealed class SqlGenerationOptions
{
    // Enforce parameters inlining into SQL as literals.
    public bool? InlineParameters { get; set; }
}
```

You can see usage examples for all API types in our [tests](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/Linq/QueryGenerationTests.cs), but general idea is that instead of final call to execute query you call `ToSqlQuery` extension and receive SQL instead of query being executed.

### Changes to Scalar Types Detection

(**RC 3**) [[PR #5065](https://github.com/linq2db/linq2db/pull/5065)] and new parser changes

To distinguish regular type/entity types from scalar types (types, mapped to single database value) during query translation and data mapping, `Linq To DB` used following configuration prior to version 6:

- all reference types (e.g. classes and interfaces) are not scalars by default
- all value types (structs) are scalars by default (this could be changed by turing off `Configuration.IsStructIsScalarType`)
- well-known scalar types (e.g. `string`, integer types, `DateTime(Offset)` etc) types recognized as scalar types
- provider-specific data types recognized as scalar types too (e.g. `SqlBoolean` for SQL CE/SQL Server or `NpgsqlTime` from `Npgsql`)

Additionally, user could enable/disable scalar flag for specific type using `ScalarTypeAttribute` or methods on mapping schema.

Version 6 brings following changes to this behavior:

- we are turning off `Configuration.IsStructIsScalarType` option by default and will remove it in version 7
- new parser use information about scalar types more consistently and in more places

Who could be affected by those changes: any user who use custom-defined type (either class or struct) to map scalar value.

How to address those changes: if you use custom-defined type as scalar type - you must explicitly annotate it as scalar type using attribute or mapping schema registration to avoid errors.

```cs
// use attribute
[ScalarType]
public class MyCustomType
{
    ...
}

// or register in mapping schema directly
mappingSchema.AddScalarType(typeof(MyCustomType), DataType.Json);
```

Note that scalar types registration functionality available in older versions too and it is recommended to register them even before version 6 of `Linq To DB`.

## API Removals/Changes

As part of work on this release we removed or changed some of existing APIs, which could require changes from you to use new version of `Linq To DB`.

- [[PR #4332](https://github.com/linq2db/linq2db/pull/4332)] We removed context cloning functionality, which includes `IDataContext.Clone(bool)` and `IAsyncDbConnection.TryClone()` interface methods alongside with `ICloneable` interface implementation on `DataConnection`. This code was used by previous versions of `Linq To DB` as quite ugly workaround for complex queries translation. It was never intended for use by users and we don't plan to introduce any replacements.
- [[PR #4002](https://github.com/linq2db/linq2db/pull/4002)] We replaced some provider configuration extension methods for `DataOptions` with extensions having better name and/or parameters. For more details see [Unification of Database Configuration API](#unification-of-database-configuration-api) below.
- [[PR #4002](https://github.com/linq2db/linq2db/pull/4002)] We removed obsoleted APIs from `<DB>Tools` classes and unified `GetDataProvider`/`CreateDataConnection` APIs in those. For more details see [Changes to `DBTools` Classes](#changes-to-dbtools-classes) below.
- Removed obsoleted T4 scaffold template property `GenerateLinqToDBConnectionOptionsConstructors`. It was replaced with `GenerateDataOptionsConstructors` property some time ago
- fix return type of some Window functions in `Sql.*` to return nullable value: `Corr`, `CovarSamp`, `StdDev`, `StdDevSamp` and `VarSamp`
- signature of `Sql.ExpressionAttribute`'s `GetExpression` method updated with extra prameters
- signature of `Sql.TableFunctionAttribute`'s `SetTable` method updated slightly

### Changes to GroupBy extensions

For `Sql.GroupBy.*` methods (`Rollup`, `Cube`, `GroupingSets`) we simplified parameter type from delegate to object, so you will need to update your calls and remove `() =>` from them:

```cs
var grouped = from q in query
    // old syntax
    //group q by Sql.GroupBy.Rollup(() => new { q.Id1, q.Id2 })
    // new syntax
    group q by Sql.GroupBy.Rollup(new { q.Id1, q.Id2 })
    into g
    select new
    {
        g.Key.Id1,
        Count = g.Count()
    };
```

### linq2db.AspNet NuGet Rename

[PR #4503](https://github.com/linq2db/linq2db/pull/4503)

`linq2db.AspNet` nuget was renamed to `linq2db.Extensions` to better reflect it's purpose as it contains extensions for integration with `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Logging`, not limited by ASP.NET applications only.

Also we renamed namespace for extensions to `LinqToDB.Extensions.DependencyInjection` and `LinqToDB.Extensions.Logging`.

### Preview 2 Obsoletions

- [Obsolete `ContinueOnCapturedContext` option](#obsolete-continueoncapturedcontext-option)
- `Update[Async]` overloads with table as first parameter were obsoleted in favor of overloads, which allow user to specify target table explicitly from update query
- `LinqException` and `SqlException` obsoleted and replaced with `LinqToDBException` use (there is no need for 3 exception types)
- `PreferApply` configuration option obsoleted and does not affect query generation
- `KeepDistinctOrdered` configuration option obsoleted and does not affect query generation
- `PreloadGroups` configuration option obsoleted and does not affect query generation

## Target Frameworks Review

[PR #4370](https://github.com/linq2db/linq2db/pull/4370)

We performed some cleanup to list of supported frameworks to remove some that are EOL and add new ones. New list of supported TFMs:

- `net462`: .NET Framework 4.6.2, which is an oldest suppored .NET Framework version. Replaces `net45`, `net46` and `net472` TFMs we shipped with previous releases
- `netstandard2.0`: to provide support for out-of-date runtimes and TFMs (`netstandard2.1` and `netcoreapp3.1` were removed)
- `net6.0`: as lowest supported .NET version
- `net8.0`: as latest supported .NET version

All listed above TFMs added to all binary nugets except `linq2db.Remote.Wcf`, which currently supports only .NET Framework.

## Performance Improvements

New query parser implementation contains a lot of performance improvements in both speed and memory use.

Related issues:

- [#3674](https://github.com/linq2db/linq2db/issues/3674) : excessive stack use on queries with big expression trees

## New features

### More `IAsyncEnumerable` Support

[PR #4486](https://github.com/linq2db/linq2db/pull/4486)

We add:

- several new arbitrary SQL query helpers that return `IAsyncEnumerable`
- add new `[Delete/Insert/Update]WithOutputAsync` APIs with `IAsyncEnumerable` return type and obsolete old `Task`-based APIs.

#### New SQL-based IAsyncEnumerable APIs

```cs
// DataConnection extensions to execute arbitrary SQL with custom mapper delegate
public static IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(
    this DataConnection   connection,
    Func<DbDataReader, T> objectReader,
    string                sql)

public static IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(
    this DataConnection    connection,
    Func<DbDataReader, T>  objectReader,
    string                 sql,
    params DataParameter[] parameters)

public static IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(
    this DataConnection   connection,
    Func<DbDataReader, T> objectReader,
    string                sql,
    object?               parameters)

// DataConnection extensions to execute arbitrary SQL with Linq To DB mapping
public static IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(
    this DataConnection connection,
    string sql)

public static IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(
    this DataConnection    connection,
    string                 sql,
    params DataParameter[] parameters)

public static IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(
    this DataConnection connection,
    string              sql,
    DataParameter       parameter)

public static IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(
    this DataConnection connection,
    string              sql,
    object?             parameters)

// DataConnection extensions to execute arbitrary SQL with Linq To DB mapping
// with parameter-sourced type specification (e.g. for mapping to anonymous type)
public static IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(
    this DataConnection    connection,
    T                      template,
    string                 sql,
    params DataParameter[] parameters)

public static IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(
    this DataConnection connection,
    T                   template,
    string              sql,
    object?             parameters)
```

#### New *OutputAsync APIs

```cs
// DeleteWithOutputAsync
public static IAsyncEnumerable<TSource> DeleteWithOutputAsync<TSource>(
    this IQueryable<TSource> source);

public static IAsyncEnumerable<TOutput> DeleteWithOutputAsync<TSource,TOutput>(
    Expression<Func<TSource, TOutput>> outputExpression);

// InsertWithOutputAsync
public static IAsyncEnumerable<TTarget> InsertWithOutputAsync<TSource, TTarget>(
    this IQueryable<TSource>           source,
    ITable<TTarget>                    target,
    Expression<Func<TSource, TTarget>> setter)

public static IAsyncEnumerable<TOutput> InsertWithOutputAsync<TSource, TTarget, TOutput>(
    this IQueryable<TSource>           source,
    ITable<TTarget>                    target,
    Expression<Func<TSource, TTarget>> setter,
    Expression<Func<TTarget, TOutput>> outputExpression)

// UpdateWithOutputAsync
public static IAsyncEnumerable<UpdateOutput<TTarget>> UpdateWithOutputAsync<TSource, TTarget>(
    this IQueryable<TSource>           source,
    ITable<TTarget>                    target,
    Expression<Func<TSource, TTarget>> setter)

public static IAsyncEnumerable<TOutput> UpdateWithOutputAsync<TSource, TTarget, TOutput>(
    this IQueryable<TSource>                             source,
    ITable<TTarget>                                      target,
    Expression<Func<TSource, TTarget>>                   setter,
    Expression<Func<TSource, TTarget, TTarget, TOutput>> outputExpression)

public static IAsyncEnumerable<UpdateOutput<TTarget>> UpdateWithOutputAsync<TSource, TTarget>(
    this IQueryable<TSource>           source,
    Expression<Func<TSource, TTarget>> target,
    Expression<Func<TSource, TTarget>> setter)

public static IAsyncEnumerable<TOutput> UpdateWithOutputAsync<TSource, TTarget, TOutput>(
    this IQueryable<TSource>                             source,
    Expression<Func<TSource, TTarget>>                   target,
    Expression<Func<TSource, TTarget>>                   setter,
    Expression<Func<TSource, TTarget, TTarget, TOutput>> outputExpression)

public static IAsyncEnumerable<UpdateOutput<T>> UpdateWithOutputAsync<T>(
    this IQueryable<T>     source,
    Expression<Func<T, T>> setter)

public static IAsyncEnumerable<TOutput> UpdateWithOutputAsync<T, TOutput>(
    this IQueryable<T>              source,
    Expression<Func<T, T>>          setter,
    Expression<Func<T, T, TOutput>> outputExpression)

public static IAsyncEnumerable<UpdateOutput<T>> UpdateWithOutputAsync<T>(
    this IUpdatable<T> source)

public static IAsyncEnumerable<TOutput> UpdateWithOutputAsync<T, TOutput>(
    this IUpdatable<T>              source,
    Expression<Func<T, T, TOutput>> outputExpression)
```

### Helper For Table Functions and Expressions Mapping

Previously client-side implementation of table function or expression required user to use reflection to get instance of `MethodInfo` for mapped method:

```cs
// table function example
[Sql.TableFunction(Name="GetParentByID")]
public ITable<Parent> GetParentByID(int? id)
{
    // to be able to call GetParentByID from non-Expression context
    // we need to provide code, that will return ITable instance
    // for mapped function.
    // This API requires MemberInfo of mapped method as parameter
    var methodInfo = typeof(Functions).GetMethod("GetParentByID", new [] {typeof(int?)})!;
    return _ctx.GetTable<Parent>(this, methodInfo, id);
}

static readonly MethodInfo _methodInfo = /* get generic MethodInfo*/;

// Table expression example for generic method and context passed as parameter
// Generic method requires that we instantiate MemberInfo for concrete generic parameter type
[Sql.TableExpression("{0} {1} WITH (TABLOCK)")]
public static ITable<T> WithTabLock<T>(IDataContext ctx)
    where T : class
{
    return ctx.GetTable<T>(null, _methodInfo.MakeGenericMethod(typeof(T)));
}
```

To make this code easier we added new `IDataContext` extenstion methods `TableFromExpression` and `QueryFromExpression`, which could be used instead:

```cs
[Sql.TableFunction(Name="GetParentByID")]
public ITable<Parent> GetParentByID(int? id)
{
    return _ctx.TableFromExpression(() => GetParentByID(id));
}

[Sql.TableExpression("{0} {1} WITH (TABLOCK)")]
public static ITable<T> WithTabLock<T>(IDataContext ctx)
    where T : class
{
    return ctx.TableFromExpression(() => ctx.WithTabLock<T>());
}

// or as IQyeryable
[Sql.TableExpression("{0} {1} WITH (TABLOCK)")]
public static ITable<T> WithTabLock<T>(IDataContext ctx)
    where T : class
{
    return ctx.QueryFromExpression(() => ctx.WithTabLock<T>());
}
```

We also updated table functions scaffolding using CLI and T4 templates to use those new extensions.

### Ordinal Generation API

New API to generate column ordinal (1-based ondex of column in select clause) added. Currently it could be used only with `order-by` expressions, but we can add support for more places on request if you will find it useful (e.g. to `group-by`).

```cs
from q in query
    orderby Sql.Ordinal(q.Field2), q.Field1
    select q;
```

```sql
SELECT
        t.Field1,
        t.Field2
    FROM SomeTable t
    ORDER BY 2, t.Field1
```

Mainly it is useful when you have complex expression for column and don't want it being duplicated in `ORDER BY`.

### Support Select With Indexer Overload

[PR 4759](https://github.com/linq2db/linq2db/pull/4759) (**Preview 2**)

This PR adds support for SQL translation of `Select` queryable API with indexer parameter:

```cs
IQueryable<TResult> Select<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, int, TResult>> selector)
```

It has two prerequisites:

- database should support `ROW_NUMBER` window function (not available for MS Access, Firebird 2.5, SQL CE, MySQL 5.7 and Sybase/SAP ASE)
- `source` parameter should have ordering

```cs
// this will work
db.Person
    .OrderByDescending(p => p.ID)
    .Select((p, idx) => new { p.FirstName, p.LastName, Index = idx })
    .Where(x => x.Index > 5);

// this will not work, as db.Person is not ordered explicitly
db.Person
    .Select((p, idx) => new { p.FirstName, p.LastName, Index = idx })
    .Where(x => x.Index > 5);
```


### Mapping Changes

Attribute `ExprParameterAttribute`, used to annotate mapped method parameters with additional metadata, was extended with `bool DoNotParametrize` property. It tells `Linq To DB` to emit parameter always as literal. This is useful when database function doesn't allow query parameter as input, e.g. `path` parameter for `OPENJSON` function in SQL Server 2016.

### New Query Parser

This is a huge piece of work that took us (mostly [@sdanyliv](https://github.com/sdanyliv)) couple of years to complete. It introduces a lot of improvements to LINQ handling and query generation and below we will try to mention most of the changes. This work contributes to wast majority of issues, fixed with this release and probably more as we need some time to verify and close issues, reported over years.

#### Composite properties tracking improvemens

You can now select composite properties in subquery without need to specify each child column explicitly in query.

```cs
class User
{
    public int UserId { get;set; }

    [Column("street", ".Street")]
    [Column("city", ".City")]
    public Address? Address { get;set; }
}

class Address
{
    public string? Street { get;set; }
    public string? City   { get;set; }
    //...
}

// now you can select composite property directly in sub-query and it will be mapped without errors
db.Users
    .Where(u => u.UserId == useId)
    .Select(u => u.Address)
    // adding additional query instruction(s) after select lead to exception in previous releases
    .Distinct()
    .FirstOrDefault();

// before you need to map each child field explicitly to avoid parser error
db.Users
    .Where(u => u.UserId == useId)
    .Select(u => new Address() { City = u.Address.City, Street = u.Address.Street })
    .Distinct()
    .FirstOrDefault();
```


#### Projection handling improvements

A lot improvements made to to track columns in intermediate projections. Some examples are:

- we now can track `Tuple<>` instances in projections
- use of client-side value in projection doesn't produce error in cases when it cannot be converted to SQL. E.g. you add some class instance to intermediate query projection just to be able to select it in final projection later

```cs
// instance of local class, which clearly not translatable to SQL
var c = new LocalClass();

var query =
    from p in db.Parent
    // tuple construnctor parameters properly mapped to tuple fields later
    select Tuple.Create(Tuple.Create(p.ParentID, p.Value1, c), Tuple.Create(p.Value1, p.ParentID));

var resultQuery = from q in query
    where q.Item2.Item1 != null
    // Item3 references instance of LocalClass we added to projection above
    select new { q.Item1, q.Item3 };
```

For final query projection we don't try to translate it to SQL anymore and set it's value on client during materialization. This has several benefits:

- we don't roundtrip column value to server and back
- we don't need to generate column typing code for databases that have issues with literals and parameters typing when they don't used in context where type could infered without inspecting parameter type
- we don't risk to fail/loose data for types which doesn't support lossless round-tripping for some or all values (e.g. `NaN` floating point number)

#### Aggregation functions improvements

Related issues:

- [#680](https://github.com/linq2db/linq2db/issues/680) : better support for standard aggregation functions with filters

We finally implement proper translation of standard LINQ aggregate methods (`Count`, `Avg`, `Min`, `Max`, `Sum`) with filters to SQL. Old implementation converted such aggregates to scalar subqueries with filter moved to sub-query filter. As workaround you could have used aggregate functions, provided by `Linq To DB`. Note that they are still useful as they provide support for more aggregate functions and support SQL-specific parameters, not available for `IEnumerable` classes.

```cs
from r in db.Address
    group r by r.City
     select
     {
        r.City,
        // this will be converterted to subquery in linq2db 5
        // and to COUNT(CASE WHEN BuildingNumber = 1 THEN 1 ELSE NULL END) in this release
        Count1 = r.Count(r => r.BuildingNumber == 1),
        // workaround with linq2db-provided aggregate mapping
        Count2 = r.CountExt(r => r.BuildingNumber == 1)
     }
```

#### Improvements to DML queries with OUTPUT/RETURNING clause

Old implementation of this feature had a lot of issues when user tried to return data from complex DML queries resulting in wrong data or even invalid SQL generation. New parser fix them all making this feature first-class citizen.

Note that this doesn't include support for querying to DML outputs, supported by some databases like PostgreSQL. This is a separate feature we didn't implemented yet.

Some fixed issues:

- [#4135](https://github.com/linq2db/linq2db/issues/4135): `UpdateWithOutput` works incorrectly
- [#4193](https://github.com/linq2db/linq2db/issues/4193): [Postgres] Wrong SQL generated of `UPDATE FROM RETURNING` clause having 2 levels chain of inner items
- [#4213](https://github.com/linq2db/linq2db/issues/4213): Outputed columns not inferred into Merge CTE
- [#4414](https://github.com/linq2db/linq2db/issues/4414): `UpdateWithOutputAsync` with `Where`, `OrderBy` and `Take`

#### Improvements to MERGE queries

Not many noticable changes here as it already worked quite well. We've made improvements to associations handling to offload them to source query in more cases instead of using sub-queries in operations clause (which was not supported by some databases too).

Some fixed issues:

- [#4338](https://github.com/linq2db/linq2db/issues/4338): `SqlException: Table not found` error in complex merge query

#### Improvements to CTE (Common Table Expression) clause

Related issues:

- [#1644](https://github.com/linq2db/linq2db/issues/1644): `Expression is not a table` error in CTE
- [#2264](https://github.com/linq2db/linq2db/issues/2264): circular reference errors in CTE

Some improvements are:

- CTE benefits from projections tracking improvements making it possible to use more complex queries in CTE
- new you can use scalar-typed CTEs without need to declare wrapping object
- selection of entities in CTE projection without need to define explicit column for each field is now supported

```cs
// no need to define wrapping class to store int-typed column anymore
// you can use int or any other scalar type as CTE record type directly
var cteRecursive = db.GetCte<int>(cte =>
    (
        from c in db.Child.Take(1)
        // select whole entity without explicit decomposition
        select new { c.ChildID, c }
    )
    .Concat
    (
        from c in db.Child
        from ct in cte.InnerJoin(ct => ct == c.ChildID + 1)
        select new { c.ChildID + 1, (Child?)null }
    )
    , "MY_CTE");

var result = cteRecursive.ToArray();
```

#### Improvements to SET operators translation

Related issues:

- [#2461](https://github.com/linq2db/linq2db/issues/2461): support for associations and eager load over SET queries
- [#2511](https://github.com/linq2db/linq2db/issues/2511): support for associations and eager load over SET queries
- [#2948](https://github.com/linq2db/linq2db/issues/2948): support complex sub-queries in SET queries
- [#3150](https://github.com/linq2db/linq2db/issues/3150): support contant columns in SET sub-queries
- [#3346](https://github.com/linq2db/linq2db/issues/3346): support projection of composite properties in SET sub-queries
- [#4225](https://github.com/linq2db/linq2db/issues/4225): `Types in Union are constructed incompatibly` error for same types
- [#4366](https://github.com/linq2db/linq2db/issues/4366): CTE with custom mapping class not working

SET operators (UNION, EXCEPT, INTERSECT) support was a big pain point in previous versions of `Linq To DB` (you can check issues above for some examples) having very limited functionality and a lot of issues with existing functionality.

This release tries to address all those issues:

- `Linq To DB` now is not anymore confused by constants in selection list of SET sub-queries leading to wrong results where constant value from one side of SET could appear as value in record, produced by another side of SET operator
- you can finally use associations and eager load functionality with records, returned by SET operator
- you can use entities with inheritance mappings with SET operator
- composite column mappings also work now.

You can find some nice use-case examples in our tests [here](https://github.com/linq2db/linq2db/blob/a11751d293353888471069ef6a11bbcfb94bd076/Tests/Linq/Linq/SetOperatorComplexTests.cs).

#### Better non-SQL `GroupBy` handling

C# `GroupBy` method allows you to write LINQ query that cannot be directly translated to SQL, because it allows you return data, which is not a part of grouping key clause or aggregate, required for SQL's `GROUP BY`.

For such queries `Linq To DB` used very old and ugly hack where it loaded this additional data using extra queries we created copy of current connection, which was already busy with main query, to load extra data for each returned group:

- cloned connection wasn't attached to main connection transaction, meaning it could have returned (or not) unexpected data
- each group record produced separate select query leading to infamous `n+1` query problem

As a partial "workaround" we had (and still have) `GuardGrouping (=true)` setting to produce exception for such queries because it is quite easy to write such query without noticing it will not translate well to SQL.

With current release where we migrated such queries handling from legacy approach to eager load mechanism where `Linq To DB` tries to load requested data effectively with few if not one query.

There are couple of side-effects:

- as mentioned above we removed connection cloning API used only by old `GroupBy` implementation
- there is a behavior change to `GuardGrouping` guard. We still have it in place in cases you don't want to have unexpected eager load from your queries, but we discovered that old guard implementation was actually faulty and failed to fire in some cases. This means you could start getting exceptions from queries that should have failed before but "worked" due to bad validation.

#### Column nullability tracing

We greatly improved tracing of nullability for translated code which could result in some rare cases in removal of unnecessary `IS [NOT] NULL` predicates (or addition of missing before).

#### Imrovements to sub-queries and JOINs

##### Ineffective ORDER BY removal

`ORDER BY` clause is not generated on sorted sub-query if it doesn't use it actually:

- sub-query has no paging options
- sub-query don't use window functions

##### Better JOINs optimization

We've made some improvements to JOIN optimization logic which helps to detect and remove more unused JOIN clauses.

##### Wider LATERAL/APPLY JOINs use

`Linq To DB` uses `LATERAL`/`APPLY` joins and window functions to generate more effective SQL in many places for databases that support such functionality. E.g. of such code could be translation of `First/Single|OrDefault` subqueries in projections.

Required functionality is enabled for supporting databases where it was missing. For more details check notes on specific database support changes below.

#### Parameters translation changes

We revisited logic for query parameters generation to prefer literals for constant values and parameters for dynamic values:

- when value is not constant (comes from variable, parameter, field, property or method result) we generate parameters
- when value is hardcoded constant we try to generate literal
- when value is fixed for query we also use literal. Good example of such values are paging options for `First/Single|OrDefault` queries

#### INSERT improvements

Improved generated SQL for INSERTs which reference other tables for some databases.

#### UPDATE improvements

- don't generate unnecessary `FROM` clause in some cases, where it was generated before.
- simplify SET clause for simple INSERT to not mention target table `UPDATE table SET table.field = value` -> `UPDATE table SET field = value`

#### DELETE improvements

Fixed issue when `Delete` applied to query with `SelectMany` to select association could delete data from wrong table:

```cs
// previously this query would generate DELETE for Parent table instead of GrandChildren table
db.GetTable<Parent>()
    .Where     (x => harnessIds.Contains(x.ParentID))
    .SelectMany(x => x.Children)
    .SelectMany(x => x.GrandChildren)
    .Delete();
```

#### Other  parser improvements and fixes

Improved parsing LINQ methods allowed to fix several issues with LINQ generated by other compilers (e.g. VB.NET and F#):

- [#417](https://github.com/linq2db/linq2db/issues/417): support F# `leftOuterJoin` LINQ operator translation
- [#649](https://github.com/linq2db/linq2db/issues/649): support VB.NET `Group By` LINQ operator translation
- [#3699](https://github.com/linq2db/linq2db/issues/3699): `NotImplementedException` translating expressions with `Invoke` calls. Such expressions could be generated by F# compiler

Some other fixed issues with parser:

- [#2494](https://github.com/linq2db/linq2db/issues/2494): fix lazy evaluation of conditional expressions in final projection
- [#3486](https://github.com/linq2db/linq2db/issues/3486): `let` changes generated SQL unnecessary
- [#3586](https://github.com/linq2db/linq2db/issues/3586): `cannot be converted to SQL` exception for complex query used as filter with `Any` LINQ method
- [#4184](https://github.com/linq2db/linq2db/issues/4184): detect issues when constructor parameter name doesn't match property/field name in intermediate projection
- [#4198](https://github.com/linq2db/linq2db/issues/4198): `NullReferenceException` in projection expression parsing
- [#4200](https://github.com/linq2db/linq2db/issues/4200): fixed exception on value-typed property nullability change in projection
- [#4274](https://github.com/linq2db/linq2db/issues/4274): support associations on entities from intermediate projections
- [#4284](https://github.com/linq2db/linq2db/issues/4284): `InvalidCastException` when parse complex query
- [#4300](https://github.com/linq2db/linq2db/issues/4300): fixed issue with caching of queries with `LambdaExpression.Compile` calls
- [#4348](https://github.com/linq2db/linq2db/issues/4348): `Sql.ExpressionAtribute.InlineParameters` ignored in some cases
- [#4390](https://github.com/linq2db/linq2db/issues/4390): `InvalidCastException : Converted FuncLikePredicate expression is not a Predicate expression` exception
- [#4454](https://github.com/linq2db/linq2db/issues/4454): `NotImplementedException` when filtering on a sub-query
- [#4458](https://github.com/linq2db/linq2db/issues/4458): Association with `QueryExpressionMethod` does not honor `CanBeNull` property
- [#4483](https://github.com/linq2db/linq2db/issues/4483): fix `VerificationException` with some dynamic column mappings
- [#4496](https://github.com/linq2db/linq2db/issues/4496): stack overflow on second query execution from query cache
- [#4497](https://github.com/linq2db/linq2db/issues/4497): fix left join nullability tracking issues
- [#4508](https://github.com/linq2db/linq2db/issues/4508): fix dynamic query filter caching

### Member Translators

We introduce new mechanism to map .NET code to SQL, which should replace some other ways in future:

- `Expressions.MapBinary` and `Expressions.MapMember` APIs
- `IExtensionCallBuilder` API

`Expressions.Map*` API's problem is that it doesn't provide direct support for mapping to SQL and just allow you to replace one member (operator, property, field or method call) in expression with some other expression (simple or complex) which should be translatable to SQL by `Linq To DB` without need of addtional such mappings as we don't do recursion here. This could lead to hard to debug issues with mappings working in one place and not working in another.

`IExtensionCallBuilder` is similar to new API, but less powerful.

New API consists of `IMemberTranslator` interface which you need to implement and then register in data provider. `IMemberTranslator` performs direct translation of member to SQL in form of `Linq To DB` SQL AST expression.

```cs
// we recommend to use MemberTranslatorBase
// instead of interface for your own translators
// as it already contains some useful low-level logic
interface IMemberTranslator
{
    Expression? Translate(
        ITranslationContext translationContext,
        Expression          memberExpression,
        TranslationFlags    translationFlags);
}

// add new member translator to context data options
new DataOptons()
    .UseMemberTranslator(translatorInstance)
```

With this release we also migrated some of existings mappings to new API:

- `Guid.NewGuid()`/`Sql.NewGuid()` methods
- `Sql.Convert()` methods
- `Convert.To*()` methods
- `Nullable<>.GetValueOrDefault()` method
- `.ToString()` method
- `System.Math.*` methods
- `Sql.Types.*` properties
- `DateOnly`, `DateTime`, `DateTimeOffset` types members
- `Sql.MakeDateOnly()`, `Sql.MakeDateTime()`, `Sql.GetDate()`, `Sql.DateAdd()`, `Sql.DateDiff()` methods
- `Sql.CurrentTimeStamp`, `Sql.CurrentTimeStampUtc` and `Sql.CurrentTimeStamp2` properties

Mainly this feature intended for internal use to improve `Linq To DB` predefined mappings, but if you want to implement your own translator we recommend to look at existings translators for examples.

### F# Support Changes

[PR #4362](https://github.com/linq2db/linq2db/pull/4362)

As a part of query parser refactoring we decided to move F# support code to separate library which has following benefits:

- support is not enabled by default, meaning it doesn't affect performance of non-F# projects
- new library use F# code instead of old approach with reflection and dynamic code where we need to access F#-specific functionality
- it will allow us to extend F# support in future more easily.

For this release there is no functional changes to F# support except some general fixes to query parsing. We just moved support code to separate library.

If you use `Linq To DB` with F# code, you shold add reference to `linq2db.FSharp` nuget and enable F# support in your database context:

```cs
using var db = new DataConnection(
  new DataOptions()
    .UseSqlServer(@"Server=.\;Database=Northwind;Trusted_Connection=True;")
    // enables F# Services for connection
    .UseFSharp());
```

### `IExceptionInterceptor` exception interceptor

[PR 4552](https://github.com/linq2db/linq2db/pull/4552) (**Preview 2**)

This PR adds new interceptor to observe and replace exceptions, thrown on query execution.

```cs
class MyExceptionInterceptor : ExceptionInterceptor
{
    // as we don't have state, we don't need to spawn multiple interceptor instances
    public static IExceptionInterceptor Instance = new MyExceptionInterceptor();

    public override void ProcessException(ExceptionEventData eventData, Exception exception)
    {
        // throw custom exception and place original one to InnerException
        throw new MyDatabaseException(exception);
    }
}


db.AddInterceptor(MyExceptionInterceptor.Instance);

try
{
    return db.Entities.Where(e => e.Id == id).SingleOrDefault();
}
catch (MyDatabaseException mdex)
{
    // process exception
}

```

### Improve `COALESCE` function optimizations

[PR 4548](https://github.com/linq2db/linq2db/pull/4548) (**Preview 2**)

This PRs adds additional optimizations for `COALESCE`-like expressions to avoid unnecessary SQL generation.

### Unification of Database Configuration API

[PR 4002](https://github.com/linq2db/linq2db/pull/4002)

#### Changes to `DataOptions` configuration API

To configure database provider for connection context using `DataOption` we had a set of methods like `Use<DB_NAME>(...)` with naming and set of parameters and overloads vary per database without any system. To address this issue we:

- introduced 4 overloads for databases with versioning support (per-provider or/and per-dialect)
- 2 overloads for databases without versioning (currently it is only `SQL CE`)
- added versioning support for databases that lacked it (and plan to add some more before final release)

New API with 4 overloads:

```cs
// DB here is database provider name, e.g. MySql

// overloads without version parameters
UseDB(this DataOptions options, Func<DBOptions, DBOptions> optionSetter);
UseDB(this DataOptions options, string connectionString, Func<DBOptions, DBOptions> optionSetter);

// overloads with versioning parameters (dialect/provider)
// database provider could have only one versioning parameter if it doesn't have versioning for second
UseDB(
    this DataOptions                 options,
         DBVersion                   dialect          = DBVersion.AutoDetect,
         DBProvider                  provider         = DBProvider.AutoDetect,
         Func<DBOptions, DBOptions>? optionSetter     = null);
UseDB(
    this DataOptions                 options,
         string                      connectionString,
         DBVersion                   dialect          = DBVersion.AutoDetect,
         DBProvider                  provider         = DBProvider.AutoDetect,
         Func<DBOptions, DBOptions>? optionSetter     = null);
```

API with 2 overloads:

```cs
UseSqlCe(this DataOptions options, Func<DBOptions, DBOptions>? optionSetter = null);
UseSqlCe(this DataOptions options, string connectionString, Func<DBOptions, DBOptions>? optionSetter = null);
```

For most of cases migration to new API will not require changes to code as we already had required overloads, but if you used removed method, you will need to update call to use new one.

#### Changes to database/provider version configuration

We adding provider/dialect versioning support for many providers that lacked it before.

New dialect configuration enums:

- FirebirdVersion
- MySqlVersion

New provider configuration enums:

- AccessProvider
- InformixProvider
- MySqlProvider
- SQLiteProvider
- SapHanaProvider
- SybaseProvider

We are open for adding more dialects and providers in future. If you want to propose dialect/provider, please fill-in corresponding feature request to issues.

#### Changes to `DBTools` classes

Each database provider has utility class `<DB>Tools` where `DB` is provider name.

##### Obsoleted members removal

We are removing bunch of obsoleted methods and properties in those classes (e.g. global provider configuration options and `BulkCopy` methods) that duplicate existing functionality.

##### Unification of `*Tools` API

- all `Tools` classes now expose `AutoDetectProvider` option to globally enable or disable provider/dialect detection logic. This option is missing for SQL CE provider as it doesn't have versioning and thus detection logic. Note that we always recommend to specify provider and dialect explicitly to avoid incorrect detection if you don't need to work with dynamic environment.
- `*Tools.GetDataProvider` API signature changed to accept provider version/dialect enumerations where applicable.
- `*Tools.CreateDataConnection` API refactored to be 3 methods with
  - `string connectionstring`/`DbConnection`/`DbTransaction` as first parameter
  - provider version/dialect enumerations as second/third parameter where applicable

Example (for SQL Server):

```cs
public static class SqlServerTools
{
    // AutoDetectProvider property
    public static bool AutoDetectProvider { get; set; }

    // GetDataProvider API shape
    public static IDataProvider GetDataProvider(
        SqlServerVersion  version          = SqlServerVersion.AutoDetect,
        SqlServerProvider provider         = SqlServerProvider.AutoDetect,
        string?           connectionString = null);

    // CreateDataConnection API shape
    public static DataConnection CreateDataConnection(
        string            connectionString,
        SqlServerVersion  version  = SqlServerVersion.AutoDetect,
        SqlServerProvider provider = SqlServerProvider.AutoDetect);
    public static DataConnection CreateDataConnection(
        DbConnection      connection,
        SqlServerVersion  version  = SqlServerVersion.AutoDetect,
        SqlServerProvider provider = SqlServerProvider.AutoDetect);
    public static DataConnection CreateDataConnection(
        DbTransaction     transaction,
        SqlServerVersion  version  = SqlServerVersion.AutoDetect,
        SqlServerProvider provider = SqlServerProvider.AutoDetect);
}
```

#### Other Mapping Changes

- `MappingSchema.AddScalarType` and `MappingSchema.SetDataType` methods for value types (structs) now automatically add mapping for nullable version of type so you don't need to use two calls to map both nullable and non-nullable value type. You still can do it if for some reason you want completely different mapping for nullable and non-nullable types (highly discouraged and not supported), but call for nullable type should go after call for non-nullable type to avoid it being overwritten
- database dialect detection now works without error with configuration that use external `DbConnection` or `DbTransaction` instance without connection string specified in configuration
- various predicate optimization improvements
- avoid removal of sub-queries with complex columns if those columns used multiple times by outer query to avoid multiple evaluations
- generate column aliases in final projection where it could confuse database otherwise for some databases
- prefer to generate `CROSS JOIN` instead of non-ANSI join syntax `FROM Table1 t1, Table2 t2` for databases that support `CROSS JOIN` for clarity and to avoid unnecessary sub-queries when database doesn't support mixed JOINs in single FROM clause

## Async Code Changes

(**Preview 2**)

Several improvements and fixes were done to async code in `Preview 2` release.

### Obsolete `ContinueOnCapturedContext` option

This option used to control parameter value for `ConfigureAwait(?)` calls inside `Linq To DB`. We are obsoleting it and replace with `ConfigureAwait(false)` calls.

### Other changes

- fixed several places in code where `CancellationToken` were handled incorrectly in enumerations to silently stop enumeration instead of throwing cancellation exception
- added guards to both `sync` and `async` APIs to throw explicit exception when `Linq To DB` API called on non-`Linq To DB` query. It was failing before too, but exception were generated by foreign query interpreter and could have been too vague to understand

## Null Comparison Improvements

[Issue #3535](https://github.com/linq2db/linq2db/issues/3535) (**Preview 2**)

`Linq To DB` have an option `bool CompareNullsAsValues`, which controls how comparison operations over nullable values converted to SQL. This option is needed as both .NET and SQL use different semantics for comparisons with `NULL`. .NET compares `null` by value, but SQL requires use of `IS [NOT] NULL` operator to check value against `NULL`. When we convert .NET comparison to SQL we allow user to select how to handle those differences. By default we convert .NET semantics to SQL (`CompareNullsAsValues = true`), and by setting `CompareNullsAsValues` to `false` we generate comparisons as-is.

```cs
// CompareNullsAsValues = true; // default, like in .NET
// C#
query.Where(e => e.NullableField1 == e.NullableField2)
// SQL: to emulate .NET behavior we need to add explicit check for null == null case
e.field1 = e.field2 OR e.field1 IS NULL AND e.field2 IS NULL

// CompareNullsAsValues = false; // as-is translation
// C#
query.Where(e => e.NullableField1 == e.NullableField2)
// SQL: don't generate NULL comparison, if both fields are null, expression will evaluate to FALSE
e.field1 = e.field2
```

Why we are changing it? For two reasons:

- `CompareNullsAsValues = false` mode doesn't convert explicit comparisons of `null` client values to `IS [NOT] NULL` making them always fail
- some of comparisons translated incorrectly (you can check linked issue for details)

New implementation not only fix known issues in comparison translation but also introduce replacement to `CompareNullsAsValues` boolean option:

```cs
CompareNulls CompareNulls;

enum CompareNulls
{
    // default, behaves like CompareNullsAsValues = true
    LikeClr,
    // fixed version of CompareNullsAsValues = false with proper handling of null parameters comparison
    LikeSql,
    // compatibility mode, behaves like CompareNullsAsValues = false
    LikeSqlExceptParameters
}
```

## Better Assertion of Database Features

(**Preview 2**)

Starting from now `Linq To DB` will throw exception for complex queries that require features, not supported by database. E.g.:

- `CROSS`/`LATERAL`/`APPLY` joins
- ordered subqueries with top/limit

Previously we tried sometimes to translate such queries and it produced invalid SQL or multiple queries to database.

Now you will get error like `LinqToDBException` (`Provider does not support xxx` or `Feature not supported by database: yyy`) and you need to rewrite your query in a way it supported by your database.

## Context API Review

(**RC 1**) [PR](https://github.com/linq2db/linq2db/pull/4900)

This PR address multiple issues in public APIs for context classes (`DataContext`, `DataConnection` and `RemoteDataContextBase`) such as:

- APIs for internal use being public
- sync-over-async calls
- some other changes

For most of API changes we leave old API in place till next major release (v7) and just obsolete it for v6 to give users time to adapt to changes, but there are also couple of breaking changes too.

`DataConnection` changes:

- obsolete public setter for `IRetryPolicy? RetryPolicy` property to encourage use of `UseRetryPolicy` method of `DataOptions` configuration APIs
- obsolete `bool IsMarsEnabled` property: this is provider-specific (SQL Server) property which is not actually adds any value for users
- obsolete public setter for `Action<TraceInfo> OnTraceConnection` property to encourage use of `UseTracing` method of `DataOptions` configuration APIs
- obsolete public setter for `TraceSwitch TraceSwitchConnection` property and introduce new `UseTraceSwitch` method of `DataOptions` configuration APIs
- obsolete public setter for `Action<string,string,TraceLevel> WriteTraceLineConnection` property to encourage use of `UseTraceWith` method of `DataOptions` configuration APIs
- obsolete `DbConnection Connection` property as it performs sync connection `Open` call internally if connection is not opened yet. Instead we introduce more predictable API as replacement:
  - `DbConnection? TryGetDbConnection()` to return current connection object if it is created already
  - `DbConnection OpenDbConnection[Async]()` to return current connection object or open new connection explicitly
- **BREAKING CHANGE** change behavior of setter for `int CommandTimeout` property to not accept negative values to reset timeout as it could lead to sync-over-async blocking calls and introduces `ResetCommandTimeout[Async]` methods instead
- obsolete `DbCommand CreateCommand()` internal API method. As a replacement, user could use `OpenDbConnection().CreateCommand()` calls
- obsolete `DisposeCommand[Async]()` internal API methods
- obsolete `void ClearObjectReaderCache()` method (`CommandInfo.ClearObjectReaderCache()` should be used instead)
- **BREAKING CHANGE** change unexpected behavior of `BeginTransaction[Async]` methods when transaction already opened. Previously it silently rolled back existing transaction and started new. Now it will throw `InvalidOperationException` if transaction already opened
- obsolete `bool? ThrowOnDisposed` property to disable `ObjectDisposedException` from disposed `DataConnection`
- obsolete `Task EnsureConnectionAsync()` internal API method, exposed by mistake

`DataContext` changes:

- hide implementation details `int ConfigurationID` property behind explicit interface implementation
- obsolete public setter for `string? LastQuery` property
- obsolete `bool IsMarsEnabled` property: this is provider-specific (SQL Server) property which is not actually adds any value for users
- obsolete setter for `bool KeepConnectionAlive` property as it could lead to sync-over-async blocking calls and introduced `SetKeepConnectionAlive[Async](bool)` methods instead
- **BREAKING CHANGE** change behavior of setter for `int CommandTimeout` property to not accept negative values to reset timeout as it could lead to sync-over-async blocking calls and introduces `ResetCommandTimeout[Async]` methods instead

`RemoteDataContextBase` changes:

- obsolete public setter for `string? ConfigurationString` property
- obsolete public setter for `MappingSchema MappingSchema` property
- add optional `CancellationToken` parameter to `Task CommitBatchAsync()` method
- **BREAKING CHANGE** change visibility from `public` to `protected` for `virtual Type SqlProviderType` property
- **BREAKING CHANGE** change visibility from `public` to `protected` for `virtual Type SqlOptimizerType` property
- fix discovered sync-over-async code pieces

Except API changes PR also fixes some discovered issues:

- context instance caching inside of cached mapping expression
- crashes in activity logging if database connection object throws exception from `DataSource` or `Database` property
- database connection being opened on command creation: postpone connection open till first `Execute*` call
- implement `IAsyncDisposable` for `KeepConnectionAliveScope`
- add `UseTraceSwitch` method to `DataOptions` configuration API to use instead of obsoleted property setter
- fix potential sync-over-async connection open in provider-specific `BulkCopyAsync` implementations (ASE, SQL Server, SAP HANA, PostgreSQL, MySql, Informix, DB2, ClickHouse)
- obsolete `IDataProvider.GetConnectionInfo` method, used only in one place, which is now obsoleted by this PR
- obsolete `bool Configuration.Data.ThrowOnDisposed` option, used to disable `ObjectDisposedException` from disposed `DataConnection`
- fix discovered issues with query tracing when query could be executed without being logging (most of async queries were not traced properly)

## Other Improvements

### Enhance telemetry events with command and connection details in tags

[Issue #4561](https://github.com/linq2db/linq2db/issues/4561) (**Preview 2**)

Telemetry events were enhanced with additional information using tags (see `LinqToDB.Tools.ActivityTagID` enumeration):

- ConfigurationString
- DataProviderName
- DataSourceName
- DatabaseName
- CommandText

### Apply custom type conversion mappings to `In`/`Contains` client-side collections

[PR #4546](https://github.com/linq2db/linq2db/pull/4546) (**Preview 2**)

This PR fixes issue where type conversions, configured in mapping schema, were not applied in some cases to client-side values, passed to `On`/`Contains` predicates.

```cs
ms.SetConvertExpression<CustomType, DataParameter>(value => new DataParameter(value.Value));

var ids = new CustomType[] { new CustomType(1), new CustomType(4) };

db.Table.Select(e => ids.Contains(e.Id))...
```

### Associations over interfaces

[Issue #4509](https://github.com/linq2db/linq2db/issues/4509)

Improved support for queries to interfaces with associations.

```cs
db.GetTable<MainEntity>()
    // convert to IQueryable<interface>, e.g. for generic helper
    .OfType<IHasSubentities>()
    .Select(x => new
    {
        // access to association, defined in interface
        x.SubEntities.Count
    });
```

Note that association load using eager load API (e.g. `LoadWith`) is not supported yet for interfaces.

### Miscellaneous minor fixes/changes

- [#2022](https://github.com/linq2db/linq2db/issues/2022) : better sanitize generated aliases to avoid invalid indentifiers generated
- [#3650](https://github.com/linq2db/linq2db/issues/3650) : avoid thread contention/deadlocks by generating object factories on request
- [#4136](https://github.com/linq2db/linq2db/issues/4136) : fix issues with reduced to constant boolean expression translation
- [#4534](https://github.com/linq2db/linq2db/issues/4534) : add missing `CancellationToken` parameter to `SelectAsync` scalar query API (**Preview 2**)
- [#4929](https://github.com/linq2db/linq2db/issues/4929) : adding empty maping schema to context could break mapping

## Scaffold Changes

[PR #4506](https://github.com/linq2db/linq2db/pull/4506)

### Simplify table function mappings

Both T4 and CLI scaffold updated to generate table function mappings using new `TableFromExpression` and `QueryFromExpression` APIs.

```cs
// old mapping
private static readonly MethodInfo _testTableFunction = MemberHelper.MethodOf<TestDataDB>(ctx => ctx.TestTableFunction(default));

[Sql.TableFunction("TEST_TABLE_FUNCTION")]
public IQueryable<TestTableFunctionResult> TestTableFunction(OracleDecimal? i)
{
    return this.GetTable<TestTableFunctionResult>(this, _testTableFunction, i);
}

// new mapping
[Sql.TableFunction("TEST_TABLE_FUNCTION")]
public IQueryable<TestTableFunctionResult> TestTableFunction(OracleDecimal? i)
{
    return this.QueryFromExpression<TestTableFunctionResult>(() => TestTableFunction(i));
}
```

### T4 Nugets Refactoring and New Scaffold Nuget

(**Preview 2**)

[PR #4161](https://github.com/linq2db/linq2db/pull/4161)

For a long time we had many database-specific T4 nugets with two functions:

- provide T4 scaffolding templates for database, supported by specific nuget
- optionally add nuget reference to database provider when provider is hosted on nuget
- add reference to `linq2db` nuget

This often lead to confusion where people were adding T4 nuget reference just for database access support and had unnecessary templates added to their projects. Another issue was addition of database provider nuget reference which could be undesirable or have too high version.

As part of working on those issues we've made following changes:

- reduced number of T4 nugets (see [table](#t4-nuget-changes) below) to have one nuget per database
- added T4 nuget for `ClickHouse` with support for `MySql` and `HTTP` protocols for scaffold connection
- remove references to `linq2db` and database provider from those nugets. Now user should add those references explicitly to their projects and free to choose required versions of them (at some sane extent)
- move most of scaffold logic from hard-to-maintain T4 templates to C# code in new `linq2db.Scaffold` library
- also move `linq2db.cli` scaffold code from `linq2db.Tools` to new `linq2db.Scaffold` library
- added new T4 scaffold option: `bool GenerateModelOnly = false` to disable data context generation
- added new T4 scaffold option: `bool GenerateModelInterface = false` to generate data context as interface with default members implementation, inherited from `LinqToDB.IDataContext`
- [cli][ClickHouse] changed default mapping type for `FixedString` from `byte[]` to `char`/`string`

#### T4 Nuget Changes

Table mention only changed/deprecated nugets.

|Old Nuget Name|New Nuget Name|
|:---------|:----------|
|linq2db.DB2.Core|linq2db.DB2|
|linq2db.Informix.Core|linq2db.Informix|
|linq2db.MySqlConnector|linq2db.MySql|
|linq2db.Oracle.Managed|linq2db.Oracle|
|linq2db.Oracle.Unmanaged|linq2db.Oracle|
|linq2db.SQLite.MS|linq2db.SQLite|
|linq2db.SqlServer.MS|linq2db.SqlServer|
|linq2db.Sybase.DataAction|linq2db.Sybase|
||linq2db.ClickHouse|

### Other Changes

- [CLI] removed `table-function-methodinfo-field-name` naming option as we don't generate `MethodInfo` fields for table functions anymore
- [CLI][**Preview 2**] added `PropertyModel.SetterModifiers` model property to specify setter access modifier using scaffold interceptors

### Provider-specific Scaffold Changes

- [Firebird] Support generation of `DataType.Boolean` mapping property for `BOOLEAN` type
- [T4][Access] Removed `LoadAccessMetadataByProvider` method, use `LoadAccessMetadata` method instead
- [T4][Informix] Replaced `string providerName` optional parameter with `InformixProvider provider` optional parameter for `LoadInformixMetadata` method
- [T4][SQL Server] Removed optional `SqlServerProvider provider` parameter from `LoadSqlServerMetadata` method
- [T4][SAP HANA] Removed `string providerName` optional parameter from `LoadSapHanaMetadata` method
- [T4][PostgreSQL] Fixed issue with excessive quotation generated for table function name that require quotation

## Database-specific Changes

### Access

General improvements to SQL generation

#### Access Preview 2 changes

- add provider versioning to allow engine-specific functionality. Currently we support only JET and ACE engines
- [OleDB] added support for GUID literal
- [ODBC] enforce parameters for GUID literal
- enforce parameters for `DateTime` with milliseconds

#### Access Preview 4 changes

- default decimal type mapping set to `DECIMAL(18, 10)` instead of `DECIMAL`

### ClickHouse

[PR #4452](https://github.com/linq2db/linq2db/pull/4452)

- dropped support for old versions (pre-`2.2.10`) of Octonica provider for TCP protocol. In any case we recommend to use latest version (currently `3.1.3`) as one having less compatibility issues with `Linq To DB`.
- integer literal not typed explicitly anymore using `toInt32(x)`

#### ClickHouse Preview 2 changes

- Enable `RECURSIVE` CTE keyword generation
- Enable `RESPECT NULLS` modifier generation for `FIRST_VALUE`/`LAST_VALUE`
- [cli] changed default mapping type for `FixedString` from `byte[]` to `char`/`string`

#### ClickHouse Preview 3 changes

- Drop support for [`ClickHouse.Client`](https://github.com/DarkWanderer/ClickHouse.Client) provider and switch to it's fork [`ClickHouse.Driver`](https://github.com/ClickHouse/clickhouse-cs)

#### ClickHouse Final Release changes

- Fix generation of `FINAL` hint

### DB2

We are bumping lowest supported versions for:

- DB2 LUW - to 11.1
- DB2 z/OS - to 12

If you still need to work with older versions, please inform us so we can enable older versions support.

- add/improve parameters wrapping into type casts in places where database could fail to type parameter and report typing error
- added support for `OFFSET` paging clause
- added support for `FETCH`, `OFFSET` and `ORDER BY` clauses for `DELETE` and `UPDATE` queries for DB2 LUW
- added support for `FETCH` clause for `DELETE` queries for DB2 z/OS

#### DB2 Preview 2 changes

- enabled/updated support for `Sql.Is[Not]DistinctFrom` API

#### DB2 Preview 4 changes

- default decimal type mapping set to `DECIMAL(18, 10)` instead of `DECIMAL`

### Firebird

- [PR #4002](https://github.com/linq2db/linq2db/pull/4002)
- [#1024](https://github.com/linq2db/linq2db/issues/1024) : `BOOLEAN` type support

#### Firebird Dialect selection

For a long time we generated Firebird SQL to be compatible with 2.5 release. To improve this situation we introduce `FirebirdVersion` dialect configuration enumeration which has 4 dialects currently:

- Firebird 2.5
- Firebird 3.0
- Firebird 4.0
- Firebird 5.0

Please inform us if you need to use older Firebird version (e.g. 2.1) or Dialect 1 databases, so we can add support for them.

#### Firebird dialect-specific SQL changes

- `3.0`+ : `bool` type mapped to `BOOLEAN` type by default. To use old mapping to `1`/`0` characters, use `2.5` dialect version or specify `DataType.Char` data type for boolean in your mapping
- `3.0`+ : instead of `TAKE/SKIP` paging clause `Linq To DB` will generate `OFFSET/FETCH` clause
- `3.0`+ : batch size for `BulkCopy` rised to 10Mb of generated SQL per batch instead of 64Kb for older versions
- `4.0`+ : enabled support for `LATERAL` JOINs
- `4.0`+ : use `BINARY(n)`/`VARBINARY(n)` type aliases
- `5.0`+ : rised limit of items in single `IN` predicate from 1 500 to 65 535 items
- `5.0`+ : added support for `WHEN NOT MATCHED BY SOURCE` `MERGE` operations
- `5.0`+ : added support for native `QUARTER` qualifier in `DatePart` functions
- improved `DataType.VarBinary`/`byte[]` mapping to use `VARBINARY(n)` database type instead of `BLOB` type for `n <= 32_765` (blob min size)
- fixed discovered issues with `Guid` type mappings

#### Other Firebird support changes

- improve parameters wrapping into type casts in places where database could fail to type parameter and report typing error

#### Firebird Preview 2 changes

- implemented generated identifiers trimming to fit in database limits: 31 character for Firebird <= 3.0 and 63 characters for Firebird >= 4.0
- enabled/updated support for `Sql.Is[Not]DistinctFrom` API
- added default max batch size for `BulkCopy` API in `MultipleRows` mode to avoid errors on large inserts without limit set by user

#### Firebird RC 1 changes

- apply type hints to parameters in binary operations

#### Firebird Final Release changes

- fix incorrect use of `FLOAT` type instead of `DOUBLE PRECISION` for casts to `double`

### Informix

General improvements to SQL generation.

#### Informix Preview 2 changes

- improved detection of cases, where query parameter requires wrap into type cast
- implemented fix in SQL to workaround incorrect nullability calculation by Informix for SET queries
- enabled support for `Sql.Is[Not]DistinctFrom` API

### MySQL/MariaDB

[PR #4002](https://github.com/linq2db/linq2db/pull/4002)

#### MySQL Dialect selection

To improve SQL generation for modern MySQL and MariaDB we introduce `MySqlVersion` dialect configuration enumeration which has 3 dialects currently:

- MySql 5.7 rolling release
- MySql 8.0 rolling release
- MariaDB 10/11 rolling release (we don't currently distinguish 10 vs 11 as MariaDB 11 doesn't have changes to SQL yet)

As side-effect it could result in unsupported SQL generation for older MySql versions (pre-5.7). If you need to work with older MySql servers, please inform us, so we can (re)add support for them.

#### MySQL dialect-specific SQL changes

- `MySQL8`/`MariaDB` : for those dialects we don't generate `FROM DUAL` fake source anymore for table-less queries with filter
- `MySQL8`/`MariaDB` : conversion to float/double now generates `CAST(? as DOUBLE/FLOAT)` instead of `CAST(? as DECIMAL(..))`
- `MySQL8` : enabled support for `LATERAL` JOINs
- `MySQL8`/`MariaDB` : enabled generation of `EXCEPT/INTERSECT ALL/DISTINCT` set operators instead of emulated SQL
- `ALL`: generation of non-standard `FLOAT(N)` type for column table creation removed. If you need such type for some reason you can create feature request and use `DbType="FLOAT(5)"` column mapping as workaround
- added support for `LIMIT` and `TOP` clauses for `DELETE` and `UPDATE` queries

#### MySQL/MariaDB Preview 2 changes

- change default mapping for `System.Decimal` from `decimal(10, 0)` to `decimal(29,10)`
- enabled/updated support for `Sql.Is[Not]DistinctFrom` API

#### MySQL/MariaDB Final Release changes

- add support for `VECTOR` type

### Oracle

#### Oracle Preview 2 changes

- Use standard `Coalesce` function instead of `Nvl` in generated SQL
- enabled/updated support for `Sql.Is[Not]DistinctFrom` API

#### Oracle Preview 3 changes

- Correct `bool` mapping for `CreateTable` to use `NUMBER(1)` instead of `CHAR(1)` by default (as it works everywhere else)

#### Oracle Final Release changes

- fix `ORA-01704 string literal too long` errors when generating string literals for LOB types (e.g. in `MultiRow` bulk copy mode and `Merge`). Now literals with more that 4000 bytes will be replaced with parameters
- explicitly type `(N)CLOB` string literals to avoid typing errors
- use `NULL` literal instead of parameter for `N(CLOB)` null values in `MultiRow` bulk copy

### PostgreSQL

- prefer to use `::` type cast instead of `CAST(...)`

#### PostgreSQL Preview 2 changes

- [#4655](https://github.com/linq2db/linq2db/issues/4655): [PG17] add support for `WHEN NOT MATCHED BY SOURCE` `MERGE` operations
- [#4670](https://github.com/linq2db/linq2db/issues/4670): [PG17] add support for `RETURNING` clause for `MERGE` operations
- enabled/updated support for `Sql.Is[Not]DistinctFrom` API

#### PostgreSQL RC1 changes

- add PostgreSQL 18 dialect
- add `OLD` and `NEW` tables support in `RETURNING` clause for `INSERT`, `UPDATE`, `DELETE` and `MERGE` queries

#### PostgreSQL Final Release changes

- fix compatibility with `Npgsql` 10, remove support for obsoleted `NpgsqlCidr` type in scaffold and use `IPNetwork` type
- use `FILTER` clause for filtered aggregates
- add support for translation of `Sql.NewGuid` and `Guid.NewGuid` (PostgreSQL 13+)

### SAP HANA

- [PR #4002](https://github.com/linq2db/linq2db/pull/4002)
- [#3154](https://github.com/linq2db/linq2db/issues/3154) : `LATERAL` JOIN support

- enabled support for `LATERAL` JOINs

#### SAP HANA Preview 2 changes

- [#3099](https://github.com/linq2db/linq2db/issues/3099): Enable support for CTE queries (`WITH`)
- enabled support for `Sql.Is[Not]DistinctFrom` API

#### SAP HANA Preview 4 changes

- add support for `HanaDecimal` type
- better support for `DECIMAL`, `DECIMAL(p, s)` and `SMALLDECIMAL` types, new `DataType.SmallDecFloat` type enum value to map `SMALLDECIMAL` type
- distinguish `DECIMAL` and `DECIMAL(p, s)` in schema API and scaffold
- experimental `REAL_VECTOR` type mapping support
- log provider-specific types for query parameters for both `ODBC` and `Sap.Data.Hana.*` providers

### SQL CE

General improvements to SQL generation.

#### SQL CE Preview 2 changes

- improved SQL generation for `DISTINCT` subqueries without `ORDER BY`

#### SQL CE Preview 4 changes

- default decimal type mapping set to `DECIMAL(18, 10)` instead of `DECIMAL`
- fix overflow exception from provider for `decimal` value, when database value has too many digits in fractional part
- fix incorrect type name for `MONEY` type when precision/scale specified in mapping

### SQLite

Lowest supported version of SQLite engine bumped to 3.33.0. If you still need to support older engine versions - you should create feature request.

- updated `InsertOrUpdate` API to generate single `INSERT .. ON CONFLICT UPDATE/IGNORE` query instead of two separate queries
- use proper `INTEGER` storage type instead of `Int` in type conversions: `CAST(smth AS INTEGER)`
- fix `DateTime` string literal milliseconds generation to emit 3 digits always, even if they are trailing zeroes
- enable SQL row support in update queries
- added support for `LIMIT` and `ORDER BY` clauses for `UPDATE` and `DELETE` queries for `System.Data.Sqlite` provider (`Microsoft.Data.SQLite` provider uses runtime build without this feature, see [here](https://github.com/ericsink/SQLitePCL.raw/issues/377))
- enable support for `DISTINCT` modifier for `COUNT()` aggregate
- enable support for `UPDATE FROM` queries
- enable support of equality and comparison operators for `ROW` type
- enable support of `ROW` type in `UPDATE` query `SET` clause
- [#4904](https://github.com/linq2db/linq2db/issues/4904) : fix comparison with `DateTime.MaxValue`

#### SQLite Preview 2 changes

- enabled/updated support for `Sql.Is[Not]DistinctFrom` API
- fix clauses order for `UPDATE` with `RETURNING` queries

#### SQLite Final Release changes

- fix support for constrains information in schema for tables with `WITHOUT ROWID` option
- complete rewrite of schema provider with a lot of improvements and bug fixes ([#5122](https://github.com/linq2db/linq2db/pull/5122)):
  - support `Microsoft.Data.Sqlite` provider
  - support non-default (`temp`) schemas
  - generate `INTEGER` type for `DataType.Int64`/`long` mappings instead of `BigInt` to support `rowid` primary keys creation
  - fix detection of `rowid`-based primary keys as identity fields
  - fix composite foreign keys detection as single key instead of separate FK for each key column
  - add support for `System.Data.SQLite` 2
  - use more standard functions, available everywhere, instead of extended functions available only with `System.Data.SQLite` v1.x (e.g. `PadR`, `Replicate`, `CharIndex`, `Left`, `Right`, `Space`, `Cot`, `Log` have new translation)

### SQL Server

- prefer to use `CAST` over `CONVERT` where it is possible
- added support for `TOP` clause for `UPDATE` and `DELETE` queries
- added support for legacy `SqlChars` type

#### SQL Server Preview 2 changes

- enabled/updated support for `Sql.Is[Not]DistinctFrom` API

#### SQL Server Preview 4 changes

- new JSON type support
- default decimal type mapping set to `DECIMAL(18, 10)` instead of `DECIMAL`
- fix incorrect type name for `MONEY`/`SMALLMONEY` type when precision/scale specified in mapping

#### SQL Server RC1 changes

- add SQL Server 2025 dialect
- update `JSON` type support
- add `VECTOR` type support

#### SQL Server Final Release changes

- [SQL2025] improve `VECTOR` type support:
 - support `float[]` mappings (and `Half[]` in future, requires `SqlClient` support)
 - add vector functions mappings in `SqlFn.*` class (`*VectorDistance`, `VectorNorm*`)

### SAP/Sybase ASE

- added support for `TOP` clause for `UPDATE` query (support for same clause in `DELETE` query was already here)
- `default(DateTime)` mapped to `DateTime(1753, 1, 1)` to avoid out-of-range errors from server in scenarios with default value used

#### SAP/Sybase ASE Preview 2 changes

- fixed schema provider issue, where `uni(var)char` and `n(var)char` types reported with incorrect length

#### SAP/Sybase ASE Preview 4 changes

- default decimal type mapping set to `DECIMAL(18, 10)` instead of `DECIMAL`
- fix incorrect type name for `MONEY`/`SMALLMONEY` type when precision/scale specified in mapping

#### SAP/Sybase ASE RC 1 changes

- handle `uint` parameters