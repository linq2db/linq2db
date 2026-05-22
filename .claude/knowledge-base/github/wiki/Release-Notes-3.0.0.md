- [Release 3.7.0](#release-370)
- [Release 3.6.0](#release-360)
- [Release 3.5.2](#release-352)
- [Release 3.5.1](#release-351)
- [Release 3.5.0](#release-350)
- [Release 3.4.5](#release-345)
- [Release 3.4.4](#release-344)
- [Release 3.4.3](#release-343)
- [Release 3.4.2](#release-342)
- [Release 3.4.1](#release-341)
- [Release 3.4.0](#release-340)
- [Release 3.3.0](#release-330)
- [Release 3.2.3](#release-323)
- [Release 3.2.2](#release-322)
- [Release 3.2.1](#release-321)
- [Release 3.2.0](#release-320)
- [Release 3.1.6](#release-316)
- [Release 3.1.5](#release-315)
- [Release 3.1.4](#release-314)
- [Release 3.1.3](#release-313)
- [Release 3.1.2](#release-312)
- [Release 3.1.1](#release-311)
- [Release 3.1.0](#release-310)
- [Release 3.0.1](#release-301)
- [Changes in 3.0.0](#final-release-changes)
- [Changes in RC.2](#rc2-changes)
- [Changes in RC.1](#rc1-changes)
- [Changes in RC.0](#rc0-changes)
- [Changes in Preview 2](#preview-2-changes)
- [Changes in Preview 1](#preview-1-changes)

---

### Release 3.7.0

- [#3076](https://github.com/linq2db/linq2db/issues/3076): added tracing for transaction start, commit and rollback events
- [#3161](https://github.com/linq2db/linq2db/issues/3161): fixed regression with selection of column values using subqueries (`First`/`FirstOrDefault`)
- [#3327](https://github.com/linq2db/linq2db/issues/3327): added `AsValueInsertable` extension to `IQueryable<T>` interface to convert it to updateable query. Thanks to [@MihailsKuzmins](https://github.com/MihailsKuzmins) for contribution.
- [#3328](https://github.com/linq2db/linq2db/issues/3328): `OUTPUT`/`RETURNING` clause support for all databases that have it (`MariaDB`, `SQLite`, `PostgreSQL` and `Firebird`) for `INSERT`, `UPDATE`, `DELETE` and `MERGE` statements. Query from `returning` is not supported yet (implemented by `PostgreSQL`).
  - [#3044](https://github.com/linq2db/linq2db/issues/3044): Fixed `OUTPUT`/`RETURNING` issues with complex queries (e.g. `UPDATE FROM complext_query`)
- [#3339](https://github.com/linq2db/linq2db/issues/3339): ROW (tuple) support
  - added support for ROW constructors (sql objects, defined in form `(x, y, ... z)` or `ROW(x, y, ... z)`) (database-dependent). See more details [below](#row-constructors)
  - [#2204](https://github.com/linq2db/linq2db/issues/2204), [#2657](https://github.com/linq2db/linq2db/issues/2657): implemented `UPDATE SET row = values` generation for `update from` queries for databases without `UPDATE FROM` support which support `UPDATE ROW` syntax: Oracle, SQLite (versions in [3.15-3.33) range), DB2
  - [#2844](https://github.com/linq2db/linq2db/issues/2844): added support for `UPDATE FROM` query generation for SQLite (requires SQLIte 3.33+)
- [#3353](https://github.com/linq2db/linq2db/issues/3353): [SQL Server] added ` Sql.Ext.SqlServer().IsNull(...)` extension for `ISNULL` mapping. Thanks to [@marcnet80](https://github.com/marcnet80) for contribution.
- [#3355](https://github.com/linq2db/linq2db/issues/3355): fixed issues, discovered by PVS-Studio team
- [#3371](https://github.com/linq2db/linq2db/issues/3371): fixed `NullReferenceException` in conditional expression handling
- [#3375](https://github.com/linq2db/linq2db/issues/3375): added support for `IReadOnlyCollection<T>.Contains` method mapping to `IN` clause
- [#3386](https://github.com/linq2db/linq2db/issues/3386): fixed regression in handling multiple similar sub-queries, introduced by v3.5.2
- [#3402](https://github.com/linq2db/linq2db/issues/3402): fixed issue when empty subquery generation in conditional expressions
- [#3423](https://github.com/linq2db/linq2db/issues/3423): Fix `default` argument type to be generic instrad of `int` for `Lag` and `Lead` window functions
  - also adds one- and two- parameters overloads for those functions: `LAG(expr)`, `LAG(expr, offset)` in addition to existing `LAG(expr, offset, default)` and same for `LEAD`
- [#3432](https://github.com/linq2db/linq2db/pull/3432): fixed issue with `OUTER APPLY` still generated instead of `LEFT JOIN` for some cases with `Configuration.Linq.PreferApply = false` option
- [#3438](https://github.com/linq2db/linq2db/pull/3438): [MySQL] add support for `MySqlDecimal` type, introduced in `MySqlConnector` 2.1.0
- [#3440](https://github.com/linq2db/linq2db/pull/3440): Fix performance degradation on materialization of large data sets
- [#3461](https://github.com/linq2db/linq2db/pull/3461): Improve PostgreSQL enums support. For proper enum handling specify both enum name and `DataType.Enum` on mapped column, e.g. `Column(DataType = DataType.Enum, DbType = "custom_enum")`
  - [#3368](https://github.com/linq2db/linq2db/pull/3368): support of custom types in native bulk copy by specifying type name in column mapping, e.g. `Column(DbName = "type_name_here")`
- [#3468](https://github.com/linq2db/linq2db/pull/3468): fixed support of `Sql.Property` in calculated columns
- [#3472](https://github.com/linq2db/linq2db/pull/3472): fixed support for `DataConnection.GetTable` method calls in expression methods
- [#3474](https://github.com/linq2db/linq2db/pull/3474): Improve handling of several .net types across providers
  - [#2839](https://github.com/linq2db/linq2db/pull/2839): [Firebird] fixed bulk copy support for `varchar` columns. Previously data inserted with char type and could have padded with spaces to match length of longest value in inserted set
  - `Guid` support fixes:
    - [DB2] fixed table creation for entity with `Guid?` columns. Update literal generation to generate `BX'...'` literal instead of `CAST(x'...' as char(16) for bit data)`
    - [Firebird] add missing `CreateTable` support (using `CHAR(16) CHARACTER SET OCTETS` type) and add binary literal generation. Added `Sql.Ext.Firebird().UuidToChar(Guid)` extension to map `UUID_TO_CHAR` Firebird function
    - [Informix] add missing `CreateTable` support (using `VARCHAR(36)` type). Literal value example: `d8948d42-7b56-4dd3-b522-51a07c818e14`
    - [SQL CE] add missing `CreateTable` support (using `UNIQUEIDENTIFIER` type)
    - [Sybase ASE] add missing `CreateTable` support (using `VARCHAR(36)` type). Literal value example: `d8948d42-7b56-4dd3-b522-51a07c818e14`
    - [SQLite] remove `CAST(... as blob)` around binary literal for `Guid`
    - [Oracle] replace `CAST(... as raw(16))` with `HEXTORAW('...')` for `Guid` literal
  - `Byte` support fixed:
    - [DB2] `Byte` type mapped to `SMALLINT`
    - [Informix] `Byte` type mapped to `SMALLINT`, fixed byte values support in parameters
  - mapped and unmapped enums support fixed:
    - [Sybase] Fix enum-typed columns support in native provider bulk copy
  - `bool` support fixed:
    - [Sybase] Create non-nullable `bit` column (type doesn't support nullability in Sybase ASE) by `CreateTable`
- [#3475](https://github.com/linq2db/linq2db/pull/3475): fixed support for overridden virtual methods(properties) in queries, when member mapping specified for base method only. Now linq2db will search for mapping in base classes if called method not mapped.
- [#3477](https://github.com/linq2db/linq2db/pull/3477): fixed multiple issues in `SET`(union)/`CTE` queries
  - [#3357](https://github.com/linq2db/linq2db/pull/3357): add missing support to `record class` and other record-like types (classes with column values set using constructor) in `SET`/`CTE` queries
  - [#3359](https://github.com/linq2db/linq2db/pull/3359): flatten queries with multiple set operators (e.g. now we generate `q1 UNION q2 UNION q3` instead of `SELECT FROM (q1 UNION q2) UNION q3`). This also fixes recursive CTEs with 3 or more subqueries as they require flat `SET`.
  - [#3357](https://github.com/linq2db/linq2db/pull/3357): fix calculated columns use in `SET` queries
  - [#3357](https://github.com/linq2db/linq2db/pull/3357): fix issues with `SET` queries with sort applied to whole query
  - [Access] Fixed compatibility issue with Access ODBC provider when select list contains `NULL` column value as parameter
- [#3478](https://github.com/linq2db/linq2db/pull/3478): [DB2][Informix] added support for `Net5.IBM.Data.Db2*` and `Net.IBM.Data.Db2*` providers
- [#3487](https://github.com/linq2db/linq2db/pull/3487): fixed empty outer apply record detection when multiple applies used by query

#### Row Constructors

See also [PR](https://github.com/linq2db/linq2db/pull/3339) notes.

Row constructor (or tuple) is SQL object, declared as set of unnamed values using `(x, y, ..., z)` or `ROW(x, y, ..., z)` syntax (syntax depends on database).

Depending on database support level, tuples could be used:
- in common operators `==`, `<>`, `IS [NOT] NULL`, `[NOT] IN`, comparison operators (supported operators vary per-database and for some we perform conversion to supported non-row syntax)
- in row-specific operators: `OVERLAPS`
- in `UPDATE` query to update multiple columns with single expression (makes sense when column values set by same sub-query and database doesn't support `UPDATE FROM` syntax)

New API:

```cs
// row value constructor
Sql.Row(...);
// row OVERLAPS operator (extension method)
Sql.Overlaps(this SqlRow thisRow, SqlRow otherRow);
```

Examples:

```cs
// update multiple columns using values from sub-query
db.UserData
  .Where(t => t.Id == 4)
  // update using values from sub-query
  .Set(
    t => Sql.Row(t.FirstName, t.LastName), 
    t => (from s in db.Users where s.Id == t.OtherId select Sql.Row(s.FirstName, s.LastName)).Single()))
  // update another two columns using literal values
  .Set(
    t => Sql.Row(t.SecondName, t.Surname)
    t => Sql.Row("John", "Doe"))
  // regular one-column SETter
  .Set(t => t.SingleColumn, 0)
  .Update();

// select all records with date range overlaps specified range
db.Tasks.Where(t => Sql.Row(t.StartDate, t.EndDate).Overlaps(Sql.Row(fromDate, toDate))).ToArray();
```

Support per-database:
| Provider | Comparisons | IS&nbsp;NULL | IN | OVERLAPS | BETWEEN | Comp. to (SELECT) | UPDATE (SELECT) | UPDATE (literal)
| --- | --- | --- | --- | --- | --- | --- | --- | ---
| Access | ⬇ | ⬇ | ⬇ | ⛔ | ⬇ | ⛔ | ⛔ | ⛔ 
| DB2 | ✅ | ⬇ | ⬇ | ✅ | ✅| ⛔ | ✅ | ✅
| Firebird | ⬇ | ⬇ | ⬇ | ⛔ | ⬇ | ⛔ | ⛔ | ⛔   
| Informix | ✅ (1) | ⬇ | ✅ | ⛔ | ⬇ | ⛔ | ⛔ | ⛔ 
| MySQL/MariaDB | ✅ | ⬇ | ✅ | ⛔ | ⬇ | ✅ | ⛔ | ⛔ 
| Oracle | ✅ (1) | ⬇ | ✅ | ✅ | ⬇ | ✅ | ✅ | ⛔
| PostgreSQL | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅
| SQLite | ✅ | ⬇ | ⬇ | ⛔ | ✅ | ✅ | ⛔ | ⛔ 
| SAP HANA 2 | ⬇ | ⬇ | ⬇ | ⛔ | ⬇ | ⛔ | ⛔ | ⛔ 
| SqlCe | ⬇ | ⬇ | ⬇ | ⛔ | ⬇ | ⛔ | ⛔ | ⛔ 
| SQL Server | ⬇ | ⬇ | ⬇ | ⛔ | ⬇ | ⛔ | ⛔ | ⛔ 
| SAP/Sybase ASE | ⬇ | ⬇ | ⬇ | ⛔ | ⬇ | ⛔ | ⛔ | ⛔ 

- ⬇ : feature coverted to non-row SQL
- ✅ : feature supported and use ROWs
- ⛔ : feature not suppored and will generate error

Notes:
1. Oracle and Informix only support equality operators (= and <>), none of >, >=, <, <= supported.


***

### Release 3.6.0

This release contains only changes ([#3343](https://github.com/linq2db/linq2db/pull/3343)), required for better support of recent major releases of [Npgsql 6.0.0](https://www.nuget.org/packages/Npgsql/6.0.0) and [Microsoft.Data.Sqlite 6.0.0](https://www.nuget.org/packages/Microsoft.Data.Sqlite/6.0.0).

- [npgsql 6]: for parameters of `DateTime` type with `DataType.DateTime` or `DataType.DateTime2` type (those are default types, used for `DateTime` by linq2db), explicitly set parameter database type to `timestamp`, because otherwise `Npgsql` will try to use `timestamptz` type starting from v6 release.
- [npgsql]: add new setting `PostgreSQLTools.NormalizeTimestampData = true` to automatically fix `DateTimeOffset.Offset` and `DateTime.Kind` properties for data, passed to database (including `BulkCopy`), according to used type (`timestamp` or `timestamptz`). This is needed as `npgsql` 6 now expects `DateTimeOffset.Offset` to be zero and `DateTime.Kind` being `Utc` (for `timestamptz`) or non-`Utc` (for `timestamp`). Set it to `false` if you want to fix your data on application level. This option doesn't handle complex types like `NpgsqlRange<T>`.
- [sqlite]: added new API `SQLiteTools.ClearAllPools(provider)` to clear connection pools for SQLite provider(s). Works with `System.Data.SQLite` and `Microsoft.Data.Sqlite` 6.0.0+ (for previous versions it does nothing).

If you want to adopt `npgsql` 6 changes and map `DateTime` to `timestamptz` by default, you need to specify new defaults for it in mapping schema (don't forget that your database also should use `timestamptz` type for affected columns):
```cs
mappingSchema.AddScalarType(typeof(DateTime), DataType.DateTimeOffset);
mappingSchema.AddScalarType(typeof(DateTime?), DataType.DateTimeOffset);
```

***

### Release 3.5.2

- [#3148](https://github.com/linq2db/linq2db/issues/3148): add handing of `DefaultExpression` (`default(T)` operator). Affects only 3rd-party expression generators, e.g. `OData` libraries as C# compiler never generates this expression
- [#3257](https://github.com/linq2db/linq2db/issues/3257): fixed subquery generation issues
- [#3259](https://github.com/linq2db/linq2db/issues/3259): [SQL Server][SAP HANA] fixed generated SQL for aggregates with outer references, improved/fixed sorting generation over complex subquery columns
- [#3304](https://github.com/linq2db/linq2db/issues/3304): fixed `ArgumentException` during mapping generation for column with custom type mapping over `DataParameter`
- [#3309](https://github.com/linq2db/linq2db/issues/3309): [SQL Server] generate `COALESCE(x, y)` instead if `IIF(x is null, y, x)`
- [#3312](https://github.com/linq2db/linq2db/issues/3312): Fixed race condition in `MappingSchema` that could lead to rare `NullReferenceException` errors
- [#3319](https://github.com/linq2db/linq2db/issues/3319): use `Assembly.Location` instead of `Assembly.CodeBase` for non-`.NET Framework` T4 hosts in T4 templates (thanks to [Pavlo Dudka](https://github.com/pavlo-dudka) for PR)
- [#3324](https://github.com/linq2db/linq2db/issues/3324): [Informix] use `CHAR_LENGTH` function instead of `LENGTH` for `string.Length`/`Sql.Length(str)` mappings
- [#3333](https://github.com/linq2db/linq2db/issues/3333): support for recent .net 6 related releases of database providers:
  - [**MySqlConnector 2.0.0**] ([#194](https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/194)): fixed `BulkCopy` support (no other changes required)
  - [**Microsoft.Data.Sqlite 6.0.0**]: we highly recommend to skip this version and wait for future releases, as we encounter multiple bugs with it. For same reason we will not provide any support for this version
  - [**Npgsql 6.0.0**]: there is no changes in current release for this version (planned for next release). In general it works, except errors with date types with `Kind.Unspecified`. We will review those errors for next release and decide wether we need to change something on our side or it's issue with user data
- [#3336](https://github.com/linq2db/linq2db/issues/3336): fixed issue where `query.ToString()` call could open connection to database

***

### Release 3.5.1

- [#3260](https://github.com/linq2db/linq2db/issues/3260): fixed `Argument types do not match` exception when `DefaultIfEmpty(?)` call followed by aggregate function with return type not matching sequence type
- [#3265](https://github.com/linq2db/linq2db/issues/3265): improved parameter deduplication logic
- [#3269](https://github.com/linq2db/linq2db/issues/3269): fix query cache miss for queries that use `Set`/`Value` method overloads with non-lambda value parameter for nullable columns
- [#3270](https://github.com/linq2db/linq2db/issues/3270): added support for `Enumerable/Queryable.Contains` extension method
- [#3276](https://github.com/linq2db/linq2db/issues/3276): fixed `LinqToDBException: Source must be enumerable` exception, when null value of `IEnumerable` type used in query
- [#3279](https://github.com/linq2db/linq2db/issues/3279): fixed regression in `Nullable<T>.Value/HasValue` properties handling
- [#3281](https://github.com/linq2db/linq2db/issues/3281): fixed performance regression, introduced in 3.5.0, and implemented more optimizations to reduce memory use
- [#3285](https://github.com/linq2db/linq2db/issues/3285): use column type for parameters/literals, used in text match operations against column value

***

### Release 3.5.0

- [#3158](https://github.com/linq2db/linq2db/issues/3158): fixed `ArgumentException` thrown from correct call of `Sql.Property(...)`
- [#3241](https://github.com/linq2db/linq2db/issues/3241): [SQL Server] added support for `OUTPUT` clause in `MERGE`. New APIs:
  - `MergeWithOutput`, `MergeWithOutputAsync`: executes merge and return output records
  - `MergeWithOutputInto`, `MergeWithOutputIntoAsync`: executes merge and insert output records into specified table
- [#3244](https://github.com/linq2db/linq2db/issues/3244):
  - [#3242](https://github.com/linq2db/linq2db/issues/3242): fixed concurrency issues in eager load
  - [#3248](https://github.com/linq2db/linq2db/issues/3248): fixed incorrect caching of UDT parameters
  - fixed 3.4.5 regression: `Sequence 'value(LinqToDB.Linq.Builder.ParameterContainer).GetValue(0)' cannot be converted to SQL`
  - fixed 3.4.5 regression: `NotImplementedException : Projection of in-memory collections is not implemented`
- [#3245](https://github.com/linq2db/linq2db/issues/3245): [Oracle] documented bulk copy mode switch `OracleTools.UseAlternativeBulkCopy`
- [#3246](https://github.com/linq2db/linq2db/issues/3246): fixed issue with property discovery when inherited property overriden using `new` keyword
- [#3251](https://github.com/linq2db/linq2db/issues/3251): fixed issue when data connection could use wrong mapping schema if you configure it with mapping schema, created using following approach: `new DataConnection(new MappingSchema(otherDataConnection.MappingSchema))` (creation of mapping schema using schema from another data connection)
- [#3261](https://github.com/linq2db/linq2db/issues/3261):
  - [#3253](https://github.com/linq2db/linq2db/issues/3253): fix serious query cache pollution by queries that use `Set`/`Value` method overloads with non-lambda value parameter for update/insert queries
  - implemented multiple memory optimizations across library code

***

### Release 3.4.5
- [#2970](https://github.com/linq2db/linq2db/issues/2970): [MySQL/MariaDB] fixed SQL for `contains` predicate
- [#3216](https://github.com/linq2db/linq2db/issues/3216): fixed query caching [regression](https://github.com/linq2db/linq2db/issues/3192#issuecomment-919105226)
- [#3217](https://github.com/linq2db/linq2db/issues/3217): fixed `null` values handling regression in eager load
- [#3220](https://github.com/linq2db/linq2db/issues/3220): fixed issues with caching of client-side collections used as tables
- [#3224](https://github.com/linq2db/linq2db/issues/3224): fixed issue in T4 templates when multiple versions of same assembly loaded. Thanks to [@AgentFire](https://github.com/AgentFire) for fix
- [#3230](https://github.com/linq2db/linq2db/issues/3230): fixed support of derived classes in associations
- [#3233](https://github.com/linq2db/linq2db/issues/3233): added support for enumerable collections to be used as tables

***

### Release 3.4.4

- [#3109](https://github.com/linq2db/linq2db/issues/3109): fixed support for `IStructuralEquatable` keys (e.g. arrays, tuples) in eager load
- [#3182](https://github.com/linq2db/linq2db/issues/3182): fixed generated SQL for scalar boolean subqueries used as predicates
- [#3184](https://github.com/linq2db/linq2db/issues/3184): fixed inheritance mapping with more than two levels of inheritance
- [#3186](https://github.com/linq2db/linq2db/issues/3186):
  - [PostgreSQL] fixed v3.3 regression in `UPDATE ... FROM ...` queries generation
  - [Sybase ASE] improved join generation in `UPDATE ... FROM ...` queries to avoid mixing ANSI and non-ANSI joins, which makes ASE unhappy
- [#3192](https://github.com/linq2db/linq2db/issues/3192): select only used columns when final projection contains condition, that could exclude columns from selection
- [#3193](https://github.com/linq2db/linq2db/issues/3193): improve generated SQL for conditions by removing excessive brackets around `AND` conditions
- [#3198](https://github.com/linq2db/linq2db/issues/3198): fixed incorrect member to sql conversion caching, that could lead to bad SQL generation
- [#3207](https://github.com/linq2db/linq2db/issues/3207): react to [undocumented](https://docs.oracle.com/en/database/oracle/oracle-database/21/odpnt/release_changes.html) breaking change is July releases of Oracle managed provider in byte order changes for Guid binary parameters: serialize `Guid` to `byte[]` on linq2db side before setting value to parameter

***

### Release 3.4.3

- [#3128](https://github.com/linq2db/linq2db/issues/3128): Fixed `InvalidCastException` when query contained some table methods applied to table, returned by `LoadWithAsTable` call (`With()`, `WithTableExpression()`, `TableName()`, `SchemaName()`, `DatabaseName()`, `ServerName()`, `IsTemporary()`, `TableOptions()`)
- [#3130](https://github.com/linq2db/linq2db/issues/3130): [PostgreSQL] Added `DataType.Binary` support in provider-specific bulk copy
- [#3135](https://github.com/linq2db/linq2db/issues/3135): [PostgreSQL] Added `DataType.UInt64` support for `long` fields in provider-specific bulk copy
- [#3137](https://github.com/linq2db/linq2db/issues/3137): Fixed thread-safety issues with `QueryHints`/`NextQueryHints` context properties
- [#3138](https://github.com/linq2db/linq2db/issues/3138): [T4] Disable generation of constructors with `LinqToDbConnectionOptions` parameter for data model if generated context derived from `DataContext`. Added option to disable generation of those constructors explicitly (`GenerateLinqToDbConnectionOptionsConstructors`, default: `true`).
- [#3147](https://github.com/linq2db/linq2db/issues/3147): [ImoutoChan](https://github.com/ImoutoChan) added support for non-public paremetrized constructors in mapping
- [#3149](https://github.com/linq2db/linq2db/issues/3149): [T4] For Access OleDb provider disabled identity columns detection, as OleDb provider reports them incorrectly. It is still possible to generate identity information using ODBC Access provider.
- [#3170](https://github.com/linq2db/linq2db/issues/3170): `EntityCreatedEventArgs` event arguments object extended with additional information: table name and table options flags to allow event receivers to distinguish between multiple tables using same mapping class (e.g. for main and temporary tables)

***

### Release 3.4.2

- [#3096](https://github.com/linq2db/linq2db/issues/3096): Removed incorrect use of `ThreadStatic` field in `RetryPolicy`
- [#3103](https://github.com/linq2db/linq2db/issues/3103): Fixed regression in query filters with custom data context classes
- [#3107](https://github.com/linq2db/linq2db/issues/3107): Fixed regression in `TableFunctionAttribute` with `IQueryable<T>` result type
- [#3111](https://github.com/linq2db/linq2db/issues/3111): Fixed regression in mapping of entities with non-public constructors

***

### Release 3.4.1

- [#1694](https://github.com/linq2db/linq2db/issues/1694): [MySQL] fixed compatibility with `ONLY_FULL_GROUP_BY` SQL mode
- [#3038](https://github.com/linq2db/linq2db/issues/3038): fixed `ArgumentException` in `First`/`Single` methods translation
- [#3043](https://github.com/linq2db/linq2db/issues/3043): fixed `Expression 'XXX' is not a Field` error for `ExpressionMethod`-based fields
- [#3049](https://github.com/linq2db/linq2db/issues/3049): fixed `InvalidCastException` accessing parameter-like properties on custom data context in query
- [#3056](https://github.com/linq2db/linq2db/issues/3056): fixed regression in dynamic columns support (`Destination array was not long enough` exception)
- [#3057](https://github.com/linq2db/linq2db/issues/3057): improved `MappingSchema` conversion lookup to look for more generic conversions in more cases
- [#3058](https://github.com/linq2db/linq2db/issues/3058): fixed stack overflow in queries with `Any()` applied to association
- [#3061](https://github.com/linq2db/linq2db/issues/3061): fixed issue with `is_empty` join flag column missing from final projection for multiple APPLY joins
- [#3082](https://github.com/linq2db/linq2db/issues/3082): support materialization of classes that doesn't have default constructor but have constructor with parameters, mapped to properties by name (e.g. C# 9 `record` types)
- [#3089](https://github.com/linq2db/linq2db/issues/3089): improved `NULL` columns support in SET queries

***

### Release 3.4.0

- [#2144](https://github.com/linq2db/linq2db/issues/2144): add support for parameters in `BulkCopy` API in `BulkCopyType = BulkCopyType.MultipleRows` mode. To enable parameters use new option `BulkCopyOptions.UseParameters`. `BulkCopyOptions.MaxParametersForBatch` allows you to override limit of parameters per batch.
- [#2321](https://github.com/linq2db/linq2db/issues/2321): [MSSQL] Add `UpdateWithOutput` and `UpdateWithOutputInto` API (including async variations) to support OUTPUT clause with update operation. For next releases we plan to extend support to other databases with OUTPUT/RETURNING clauses
- [#2363](https://github.com/linq2db/linq2db/issues/2363): [MSSQL] Add CTE support for MERGE satement source and target (support for CTE in MERGE for other databases will be implemented in next releases)
- [#2831](https://github.com/linq2db/linq2db/issues/2831): [Oracle] `INSERT ALL`/`INSERT FIRST` statements support (see details below). Thanks to [@jods4](https://github.com/jods4) for contribution
- [#2895](https://github.com/linq2db/linq2db/issues/2895): fix `NotImplementedException` for some complex eager load queries
- [#2903](https://github.com/linq2db/linq2db/issues/2903): Query tags support (see details below). Thanks to [@jlukawska](https://github.com/jlukawska) for contribution
- [#2919](https://github.com/linq2db/linq2db/issues/2919): fix support for internal members in projections
- [#2925](https://github.com/linq2db/linq2db/issues/2925): allow override of `TempTable.DisposeAsync` by making it `virtual`. Thanks to [David Acker](https://github.com/david-acker) for contribution
- [#2928](https://github.com/linq2db/linq2db/issues/2928): removed artifical limitations, that prevented use of `BulkCopy` API with `DataContext` instead of `DataConnection`
- [#2936](https://github.com/linq2db/linq2db/issues/2936): added `Sql.TryConvert` and `Sq.TryConvertOrDefault` extentions:
  - `Sql.TryConvert`: will return `NULL` if conversion failed. Supported by SQL Server 2012+ and Oracle 12.2+
  - `Sql.TryConvertOrDefault`: will return provided default value if conversion failed. Supported by Oracle 12.2+
  - Note that Oracle supports only limited set of conversions here (check for supported conversions in Oracle's documentation on CAST expression)
- [#2937](https://github.com/linq2db/linq2db/issues/2937): fix `NullReferenceException` from `DataContext` on client-side `GroupBy` calls. Thanks [Alexey Yakovlev](https://github.com/yallie) for contribution
- [#2939](https://github.com/linq2db/linq2db/issues/2939): fixed regression in interfaces support in queries
- [#2944](https://github.com/linq2db/linq2db/issues/2944): reduce number of allocations in codebase arround arrays materialization
- [#2946](https://github.com/linq2db/linq2db/issues/2946): added new extensions `Sql.IsDistinctFrom` and `Sql.IsNotDistinctFrom` that could be used to generate `IS [NOT] DISTINCT FROM` clause (or compatible expression for databases without it)
- [#2947](https://github.com/linq2db/linq2db/issues/2947): improved case-(in)sensitive search using `string.EndsWith/StartsWith/Contains` methods with `StringComparison` parameter
- [#2954](https://github.com/linq2db/linq2db/issues/2954): added `Sql.Default<T>()` helper to generate `DEFAULT` keyword (e.g. in insert queries)
- [#2956](https://github.com/linq2db/linq2db/issues/2956): [Oracle] fixed support for `OracleBlob`-typed parameters
- [#2957](https://github.com/linq2db/linq2db/issues/2957): [Oracle] generate `DATE` and `TIMESTAMP` literals instead of `TO_DATE()` and `TO_TIMESTAMP()/TO_TIMESTAMP_TZ()` functions
- [#2959](https://github.com/linq2db/linq2db/issues/2959): Initial Firebird 4 support (`FirebirdSql.Data.FirebirdClient` 8.0.1 or newer recommended)
  - [Firebird] support new types `decfloat`, `decfloat(16)`, `timespan with time zone`, `time with time zone` and `int128` in queries and T4 templates. Types mapped to:
    - `decfloat`/`decfloat(16)`: `FbDecFloat` (`DataType.DecFloat`). Specifying `Precision` <= 16 in mapping, `decfloat(16)` will be used
    - `timespan with time zone`: `FbZonedDateTime` (`DataType.DateTimeOffset`)
    - `time with time zone`: `FbZonedTime` (`DataType.TimeTZ`)
    - `int128`: `BigInteger` (`DataType.Int128`)
    - literals generation supported only for `int128` type, as for other types it is not allways possible to build literal
  - [Firebird] remap `Sql.CurrentTimeStamp` from `CURRENT_TIMESTAMP` to `LOCALTIMESTAMP` as `CURRENT_TIMESTAMP` changed type to zoned type in Firebird 4
  - [Firebird] use `TRIM(LEADING/TRAILING FROM {0})` instead of `RTRIM`/`LTRIM` UDFs, as UDFs were removed from Firebird 4
  - [Mapping] fixed issue were `Linq To DB` searched for explicit/implict conversion operators for type conversion only in target type. Now it will check both types for suitable conversion
  - [T4] added `Microsoft.Bcl.AsyncInterfaces.dll` assembly to T4 nugets to avoid irregular error from Visual Studio being unable to load it when running T4 template
  - [T4][Firebird] improved model generation by T4 templates:
    - do not emit `Precision`, `Scale` and `Length` properties on columns of types, that doesn't support them
    - do not emit output parameters for `FOR SELECT` stored procedures, that duplicate result table columns
- [#2961](https://github.com/linq2db/linq2db/issues/2961): Fixed regression in 3.3.0 with `Linq To DB` failing to distinguish between `COUNT()` columns from different subqueries
- [#2963](https://github.com/linq2db/linq2db/issues/2963): Fixed regression in 3.3.0 where `ArgumentException` could be thrown for `Sql.Expression`
- [#2967](https://github.com/linq2db/linq2db/issues/2967): Renamed async method `DeleteWithOutput` to `DeleteWithOutputAsync`
- [#2972](https://github.com/linq2db/linq2db/issues/2972): [PostgreSQL][DB2][Informix] hint types for `NULL` columns in queries to avoid errors when server cannot infer column type
  - [Informix] improve boolean column selection to use `boolean` type instead of `char` when column selects boolean literals
- [#2979](https://github.com/linq2db/linq2db/issues/2979): Fixed array-typed parameters support in extensions
- [#2981](https://github.com/linq2db/linq2db/issues/2981): Fixed regression where some generic expression associations stopped to work in v3.3.0
- [#2983](https://github.com/linq2db/linq2db/issues/2983): [SQL Server] Added support for SQL Server identity columns in `DataExtensions.RetrieveIdentity()` API. To retrieve values using table identity definition, use `useIdentity: true` parameter value
- [#2987](https://github.com/linq2db/linq2db/issues/2987):
  - [#2978](https://github.com/linq2db/linq2db/issues/2978), [#2997](https://github.com/linq2db/linq2db/issues/2997): [PostgreSQL][Oracle][Firebird] support generation of `NaN`/`Infinity` literals for `real`/`double precision` types
  - use roundtrip format for float literals
- [#2994](https://github.com/linq2db/linq2db/issues/2994): Fixed case where `LinqToDB` were failing to translate entity comparison to comparison of primary keys
- [#2998](https://github.com/linq2db/linq2db/issues/2998): added generation of data context constructor with generic `LinqToDbConnectionOptions<TContext>` argument in T4 and fixed multiple contexts generation in service container
- [#3001](https://github.com/linq2db/linq2db/issues/3001): Fixed `Expression 'Alias(...)' is not a Field.` exception in assocations
- [#3002](https://github.com/linq2db/linq2db/issues/3002): Fixed support of custom string-like types in search functions when type implements `IConvertible` and user-defined string conversions
- [#3006](https://github.com/linq2db/linq2db/issues/3006):
  - [#3005](https://github.com/linq2db/linq2db/issues/3005) `InsertOrUpdate` API will recognize more update setter values as empty update to perform `INSERT IF NOT EXISTS` operation (in addition to already supported `r => new Table() {}` template): `null`, `record => null`, `record => new Table()`
  - [SQL Server 2005-][Sybase]: removed explicit transaction, generated for `InsertOrUpdate` operation
- [#3010](https://github.com/linq2db/linq2db/issues/3010): added missing `TraceInfoStep.Completed` logging step to raw SQL queries, triggered from `CommandInfo` instance using `db.SetCommand` API. Thanks [Alexey Yakovlev](https://github.com/yallie) for contribution
- [#3013](https://github.com/linq2db/linq2db/issues/3013): fixed issues with unsigned types support with PostgreSQL
  - fixed parameters support for `ushort`, `uint`, `ulong` values
  - map `ulong` to `decimal(20)` for parameters and `CREATE TABLE` statement
- [#3014](https://github.com/linq2db/linq2db/issues/3014): fixed issues with query parsing with operations after complex projection
- [#3017](https://github.com/linq2db/linq2db/issues/3017): fixed concurrency issue in queries with `IN` expression with parameters
- [#3032](https://github.com/linq2db/linq2db/issues/3032): fixed several issues in `CreateTempTable` API:
  - [DB2] removed primary key generation for DB2 as it doesn't allow constraints in temporary tables
  - [#2922](https://github.com/linq2db/linq2db/issues/2922): [SQL Server] don't generate name for primary key constraint in temporary table to improve compatibility with contained databases
- [#3035](https://github.com/linq2db/linq2db/issues/3035): fixed several issues in generated SQL for queries that combine `Distinct`, `Order` and `Take/Skip` calls

#### Query Tags

Query tag is a comment, attached before query for any reason, but usually to trace source of specific query in logs/execution plans. To attach tag to query, call `TagQuery(commentText)` method on `IQueryable<T>`/`ITable<T>` instance in any place. All comments, attached to query will be aggregated into single comment.

```cs
var query = from x in db.Person.TagQuery("first tag").TagQuery("second tag") select x;
query.ToList();
```

```sql
/* first tag
second tag */
SELECT
	x."FirstName",
	x."PersonID",
	x."LastName",
	x."MiddleName",
	x."Gender"
FROM
	"Person" x
```

##### Limitations
- tags not supported for `CREATE TABLE` statement as we don't have `IQueryable` `CreateTable` API where you can attach `TagQuery` call
- tags not supported for Access as Access SQL doesn't have comments

#### INSERT ALL/FIRST support

New API added to provide access to `INSERT ALL/FIRST` multi-table inserts, supported by Oracle.

To define such query over table or subquery, you should apply `MultiInsert` method to them, define insert operations using `Into` (unconditional insert) or `When/Else` (conditional insert) method and execute it with `Insert`/`InsertAll`/`InsertFirst` or their async versions.

<details> 
<summary>SHOW EXAMPLES</summary>
<p>

```cs
// INSERT ALL (unconditional)
await source // query or table
    .MultiInsert()
        .Into(
            db.GetTable<Table1>(),
            x => new Table1 { ID = x.ID + 1, Value = x.N })
        .Into(
            db.GetTable<Table2>(),
            x => new Table2 { ID = x.ID + 3, Int = x.ID + 1 })
        .Into(
            db.GetTable<Table3>(),
            x => new Table3 { ID = x.ID + 3, Int = x.ID + 1 })
    // execute
    .InsertAllAsync();

// INSERT ALL (conditional)
await source // query or table
    .MultiInsert()
        .When(
            src => src.Field1 > 10,
            db.GetTable<Table1>(),
            x => new Table1 { ID = x.ID + 1, Value = x.N })
        .When(
            src => src.Field1 < 5,
            db.GetTable<Table2>(),
            x => new Table2 { ID = x.ID + 3, Int = x.ID + 1 })
        // optional Else
        .Else( // for other records (Field1 in [5; 10] range)
            db.GetTable<Table3>(),
            x => new Table3 { ID = x.ID + 3, Int = x.ID + 1 })
    // execute
    .InsertAllAsync();

// INSERT FIRST
source // query or table
    .MultiInsert()
        .When(
            src => src.Field1 > 10,
            db.GetTable<Table1>(),
            x => new Table1 { ID = x.ID + 1, Value = x.N })
        .When(
            src => src.Field1 < 5,
            db.GetTable<Table2>(),
            x => new Table2 { ID = x.ID + 3, Int = x.ID + 1 })
        // optional Else
        .Else( // for other records (Field1 in [5; 10] range)
            db.GetTable<Table3>(),
            x => new Table3 { ID = x.ID + 3, Int = x.ID + 1 })
    // execute
    .InsertFirst();
```

</p></details>

***

### Release 3.3.0

- [#1277](https://github.com/linq2db/linq2db/issues/1277): added new `LinqToDB.Common.Compilation.SetExpressionCompiler(...)` extension point to use alternative expressions compilers, e.g. [FastExpressionCompiler](https://github.com/dadhi/FastExpressionCompiler)
  - right now you can face issues using FEC, e.g. see this [issue](https://github.com/dadhi/FastExpressionCompiler/issues/287)
- [#1591](https://github.com/linq2db/linq2db/issues/1591): support for `dynamic` as raw query result
- [#1752](https://github.com/linq2db/linq2db/issues/1752): fixed generated code for expression test generator
- [#2509](https://github.com/linq2db/linq2db/issues/2509): fixed issue with wrong column selected from subquery with `UNION`
- [#2619](https://github.com/linq2db/linq2db/issues/2619): fixed sql generation for `UNION` queries over sorted sub-queries
- [#2645](https://github.com/linq2db/linq2db/issues/2645): [PostgreSQL] implement `DataExtensions.RetrieveIdentity` helper for `PostgreSQL` (this method could be used to assign autogenerated identifiers to in-memory collection of entities)
- [#2678](https://github.com/linq2db/linq2db/issues/2678): fixed mapping of F# classes
- [#2680](https://github.com/linq2db/linq2db/issues/2680):
  - [#2819](https://github.com/linq2db/linq2db/issues/2819): added more `Use<DB_NAME>` overloads for `LinqToDbConnectionOptionsBuilder` configuration builder to cover all supported databases and providers
  - [#2820](https://github.com/linq2db/linq2db/issues/2820): handle `Type.GetInterfaceMap()` throwing `PlatformNotSupported` exception on `corert` runtime. This will allow `linq2db` being used in UWP apps built with native build toolkit. Note that you still cannot use interface-based mappings there
- [#2691](https://github.com/linq2db/linq2db/issues/2691): add translation of `byte[].Length` to SQL
- [#2729](https://github.com/linq2db/linq2db/issues/2729): add `Sql.Collation` method to generate `COLLATE` operator
- [#2752](https://github.com/linq2db/linq2db/issues/2752): [T4] fixed model generation exception when multiple foreign keys with same name referencing same table
- [#2758](https://github.com/linq2db/linq2db/issues/2758): [SQL Server] fix schema provider and spatial types support for database with case-sensitive catalog collation
- [#2760](https://github.com/linq2db/linq2db/issues/2760): improve and fix joins optimization
- [#2763](https://github.com/linq2db/linq2db/issues/2763): [DB2] fix table columns load for some schemas
- [#2769](https://github.com/linq2db/linq2db/issues/2769): fixed regression in anonymous classes support in queries
- [#2774](https://github.com/linq2db/linq2db/issues/2774): improved memory use for binary literals generation
  - introduced limits for binary and string parameter values logging: by default we will log only first 100 bytes/200 characters of parameter to avoid full logging of huge values. This could be configured through `Configuration.MaxBinaryParameterLengthLogging` and `Configuration.MaxStringParameterLengthLogging` settings
- [#2776](https://github.com/linq2db/linq2db/issues/2776): fixed exception thrown for eager-load queries with multiple projections
- [#2785](https://github.com/linq2db/linq2db/issues/2785): [Oracle] fixed `ORA-00918` for selects with duplicate column names and `OFFSET/LIMIT` clause in Oracle 12+
- [#2797](https://github.com/linq2db/linq2db/issues/2797): fixed incorrect ternary expression translation to SQL
- [#2816](https://github.com/linq2db/linq2db/issues/2816): added `string.IsNullOrWhiteSpace` method translation to SQL. Note that it translates handling of all whitespace characters as .net version. If you need to handle only spaces, use `Sql.Trim` method
  - Access implementation handles only ASCII whitespace characters (due to limitations of default Access configuration)
- [#2822](https://github.com/linq2db/linq2db/issues/2822): ignore function/expression parameters, not used in SQL
- [#2829](https://github.com/linq2db/linq2db/issues/2829): improve SQL generation for predicates
  - [#845](https://github.com/linq2db/linq2db/issues/845): generate better SQL for predicates
  - [#2800](https://github.com/linq2db/linq2db/issues/2800): fix incorrect predicate generation in `ExpressinoMethod`
- [#2832](https://github.com/linq2db/linq2db/issues/2832): improved sub-query elimination from resulting SQL
- [#2843](https://github.com/linq2db/linq2db/issues/2843): skip `Update` operation without explicit field setters for `Merge` over non-updateable entity
- [#2853](https://github.com/linq2db/linq2db/issues/2853): allow `DataContext` override using `protected virtual void Dispose(bool disposing)` method
- [#2857](https://github.com/linq2db/linq2db/issues/2857): improve duplicate columns detection
  - [#2856](https://github.com/linq2db/linq2db/issues/2856): fix `ArgumentOutOfRangeException` in `UNION` queries
- [#2859](https://github.com/linq2db/linq2db/issues/2859): `INSERT` queries improvements
  - [#2700](https://github.com/linq2db/linq2db/issues/2700): fixed cases when generated insert from select query could use default values intead of columns from select query
  - [#2809](https://github.com/linq2db/linq2db/issues/2809): improve query build to support multiple `Set` calls for same column with last call win
- [#2871](https://github.com/linq2db/linq2db/issues/2871): [PostgreSQL] Add set of `Sql.Ext.PostgreSQL().ValueIs*Any(...)` extensions to support operations with [`ANY`](https://www.postgresql.org/docs/current/functions-comparisons.html) array operator
- [#2875](https://github.com/linq2db/linq2db/issues/2875): [PostgreSQL, Sybase] improve `UPDATE FROM subquery` SQL generation
- [#2877](https://github.com/linq2db/linq2db/issues/2877): Downgraded version of `System.ComponentModel.Annotations` dependency for `netcoreapp3.1` to 4.7.0 from 5.0.0 to fix compatibility [issue](https://github.com/linq2db/linq2db/issues/2868) with Azure Functions runtime
- [#2882](https://github.com/linq2db/linq2db/issues/2882): Prevent exceptions from `TempTable.Dispose`
- [#2884](https://github.com/linq2db/linq2db/issues/2884): [SQL Server] use `DROP IF EXISTS` for `DropTable` for SQL Server 2016+
  - [#2885](https://github.com/linq2db/linq2db/issues/2885): Add SQL Server 2016 dialect support
- [#2887](https://github.com/linq2db/linq2db/issues/2887): fix issue with `Sql.SqlFunctionAttribute` not respecting `IsPure` property
  - [#2886](https://github.com/linq2db/linq2db/issues/2886): `Sql.NewGuid()` function marked as non-pure function so it could be used in sorting and groupping
- [#2890](https://github.com/linq2db/linq2db/issues/2890): add .net framework 4.7.2 TFM with native support for `ValueTask`, `IAsyncEnumerable<T>` and `IAsyncDisposable` types
  - adds `IAsyncEnumerable<T>` sources support for `BulkCopy` with `net472` TFM
- [#2901](https://github.com/linq2db/linq2db/issues/2901): Fixed incorrect implementation of `DisposeAsync` for transaction
  - [#2905](https://github.com/linq2db/linq2db/issues/2905): Call transaction's `DisposeAsync` in call async contexts instead of `Dispose`
- [#2906](https://github.com/linq2db/linq2db/issues/2906):
  - [#2898](https://github.com/linq2db/linq2db/issues/2898): Add `Operation` property to `TraceEvent` to identify operation, associated with event
  - implement `IAsyncDisposable` in data context

***

### Release 3.2.3

- [#2742](https://github.com/linq2db/linq2db/issues/2742): [SAP HANA] fix identity value selection for `InsertWithIdentity` APIs. Thanks to [@deus348](https://github.com/deus348) for contribution
- [#2745](https://github.com/linq2db/linq2db/issues/2745): fixed issues in query parameters processing
- [#2746](https://github.com/linq2db/linq2db/issues/2746): fixed regression in method-based type conversions handling
- [#2749](https://github.com/linq2db/linq2db/issues/2749): fixed issue with boolean SQL `CASE` expression generation for some providers
- [#2751](https://github.com/linq2db/linq2db/issues/2751): multiple T4/schema improvements/fixes
  - [#2485](https://github.com/linq2db/linq2db/issues/2485): support multiple linq2db T4 nugets installed in single project
  - [#2629](https://github.com/linq2db/linq2db/issues/2629): T4 now generates constructor that accepts `LinqToDbConnectionOptions` parameter
  - [#2633](https://github.com/linq2db/linq2db/issues/2633): Reduce numer of errors, shown for T4 templates in JetBrains Rider
  - [#2663](https://github.com/linq2db/linq2db/issues/2663): [SQL Server] fix schema read for databases with database collation differ from catalog collation
  - [#2679](https://github.com/linq2db/linq2db/issues/2679): [PostgreSQL] add missing schema escaping for function with schema, that requires escaping
  - update T4 templates code to be compatible with old T4 hosts that support only C# 6
  - [MySql] use `MySqlConnector` provider instead of `MySql.Data` to read database schema by MySql T4 templates
  - [Sybase ASE] use `AdoNetCore.AseClient` managed provider instead of native provider to read database schema by Sybase ASE T4 templates
  - include `Humanizer` into T4 nugets to not require user to install it, if he wants to use pluralization templates
  - fixed typos in T4 configuration properties names:
    - `GetAssociationExtensionSinglularName` -> `GetAssociationExtensionSingularName`
    - `GetAssociationExtensionSinglularNameDefault` -> `GetAssociationExtensionSingularNameDefault`
  - [**BREAKING**] due to name change for MSBUILD properties, existing T4 templates should be updated to use new names in `$(...)` directives (for proper names check `CopyMe.<DB_NAME>.tt.txt` sample file)


***

### Release 3.2.2

- [#2672](https://github.com/linq2db/linq2db/issues/2672): fix `RetryPolicy` not applied for some `DataConnection` constructors and external connections
- [#2709](https://github.com/linq2db/linq2db/issues/2709): [3.2.0 regression] bad SQL generated for complex comparisons for some databases (SQL Server, SQL CE, Informix, Oracle, SAP HANA, SAP/Sybase ASE)
- [#2710](https://github.com/linq2db/linq2db/issues/2710): build symbol packages (.snupkg) for nuget
- [#2713](https://github.com/linq2db/linq2db/issues/2713): [3.2.0 regression] bad SQL generated for complex comparisons for SQL Server 2012+
- [#2716](https://github.com/linq2db/linq2db/issues/2716): fix incorrect `GROUP BY` optimization, when grouping applied to subquery returning constant values
- [#2719](https://github.com/linq2db/linq2db/issues/2719): fix incorrect reduction of `field == null ? null : expr` expression in condition to `expr`
- [#2722](https://github.com/linq2db/linq2db/issues/2722): add missing support for `SequenceNameAttribute.Schema` property for Oracle provider
- [#2725](https://github.com/linq2db/linq2db/issues/2725): [3.2.0 regression] `No coercion operator is defined between types 'System.TimeSpan' and 'System.Nullable`1[System.DateTime]'` error generated for `DateTime?/DateTimeOffset? +/- TimeSpan` expressions
- [#2727](https://github.com/linq2db/linq2db/issues/2727):
  - [SQL Server]: fix `datetime2` literal generation to not ommit microseconds, when milliseconds part is 0. Thanks to [@thechups](https://github.com/thechups) for contribution
  - [SQL Server]: trim `datetime2` parameter fractional seconds to precision, configured for column
  - [Informix]: fix `datetime` literal generation to not ommit microseconds, when milliseconds part is 0
- [#2730](https://github.com/linq2db/linq2db/issues/2730): fix `System.TypeLoadException: Method 'DisposeAsync' in type 'Npgsql.NpgsqlBinaryImporter' from assembly 'Npgsql, Version=4.1.7.0, Culture=neutral, PublicKeyToken=5d8b90d52f46fda7' does not have an implementation` exception when running T4 templates for PostgreSQL

***

### Release 3.2.1
- [#2705](https://github.com/linq2db/linq2db/issues/2705): fix incorrect ternary expression optimization with `null !=/== null` condition
- [#2706](https://github.com/linq2db/linq2db/issues/2706): [3.2.0 regression] `NotImplementedException` for `bool? variable == true/false` condition
- [#2707](https://github.com/linq2db/linq2db/issues/2707): fix `SourceLink` support

***

### Release 3.2.0

- [#2391](https://github.com/linq2db/linq2db/issues/2391) ([#1609](https://github.com/linq2db/linq2db/issues/1609), [#2368](https://github.com/linq2db/linq2db/issues/2368)): Added support for temporary tables and table existence checks to create/drop table API (see [below](#table-options-and-temporary-tables))
- [#2499](https://github.com/linq2db/linq2db/issues/2499): fixed custom data reader mapping expressions support for wrapped connection (e.g. using [MiniProfiler](https://miniprofiler.com/))
- [#2521](https://github.com/linq2db/linq2db/issues/2521): `LinqToDB.Common.Configuration.Linq.AllowMultipleQuery` setting was obsoleted and doesn't affect linq2db behavior anymore
- [#2545](https://github.com/linq2db/linq2db/issues/2545):
  - [#2526](https://github.com/linq2db/linq2db/issues/2526): **BREAKING** [Oracle] changed default escaping options for Oracle to escape lower-cased names (see [more details](#oracle-escaping-changes) below). Thanks to [@edsdck](https://github.com/edsdck) for contribution
  - [Oracle] added missing escaping for identity trigger/sequence names
  - [Oracle] enabled fallback to sql-based insert for native bulk copy into table if table name requires escaping (ODP.NET bug workaround)
  - [Oracle] `OracleXmlTable` now properly escapes column names
- [#2565](https://github.com/linq2db/linq2db/issues/2565):
  - [#2564](https://github.com/linq2db/linq2db/issues/2564): [Oracle] added support for date add/diff for Oracle
  - [SAP HANA] added support for milliseonds add/diff for SAP HANA
  - [DB2] fixed numeric overflow of milliseconds add/fiff for DB2 on large intervals
- [#2588](https://github.com/linq2db/linq2db/issues/2588): Fixed `DefaultIfEmpty()` method support when used with aggregates
- [#2596](https://github.com/linq2db/linq2db/issues/2596): Fixed infinite loop issue in eager load
- [#2599](https://github.com/linq2db/linq2db/issues/2599): `LinqToDB.Common.Configuration.Linq.UseBinaryAggregateExpression` setting were removed. If you used it - just remove code that changed it's value
- [#2601](https://github.com/linq2db/linq2db/issues/2601):
  - fixed issue with `group by` optimization that could produce incorrect sql
  - [Informix] fixed incorrect `group by` optimization for Informix (incorrect sql)
  - more constant `group by` columns detected and removed (e.g. constant functions)
- [#2602](https://github.com/linq2db/linq2db/issues/2602): Fixed regression with parameters caching in eager load queries
- [#2604](https://github.com/linq2db/linq2db/issues/2604): [Access] Implemented date add/diff for Access ([#2598](https://github.com/linq2db/linq2db/issues/2598)). Thanks to [Jarosław Kluz](https://github.com/yarecky1) for contribution
- [#2606](https://github.com/linq2db/linq2db/issues/2606): Fixed client-side implementation for several string Sql functions: `Sql.Left`, `Sql.Substring`, `Sql.CharIndex`, `Sql.Right`, `Sql.Stuff`, `Sql.Space`, `Sql.PadLeft`, `Sql.PadRight`, `Sql.Replace` ([#2605](https://github.com/linq2db/linq2db/issues/2605)). Thanks to [Jarosław Kluz](https://github.com/yarecky1) for contribution
- [#2618](https://github.com/linq2db/linq2db/issues/2618): Fixed reverse order of sort expressions in window functions
- [#2621](https://github.com/linq2db/linq2db/issues/2621):
  - [#2620](https://github.com/linq2db/linq2db/issues/2620): Fixed issue with `Nullable<T>` columns handling in some cases
  - [SQLite] Improved data mapping for `Microsoft.Data.Sqlite` provider to avoid fallback to slow mapping mode when provider fails to report column type
  - fixed issue, when post-load data conversion `T -> T` could be applied twice to loaded value
  - fixed issue, when custom column converter's `null` mapping could be ignored and default value used instead
- [#2623](https://github.com/linq2db/linq2db/issues/2623): Fixed NRT annotations for `Validation.ttinclude` template
- [#2625](https://github.com/linq2db/linq2db/issues/2625): [PostgreSQL] Added new default type to parameter mappings (previously it was giving error). Fixes [#2624](https://github.com/linq2db/linq2db/issues/2624)
  - `UInt16` -> `int4`
  - `UInt32` -> `int8`
  - `UInt64` -> `numeric`
- [#2626](https://github.com/linq2db/linq2db/issues/2626): Support multiple `ColumnAttribute`s on property/field. Fixes complex types mapping support ([#2590](https://github.com/linq2db/linq2db/issues/2590))
- [#2627](https://github.com/linq2db/linq2db/issues/2627): Fixed support for `timestamptz` columns mapping in Oracle with MiniProfiler enabled ([#2499](https://github.com/linq2db/linq2db/issues/2499))
- [#2642](https://github.com/linq2db/linq2db/issues/2642): fix `TargetInvocationException` in `LoadWith` queries
- [#2647](https://github.com/linq2db/linq2db/issues/2647): fix incorrect order of `ORDER BY` columns if sorted by subquery
- [#2651](https://github.com/linq2db/linq2db/issues/2651):
  - [PostgreSQL] `npgsql` 5: support changes to provider-specific bulk copy API
  - [Oracle] [#2632](https://github.com/linq2db/linq2db/issues/2632): support provider-specific bulk copy for managed provider (requires latest version of providers)
  - [Oracle] support explicit schema name for target table in provider specific bulk copy
  - [Oracle] fallback provider specific bulk copy to sql-based implementation if server name specified for target table
  - [Oracle] allow explicit transaction use with provider-specific bulk copy
  - fix NRT-related issues in T4 templates
- [#2659](https://github.com/linq2db/linq2db/issues/2659):
  - [T4] remove last remaining pieces of silverlight support
  - [T4] fix validation generation for properties with `Conditional` set
  - [T4] fix `IsValid` method of generated validator to return `true` only when all validations passed
- [#2662](https://github.com/linq2db/linq2db/issues/2662): fixed SQL generation for `group by` with `distinct` over group
- [#2665](https://github.com/linq2db/linq2db/issues/2665): fixed invalid SQL generation for select list for `group by` subqueries in `EXISTS()` clause
- [#2661](https://github.com/linq2db/linq2db/issues/2661): `CommandBehavior.SequentialAccess` support. See more details [below](#sequentialaccess-support)
- [#2670](https://github.com/linq2db/linq2db/issues/2670): queries optimization/predicates refactoring
  - [#2005](https://github.com/linq2db/linq2db/issues/2005): [Firebird] generate `[NOT ]CONTAINS`/`[NOT ]STARTING WITH` predicates instead of `LIKE` for corresponding string predicates
  - [#2490](https://github.com/linq2db/linq2db/issues/2490): improved `group by` optimization
  - [#2540](https://github.com/linq2db/linq2db/issues/2540): improved `string.IsNullOrEmpty` translation
  - [#2619](https://github.com/linq2db/linq2db/issues/2619): fixed exception in `UNION` queries with sort
  - [#2669](https://github.com/linq2db/linq2db/issues/2669): fixed generation of long parameter names for take/skip parameters
  - overal performance improvements that should address [#2556](https://github.com/linq2db/linq2db/issues/2556), [#2677](https://github.com/linq2db/linq2db/issues/2677)
  - [#1189](https://github.com/linq2db/linq2db/issues/1189): fix `DateTime.Now` support in `ExpressionMethod`
  - [#1455](https://github.com/linq2db/linq2db/issues/1455): `ArgumentException` for `group by` query with `DefaultIfEmpty` calls
  - [#913](https://github.com/linq2db/linq2db/issues/913): fix incorrect `GROUP BY` generation for some providers
- [#2688](https://github.com/linq2db/linq2db/issues/2688): fixed issue when `Nullable<T>` member could be invoked on `null` literal
- [#2692](https://github.com/linq2db/linq2db/issues/2692): added support for `IsPrimaryKey`, `IsDbGenerated`, `IsDiscriminator` properties of `System.Data.Linq.Mapping.ColumnAttribute`

#### Table Options and Temporary Tables

Linq2db has `CreateTempTable` API that actually creates regular table, which is dropped on table object disposal.

This feature adds support for real temporary tables into linq2db for databases that support temporary tables. Additionally it adds support for `TABLE EXISTS` checks for `CREATE/DROP TABLE` statements (APIs).

With this feature we introduce table flags/options, which could be used to mark table as temporary table. This will allow linq2db to generate proper SQL and table name for such tables and corresponding create/drop table SQL ([TableOptions.cs](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/TableOptions.cs)):
```cs
enum TableOptions
{
    // options not set
    NotSet,
    // default options for regular table
    None,

    // these flags require database support, see list below

    // generates table existense check for CREATE TABLE statement
    CreateIfNotExists,
    // generates table existense check for DROP TABLE statement
    DropIfExists,

    // temporary table flags:
    // temporary table and table content (data) has visibility flags
    // - local: table/data visible only from session that created them (current session)
    // - global: table/data visible to all sessions
    // - transaction: (for data only) data is visible only from current transaction 
    // LinqToDB will select most suitable temp table based on specified flags when database support
    // more than one kind of temp tables, or throw exception when incompatible flags specified

    // local (not visible to other sessions) temporary table
    IsTemporary,

    // table visibility flags
    IsLocalTemporaryStructure,
    IsGlobalTemporaryStructure,

    // data visibility flags
    IsLocalTemporaryData,
    IsGlobalTemporaryData,
    IsTransactionTemporaryData,

    // temporary globally-scoped table:
    // table is visible to all sessions, data visibility depends on database
    IsGlobalTemporary,

}
```

Changes to existing API surface:
- all APIs that accept table name components now also accept `TableOptions tableOptions` enum parameter
- for `ITable<T>` instances table options could be set using `IsTemporary()` and `TableOptions(options)` extensions
- `HasIsTemporary(bool temporary)` and `HasTableOptions(TableOptions options)` methods added to fluent mapping API to configure entity table options
- `bool IsTemporary` and `TableOptions TableOptions` properties added to `TableAttribute` mapping attribute
- `IntoTempTable\IntoTempTableAsync` `IQueryable<T>` and `IEnumerable<T>` extensions added to create table and save query results/collection into it
- [**BREAKING**] `CreateTempTable\CreateTempTableAsync` APIs default behavior changed to create temporary table instead of regular disposable table


Table existence check support:
|Database      | Drop Table |  Create Table  |
|-------------:|:----------:|:--------------:|
|Access        |            |                |
|DB2           |     √      |        √       |
|Firebird      |     √      |        √       |
|Informix      |     √      |        √       |
|MySQL/MariaDB |     √      |        √       |
|Oracle        |     √      |        √       |
|PostgreSQL    |     √      |        √       |
|SAP HANA      |            |                |
|SQL CE        |            |                |
|SQLite        |     √      |        √       |
|SQL Server    |     √      |        √       |
|SAP/Sybase ASE|     √      |        √       |

Temporary table support:
|Database      |Local Structure|Global Structure|Local Data|Global Data|TransactionData |
|-------------:|:-------------:|:--------------:|:--------:|:---------:|:--------------:|
|Access        |               |                |          |           |                |
|DB2           |       √       |        √       |    √     |           |                |
|Firebird      |               |        √       |    √     |           |       √        |
|Informix      |       √       |                |    √     |           |                |
|MySQL/MariaDB |       √       |                |    √     |           |                |
|Oracle        |               |        √       |    √     |           |       √        |
|PostgreSQL    |       √       |                |    √     |           |       √        |
|SQLite        |       √       |                |    √     |           |                |
|SAP HANA      |       √       |        √       |    √     |           |                |
|SQL CE        |               |                |          |           |                |
|SQL Server    |       √       |        √       |    √     |    √      |                |
|SAP/Sybase ASE|       √       |        √       |    √     |    √      |                |

#### Oracle Escaping Changes

Prior to this release, we didn't escaped database identifiers (e.g. table or column name) if they contained lower-cased letters by default. User was able to enable this escaping manually by setting
```cs
OracleTools.DontEscapeLowercaseIdentifiers = false;
```

With this release we switch default value for this option to `false`, so lowercase identifiers will be escaped by default (as they should).

What code will be broken with this change:
- mapping with explicit lowercase identifier for database object without lowercase letters in name
- mapping on class/property/field with lowercase letter in name without explicit uppercase mapping for database object without lowercase letters in name

How to fix:
- specify proper names for database objects in mapping
- enable option back explicitly

Note: code, generated by T4 templates is not affected as it use names from database

Example:
```sql
MYTABLE
(
    ID NUMBER,
    COLUMN NUMBER
)
```
```cs
// table name not specified and defaults to class name "MyTable"
[Table]
class MyTable
{
    // column name not specified and defaults to property name "Id"
    [Column]
    public int Id { get; set; }

    // column name specified but not correct by case
    [Column("CoLuMN")]
    public int MyColumn { get; set; }
}

// correct table name specified
[Table("MYTABLE")]
class MyTable
{
    // property name has same casing as column in db
    [Column]
    public int ID { get; set; }

    // correct column name specified
    [Column("COLUMN")]
    public int MyColumn { get; set; }
}
```

#### SequentialAccess support

[#2661](https://github.com/linq2db/linq2db/pull/2661) adds support for `CommandBehavior.SequentialAccess` behavior support in query results mapping. Fixes [#1185](https://github.com/linq2db/linq2db/pull/1185), [#2116](https://github.com/linq2db/linq2db/pull/2116).

Right now mapping of row to .NET object could read row columns from provider multiple times and in arbitrary order. It works fine with `CommandBehavior.Default`, which loads whole data row from server into memory, but doesn't work with `CommandBehavior.SequentialAccess`, which requires consumer to read each column once (or zero) and in order.

With this release we add new option `LinqToDB.Common.Configuration.OptimizeForSequentialAccess` (`false` by default), which will enable mapping optimization to read data row columns only once and in proper order to make our mapping compatible with `SequentialAccess`. In later releases we plan to enable this optimization permanently and remove option.

Note that:
1. option doesn't enable `SequentialAccess` behavior for queries. You need to do it itself, e.g. by using custom command processor. See example below
2. (1) means that option could be enabled even if you don't plan to use `SequentialAccess` behavior to generate a bit more optimal mapping, but we wouldn't expect any noticable gain from it
3. linq2db fallbacks to slow mapping mode if data row mapping fails. This will not work with `SequentialAccess` as it doesn't allow data row re-read. Not really an issue, as valid case of mapping failure occurs only for quite strange queries and indication that there is something wrong with them
4. if data provider doesn't support `SequentialAccess` behavior, behavior change will have no effect on it. See list of providers that are known to support `SequentialAccess` below

Example of custom command processor to enable `SequentialAccess` behavior:
```cs
// register custom command processsor
DbCommandProcessorExtensions.Instance = new SequentialAccessCommandProcessor();

// custom command processor to enable SequentialAccess query behavior
public class SequentialAccessCommandProcessor : IDbCommandProcessor
{
  DbDataReader IDbCommandProcessor.ExecuteReader(DbCommand command, CommandBehavior commandBehavior)
  {
    // override only Default behavior, we don't want to break schema queries
    return command.ExecuteReader(
      commandBehavior == CommandBehavior.Default
        ? CommandBehavior.SequentialAccess
        : commandBehavior);
  }

  Task<DbDataReader> IDbCommandProcessor.ExecuteReaderAsync(
    DbCommand command,
    CommandBehavior commandBehavior,
    CancellationToken cancellationToken)
  {
    return command.ExecuteReaderAsync(
      commandBehavior == CommandBehavior.Default
        ? CommandBehavior.SequentialAccess
        : commandBehavior,
      cancellationToken);
  }

  int IDbCommandProcessor.ExecuteNonQuery(DbCommand command) => command.ExecuteNonQuery();
  Task<int> IDbCommandProcessor.ExecuteNonQueryAsync(DbCommand command, CancellationToken cancellationToken)
    => command.ExecuteNonQueryAsync(cancellationToken);
  object? IDbCommandProcessor.ExecuteScalar(DbCommand command) => command.ExecuteScalar();
  Task<object?> IDbCommandProcessor.ExecuteScalarAsync(DbCommand command, CancellationToken cancellationToken)
    => command.ExecuteScalarAsync(cancellationToken);
}
```

List of providers that actually support `SequentialAccess`:
- [Access] MS Access OleDb provider
- [MySql / MariaDB] `MySql.Data` (but not `MySqlConnector`)
- [PostgreSQL] `npgsql` (at least v4.1.0 recommended, as lower versions contain serious bug in SequentialAccess implementation)
- [SQL Server] `System.Data.SqlClient`, `Microsoft.Data.SqlClient`
- [SQL CE] `SqlCe`

***

### Release 3.1.6
- fixed issue with nesting `Sql.Extension` within `ExpressionMethod` extensions ([#2562](https://github.com/linq2db/linq2db/issues/2562))
- fixed handling of custom converters in some places ([#2566](https://github.com/linq2db/linq2db/issues/2566))
- fully qualify `LinqToDB.Mapping.Relationship` in code, generated by T4 templates, to avoid naming conflicts ([#1586](https://github.com/linq2db/linq2db/issues/1586)). Thanks to [Daniel Kaschel](https://github.com/dipique) for this contribution
- fully qualify `LinqToDB.DataType` in code, generated by T4 templates, to avoid naming conflicts when there is a table with same name ([#2168](https://github.com/linq2db/linq2db/issues/2168)). Thanks to [Daniel Kaschel](https://github.com/dipique) for this contribution
- fixed CTE generation for MySql ([#2573](https://github.com/linq2db/linq2db/issues/2573))
- fixed issue where `ToDictionaryAsync` could ignore passed comparer ([#2585](https://github.com/linq2db/linq2db/issues/2585))

***

### Release 3.1.5

- fixed compatibility issues with DB2 iSeries OleDb provider ([#2544](https://github.com/linq2db/linq2db/issues/2544))
- fixed 3.1.4 regression in parameters caching for `LoadWith` queries ([#2532](https://github.com/linq2db/linq2db/issues/2532))
- remove potential `InvalidOperationException` from Access schema provider ([#2534](https://github.com/linq2db/linq2db/issues/2534))
- fixed regression in handling multiple mappings to same column ([#2530](https://github.com/linq2db/linq2db/issues/2530))
- fixed `InvalidOperationException` when use mapped properties as parameters ([#2546](https://github.com/linq2db/linq2db/issues/2546))

***

### Release 3.1.4

- fixed compatibility issues with DB2 iSeries provider ([#2516](https://github.com/linq2db/linq2db/issues/2516), [#2517](https://github.com/linq2db/linq2db/issues/2517))
- fixed `Table 'LinqToDB.SqlQuery.SqlMergeSourceTable' not found` error in Merge queries with queryable source (e.g. when nullable parameters used) ([#2522](https://github.com/linq2db/linq2db/issues/2522))
- disabled `DateTimeOffset.Now` mapping to SQL, introduced by previous release as it could break code, that worked before ([#2512](https://github.com/linq2db/linq2db/issues/2512)). Functionality will be returned later with proper implementation.
- added date operations support for `DateTimeOffset` for `postgresql` ([#2508](https://github.com/linq2db/linq2db/issues/2508))
- [breaking] `DataConnection` now will reuse `MappingSchema` between connections when same nested mapping schemas used to improve memory footprint. If your code depended on old behavior with each `DataConnection` having own mapping schema instance, let us now ([#2466](https://github.com/linq2db/linq2db/issues/2466))
- fixed rare issue, when sql logging could create new command instance on when command execution failed ([#2523](https://github.com/linq2db/linq2db/issues/2523))
- fixed parameter values caching in eager load subqueries ([#2506](https://github.com/linq2db/linq2db/issues/2506))
- fix issues with `SequenceNameAttribute` discovery ([#2504](https://github.com/linq2db/linq2db/issues/2504))

***

### Release 3.1.3

- fixed `StackOverflowException` in `LoadWith` queries ([#2474](https://github.com/linq2db/linq2db/issues/2474))
- support `TransactionScope` with `LoadWith` queries ([#2471](https://github.com/linq2db/linq2db/issues/2471))
- fixed incorrect order of schema/database/server name parameters in some `Update`, `UpdateAsync`, `Insert`, `InsertAsync`, `InsertOrReplace`, `InsertOrReplaceAsync`, `InsertWith?Identity`, `InsertWithIdentity?Async` overloads ([#2479](https://github.com/linq2db/linq2db/issues/2479))
- fixed return value for `SqlDataType.SystemType` property ([#2476](https://github.com/linq2db/linq2db/issues/2476))
- [MSSQL][T4][Schema] added support for `MS_Description` property on procedures, functions and parameters ([#2256](https://github.com/linq2db/linq2db/issues/2256))
- fixed incorrect query simplification in cases when only aggregated values selected ([#2478](https://github.com/linq2db/linq2db/issues/2478))
- added support for `DateTimeOffset.Now/Sql.CurrentTzTimestamp` (mssql, posgresql, oracle)  and `Sql.DateDiff(DateTimeOffset)` (mssql) ([#2382](https://github.com/linq2db/linq2db/issues/2382))
- fixed regression in SQL generation for queries with `SelectMany` ([#2470](https://github.com/linq2db/linq2db/issues/2470))

***

### Release 3.1.2

- restored support for MySqlConnector 0.x provider ([#61](https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/61))
- added support for string interpolation in expressions ([#2434](https://github.com/linq2db/linq2db/issues/2434))
- fixed issue with column order when `SkipOnEntityFetch=true` mapping used ([#2456](https://github.com/linq2db/linq2db/issues/2456))
- fixed issue with `Nullable<T>` null values handling `object`-typed context ([#2465](https://github.com/linq2db/linq2db/issues/2465))
- performance improvements ([#2453](https://github.com/linq2db/linq2db/issues/2453))
- improve handling of `ExpressionMethodAttribute` applied to overriden member ([#2429](https://github.com/linq2db/linq2db/issues/2429))
- added guard exception for unsupported use of associations with set operations ([#2464](https://github.com/linq2db/linq2db/issues/2464))
- fixed issues in `Expressions.MapMember` ([#2468](https://github.com/linq2db/linq2db/issues/2468))

***

### Release 3.1.1

- [Firebird] fix escaping of table aliases ([#2445](https://github.com/linq2db/linq2db/issues/2445))
- fix `IAsyncEnumerable` support for eager load queries ([#2442](https://github.com/linq2db/linq2db/issues/2442))
- fix `IAsyncEnumerable<T>.GetAsyncEnumerator` signature for netfx builds to support `async foreach` ([#2444](https://github.com/linq2db/linq2db/issues/2444))
- convert some sync code in async eager load queries to async ([#2444](https://github.com/linq2db/linq2db/issues/2444))
- [PostgreSQL] fix enum mapping regression in provider-specific bulk copy ([#2433](https://github.com/linq2db/linq2db/issues/2433))
- fix regression in parameter-less `Sql.Expression` throwing exception when used inside `CASE` clause and expression text contains `{}` characters ([#2431](https://github.com/linq2db/linq2db/issues/2431))

***

### Release 3.1.0

- [mysql][breaking] Minimal supported `MySqlConnector` provider version bumped to 1.0.0 ([#2270](https://github.com/linq2db/linq2db/issues/2270))
- [sqlserver] `GetSchema` API use `sp_describe_first_result_set` procedure for stored procedure result set schema load with SQL Server 2012+ instead of `CommandBehavior.SchemaOnly`. This will improve support for procedures that were failing to load schema before. Old behavior could be enabled by `GetSchemaOptions.UseSchemaOnly = true` property or globally using `Common.Configuration.SqlServer.UseSchemaOnlyToGetSchema = true` ([#2348](https://github.com/linq2db/linq2db/issues/2348))
- fixed issue with query caching that could potentially lead to exceptions after query rewrite ([#2367](https://github.com/linq2db/linq2db/issues/2367))
- fixed support for multiple levels of association nesting for `LoadWithAsTable` API ([#2351](https://github.com/linq2db/linq2db/issues/2351))
- fixed version 3 regression when `T => T` conversions was not applied to query results ([#2356](https://github.com/linq2db/linq2db/issues/2356))
- added new option `LinqToDB.Common.Configuration.Data.BulkCopyUseConnectionCommandTimeout = false` to enable use of connection timeout from data connection for provider-specific `BulkCopy` operations if it is not set explicitly in `BulkCopyOptions.BulkCopyTimeout` property ([#418](https://github.com/linq2db/linq2db/issues/418))
- fixed `ORDER BY` clause merging to specify ordered columns in proper order and remove duplicate columns ([#2378](https://github.com/linq2db/linq2db/issues/2378), [#2405](https://github.com/linq2db/linq2db/issues/2405))
- fixed regression with bad optimization of ternary operations, which could lead to exceptions during query execution ([#2398](https://github.com/linq2db/linq2db/issues/2398))
- fixed support for eager load queries with complex filters ([#2392](https://github.com/linq2db/linq2db/issues/2392), [#2393](https://github.com/linq2db/linq2db/issues/2393))
- fixed duplicate records load for some eager load queries ([#2392](https://github.com/linq2db/linq2db/issues/2392))
- [oracle] [@lytico](https://github.com/lytico) added support for materialized views to schema API and fixed `COMMENT` load for tables/columns ([#2404](https://github.com/linq2db/linq2db/issues/2404))
- added out/ref parameter values rebind for `ExecuteProc<T>`, `QueryProc` and `QueryProcMultiple` procedure execution APIs ([#2361](https://github.com/linq2db/linq2db/issues/2361))
- added missing async versions of `QueryProc` stored procedure APIs ([#1501](https://github.com/linq2db/linq2db/issues/1501))
- fixed issue with non-provider-specific `BulkCopy` API using wrong table name if passed target table object and source data use different table names ([#2408](https://github.com/linq2db/linq2db/issues/2408))
- fixed regression in enum types mapping ([#2372](https://github.com/linq2db/linq2db/issues/2372))
- fixed support for interfaces in `Merge API` ([#2388](https://github.com/linq2db/linq2db/issues/2388))
- improved support for parameters with unsupported characters (e.g. spaces) ([#2403](https://github.com/linq2db/linq2db/issues/2403))
- fixed caching for client-side merge source ([#2377](https://github.com/linq2db/linq2db/issues/2377))
- [sybase] fixed incorrect type returned by schema provider for `varbinary` db type
- fixed `Conditional` property for `NotifyPropertyChanged.tt` template to wrap all generated property code into condition
- [firebird] `FirebirdIdentifierQuoteMode.Auto` now will quote identifiers with lowercase latin characters too
- [firebird] added sequence name escaping for `SequenceNameAttribute`
- [firebird][T4] removed generation of escaped identifiers in mapping attributes as now it handled by sql builder. Could require T4 model regeneration if such identifiers used
- [access] return `char` .net type for `text(1)` db type from schema provider instead of `string`
- [sybase] schema provider now supports external transactions for procedure schema load (for both DataAction and native providers)
- [SAP HANA] fixed overflow exception when query returns `blob`/`clob` columns with ODBC x64 provider
- [SAP HANA] schema provider loads database objects from current schema only (or schemas, specified in API options)
- [SAP HANA] schema provider now maps spatial types to `byte[]` instead of `object`
- fixed `InvalidCastException` in update query when column and value types doesn't match (but conversion exists) ([#2415](https://github.com/linq2db/linq2db/issues/2415))
- fixed issues with parameters in groupped queries ([#2375](https://github.com/linq2db/linq2db/issues/2375))
- fixed extension methods chaining ([#2418](https://github.com/linq2db/linq2db/issues/2418))
- fixed sql generation for inverted string comparison with `<=` or `>=` operator (e.g. `0 <= table.str.CompareTo(otherStr)`) ([#2424](https://github.com/linq2db/linq2db/issues/2424))

#### Query parameters improvements
PR [#2347](https://github.com/linq2db/linq2db/issues/2347) fixes several issues related to parameters:
- improve parameter inlining flag handling to avoid situations, when flag ignored. Fixes [#2346](https://github.com/linq2db/linq2db/issues/2346)
- add global configuration flag `bool Configuration.Linq.ParameterizeTakeSkip = true` to allow inlining for `Take/Skip` parameters only
- add `IQueryable<TSource> InlineParameters<TSource>(this IQueryable<TSource> source)` extension to enable parameters inlining for specific query

#### New APIs

##### Async CreateTempTable API
Feature [#2408](https://github.com/linq2db/linq2db/issues/2408) adds async overloads to `CreateTempTable` API:
```cs
Task<TempTable<T>> CreateTempTableAsync<T>(
    this IDataContext db,
    string? tableName    = null,
    string? databaseName = null,
    string? schemaName   = null,
    string? serverName   = null);
Task<TempTable<T>> CreateTempTableAsync<T>(
    this IDataContext db,
    IEnumerable<T> items,
    BulkCopyOptions? options = null,
    string? tableName        = null,
    string? databaseName     = null,
    string? schemaName       = null,
    string? serverName       = null);
Task<TempTable<T>> CreateTempTableAsync<T>(
    this IDataContext db,
    string? tableName,
    IEnumerable<T> items,
    BulkCopyOptions? options = null,
    string? databaseName     = null,
    string? schemaName       = null,
    string? serverName       = null);
Task<TempTable<T>> CreateTempTableAsync<T>(
    this IDataContext db,
    IQueryable<T> items,
    string? tableName             = null,
    string? databaseName          = null,
    string? schemaName            = null,
    Func<ITable<T>, Task>? action = null,
    string? serverName            = null);
Task<TempTable<T>> CreateTempTableAsync<T>(
    this IDataContext db,
    IQueryable<T> items,
    Action<EntityMappingBuilder<T>> setTable,
    string? tableName             = null,
    string? databaseName          = null,
    string? schemaName            = null,
    Func<ITable<T>, Task>? action = null,
    string? serverName            = null);
Task<TempTable<T>> CreateTempTableAsync<T>(
    this IDataContext db,
    string? tableName,
    IQueryable<T> items,
    string? databaseName          = null,
    string? schemaName            = null,
    Func<ITable<T>, Task>? action = null,
    string? serverName            = null);
Task<TempTable<T>> CreateTempTableAsync<T>(
    this IDataContext db,
    string? tableName,
    IQueryable<T> items,
    Action<EntityMappingBuilder<T>> setTable,
    string? databaseName          = null,
    string? schemaName            = null,
    Func<ITable<T>, Task>? action = null,
    string? serverName            = null);
```

##### Async BulkCopy API
Feature [#2314](https://github.com/linq2db/linq2db/issues/2314) adds async overloads to BulkCopy API:
```cs
Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
    ITable<T> table,
    BulkCopyOptions options,
    IEnumerable<T> source);
Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
    ITable<T> table,
    BulkCopyOptions options,
    IAsyncEnumerable<T> source);
```

Note that as usual it requires support from underlying provider, as if it doesn't support required async APIs, execution will be done in synchronous mode.
For native bulk copy following providers provide async API:
- [MySql] MySqlConnector
- [PostgreSQL] Npgsql
- [SAP HANA] SAP HANA .NET Framework native provider
- [SQL Server] `System.Data.SqlClient` and `Microsoft.Data.SqlClient`

For 3 other types of bulk copy provider should support `ExecuteNonQueryAsync` API on command.

Also bulk copy methods, exposed by `<DB_NAME>Tools` classes were obsoleted, as they doesn't add anything new and just call `BulkCopy` API internally.

##### New QueryProc overloads

```cs
// new QueryProc overloads to support results of anonymous type
IEnumerable<T> QueryProc<T>(
    this DataConnection connection,
    T template,
    string sql,
    params DataParameter[] parameters);
IEnumerable<T> QueryProc<T>(
    this DataConnection connection,
    T template,
    string sql,
    object? parameters);
```

##### SkipOnEntityFetch column mapping flag

Feature [#2387](https://github.com/linq2db/linq2db/issues/2387) adds new column mapping flag `SkipOnEntityFetch` to ignore column on entity select queries without explicit column column list specified, e.g. `db.Table.ToList()`.

This flag could be useful if you have columns, you want to select only explicitly, e.g. big blob-like columns.

Flag could be set:
- using `ColumnAttribute.SkipOnEntityFetch` property
- `SkipOnEntityFetch(bool skipOnEntityFetch = true)` method of fluent mapper


***

### Release 3.0.1

- use of `RetryPolicy` blocks access to provider-specific functionality for command and connection ([#2342](https://github.com/linq2db/linq2db/issues/2342))

***

## Final Release Changes

- [T4][Schema] improved reverse engineering of mysql types also for stored procedures (parameters and result table) ([#2313](https://github.com/linq2db/linq2db/issues/2313))
- fixed incorrect paging values, when more than one `Take`/`Skip` call applied to (sub)query ([#2325](https://github.com/linq2db/linq2db/issues/2325))
- fixed RC2 SQL generation regression for `EXISTS` subqueries being wrapped in unnecessary `IIF`/`CASE` ([#2329](https://github.com/linq2db/linq2db/issues/2329))
- `ILoadWithQueryable.ToString()` now returns SQL for main query (no SQL for additional queries, that could be added by `LoadWith` calls) ([#2336](https://github.com/linq2db/linq2db/issues/2336))
- fixed exception when selecting related records using whole record projection ([#2307](https://github.com/linq2db/linq2db/issues/2307))
- fixed RC2 regression with OUTER APPLY queries  ([#2338](https://github.com/linq2db/linq2db/issues/2338))

---

## RC.2 Changes

##### New TFMs

We added new target frameworks (`netstandard2.1` and `netcoreapp3.1`) to support new functionality, added there.

For now following changes are implemented:
- we updated our local definition of `LinqToDB.Async.IAsyncEnumerable` to match `netstandard2.1` definition and limited it to `net45`/`net46` targets. For `netstandard2.0`/`netcoreapp2.1` targets we switched to use `Microsoft.Bcl.AsyncInterfaces` nuget
- added `IAsyncDisposable` interface definition in same way as it is done for `IAsyncEnumerable`
- updated result type of `BeginTransactionAsync` method in `IAsyncDbConnection` interface to return `ValueTask` for all targets except `net45`/`net46`
- added `IAsyncEnumerable<TSource> AsAsyncEnumerable<TSource>(this IQueryable<TSource> source)` extension method to convert query to `IAsyncEnumerable<T>`

Thanks to [@Shane32](https://github.com/Shane32) for implementing it.

##### Bugfixes and small improvements

- fixed SQL Server connection initialization in T4 templates ([#2286](https://github.com/linq2db/linq2db/issues/2286))
- opened some internal APIs for provider developers ([#2290](https://github.com/linq2db/linq2db/issues/2290))
- added support for arrays and collections in select expression ([#2289](https://github.com/linq2db/linq2db/issues/2289))
- [SQL Server] added `OUTPUT` clause support for `DELETE` operations (`DeleteWithOutput(...)`, `DeleteWithOutputInto(...)` + async versions) ([#2284](https://github.com/linq2db/linq2db/issues/2284)). Thanks to [@viceroypenguin](https://github.com/viceroypenguin) for implementing it
- [SQL Server] improve `TOP` queries generation for SQL Server 2017 and cleanup `TOP` implementation for other dialects. ([#2282](https://github.com/linq2db/linq2db/issues/2282)). Thanks to [@Shane32](https://github.com/Shane32) for implementing it
- [SQL Server] fixed some issues with SQL Server 2000 support (anybody still use it?): schema API, `ConcatStrings` implementation. ([#2293](https://github.com/linq2db/linq2db/issues/2293)). Thanks to [@Shane32](https://github.com/Shane32) for implementing it
- improved generation of boolean predicates. Always use `=` instead of `<>` to improve execution plans for indexed columns ([#2288](https://github.com/linq2db/linq2db/issues/2288))
- added `FromSqlScalar` API, which is similar to `FromSql`, but works only with scalar queries
- fixed known cases when linq2db could generate top-level SELECT * queries ([#2300](https://github.com/linq2db/linq2db/issues/2300))
- fixed query parsing exception (regression in rc1) ([#2296](https://github.com/linq2db/linq2db/issues/2296))
- fixed support for static projection methods in `LoadWith` queries ([#2309](https://github.com/linq2db/linq2db/issues/2309))
- improved nullable reference types annotations and fixed found incorrect annotations generation in T4 ([#2311](https://github.com/linq2db/linq2db/issues/2311))
- [T4][Schema] added new option `string GetSchemaOptions.DefaultSchema` to explicitly specify which schema should be treated as default instead of one, used by current database connection. Will help with issues like [#2133](https://github.com/linq2db/linq2db/issues/2133)
- [T4][Schema] improved reverse engineering of mysql types ([#2313](https://github.com/linq2db/linq2db/issues/2313))
- take into account `InlineParameters` and `Configuration.Linq.GuardGrouping` settings when cache queries ([#2306](https://github.com/linq2db/linq2db/issues/2306))

##### PostgreSQL

- improved support for array types by linq2db and schema API/T4 templates ([#2294](https://github.com/linq2db/linq2db/issues/2294)).
- default `TimeSpan` mapping changed from `time` to `interval` PostgreSQL type
- [T4][Schema] disabled read of metadata for default PostgreSQL objects from `information_schema` and `pg_catalog` schemas
- added several exension methods for postgresql functions (schema- and array-related) :

```cs
// Sql.ISqlExtension/IPostgreSQLExtensions is Sql.Ext.PostgreSQL extension point
//
// ARRAY_AGG
ArrayAggregate<T>(this Sql.ISqlExtension? ext, T expr);
ArrayAggregate<T>(this Sql.ISqlExtension? ext, T expr, Sql.AggregateModifier modifier);
ArrayAggregate<TEntity, TV>(
    this IEnumerable<TEntity> source,
    Func<TEntity, TV> expr,
    Sql.AggregateModifier modifier);
ArrayAggregate<TEntity, TV>(this IQueryable<TEntity> source, Expression<Func<TEntity, TV>> expr);
ArrayAggregate<TEntity, TV>(
    this IQueryable<TEntity> source,
    Expression<Func<TEntity, TV>> expr,
    Sql.AggregateModifier modifier);

// UNNEST
Unnest<T>(this IDataContext dc, T[] array);
UnnestWithOrdinality<T>(this IDataContext dc, T[] array);

// various array operators: || (concat), <, <=, >, @> (contained), && (overlap)
ConcatArrays<T>(this IPostgreSQLExtensions? ext, params T[][] arrays);
ConcatArrays<T>(this IPostgreSQLExtensions? ext, T[] array1, T[][] array2);
ConcatArrays<T>(this IPostgreSQLExtensions? ext, T[][] array1, T[] array2);
LessThan<T>(this IPostgreSQLExtensions? ext, T[] array1, T[] array2);
LessThanOrEqual<T>(this IPostgreSQLExtensions? ext, T[] array1, T[] array2);
GreaterThan<T>(this IPostgreSQLExtensions? ext, T[] array1, T[] array2);
GreaterThanOrEqual<T>(this IPostgreSQLExtensions? ext, T[] array1, T[] array2);
Contains<T>(this IPostgreSQLExtensions? ext, T[] array1, T[] array2);
ContainedBy<T>(this IPostgreSQLExtensions? ext, T[] array1, T[] array2);
Overlaps<T>(this IPostgreSQLExtensions? ext, T[] array1, T[] array2);

// ARRAY_CAT
ArrayCat<T>(this IPostgreSQLExtensions? ext, T[] array1, T[] array2);

// ARRAY_NDIMS
ArrayNDims<T>(this IPostgreSQLExtensions? ext, T[] array);

// ARRAY_DIMS
ArrayDims<T>(this IPostgreSQLExtensions? ext, T[] array);

// ARRAY_LENGTH
ArrayLength<T>(this IPostgreSQLExtensions? ext, T[] array, int dimension);

// ARRAY_LOWER
ArrayLower<T>(this IPostgreSQLExtensions? ext, T[] array, int dimension);

// ARRAY_POSITION
ArrayPosition<T>(this IPostgreSQLExtensions? ext, T[] array, T element);
ArrayPosition<T>(this IPostgreSQLExtensions? ext, T[] array, T element, int start);

// ARRAY_POSITIONS
ArrayPositions<T>(this IPostgreSQLExtensions? ext, T[] array, T element);

// ARRAY_PREPEND
ArrayPrepend<T>(this IPostgreSQLExtensions? ext, T element, T[] array);

// ARRAY_REMOVE
ArrayRemove<T>(this IPostgreSQLExtensions? ext, T[] array, T element);

// ARRAY_REPLACE
ArrayReplace<T>(this IPostgreSQLExtensions? ext, T[] array, T oldElement, T newElement);

// ARRAY_UPPER
ArrayUpper<T>(this IPostgreSQLExtensions? ext, T[] array, int dimension);

// CARDINALITY
Cardinality<T>(this IPostgreSQLExtensions? ext, T[] array);

// ARRAY_TO_STRING
ArrayToString<T>(this IPostgreSQLExtensions? ext, T[] array, string delimiter);
ArrayToString<T>(this IPostgreSQLExtensions? ext, T[] array, string delimiter, string nullString);

// STRING_TO_ARRAY
StringToArray(this IPostgreSQLExtensions? ext, string str, string delimiter);
StringToArray(this IPostgreSQLExtensions? ext, string str, string delimiter, string nullString);

// ARRAY_APPEND
ArrayAppend<T>(this IPostgreSQLExtensions? ext, T[] array, [T element);

// GENERATE_SERIES
GenerateSeries(this IDataContext dc, int start, int stop);
GenerateSeries(this IDataContext dc, int start, int stop, int step);
GenerateSeries(this IDataContext dc, DateTime start, DateTime stop, TimeSpan step);

// GENERATE_SUBSCRIPTS
GenerateSubscripts<T>(this IDataContext dc, T[] array, int dimension);
GenerateSubscripts<T>(this IDataContext dc, T[] array, int dimension, bool reverse);

// VERSION
Version(this IPostgreSQLExtensions? ext, IDataContext dc);

// CURRENT_CATALOG
CurrentCatalog(this IPostgreSQLExtensions? ext, IDataContext dc);

// CURRENT_DATABASE
CurrentDatabase(this IPostgreSQLExtensions? ext, IDataContext dc);

// CURRENT_ROLE
CurrentRole(this IPostgreSQLExtensions? ext, IDataContext dc);

// CURRENT_SCHEMA
CurrentSchema(this IPostgreSQLExtensions? ext, IDataContext dc);

// CURRENT_SCHEMAS
CurrentSchemas(this IPostgreSQLExtensions? ext, IDataContext dc);
CurrentSchemas(this IPostgreSQLExtensions? ext, IDataContext dc, bool includeImplicit);

// CURRENT_USER
CurrentUser(this IPostgreSQLExtensions? ext, IDataContext dc);

// SESSION_USER
SessionUser(this IPostgreSQLExtensions? ext, IDataContext dc);
```

---

## RC.1 Changes

Bugfix release to address issues, discovered in RC0 + couple of new features.

#### Breaking changes

##### MySql database objects escaping

With this release we fix escaping logic for database objects (e.g. table, schema, column, database, alias) and remove couple of quirks in escaping logic (could be a breaking change, but we don't expect it to be used by anyone as it wasn't documented):
- [fix] backticks in db object name are now correctly escaped
- [breaking][removal] previously we didn't escaped identifiers, if they were starting from backtick.  If you used this functionality to specify escaped identifiers, just remove escaping from your mappings
- [breaking][removal] previously if identifier (only database, schema or table) contained dots, we were splitting identifier into parts, escape them and join back. E.g. `schema.table` identifier were converted to ``` `schema`.`table` ```. If you used this type of mapping, just move table name components to corresponging mapping properties (`Database`, `Schema`, `Table`)

#### Bugfixes

- fixed `ArgumentException` error in eager load functionality ([#2249](https://github.com/linq2db/linq2db/issues/2249))
- fixed `Value does not fall within the expected range` error in eager load functionality ([#2251](https://github.com/linq2db/linq2db/issues/2251))
- fixed incorrect SQL generated for some queries with `Distinct()` call ([#2252](https://github.com/linq2db/linq2db/issues/2252))
- fixed parameter name collision in Window Functions that could cause incorrect parameter value used for function ([#2258](https://github.com/linq2db/linq2db/issues/2258))

#### Improvements

- detect and remove constant columns in ORDER BY clause to avoid SQL errors ([#2255](https://github.com/linq2db/linq2db/issues/2255))
- add `bool IsPure` property to `Sql.ExpressionAttribute`, `Sql.ExtensionAttribute`, `SqlExpression`, `SqlFunction` attributes and classes to mark [pure](https://en.wikipedia.org/wiki/Pure_function) functions and expressions. This information could be used by linq2db to generate better SQL ([#2255](https://github.com/linq2db/linq2db/issues/2255))
- to simplify v3 migration, we added new extension method `IQueryable<IGrouping<TKey, TElement>> DisableGuard<TKey, TElement>(this IQueryable<IGrouping<TKey, TElement>> grouping)` to allow unguarded `group by` for specific groupping statement when guarding enabled globally using `Configuration.Linq.GuardGrouping = true` ([#2257](https://github.com/linq2db/linq2db/issues/2257))
- to simplity v3 migration added new extension `LoadWithAsTable` wich is same as `LoadWith`, but returns `ITable<T>` (as v2 eager load API) instead of `IQueryable<T>` ([#2248](https://github.com/linq2db/linq2db/issues/2248))

#### MySqlConnector BulkCopy
This [feature](https://github.com/linq2db/linq2db/issues/2113) adds support for `BulkCopy` functionality, provided by `MySqlConnector` provider. It's enabled if provider version is v0.67.0 or greater.

To use it you should enable `LocalInFile` functionality both on [server](https://stackoverflow.com/questions/10762239) and in [client](https://mysqlconnector.net/api/mysqlconnector/mysqlbulkcopytype) and specify `BulkCopyType.ProviderSpecific` copy type when call `BulkCopy API`.

Also take into account that provider-specific bulk copy operation will fail if target table contains fields of `YEAR` type.

#### Value Converters [#2273](https://github.com/linq2db/linq2db/issues/2273)

This feature adds another way to specify conversion between column type, used in mapping and database type. Existed before configuration using `MappingSchema` allowed you to specify conversions for specific types using only type information using properties of [DbDataType](https://linq2db.github.io/api/LinqToDB.Common.DbDataType.html). New feature allows you to specify conversions for specific columns.

Value converter contains two conversion expressions (C#->Db, Db->C#) and `HandlesNulls` boolean flag to tell linq2db if it should process `NULL` values from database or pass them to converter for processing (e.g. if you want to map database `NULL` value to some not-null value).

To configure value converter for column you can use attributes or fluent mapping:
```cs
[Table]
class Table
{
    [Column]
    // using attribute you can provide type that implements value converter
    [ValueConverter(ConverterType = typeof(EnumValueConverter))]
    public EnumValue EnumColumn { get; set; }

    [Column]
    public JToken JsonValue1 {get; set; }

    [Column]
    public JToken JsonValue2 {get; set; }
}

// or using fluent mapping you can provide conversion methods or expressions for column
builder
    .Entity<Table>()
        .Property(e => e.JsonValue1)
            // specify conversion logic using expressions (prefferable)
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                p => JsonConvert.DeserializeObject<JToken>(p))
        .Property(e => e.JsonValue1)
            // specify conversion logic using delegates
            // least prefferable and should be used only if you have conversion helpers
            // you want to share between mappings or your conversion logic contains
            // language constructs, not allowed in C# expressions
            .HasConversionFunc(
                v => JsonConvert.SerializeObject(v),
                p => JsonConvert.DeserializeObject<JToken>(p));
```

Value converter type implementation for use with `ValueConverterAttribute`
```cs
// value converter type should implement LinqToDB.Common.IValueConverter interface
// public interface IValueConverter
// {
//     bool HandlesNulls                       { get; }
//     LambdaExpression FromProviderExpression { get; }
//     LambdaExpression ToProviderExpression   { get; }
// }
// you can implement it yourself or derive your coverter
// from LinqToDB.Common.ValueConverter<From, To> class

class EnumValueConverter: ValueConverter<EnumValue, string?>
{
    public EnumValueConverter()
        : base(
            v => v == EnumValue.Null ? null : v.ToString(),
            p => p == null
                ? EnumValue.Null
                : (EnumValue)Enum.Parse(typeof(EnumValue), p),
            true)
    {
    }
}

```

---

## RC.0 Changes

#### Important Changes

##### `Configuration.Linq.GuardGrouping` setting default value change
`Configuration.Linq.GuardGrouping` option default value changed from `false` to `true`. This option enables generation of exception in cases, when linq query uses `GroupBy` as LINQ API to get all data for all groups. Such queries produce `1 + N` queries: 1 query to get group keys and 1 query for each group key using cloned connection (meaning in separate transaction). This is usually unwanted behavior and could lead to bad performance and unexpected results.

For more details check [#365](https://github.com/linq2db/linq2db/issues/365).

#### ASP.NET Core support
To improve ASP.NET Core support and as initial step to move from static configuration we added initial support for `options` configuration pattern (for now it added only to `DataConnection` class) and released new package `linq2db.AspNet` to add configuration options support for ASP.NET Core projects.

For more details check [documentation](https://linq2db.github.io/articles/get-started/asp-dotnet-core/index.html).

#### Eager load
- fixed generation of unnecessary joins ([#2161](https://github.com/linq2db/linq2db/issues/2161))
- fixed `ArgumentException` when loading inherited entities ([#2196](https://github.com/linq2db/linq2db/issues/2196))

#### Associations
This release brings-in big refactoring of associations, which fixes a lot of known (and even more unknown) issues with associations:
- ([`Member is not table column` error accessing association from association](https://github.com/linq2db/linq2db/issues/860))
- ([`ArgumentException` exception accessing `IQueryable` from association expression](https://github.com/linq2db/linq2db/issues/1711))
- ([unknown columns referenced from select statement on complex queries with associations](https://github.com/linq2db/linq2db/issues/2199))

##### Insert/Update Column Filters

[#2185](https://github.com/linq2db/linq2db/issues/2185) adds new overloads to `IDataContext`'s `Insert*(...)` and `Update*(...)` extensions that accept additional parameter: column filter delegate to exclude column from update or insert operation.

##### Association parameters
Feature [#2238](https://github.com/linq2db/linq2db/issues/2238) adds support for extra parameters to associations, defined using methods.

#### Query filters
This feature allows you to attach custom filter expression to mapped table, that will be added to all queries, that query this table.

Filter could be configured using fluent mapping:
```cs
class MyDataConnection : DataConnnection
{
    public bool IsSoftDeleteFilterEnabled { get; set; } = true;

    // ...
}

[Table]
class Customer
{
    [Column]
    public bool IsDeleted { get; set; }

    // ...
}

// setup filter using fluent mapping
builder.Entity<Customer>().HasQueryFilter<MyDataContext>(
    (q, dc) => q.Where(c => !dc.IsSoftDeleteFilterEnabled || !c.IsDeleted));

// disable filter for specific query using IgnoreFilters helper
var results = ctx.Customers.IgnoreFilters().Where(c => c.Id > 1000).ToArray();
```

#### Advanced `GROUP BY` features
We've added support for `GROUPING SET`s, `ROLLUP`, `CUBE` statements in `GROUP BY` clause and `GROUPING` function. This feature supported by SQL Server, Oracle, DB2, PostgreSQL, SAP HANA and partially by MySql (`WITH ROLLUP` and `GROUPING`) and MariaDB (`WITH ROLLUP`).
To use this functionality, use following new extensions:
```cs
int Sql.Grouping(params object[] fields);
Sql.GroupBy.Rollup<T>(Expression<Func<T>> rollupKey);
Sql.GroupBy.Cube<T>(Expression<Func<T>> cubeKey);
Sql.GroupBy.GroupingSets<T>(Expression<Func<T>> setsExpression);
```
You can find examples in [our tests](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/Linq/GroupByExtensionsTests.cs).

#### T4
- [MS SQL] `GenerateSqlServerFreeText` option generates full-table search helper method using FTS API, introduced in v2.7
- not needed `AddNullablePragma` option removed

#### Provider-specific changes

##### Access
- added support for ODBC provider ([#333](https://github.com/linq2db/linq2db/issues/333))
- fixed issue where `1899/12/30` date was replaced with `1/1/1`
- [T4][Schema] improved types information and fixed SELECT-based procedures result schema load ([#2188](https://github.com/linq2db/linq2db/issues/2188))
- improved joins optimization to convert `INNER JOIN` inside of `LEFT JOIN` into query, supported by Access ([#1906](https://github.com/linq2db/linq2db/issues/1906))

##### Informix
- removed forced passing of binary parameters as parameters when parameters inlining enabled

##### Firebird
- enabled parameters in queries ([#1624](https://github.com/linq2db/linq2db/issues/1624))

##### MySql
- handle incorrect report of nullable column as non-nullable by data reader in `MySql.Data` provider

##### Oracle
- handle incorrect report of nullable column as non-nullable by data reader in native and managed Oracle providers
- fixed incorrect length returned for multi-byte character columns by schema provider ([#2224](https://github.com/linq2db/linq2db/issues/2224))

##### PostgreSQL
- added PostgreSQL support to `Sql.DateDiff` function ([#2225](https://github.com/linq2db/linq2db/issues/2225))

##### SAP HANA
- removed forced passing of `TimeSpan` parameters as parameters when parameters inlining enabled

##### SQL CE
- removed forced parameters inlining in selects

##### SQL Server
- fixed incorrect table name generation for `INSERTED` table by `InsertWithOutput` API when target table has schema or database name specified in mapping schema ([#2208](https://github.com/linq2db/linq2db/issues/2208))

###### Full-text API changes
Signatures for `FreeText`/`Contains` FTS API were simplified and removed unnecessary overloads:
- removed signatures without `params` parameter
- remained signatures lost `TEntity` type parameter and `TEntity entity` parameter (you should remove this parameter from call)
To migrate existing calls from removed signatures, move old `TEntity entity` parameter to the end of parameter list

#### Misc changes
- completed code annotation with nullable reference types and corrected found incorrect annotations
- added `Precision` and `Scale` properties to `DataParameter` type
- removed obsolete APIs, for full list and migration notes check notes [here](https://github.com/linq2db/linq2db/issues/2164)
- old Merge API (existed prior to API, introduced by v1.9.0 with limited functionality) were marked as obsolete. If you still used it, check [migration guide](https://linq2db.github.io/articles/sql/merge/Merge-API-Migration.html) or code of [old API methods](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/Data/DataConnectionExtensions.LegacyMerge.cs), as they are just translate call to modern API.
- added support for `Enum`-typed parameters mapping ([#2189](https://github.com/linq2db/linq2db/issues/2189))
- implemented various memory/performance improvements and improved caching of direct `FromSql` calls ([#2195](https://github.com/linq2db/linq2db/issues/2195))
- added support for properties, defined on `DataConnection`, to be used as query parameters ([#2226](https://github.com/linq2db/linq2db/issues/2226))
- return SQL from DML query `ToString()` calls ([#2207](https://github.com/linq2db/linq2db/issues/2207))
- fixed parameters handling in nested queries ([#2174](https://github.com/linq2db/linq2db/issues/2174))
- fixed duplicate CTEs generation in some cases ([#2201](https://github.com/linq2db/linq2db/issues/2201))
- fixed issue with `InsertOrReplace` API to not pass values for update-only columns for providers, that support single-command upserts ([#2243](https://github.com/linq2db/linq2db/issues/2243))

## Preview 2 Changes

#### Eager Load rewrite ([#1756](https://github.com/linq2db/linq2db/issues/1756))
Previous versions of linq2db had naive implementation of eager load and wasn't really recommended for use:
- related records were loaded per-parent record, which resulted in a lot of queries for big relations ([#214](https://github.com/linq2db/linq2db/issues/214))
- load of extra records used new connection with own tranaction, which could have resulted in incorrect results ([#482](https://github.com/linq2db/linq2db/issues/482))

New implementation address those issues and introduce additional APIs for more flexibility.
In general new API is similar to old one - using LoadWith method and selector expression user specify relations he want to load by query with following improvements:
- now it is possible to split load selector into chain of calls using `ThenLoad` method
- it is possible to add additional constrains (e.g. filters or even more `LoadWith` calls) to relation using `loadFunc` parameter.
```cs
public static ILoadWithQueryable<TEntity, TProperty> LoadWith<TEntity, TProperty>(
    this IQueryable<TEntity> source,
    Expression<Func<TEntity, TProperty>> selector);

public static ILoadWithQueryable<TEntity, TProperty> LoadWith<TEntity, TProperty>(
    this IQueryable<TEntity> source,
    Expression<Func<TEntity, IEnumerable<TProperty>>> selector,
    Func<IQueryable<TProperty>, IQueryable<TProperty>> loadFunc);

public static ILoadWithQueryable<TEntity, TProperty> ThenLoad<TEntity, TPreviousProperty, TProperty>(
    this ILoadWithQueryable<TEntity, TPreviousProperty> source,
    Expression<Func<TPreviousProperty, TProperty>> selector);

public static ILoadWithQueryable<TEntity, TProperty> ThenLoad<TEntity, TPreviousProperty, TProperty>(
    this ILoadWithQueryable<TEntity, TPreviousProperty> source,
    Expression<Func<TPreviousProperty, IEnumerable<TProperty>>> selector);

public static ILoadWithQueryable<TEntity, TProperty> ThenLoad<TEntity, TPreviousProperty, TProperty>(
    this ILoadWithQueryable<TEntity, IEnumerable<TPreviousProperty>> source,
    Expression<Func<TPreviousProperty, IEnumerable<TProperty>>> selector,
    Func<IQueryable<TProperty>, IQueryable<TProperty>> loadFunc);

public static ILoadWithQueryable<TEntity, TProperty> ThenLoad<TEntity, TPreviousProperty, TProperty>(
    this ILoadWithQueryable<TEntity, IEnumerable<TPreviousProperty>> source,
    Expression<Func<TPreviousProperty, TProperty>> selector);
```

##### Projections support
New implementation also used for loading of related records explicitly in projected queries:
```cs
// query below will produce only 3 queries to load
// - parents
// - children
// - grandchildren
db.Parents
    .Where(...)
    .Select(r => new
    {
        r.Id,
        r.Name,
        // loading of related records into collection (array in this case)
        ChildRecords = r.Children.Where(...)
            .Select(c => new
            {
                c.Id,
                c.Name,
                // example of nested relation load
                GrandChildren = c.Children.ToList()
            })
            .ToArray()
    })
```

#### SQL : `OUTPUT` clause support
[#1703](https://github.com/linq2db/linq2db/issues/1703) adds initial support for OUTPUT queries. Initial version supports only SQL Server `INSERT .. OUTPUT` and `INSERT .. OUTPUT INTO` queries, and later will be extended with more query types and databases.

This new query types available throught two new API: `InsertWithOutput/InsertWithOutputAsync` and `InsertWithOutputInto/InsertWithOutputIntoAsync` with various overloads.

`InsertWithOutput` executes `INSERT` query and returns output record
```cs
// inserts single record (using xpression) and returns it
TTarget InsertWithOutput<TTarget>(
    this ITable<TTarget>      target,
    Expression<Func<TTarget>> setter);

Task<TTarget> InsertWithOutputAsync<TTarget>(
    this ITable<TTarget>      target,
    Expression<Func<TTarget>> setter,
    CancellationToken         token);

// inserts single record (using object) and returns it
TTarget InsertWithOutput<TTarget>(
    this ITable<TTarget> target,
    TTarget              obj);

Task<TTarget> InsertWithOutputAsync<TTarget>(
    this ITable<TTarget> target,
    TTarget              obj,
    CancellationToken    token);

// inserts single record (using expression) and returns customized output record
TTarget InsertWithOutput<TTarget>(
    this ITable<TTarget>              target,
    Expression<Func<TTarget>>         setter,
    Expression<Func<TTarget,TOutput>> outputExpression);

Task<TTarget> InsertWithOutputAsync<TTarget>(
    this ITable<TTarget>              target,
    Expression<Func<TTarget>>         setter,
    Expression<Func<TTarget,TOutput>> outputExpression,
    CancellationToken                 token);

// inserts multiple records (from query with set expression) into specified table
// and returns inserted records
IEnumerable<TTarget> InsertWithOutput<TSource,TTarget>(
    this IQueryable<TSource>          source,
    ITable<TTarget>                   target,
    Expression<Func<TSource,TTarget>> setter);

Task<TTarget[]> InsertWithOutputAsync<TSource,TTarget>(
    this IQueryable<TSource>          source,
    ITable<TTarget>                   target,
    Expression<Func<TSource,TTarget>> setter,
    CancellationToken                 token);

// inserts multiple records (from query with set expression) into specified table
// and returns customized inserted records
IEnumerable<TOutput> InsertWithOutput<TSource,TTarget,TOutput>(
    this IQueryable<TSource>          source,
    ITable<TTarget>                   target,
    Expression<Func<TSource,TTarget>> setter,
    Expression<Func<TTarget,TOutput>> outputExpression);

Task<TOutput[]> InsertWithOutputAsync<TSource,TTarget,TOutput>(
    this IQueryable<TSource>          source,
    ITable<TTarget>                   target,
    Expression<Func<TSource,TTarget>> setter,
    Expression<Func<TTarget,TOutput>> outputExpression,
    CancellationToken                 token);

// inserts record into specified table and returns inserted record
TTarget InsertWithOutput<TSource,TTarget>(
    this ISelectInsertable<TSource,TTarget> source);

Task<int> InsertWithOutputIntoAsync<TSource,TTarget>(
    this ISelectInsertable<TSource,TTarget> source,
    ITable<TTarget>                         outputTable,
    CancellationToken                       token);
```

`InsertWithOutputInto` executes `INSERT` query and inserts output into specified table
```cs
// inserts record (using expression) into table
// and inserts inserted record into another table
int InsertWithOutputInto<TTarget>(
    this ITable<TTarget>      target,
    Expression<Func<TTarget>> setter,
    ITable<TTarget>           outputTable);

Task<int> InsertWithOutputIntoAsync<TTarget>(
    this ITable<TTarget>      target,
    Expression<Func<TTarget>> setter,
    ITable<TTarget>           outputTable,
    CancellationToken         token);

// inserts record (using expression) into table
// and inserts customized output record into another table
int InsertWithOutputInto<TTarget>(
    this ITable<TTarget>              target,
    Expression<Func<TTarget>>         setter,
    ITable<TOutput>                   outputTable,
    Expression<Func<TTarget,TOutput>> outputExpression);

Task<int> InsertWithOutputIntoAsync<TTarget>(
    this ITable<TTarget>              target,
    Expression<Func<TTarget>>         setter,
    ITable<TOutput>                   outputTable,
    Expression<Func<TTarget,TOutput>> outputExpression,
    CancellationToken                 token);

// inserts multiple records (from query with set expression) into specified table
// and outputs inserted records into another table
int InsertWithOutputInto<TSource,TTarget>(
    this IQueryable<TSource>          source,
    ITable<TTarget>                   target,
    Expression<Func<TSource,TTarget>> setter,
    ITable<TTarget>                   outputTable);

Task<int> InsertWithOutputIntoAsync<TSource,TTarget>(
    this IQueryable<TSource>          source,
    ITable<TTarget>                   target,
    Expression<Func<TSource,TTarget>> setter,
    ITable<TTarget>                   outputTable,
    CancellationToken                 token);

// inserts multiple records (from query with set expression) into specified table
// and outputs customized inserted into another table
int InsertWithOutputInto<TSource,TTarget,TOutput>(
    this IQueryable<TSource>          source,
    ITable<TTarget>                   target,
    Expression<Func<TSource,TTarget>> setter,
    ITable<TOutput>                   outputTable,
    Expression<Func<TTarget,TOutput>> outputExpression);

Task<int> InsertWithOutputIntoAsync<TSource,TTarget,TOutput>(
    this IQueryable<TSource>          source,
    ITable<TTarget>                   target,
    Expression<Func<TSource,TTarget>> setter,
    ITable<TOutput>                   outputTable,
    Expression<Func<TTarget,TOutput>> outputExpression,
    CancellationToken                 token);

// inserts record into table and inserts output record into another table
int InsertWithOutputInto<TSource,TTarget>(
    this ISelectInsertable<TSource,TTarget> source,
    ITable<TTarget>                         outputTable);

Task<int> InsertWithOutputIntoAsync<TSource,TTarget>(
    this ISelectInsertable<TSource,TTarget> source,
    ITable<TTarget>                         outputTable,
    CancellationToken                       token);
```

#### SQL : Use parameters for pagination clauses (TOP/TAKE/FETCH/etc)

This [feature](https://github.com/linq2db/linq2db/issues/2118) enables use of parameters in pagination clauses instead of literals, which could improve query plans caching for some databases.

#### SQL : Custom SET expressions

This feature adds API to define custom SET clause for UPDATE statements instead of standard `column = expr`:
```cs
// update using custom set operators, supported by SQL Server
db.Table
    .Where(r => r.Id == 1)
    .Set(r => $"{r.Field} += {value}")
    .Set(r => $"{r.LOBField}.WRITE({data}, {offset}, {size})")
    .Update();
```
This is especially useful for Sql Server, as it allows to use `.WRITE` operator to effectively modify LOB fields.

#### SQL : `FromSql` improvements

###### Scalar queries

It is possible now to create scalar queries using `FromSql<SCALAR_TYPE>()` without wrapper class to contain scalar value:
```cs
// before
class IntValue
{
    public int x { get; set; }
}
var result = db.FromSql<IntValue>($"select 1 as x").ToArray();
// after
var result = db.FromSql<int>($"select 1 as x").ToArray();
```

###### Alias placeholder helper

New helper `Sql.AliasExpr()` allows to explicitly specify where query alias should be placed. Could be useful for cases, when alias should be generated not at the end of query. In example below, query alias located in the middle of query:
```cs
// "unnest (...) with ordinality <alias>(...)" postgresql clause
db.FromSql<UnnestEnvelope<TValue>>($"unnest({member}) with ordinality {Sql.AliasExpr()} (value, index)");
```

#### T4 / Schema
- improved support for generic types. Before it was possible to get ``TypeName`X`` instead of proper C# type name in some cases
- added new option `GetSchemaOptions.PreferProviderSpecificTypes = false;` to specify what types should be used in schema when dual mappings supported. Currently used only for `npgsql` to select between some `Npgsql*Type` types and non-npgsql types.
- [PostgreSQL] PostgreSQL T4 template will now use more `npgsql` types if `GetSchemaOptions.PreferProviderSpecificTypes = true` option specified

#### Changes to Database Providers

##### Providers support changes

Preview 2 was tested against recent versions of all providers and following changes/fixes were implemented:
- [Microsoft.Data.Sqlite] `Microsoft.Data.Sqlite` 3.0.0: implemented workaround for this [issue](https://github.com/aspnet/EntityFrameworkCore/issues/17521)
- [Microsoft.Data.Sqlite] `Microsoft.Data.Sqlite` 3.0.0: implemented workaround for [breaking change](https://github.com/aspnet/EntityFrameworkCore/issues/15078) to avoid compatibility issues. linq2db will continue use binary type for Guid by default.
- [Microsoft.Data.Sqlite] `Microsoft.Data.Sqlite` 3.0.0: we would recommend to explicitly reference SQLitePCLRaw.bundle_e_sqlite3 v2.0.1 as it contains fix for this [issue](https://github.com/aspnet/EntityFrameworkCore/issues/17494)
- [MySqlConnector] added workaround for [issue](https://github.com/mysql-net/MySqlConnector/issues/722) in `MySqlConnector` 0.57 to 0.60 where incorrect stored procedure schema could be returned by schema API. Issue could affect only linq2db users that use linq2db schema API directly
- [Npgsql] dropped support for npgsql 2.x by removing handling of old `npgsql` types, existed prior to npgsql3
- [Npgsql] improved support for `interval` type mapping to `TimeSpan` by adding new `DataType.Interval` enumeration field. Fixes [#1429](https://github.com/linq2db/linq2db/issues/1429)
- [Access, SAP HANA] removed explicit dependencies on `System.Data.OleDb` and `System.Data.ODBC`, introduced in preview1. To use Access and ODBC SAP Hana providers with .net core, users must manually add required dependency

###### DB2
- enabled `time` literals and implemented literals generation for `date`, `time` and `timestamp` (wich precision) types ([#1663](https://github.com/linq2db/linq2db/issues/1663))

###### Informix

Preview1 introduced basic support for IBM.Data.DB2(.Core) IDS provider for Informix. Preview2 greatly improved it by:
- adding support for IDS version of IMB.Data.Informix provider
- adding support for native bulk copy for both DB2 and Informix IDS providers

###### Oracle

- [T4, Schema] add support for `IDENTITY` columns in schema API ([#2034](https://github.com/linq2db/linq2db/issues/2034))

###### Date/Time types refactoring
As part of work on issue [#431](https://github.com/linq2db/linq2db/issues/431) ([pr](https://github.com/linq2db/linq2db/issues/2058)), we improved support for date/time-related oracle types.
With this change following mappings used:
- `DateTime` with `DateTime.Date` type is mapped to `date` type without time component
- `DateTime` with `DateTime.DateTime` (default) type is mapped to `date` type with time component
- `DateTime` with `DateTime.DateTime2` type is mapped to `timespan` type. Precision specification supported and default precision matches Oracle defaults (6)
- `DateTimeOffset` with `DateTime.DateTimeOffset` (default) type is mapped to `timespan with time zone` type. Precision specification supported and default precision matches Oracle defaults (6)

All 4 types generare properly types literals, when parameters inlining enabled and trim parameter value precision to configured precision.

###### Oracle 12 dialect support

Preview 2 introduce initial support for Oracle 12 dialect features:
- default dialect set to 12, if you still need to use v11 dialect, specify it in your connection settings
- `FETCH NEXT n ROWS` / `OFFSET n ROWS` support added
- `APPLY JOIN` support added

###### SAP/Sybase ASE
- added support for provider-specific bulk copy for native provider

###### SQL CE
- string literals doesn't use N prefix anymore

##### Async support
Improved ([PR](https://github.com/linq2db/linq2db/issues/1942)) handling of connection and transaction async API by recognizing more signatures of async methods and supporting more methods. New methods are:
- `DisposeAsync` for transactions and connections
- `OpenAsync` for connections

Note that there is still lack of real async support by providers, so you will be able to benefit from it only with recent `MySqlConnector` and `npgsql` providers.

##### MiniProfiler and other custom ADO.NET wrappers support

Previous versions of `linq2db` exposed `Configuration.AvoidSpecificDataProviderAPI` option to disable use of provider-specific API to made linq2db work with custom ADO.NET wrappers (usually [MiniProfiler](https://miniprofiler.com/)). That approach had two most important downfalls:
- configuration option wasn't consistently supported over linq2db codebase, which resulted in errors and regressions due to attempted calls of provider-specific APIs from wrappers
- disabling provider-specific API resulted in functionality degradation or even impossibility to use some functionality

With this release we remove that option completely as now linq2db:
- could automatically detect if it works with custom wrapper and will not invoke unavailable APIs
- of wrapper provides access to underlying wrapped instance, it is possible to tell linq2db how to access it

E.g. for `MiniProvider` you should register following unwrap conversions in mapping schema to allow linq2db to use provider-specific API even with `MiniProfiler` enabled:
```cs
ms.SetConvertExpression<ProfiledDbConnection,  IDbConnection> (db => db.WrappedConnection);
ms.SetConvertExpression<ProfiledDbDataReader,  IDataReader>   (db => db.WrappedReader);
ms.SetConvertExpression<ProfiledDbTransaction, IDbTransaction>(db => db.WrappedTransaction);
ms.SetConvertExpression<ProfiledDbCommand,     IDbCommand>    (db => db.InternalCommand);
// also if needed, you can register unwrap conversion for IDbDataParameter
// (MiniProfiler doesn't wrap parameters)
```

Also see refactoring section below.

##### Providers refactoring

PR: [#1961](https://github.com/linq2db/linq2db/pull/1961)

With this PR we performed big overhaul of our database providers implementation, which will be a breaking change for anyone imlementing custom providers or subclassing existing providers.

Major changes:
- logic of integration with underlying ADO.NET provider was moved from `*DatabaseProvider` to separate `*ProviderAdapter` class wich provides typed access to ADO.NET provider functionality
- linq2db doesn't pre-create all supported provider instances anymore which has two consequences:
  - there should be no more issues when linq2db fails due to exception during [unrequested provider initialization](https://github.com/linq2db/linq2db/issues/1715)
  - `DataConnection.GetRegisteredProviders()` API will return only actually used providers

#### Other changes
- added more nullable reference types annotations. Completely annotated data providers
- removed silverlight support from some T4 templates


---

## Preview 1 Changes

---
#### General Changes
- Nightly builds [MyGet feeds](https://www.myget.org/gallery/linq2db) replaced with [AzureDevOps feeds](https://dev.azure.com/linq2db/linq2db/_packaging?_a=feed&feed=linq2db)
- Project codebase migrated to C# 8 with initial nullable reference types support, which will be improved in next releases
- Testing and deployment pipeline migrated from Travis+Appveyor to [Azure DevOps](https://dev.azure.com/linq2db/linq2db) with huge improvement in test coverage across databases, providers and environments, which makes testing process much more easier as you don't need to setup local test environments in most of cases
- added several new providers support for existing databases and improved .net core support for existing providers. See corresponding database section for more details
- removed direct dependency on `System.Data.SqlClient` nuget, which was causing issues in some scenarios where users don't have it installed: [1487](https://github.com/linq2db/linq2db/issues/1487), [1704](https://github.com/linq2db/linq2db/issues/1704), [1715](https://github.com/linq2db/linq2db/issues/1715)

---

#### Breaking Changes
- Linq To DB doesn't support `netstandard1.6`, `netcoreapp1.0` and `netcoreapp2.0` targets anymore. New target `netcoreapp2.1` added instead of removed `netcoreapp2.0`
- Added some missing use-after-dispose guards to `DataConnection` and `DataContext` classes, which could result in `ObjectDisposedException`'s in some scenarios that worked before [1877](https://github.com/linq2db/linq2db/issues/1877)

Also check linked server breaking changes section below. 

---
#### Linked Server Support

[Issue](https://github.com/linq2db/linq2db/issues/1125), [PR](https://github.com/linq2db/linq2db/pull/1357)

This feature adds support for server name component in fully-qualified name of database object (e.g. table). In all APIs, that accepted name, schema/owner name or database name, we added additional member (parameter, property or method) to accept server name. Full list of changes could be found in first message in this [PR](https://github.com/linq2db/linq2db/pull/1357).

Feature supported for following databases:
- Microsoft SQL Server
- IBM Informix
- Oracle Database
- SAP HANA2

##### Breaking changes
While we tried to avoid breaking changes with this feature, there are two cases, where it could require you to change your code:
- for asynchronous API methods server name parameter was added before `CanncelationToken` parameter, which could lead to compilation error against 3.0 version if you explicitly passed token to those methods using positional parameter. To fix it, you will need to add new `null` parameter before token or convert positional token parameter to named parameter.

```cs
// old code (broken)
return await db.InsertAsync(record, /*tableName*/ null, /*databaseName*/ null,
                                    /*schemaName*/ null, token);

// fix variant 1
return await db.InsertAsync(record, /*tableName*/ null, /*databaseName*/ null,
                                    /*schemaName*/ null, null, token);

// fix variant 2
return await db.InsertAsync(record, token: token);
```
- second breaking change affects only provider implementors. Two methods of `ISqlBuilder` interface (`ConvertTableName` and `BuildTableName`) now have one more parameter (server name), placed right after first parameter.

##### T4 Support
New T4 option `string ServerName` added to generate server name property in mappings. To enable generation, assign desired server name to this property.

---

#### SQL: Better `UPDATE FROM` queries support

[PR](https://github.com/linq2db/linq2db/pull/1278)

This change greatly improves support for `UPDATE FROM` queries across providers including emulation for databases, that doesn't support `UPDATE FROM` or similar statements. Check this [message](https://github.com/linq2db/linq2db/pull/1278) for sample SQL for different databases.

---

#### SQL: MERGE Statement

[PR](https://github.com/linq2db/linq2db/pull/1512)

Pre-3.0 Merge API was introduced in linq2db 1.9.0 as a replacement for older API to provide full access to MERGE statement generation for all databases (which have support for MERGE statements) instead of older API, which had limited MERGE statement support with focus on SQL Server syntax. While it succeeded, it had major design issue - it was implemented as separate non-linq API with hidden dependencies on linq2db internals. As result it was impossible to use it with remote contexts and it started to interfere with improvements to main linq2db codebase.

This release resolves all those issues by integrating Merge API into existing linq-based infrastructure while preserving public API (so you don't need to change your code that calls Merge API methods). This allowed us:
- generate better SQL for MERGE in some cases (especially for MERGE SOURCE clauses)
- use MERGE over remote contexts

Also we replacing pre-1.9.0 legacy Merge API implementation to just forward calls to new API. This will give you better MERGE SQL generation for free and remove duplicate implementation of same functionality. Note that we don't plan to extend this API in any way, so if it doesn't provide some functionality - you should use main Merge API. Also don't forget that we have [article](https://linq2db.github.io/articles/sql/merge/Merge-API-Migration.html) with all information you need to convert your legacy Merge API calls to new API.

##### Known issues/regressions
Before version 3, call to merge API with empty client-side (enumerable) source for Oracle resulted in no-op operation (no query were sent to server) with 0 records affected result returned. Version 3 will throw exception, telling that empty enumerable source is not supported for Oracle provider. Tell us if it creates issues for you, so we can try to find solution or restore old behavior.

---

#### Remote Context
- now you can use Merge API over remote context ([#1512](https://github.com/linq2db/linq2db/pull/1512))
- fixed old standing issue with missing support for provider-specific data read logic ([#730](https://github.com/linq2db/linq2db/pull/730))
- fixed issue where concurrent calls to same queries over remote context could result in parameters corruption

##### Serialization
Serialization of values, sent over remote context, was improved to use conversions to/from string, defined in mapping schema. By default, remote context already supports propert roundtrip serialization for following types (including nullable variants for value types):
- `bool`
- `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `decimal`, `float`, `double`
- `char`
- `DateTime`, `DateTimeOffset`, `TimeSpan`
- `Guid`
- `Binary` and `byte[]` binary types

for other types, it will ask mapping schema to provide conversion between value and string. Don't forget that you need to configure mapping schema on both client and server sides. For this we added additional constructor to `LinqService` that accepts `MappingSchema` object (or you can set it using `LinqService.MappingSchema` property) to specify mapping schema for server-side.

---

#### T4 Templates
- added placeholder comments to sample `.tt.txt` files to make it more clear where in template user should put custom schema and model configuration options ([#1919](https://github.com/linq2db/linq2db/issues/1919))

---

#### Various changes to API surface

- fixed typo in `MappingSchema` and `MappingSchemaInfo` classes method `GetEntites` name -> `GetEntities` ([#1275](https://github.com/linq2db/linq2db/issues/1275))
- removed `SqlServerTools.SetIdentityInsert` method (it wasn't working anyway, so hardly anyone will miss it)
- `MappingSchema.GetUnderlyingDataType` second parameter changed from `ref` to `out` to better reflect behavior of this method

---

#### Provider-specific changes

##### MS Access
- switched command parameters generation to use positional parameters ([#1687](https://github.com/linq2db/linq2db/issues/1687))
- provider support enabled for `netcoreapp2.0` and `netstandard2.0` targets ([#1914](https://github.com/linq2db/linq2db/issues/1914)). Windows-only, as it requires JET or ACE OLE DB provider installed.
- added `UPDATE FROM` queries emulation [1278](https://github.com/linq2db/linq2db/pull/1278)

##### DB2
- re-enable parameters use in generated SQL
- added new T4 package `linq2db.DB2.Core` to use `IBM.Data.DB2.Core` with DB2 database ([#1872](https://github.com/linq2db/linq2db/issues/1872))
- added `UPDATE FROM` queries emulation [1278](https://github.com/linq2db/linq2db/pull/1278)

##### Firebird
- added `UPDATE FROM` queries emulation [1278](https://github.com/linq2db/linq2db/pull/1278)

##### Informix
- fixed issue, when generated command could contain wrong number of parameters or in wrong order ([#1685](https://github.com/linq2db/linq2db/issues/1685))
- added support for `IBM.Data.DB2.Core` provider ([#1872](https://github.com/linq2db/linq2db/issues/1872))
- added new T4 package `linq2db.Informix.Core` to use `IBM.Data.DB2.Core` with Informix database ([#1872](https://github.com/linq2db/linq2db/issues/1872))
- added `UPDATE FROM` queries emulation [1278](https://github.com/linq2db/linq2db/pull/1278)

##### MySQL/MariaDB
- added `UPDATE FROM` queries emulation [1278](https://github.com/linq2db/linq2db/pull/1278)

##### Oracle
- re-enable parameters use in generated SQL
- added `UPDATE FROM` queries emulation [1278](https://github.com/linq2db/linq2db/pull/1278)

##### PostgreSQL
- added `UPDATE FROM` queries support [1278](https://github.com/linq2db/linq2db/pull/1278)

##### SAP HANA
- fixed issue, when generated command could contain wrong number of parameters or in wrong order ([#1685](https://github.com/linq2db/linq2db/issues/1685))
- added support for .NET Core native provider for `netcoreapp2.1` targets (released by SAP as part of HANA SPS04 release) ([#1917](https://github.com/linq2db/linq2db/issues/1917)). Windows-only.
- updated existing ODBC provider to work for all targets (including .net CORE) ([#1917](https://github.com/linq2db/linq2db/issues/1917)). To enable this provider, use `SapHana.Odbc` provider name in your connection configuration.
- added `UPDATE FROM` queries emulation [1278](https://github.com/linq2db/linq2db/pull/1278)

##### SAP/Sybase
- added `UPDATE FROM` queries support [1278](https://github.com/linq2db/linq2db/pull/1278)

##### SQLite
- added `UPDATE FROM` queries emulation [1278](https://github.com/linq2db/linq2db/pull/1278)

##### SQL CE
- provider support updated for `netcoreapp2.0` and `netstandard2.0` to support `DbProviderFactories` class ([#1914](https://github.com/linq2db/linq2db/issues/1914)). Windows-only.
- added `UPDATE FROM` queries emulation [1278](https://github.com/linq2db/linq2db/pull/1278)

##### SQL Server
- improved `UPDATE FROM` queries support [1278](https://github.com/linq2db/linq2db/pull/1278)

###### `Microsoft.Data.SqlClient` support
We have added [support](https://github.com/linq2db/linq2db/pull/1278) for new SQL Server provider [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient). To use this provider, specify `"Microsoft.Data.SqlClient"` as provider name in your connection settings.

For now this provider doesn't add anything new in terms of linq2db support, but we expect it to support new async APIs from `netstandard2.1` in future releases.

There is one functional regression you should be aware before migrating to this provider from `System.Data.SqlClient`. If you use UDT types like `Microsoft.SqlServer.Types` or `dotMorten.Microsoft.SqlServer.Types` (or maybe some other UDT type), they will not work with new provider. Check this [message](https://github.com/linq2db/linq2db/pull/1929#issuecomment-541396922) for more details. Probably it could be workarounded by custom build of `dotMorten.Microsoft.SqlServer.Types` against new provider, but this scenario wasn't tested by us.