- [Release 2.9.8](#release-298)
- [Release 2.9.7](#release-297)
- [Release 2.9.6](#release-296)
- [Release 2.9.5](#release-295)
- [Release 2.9.4](#release-294)
- [Release 2.9.3](#release-293)
- [Release 2.9.2](#release-292)
- [Release 2.9.1](#release-291)
- [Release 2.9.0](#release-290)
- [Release 2.8.1](#release-281)
- [Release 2.8.0](#release-280)
- [Release 2.7.4](#release-274)
- [Release 2.7.3](#release-273)
- [Release 2.7.2](#release-272)
- [Release 2.7.1](#release-271)
- [Release 2.7.0](#release-270)
- [Release 2.6.4](#release-264)
- [Release 2.6.3](#release-263)
- [Release 2.6.2](#release-262)
- [Release 2.6.1](#release-261)
- [Release 2.6.0](#release-260)
- [Release 2.5.4](#release-254)
- [Release 2.5.3](#release-253)
- [Release 2.5.2](#release-252)
- [Release 2.5.1](#release-251)
- [Release 2.5.0](#release-250)
- [Release 2.4.0](#release-240)
- [Release 2.3.0](#release-230)
- [Release 2.2.0](#release-220)
- [Release 2.1.0](#release-210)
- [Release 2.0.0](Release-Notes-2.0.0)
- [Release 1.10.1](#release-1101)
- [Release 1.10.0](#release-1100)
- [Release 1.9.0](#release-190)
- [Release 1.8.3](#release-183)
- [Release 1.8.2](#release-182)
- [Release 1.8.1](#release-181)
- [Release 1.8.0](#release-180)
- [Release 1.7.6](#release-176)
- [Release 1.0.7.5](#release-1075)
- [Release 1.0.7.4](#release-1074)
- [Release 1.0.7.3](#release-1073)

***

### Release 2.9.8

- react to ADO.NET API [change](https://github.com/dotnet/runtime/issues/509) affecting procedure schema read for MySqlConnector 0.62.0+
- [T4, Oracle] fixed `NullReferenceException` when loading schema with procedures ([#2132](https://github.com/linq2db/linq2db/issues/2132))
- fixed another case, where C# compiler were inserting unwanted convert expressions ([#2166](https://github.com/linq2db/linq2db/issues/2166))
- improved `DataConnection` cloning (used by linq2db internally for some operations) by cloning following members: `OnEntityCreated`, `RetryPolicy`, `OnTraceConnection`, `CommandTimeout`, `InlineParameters`, `QueryHints`, `ThrowOnDisposed`, `OnClosing`, `OnClosed`, `OnBeforeConnectionOpen`, `OnBeforeConnectionOpenAsync`, `OnConnectionOpened`, `OnConnectionOpenedAsync` ([#2169](https://github.com/linq2db/linq2db/issues/2169))
- add `protected virtual` methods `CreateDataConnection` and `CloneDataConnection` to `DataContext` to implement custom `DataConnection` creation logic ([#2146](https://github.com/linq2db/linq2db/issues/2146))
- add `AsSubQuery()` extension to apply to `GroupBy()` results to filter-out non-key properties, not available in SQL ([#2105](https://github.com/linq2db/linq2db/issues/2105))

***

### Release 2.9.7
- fix `NullReferenceException` regression in CTE queries ([#2076](https://github.com/linq2db/linq2db/issues/2076))
- disable caching of sql query text for `expression.ToString` calls ([#2073](https://github.com/linq2db/linq2db/issues/2073))
- fix potential `KeyNotFoundException` in complex CTEs ([#2072](https://github.com/linq2db/linq2db/issues/2072))
- fix issues with `Microsoft.AspNet.OData` support ([#2070](https://github.com/linq2db/linq2db/issues/2070), [#2092](https://github.com/linq2db/linq2db/issues/2092))
- fixed `ArgumentException` exception when query performed against interface ([#2048](https://github.com/linq2db/linq2db/issues/2048))
- fixed issue with `DataContextTransaction.BeginTransactionAsync` not passing cancellation token to underlying calls  ([#2096](https://github.com/linq2db/linq2db/issues/2096))
- improved handling of non-mapped members ([#2097](https://github.com/linq2db/linq2db/issues/2097))
- [MSSQL] fixed case-sensitivity in column mapping for stored procedures ([#2088](https://github.com/linq2db/linq2db/issues/2088))
- added missing overloads to `QueryProc*` / `ExecuteProc*` API with object as parameters source ([#2089](https://github.com/linq2db/linq2db/issues/2089), [#2090](https://github.com/linq2db/linq2db/issues/2090))
- improved support for custom implicit/explicit conversion operators with nullable parameters ([#1998](https://github.com/linq2db/linq2db/issues/1998))
- fixed several issues, caused by unwanted expression optimizations by C# compiler ([#2039](https://github.com/linq2db/linq2db/issues/2039), [#2041](https://github.com/linq2db/linq2db/issues/2041))
- [Oracle] fixed handing of `bool` mapping to `smallint` column with recent native provider versions in `BulkCopy`

***

### Release 2.9.6
- fix `NullReferenceException` for queries with sub-query ([#2031](https://github.com/linq2db/linq2db/issues/2031))
- [Oracle] fix `NotSupportedException` when reading decimal values for Oracle ([#2032](https://github.com/linq2db/linq2db/issues/2032))
- fix exception thrown for mapping classes that expose members of reference type ([#2052](https://github.com/linq2db/linq2db/issues/2052))
- add `DataContext.CommandTimeout` property to support configurable command timeout for `DataContext` queries ([#2047](https://github.com/linq2db/linq2db/issues/2047))
- [PostgreSQL] add support for materialized views to database schema reader (now materialized views are visible to `T4` templates) ([#2023](https://github.com/linq2db/linq2db/issues/2023))
- [MySQL] fix `DateTimeOffset` values read for `TIMESPAN` values using `MySqlConnector` provider with proper offset. Note that `MySql.Data` provider is still broken here and will be fixed if anybody will request it ([#2044](https://github.com/linq2db/linq2db/issues/2044))
- fix multiple cases of invalid SQL generation in CTE ([#2061](https://github.com/linq2db/linq2db/issues/2061), [#2033](https://github.com/linq2db/linq2db/issues/2033))
- get rid of usings of `ConcurrentBag<T>`, as it doesn't play well with debugger  ([#1795](https://github.com/linq2db/linq2db/issues/1795), [#2064](https://github.com/linq2db/linq2db/issues/2064))
- [T4] disable generation of `InitDataContext`/`InitMappingSchema` partial methods if `GenerateConstructors=false` ([#2049](https://github.com/linq2db/linq2db/issues/2049))

##### MySQL CreateTable API improvements
**IMPORTANT**: this brings some breaking changes to `CreateTable` API for MySQL provider, so review list of changes if you use this API.

Reviewed and fixed most of MySQL types support for CreateTable API. Related issues: [#1206](https://github.com/linq2db/linq2db/issues/1206), [#2020](https://github.com/linq2db/linq2db/issues/2020).

List of changes/fixes (most of changes specififed in terms of `DataType` enum, but you can read it as corresponfing .net type for most of cases):
- [BREAKING] columns of `DataType.VarChar`/`DataType.NVarChar` types use `VARCHAR` type. Previously used `CHAR` type
- columns of `Guid` type use `CHAR(36)` type. Previously generated incorrect type name `Guid`
- [BREAKING] columns of `DataType.Binary`/`VarBinary` types default to `BINARY(255)`\`VARBINARY(255)` if length is not configured. Previously generated typename without length, which is not valid for `VARBINARY` and for `BINARY` creates BINARY(1) column
- columns of `DataType.Blob`/`Text`/`NText` types generate corresponding sub-types based on length (if specified): `TINY-/MEDIUM-/LONG- BLOB/TEXT`.
- [BREAKING] columns of `DataType.DateTime`/`DateTime2`/`SmallDateTime` types generate fractional seconds if precision specified. Previously created 0-precision columns
- columns of `DataType.DateTimeOffset` type generate `TIMESPAN` type. Fractional seconds generated if precision specified. Previously generated incorrect type name `DateTimeOffset`
- columns of `DataType.Time` type generate fractional seconds if precision specified. Previously generated incorrect type name`TIME(x, )`
- [BREAKING] columns of `DataType.Byte` type generate `TINYINT UNSIGNED` type. Previously: `TINYINT`
- [BREAKING] columns of `DataType.Uint16` type generate `SMALLINT UNSIGNED` type. Previously: `INT`
- [BREAKING] columns of `DataType.Uint32` type generate `INT UNSIGNED` type. Previously: `BIGINT`
- [BREAKING] columns of `DataType.Uint64` type generate `BIGINT UNSIGNED` type. Previously: `DECIMAL`
- columns of `DataType.Decimal` type generate proper precision and scale values. Previously generated incorrect type name if only precision or scale specified
- [BREAKING] columns of `DataType.Single`/`Double` types generate proper `FLOAT`/`DOUBLE` type names, including `FLOAT(p)`. Previously generated `DECIMAL(29,10)`
- columns of `Data.Type.BitArray` type generate `BIT/BIT(N)` type. For integer-typed columns use integer type size in bits if length not specified explicitly. E.g. for `byte` it will create `BIT(8)` field. Previously generated incorrect type name `BitArray`/`BitArray(N)`

***

### Release 2.9.5

- fix `StackOverflowException` on query AST processing ([#2008](https://github.com/linq2db/linq2db/issues/2008))
- fix regression with interface mappings ([#2000](https://github.com/linq2db/linq2db/issues/2000))
- fix `IndexOutOfRangeException` in queries with `ExpressionMethod` extensions ([#1983](https://github.com/linq2db/linq2db/issues/1983))
- fix `Sql.ConcatStrings` implementation for SQL Server 2016 and older ([#1977](https://github.com/linq2db/linq2db/issues/1977))
- fixed long-standing issue with escaping applied to `Sql.Like` template ([#776](https://github.com/linq2db/linq2db/issues/776), [#1925](https://github.com/linq2db/linq2db/issues/1925))
- fix `DateTime.Time` component typing in MS SQL 2008+ to use `time` type instead of `datetime` ([#1982](https://github.com/linq2db/linq2db/issues/1982))
- fix version of `System.Threading.Tasks.Extensions` dependency, used internally by `linq2db.PostgreSQL` package T4 templates ([#2001](https://github.com/linq2db/linq2db/issues/2001))
- updated T4 [documentation](https://linq2db.github.io/articles/T4.html) with information how to fix `error : Failed to resolve include text for file:` error when running template ([#2002](https://github.com/linq2db/linq2db/issues/2002))
- changed suffix, added to generated column properties in T4 when property name matches type name from `_Column` to `Column` ([#2004](https://github.com/linq2db/linq2db/issues/2004))
- added T4 configuration property `NormalizeNamesWithoutUnderscores = false` to enable normalization for names without underscores ([#2003](https://github.com/linq2db/linq2db/issues/2003))
- fixed `LinqException: Sequence ... cannot be converted to SQL` for client-side method calls with nested lambdas ([#2017](https://github.com/linq2db/linq2db/issues/2017))
- [@alexey-tyulkin](https://github.com/alexey-tyulkin) fixed table name parsing by `SystemDataLinqAttributeReader`, used to work with linq2sql mappings ([#2019](https://github.com/linq2db/linq2db/issues/2019))

***

### Release 2.9.4
- improve support for query-based associations ([#1965](https://github.com/linq2db/linq2db/issues/1965))
- improved `ExpressionMethod` support in subqueries ([#1969](https://github.com/linq2db/linq2db/issues/1969))
- fixed bad SQL generation for joined subqueries ([#1964](https://github.com/linq2db/linq2db/issues/1964))
- fixed bad SQL generation for `INSERT FROM` queries ([#1962](https://github.com/linq2db/linq2db/issues/1962))
- RetryingDbConnection.BeginTransaction() ignored isolation level, specified by caller ([#1973](https://github.com/linq2db/linq2db/issues/1973))
- fixed regression in materialization of nullable association ([#1975](https://github.com/linq2db/linq2db/issues/1975))

***

### Release 2.9.3
- [SQLite] add type name trailing whitespaces trimming in schema reader ([#1930](https://github.com/linq2db/linq2db/issues/1930))
- avoid generation of table alias with same name as table ([#1939](https://github.com/linq2db/linq2db/issues/1939))
- fix issue with incorrect operands order for non-equality binary operations with enums ([#1946](https://github.com/linq2db/linq2db/issues/1946))
- [Oracle][T4] fixed `DataType` hint for LONG and LONG RAW types ([#1940](https://github.com/linq2db/linq2db/issues/1940))
- [MSSQL][Schema] fixed table function result set metadata load for compatibility level 140+ ([#1921](https://github.com/linq2db/linq2db/issues/1921))
- fixed issue with `Sql.ExpressionAttribute` querying from method in wrong type, causing [exception](https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/20) in `linq2db.EntityFrameworkCore`

##### Custom types support
This [PR](https://github.com/linq2db/linq2db/pull/1954) adds support for creation of custom objects in queries using constructor or static fabric method.
Important notes:
- names of constructor/method parameters should match names of fields/properties (case-insensitive)
- logic in constructor/method body is not executed - it is only used to change shape of data in query

With this feature it is possible to use `Tuple` in queries through constructor or `Tuple.Create` fabric method.

Fixes issues: [#266](https://github.com/linq2db/linq2db/issues/266), [#420](https://github.com/linq2db/linq2db/issues/420), [#1084](https://github.com/linq2db/linq2db/issues/1084) and [#1953](https://github.com/linq2db/linq2db/issues/1953)

***

### Release 2.9.2

[API diff](https://www.fuget.org/packages/linq2db/2.9.2/lib/netstandard2.0/diff/2.9.1/) since v2.9.1

- fix issue, where custom `DataContext.MappingSchema` could be ignored ([#1922](https://github.com/linq2db/linq2db/issues/1922))
- [Oracle] improved support for `LONG`/`LONG RAW` data types and enabled fetching of LONG-types data ([#1899](https://github.com/linq2db/linq2db/issues/1899), [#1927](https://github.com/linq2db/linq2db/issues/1927))
- fixed query parameters caching issue for `SqlQueryDependentAttribute` parameters ([#1928](https://github.com/linq2db/linq2db/issues/1928))

***

### Release 2.9.1

[API diff](https://www.fuget.org/packages/linq2db/2.9.1/lib/netstandard2.0/diff/2.9.0/) since v2.9.0

- fixed issue with dynamic properties use in `ExpressionMethod`'s ([#1900](https://github.com/linq2db/linq2db/issues/1900))
- improve joins optimization in queries with CTE ([#1881](https://github.com/linq2db/linq2db/issues/1881))
- fix association queries with `FromSql` sources ([#1885](https://github.com/linq2db/linq2db/issues/1885))
- fix issues with left-joined data nullability and SQL generation ([#1869](https://github.com/linq2db/linq2db/issues/1869))
- fixed aliases generation for `ExpressionMethod` properties and added possibility to set aliases explicitly ([#1892](https://github.com/linq2db/linq2db/issues/1892))
- fix database connection leak in `DataContext` when connection was released only on context disposal ([#1891](https://github.com/linq2db/linq2db/issues/1891))
- [T4] added generation of `ReturnValue` parameters for stored procedures ([#1897](https://github.com/linq2db/linq2db/issues/1897)). To enable parameter generation for SQL Server procedure, it should be enabled for specific procedure using new helper method `AddReturnParameter(string procName, string paramName = "@return")` in T4 template. Call to method should be added between `LoadSqlServerMetadata(...)` and `GenerateModel()` calls.
- [T4] fixed issue, when custom attribute, added to column in t4 template, was removed during model generation ([#1866](https://github.com/linq2db/linq2db/issues/1866))
- [Schema][T4] added `GetSchemaOptions.GetForeignKeys = true` schema read option to skip load of foreign keys information from database ([#1907](https://github.com/linq2db/linq2db/issues/1907)). Added to workaround `Access JET` provider issue, where provider could crash application during FK information retrieval.

***

### Release 2.9.0

[API diff](https://www.fuget.org/packages/linq2db/2.9.0/lib/netstandard2.0/diff/2.8.1/) since v2.8.1

#### Multiple query result sets support ([#1844](https://github.com/linq2db/linq2db/issues/1844))
[@sami016](https://github.com/sami016) added support for multiple query result sets handling by using following API:
```cs
// methods to execute arbitrary sql
T QueryMultiple<T>(
                    this DataConnection connection,
                    string sql,
                    params DataParameter[] parameters);
Task<T> QueryMultipleAsync<T>(
                    this DataConnection connection,
                    string sql,
                    params DataParameter[] parameters);
Task<T> QueryMultipleAsync<T>(
                    this DataConnection connection,
                    string sql,
                    CancellationToken cancellationToken,
                    params DataParameter[] parameters);

// methods to execute stored procedure
T QueryProcMultiple<T>(
                    this DataConnection connection,
                    string sql,
                    params DataParameter[] parameters);
Task<T> QueryProcMultipleAsync<T>(
                    this DataConnection connection,
                    string sql,
                    CancellationToken cancellationToken,
                    params DataParameter[] parameters);
Task<T> QueryProcMultipleAsync<T>(
                    this DataConnection connection,
                    string sql,
                    params DataParameter[] parameters)
T QueryProcMultiple<T>(
                    this DataConnection connection,
                    string sql,
                    params DataParameter[] parameters);
```

To call this new API, you need to provide class (`T`), which will contain writeable properties to store each required resultset.
```cs
// no mapping attributes provided, so each recordset will be mapped to
// corresponding property in declaration order
class QueryResult1
{
    // first recordset mapped to array collection property
    public RecordSet1Record[]            Recordset1 { get;set; }
    // second recordset mapped to list collection property
    public IList<RecordSet2Record>       Recordset2 { get;set; }
    // third recordset mapped to array collection property
    public IEnumerable<RecordSet3Record> Recordset3 { get;set; }
    // fourth recordset mapped to single record
    // using FirstOrDefault() logic
    public RecordSet4Record              Recordset4 { get;set; }
    // fifth recordset mapped to list of scalars
    public int[]                         Recordset5 { get;set; }
    // sixth recordset mapped to single scalar
    // using FirstOrDefault() logic
    public int?                          Recordset6 { get;set; }
}

// using ResultSetIndexAttribute you can map only recordsets
// you need by specifying index of mapped resultset
class QueryResult2
{
    // third recordset mapped to array collection property
    [ResultSetIndex(2)]
    public IEnumerable<RecordSet3Record> Recordset3 { get;set; }
    // sixth recordset mapped to single scalar
    // using FirstOrDefault() logic
    [ResultSetIndex(5)]
    public int?                          Recordset6 { get;set; }
}
```

#### Table-less single-record queries support ([#1847](https://github.com/linq2db/linq2db/issues/1847))
Linq To DB already have `DataConnection.Select()` API to execute table-less single-record query against database, which could be usefull e.g. to query database properties or to evaluate some expression on server side.

Because not all databases support `SELECT` statements without `FROM <table>` statement, this API uses db-specific approaches to execute such queries. E.g. using `FROM dual` for Oracle.

In addtion to it, this feature adds new method
```cs
IQueryable<TEntity> SelectQuery<TEntity>(
                        this IDataContext         dataContext,
                        Expression<Func<TEntity>> selector)
```
which returns IQueryable<T> result. This allows you to use such queries in query composition as subquery or call it not only for `DataConnection` but also for other types of contexts like remote context.

#### LINQ
- fixed regression in handling some ternary expressions in queries, introduced in v2.7 ([#1838](https://github.com/linq2db/linq2db/issues/1838), [#1840](https://github.com/linq2db/linq2db/issues/1840))
- reintroduced proper fix for cases when enum mapping could fail due to type casts ([#1554](https://github.com/linq2db/linq2db/issues/1554))
- fixed 2.8.0 regression in handling `SqlPredicate.FuncLike` predicates ([#1848](https://github.com/linq2db/linq2db/issues/1848))

#### SQL
##### Set operators ([#1835](https://github.com/linq2db/linq2db/issues/1835))
In addition to already supported `UNION` and `UNION ALL` set operators, this feature also adds support for remaining operators:
- `EXCEPT` (old implementation allways used emulation)
- `EXCEPT ALL`
- `INTERSECT` (old implementation allways used emulation)
- `INTERSECT ALL`

Following list shows which methods should be used for each set operator:
- `UNION`: `IQueryable<T>.Union()`
- `UNION ALL`: `IQueryable<T>.UnionAll()` or `IQueryable<T>.Concat()`
- `EXCEPT`: `IQueryable<T>.Except()`
- `EXCEPT ALL`: `IQueryable<T>.ExceptAll()`
- `INSERSECT`: `IQueryable<T>.Intersect()`
- `INSERSECT ALL`: `IQueryable<T>.IntersectAll()`

If specific operator is not supported by database, emulation using other SQL statements will be used.

#### Provider-specific changes

#### DB2
- fixed incorrect precedence of operations in date add opeations, when interval is a composite expression ([#1850](https://github.com/linq2db/linq2db/issues/1850))

##### MS SQL
- imroved support for spatial types for .net core applications [#1843](https://github.com/linq2db/linq2db/issues/1843)
- added [FAQ article](https://linq2db.github.io/articles/FAQ.html#how-can-i-use-sql-server-spatial-types) with information how to configure spatial types support

#### Oracle
- fixed incorrect precedence of operations in date add opeations, when interval is a composite expression ([#1850](https://github.com/linq2db/linq2db/issues/1850))

#### PostgreSQL
- fixed incorrect precedence of operations in date add opeations, when interval is a composite expression ([#1850](https://github.com/linq2db/linq2db/issues/1850))

#### SQLite
- fixed incorrect precedence of operations in date add opeations, when interval is a composite expression ([#1850](https://github.com/linq2db/linq2db/issues/1850))

***

### Release 2.8.1
- revert fix for [#1554](https://github.com/linq2db/linq2db/issues/1554) as it introduced major regression in `Set`/`Value` functions at least for `Guid?` values
- fixed incorrect SQL generation for some joins with subqueries ([#1760](https://github.com/linq2db/linq2db/issues/1760))

***

### Release 2.8.0

[API diff](https://www.fuget.org/packages/linq2db/2.8.0/lib/netstandard2.0/diff/2.7.4/) since v2.7.4

#### Mapping
- Fixed Issue, where when setting an enum to sql mapping would override mapping for underlying enum type ([#1622](https://github.com/linq2db/linq2db/issues/1622))
- Improved `MappingSchema.ValueToSqlConverter` support and usage ([#1797](https://github.com/linq2db/linq2db/issues/1797))
- Added new set of `Expressions.MapBinary(...)` methods to define mappings of binary operators to SQL ([#1725](https://github.com/linq2db/linq2db/issues/1725), [#1723](https://github.com/linq2db/linq2db/issues/1723))
```cs
// plain extension function, that could be used as-is in query
[Sql.Expression("{0} << {1}", Precedence = Precedence.Primary)]
public static long Shl(long v, int s) => v << s;


// define mapping for << operator to our extension
// to use more fancy syntax in linq query
Expressions.MapBinary((long v, int s) => v << s, (v, s) => Shl(v, s));

// generate << sqlite operator through function
from p in db.Parent
    where Shl(p.ParentID, 1) > 0
    select p;

// generate << sqlite operator using binary opration syntax
from p in db.Parent
    where p.ParentID >> 1 > 0
    select p;

// available overloads:

// specify mapping in verbose way and bind it to specific configuration (SQLite)
Expressions.MapBinary(
                    ProviderNames.SQLite,
                    ExpressionType.LeftShift,
                    typeof(long), typeof(int),
                    (v, s) => Shl(v, s));

// specify mapping in verbose way and bind it to default configuration
Expressions.MapBinary(
                    ExpressionType.LeftShift,
                    typeof(long), typeof(int),
                    (v, s) => Shl(v, s));

// shorthand variants of previous overloads
Expressions.MapBinary(
                    ProviderNames.SQLite,
                    (long v, int s) => v << s,
                    (v, s) => Shl(v, s));
Expressions.MapBinary(
                    (long v, int s) => v << s,
                    (v, s) => Shl(v, s));
```

##### ConfigureAwait management
This feature adds new option `bool Configuration.ContinueOnCapturedContext = true` to change default behavior of async calls inside of linq2db using `ConfigureAwait(Configuration.ContinueOnCapturedContext)` calls ([#1366](https://github.com/linq2db/linq2db/issues/1366), [#1754](https://github.com/linq2db/linq2db/issues/1754))

##### Dynamic Columns
###### Configurable dynamic column store ([#1477](https://github.com/linq2db/linq2db/issues/1477), [#1695](https://github.com/linq2db/linq2db/issues/1695))
This feature allows you to configure custom setter and getter methods instead of having dictionary on target entity. Using it you can store column values any way you want, e.g. in external collection.


To configure setters/getters you can use new mapping capabilities:
- `DynamicColumnAccessorAttribute` attribute, applied to entity class, that accepts setter/getter method name or name of method/property, that defines getter/setter expression
- fluent mapping method `DynamicPropertyAccessors` to configure storage accessors
- fluent mapping method `DynamicColumnsStore` to configure dictionary-based storage using fluent mapping instead of attribute in entity class

```cs
// configure storage accessors on entity using attribute
// demonstrates both plain instance/static methods and expression properties/methods
// note that while example below contains several variants of setter
// and getter methods
// you must map exactly one setter and getter
[Table]
[DynamicColumnAccessor(
    Configuration = ProviderName.SqlServer,
    GetterMethod = nameof(GetProperty),
    SetterExpressionMethod = nameof(SetPropertyExpression))]
public Entity
{
    [PrimaryKey]
    public int Id { get; set; }

    public static Dictionary<string, Dictionary<int, object>>
        StaticStorage { get; set; }
            = new Dictionary<string, Dictionary<int, object>>();

    // instance method - takes name and default value (default(TProperty)
    // if not set in mapping schema)
    private object GetProperty(string name, object defaultValue)
    {
        return GetPropertyStatic(this, name, defaultValue);
    }

    // static method - takes entity object additionally
    private static object GetPropertyStatic(Entity entity,
                                            string name,
                                            object defaultValue)
    {
        if (!StaticStorage.TryGetValue(name, out object values))
            StaticStorage[name] = values = new Dictionary<int, object>;

        if (!values.TryGetValue(entity.Id, out object value))
            value = defaultValue;

        return value;
    }

    // getter using expression property
    public static Expression<Func<
                                InstanceGetterSetterExpressionMethods,
                                string,
                                object,
                                object>> GetPropertyExpression
    {
        get
        {
            return (instance, name, defaultValue)
                => instance.GetProperty(name, defaultValue);
        }
    }

    // instance setter
    public void SetProperty(string name, object value)
    {
        SetPropertyStatic(this, name, value);
    }

    // static setter
    private static void SetPropertyStatic(
                                        Entity instance,
                                        string name,
                                        object value)
    {
        if (!StaticStorage.ContainsKey(name))
            StaticStorage.Add(name, new Dictionary<int, object>());

        InstanceValues[name][instance.Id] = value;
    }

    // setter using expression method
    public static Expression<Action<
                                InstanceGetterSetterExpressionMethods,
                                string,
                                object>> SetPropertyExpression()
    {
        return (instance, name, value) => instance.SetProperty(name, value);
    }
}

```

For more examples you can check [tests](https://github.com/linq2db/linq2db/pull/1695/files#diff-49e18ae5b7a7da8e19085db50ef202c1)

###### Configuration-specific dynamic column store ([#1521](https://github.com/linq2db/linq2db/issues/1521), [#1695](https://github.com/linq2db/linq2db/issues/1695))

This fix adds support for `Configuration` property for `DynamicStoreAttribute` and `DynamicColumnAccessorAtribute`, including their fluent-mapping companions.

#### GetSchema and T4 Templates
- added new `GetSchemaOptions` option `Func<LoadTableData, bool> LoadTable` to filter loaded tables. In T4 templates available as `GetSchemaOptions.LoadTable` ([#1640](https://github.com/linq2db/linq2db/issues/1640), [#1667](https://github.com/linq2db/linq2db/issues/1667))
- added new T4 option `GenerateDatabaseInfo` (default: `true`) to manage generation of comment with database details (db name, db version, etc.) on generated data manager class ([#1632](https://github.com/linq2db/linq2db/issues/1632))
- fixed tools folder conent for T4 packages `linq2db.MySql`, `linq2db.MySqlConnector` to work with SSL connection strings ([#1772](https://github.com/linq2db/linq2db/issues/1772))
- added tools folder for `linq2db.Sybase.DataAction` T4 package to work with netcore projects ([#1780](https://github.com/linq2db/linq2db/issues/1780))
- added new option `bool PrefixTableMappingForDefaultSchema = false` to enable schema-named prefix for generated table mapping classes for default schema. Option works only with following condition also true: `IncludeDefaultSchema = true && GenerateSchemaAsType = false && PrefixTableMappingWithSchema = true` ([#1656](https://github.com/linq2db/linq2db/issues/1656))
- added new option `bool GenerateProceduresOnTypedContext = true`, to control type of `DataConnection` parameter of generated store procedure mappings. When set to `true`, will use generated context type, otherwise will use `DataConnection` ([#1805](https://github.com/linq2db/linq2db/issues/1805))

##### Support generation of interfaces ([#1824](https://github.com/linq2db/linq2db/issues/1824))
Now you can generate interfaces by changing type keyword for type model by setting new property:
```cs
public abstract partial class TypeBase : IClassMember
{
    ...
    // assign "interface" to this field to generate this type as interface
    public string ClassKeyword = "class";
    ...
}

public enum AccessModifier
{
    ....
    // new enum value to disable access modifier generation for type member
    None
}
```

##### `IEquatable<T>` inteface implementation generation support
Thanks to [@jossik](https://github.com/jossik), now we have new T4 template `Equatable.ttinclude` to generate implementation of `IEquatable<T>` in generated classes.

To enable `IEquatable<T>` generation, just include `Equatable.ttinclude` to your template.

You can use following options to configure generation:
- `string EqualityComparerFieldName = "_comparer"` - specify name of static field, which will store generated comparer implementation
- `Func<Class, Property, bool> EqualityPropertiesFilter` - filter function to select types and properties of those types, used for comparison. To generate equality functionality, class should have at least one not filtered out property. Default filter implementation allows only table mapping classes and use primary key properties for comparison.
- `bool DefaultEquatable = true` - option to disable `IEquatable<T>` generation by default for all classes. In this case you should enable generation for specific class by setting it's `bool IsEquatable` property to true

##### Nullable reference types support ([#1632](https://github.com/linq2db/linq2db/issues/1632))

This feature adds support for C# 8.0 nullable reference types feature. It adds:
- generation of nullable reference type annotations in generated T4 model
- generation of `#nullable enable/disable` pragma for generated code
- several new T4 generation options to control generation behavior for this feature

New T4 options:
```cs
// this option enables generation of nullable reference type annotations
// for code, generated by T4 template
bool EnableNullableReferenceTypes = false;

// this option enables generation of #nullable pragma for generated code
// it generates "#nullable enabled" if EnableNullableReferenceTypes set to true
// and "#nullable disabled" otherwise
bool AddNullablePragma = false;

// wraps generated column and association members with "#nullable disable" pragma
// to not produce `CS8618: Non-nullable member 'membername' is uninitialized`
// warnings on members, initialized by linq2db
// applied only if EnableNullableReferenceTypes set to true
bool EnforceModelNullability = true;

// this delegate allows you to tell linq2db if provided type
// (as type name string parameter) is a value or reference type
// so linq2db can generate nullable annotations properly for this type.
// By default unknown to linq2db type treatened as reference type
// except case, when type name ends with '?'
Func<string, boolean> IsValueType;
```

#### LINQ
- fixed another case of `NullReferenceException` from complex select expressions ([#1202](https://github.com/linq2db/linq2db/issues/1202))
- fixed issues, where `Nullable<T>.HasValue` property treated differently compared to `val != null` ([#1755](https://github.com/linq2db/linq2db/issues/1755), [#1776](https://github.com/linq2db/linq2db/issues/1776), [#1777](https://github.com/linq2db/linq2db/issues/1777))
- fixed issue, when nullability change functions, e.g. `Sql.ToNullable()`, used as part of select field expression, had no effect ([#1788](https://github.com/linq2db/linq2db/issues/1788))
- fixed selection of empty record as entity through outer join (left, right or full) using `Join()/LeftJoin()/RightJoin()/FullJoin()` methods as `null` instead of uninitialized instance ([#1773](https://github.com/linq2db/linq2db/issues/1773))
- fixed joins optimization ignored for left part of full join statement ([#1785](https://github.com/linq2db/linq2db/issues/1785))
- fixed several cases when enum mapping could fail due to type casts ([#1554](https://github.com/linq2db/linq2db/issues/1554))
- fixed issue, when weak join could be removed, if it is used only in join conditions for other joins ([#1614](https://github.com/linq2db/linq2db/issues/1614), [#1816](https://github.com/linq2db/linq2db/issues/1816))
- fixed issue, when subquery with window function could be converted to direct query to table, if it doesn't have filter expression ([#1799](https://github.com/linq2db/linq2db/issues/1799))
- fixed issue when union subquery with aggregates in main query could fail with `ArgumentOutOfRangeException` ([#1774](https://github.com/linq2db/linq2db/issues/1774))
- imporved custom aggregates support ([#1564](https://github.com/linq2db/linq2db/issues/1564))

##### Subquery optimization management ([#1360](https://github.com/linq2db/linq2db/issues/1360))
This feature adds two `IQueryable<T>` extension methods to help you control how linq2db treats your subqueries and optimize joins.

###### `AsSubQuery()` extension
```cs
// tells linq2db that it cannot optimize source by removing subquery
// when it sees that is it possible
public static IQueryable<TSource> AsSubQuery<TSource>(
                                     this IQueryable<TSource> source);

// example
var query = from t1 in table1.Where(v => v.Key1 != 1).AsSubQuery()
            from t2 in table2
                .LeftJoin(f => t2.Key1 == t1.Key1 && t2.Key2 == t1.Key2)
            select new { t1 , t2 };
```

with `AsSubQuery()` call you will have subquery joined with table
```sql
SELECT <fields>
    FROM (SELECT <fields> FROM [table1] [v] WHERE [v].[Key1] <> 1 ) [t1]
        LEFT JOIN [table2] [t2] ON [t2].[Key1] = [t1].[Key1]
                                AND [t2].[Key2] = [t1].[Key2]
```

without `AsSubQuery()` call, linq2db will remove unnecessary subquery
```sql
SELECT <fields>
    FROM [table1] [t1]
        LEFT JOIN [table2] [t2] ON [t2].[Key1] = [t1].[Key1]
                                AND [t2].[Key2] = [t1].[Key2]
    WHERE [t1].[Key1] <> 1
```

###### HasUniqueKey() extension
```cs
// tells linq2db which columns from source (e.g. subquery or table) form unique key
// e.g. could be usefull if you want to tell linq2db, that left join to
// subquery will produce 1 result record per joined source record
// so linq2db could apply subquery removal optimizations when it applicable
public static IQueryable<TSource> HasUniqueKey<TSource, TKey>(
    this IQueryable<TSource>             source,
    Expression<Func<TSource, TKey>> keySelector);

// example
// define keys on subquery
// could have multiple HasUniqueKey() calls
// if it reused in joins with different sets of keys
var subqueryWithKeyDefined = table1
    .Select(f => new { First = f })
    .HasUniqueKey(f => new {f.First.Key1})
    .Select(f => f.First);

// left join it with table
var query = from t2 in table2
    from a in subqueryWithKeyDefined.LeftJoin(a => a.Key1 == s.Key1)
        select new
        {
            Second = t2,
            First  = t1
        };

// select only table
// because linq2db knows that left join will not produce multiple records
// it can freely remove join, as joined subquery columns are not used
query.Select(p => p.Second).ToList();
```

query with HasUniqueKey() call
```sql
SELECT <table2_columns>
    FROM table2 t2
```

query without HasUniqueKey() call
```sql
SELECT <table2_columns>
    FROM table2 t2
        LEFT JOIN table1 t1 ON t1.Key1 = t2.Key1
```

#### SQL
- don't generate `RESPECT NULLS` token for `LAG`, `LEAD`, `FIRST_VALUE`, `LAST_VALUE`, `NTH_VALUE` window functions as it is default behavior and token not supported by most databases ([#1732](https://github.com/linq2db/linq2db/issues/1732))
- fixed `'CTE_NAME' has fewer columns than were specified in the column lis` error due to generation of SQL with incorrect number of CTE columns ([#1584](https://github.com/linq2db/linq2db/issues/1584), [#1817](https://github.com/linq2db/linq2db/issues/1817))

#### Other changes
##### New overridable methods on `DataConnection`
Three new overridable methods added to `DataConnection` to allow you override command execution logic for async operations:
```cs
protected virtual Task<int> ExecuteNonQueryAsync(
                                 IDbCommand command,
                                 CancellationToken cancellationToken);

protected virtual Task<object> ExecuteScalarAsync(
                                 IDbCommand command,
                                 CancellationToken cancellationToken);

protected virtual Task<DbDataReader> ExecuteReaderAsync(
                                 IDbCommand        command,
                                 CommandBehavior   commandBehavior,
                                 CancellationToken cancellationToken);
```

#### Provider-specific changes

##### Firebird
- enabled `SchemaName` property population in GetSchema API. Could lead to `Schema` mapping property generation in T4 templates, so if you don't want them, set `IncludeDefaultSchema = false` option in your template ([#1798](https://github.com/linq2db/linq2db/issues/1798))

##### Azure SQL
- fixed long-standing issue with GetSchema API throwing collation exception when database collation is not `SQL_Latin1_General_CP1_CI_AS` ([#1733](https://github.com/linq2db/linq2db/issues/1733))

##### MS SQL
- fixed incorrect type used for separator for `STRING_AGG` function, called over `varchar` data ([#1765](https://github.com/linq2db/linq2db/issues/1765))
- added support for `DateTimeOffset` members conversion to SQL ([#1666](https://github.com/linq2db/linq2db/issues/1666))

##### MySQL
- migrated `ISchemaProvider.GetSchema` API to use `INFORMATION_SCHEMA` views instead of ADO.NET GetSchema API. This change enables `ISchemaProvider.GetSchema` API for `MySqlConnector` provider, because it is lacking ADO.NET GetSchema API support ([#1780](https://github.com/linq2db/linq2db/issues/1780))

##### Oracle
- changed behavior of linq2db for native provider to set `BindByName` property only for commands with parameters. Fixes `CreateTable` calls for tables with identity generation triggers ([#1693](https://github.com/linq2db/linq2db/issues/1693))
- changed generated SQL for adding intervals to use interval literals ([#1810](https://github.com/linq2db/linq2db/issues/1810))

##### SAP/Sybase ASE
- fixed issue, when `DateTime` value with date part `1900/1/1` were converted to value with date `0001/1/1`. With this fix it will be done only for `time` database values as expected ([#1707](https://github.com/linq2db/linq2db/issues/1707))
- fixed incorrect time literal generation in SQL command when negative `TimeSpan` passed not as parameter, but as literal to database. E.g. prior to fix negative interval with time part (days part always ignored, as Sybase support only 0:00-24:00 intervals) `-1:00` were sent to server as `1:00`. After fix it will be sent as `23:00` ([#1792](https://github.com/linq2db/linq2db/issues/1792))

##### SAP HANA
- fixed issue with parameter used for separator for `STRING_AGG` function ([#1775](https://github.com/linq2db/linq2db/issues/1775))

***

### Release 2.7.4

Bugfix release:
- fixed issue when you can get `Table not found for '<column>` error ([#1761](https://github.com/linq2db/linq2db/issues/1761))

***

### Release 2.7.3

Bugfix release:
- fixed regression in conditions optimization ([#1750](https://github.com/linq2db/linq2db/issues/1750))
- fixed incorrect comparison generation for complex nullable expressions ([#1758](https://github.com/linq2db/linq2db/issues/1758))

***

### Release 2.7.2

Bugfix release:
- fixed `v2.7.0` regression in `linq2db` nuget dependencies to reference wrong versions of some packages ([#1704](https://github.com/linq2db/linq2db/issues/1704))
- fixed edge cases in SQL generation for ternary operation ([#1734](https://github.com/linq2db/linq2db/issues/1734))
- fixed exceptions during UNION query generation ([#1712](https://github.com/linq2db/linq2db/issues/1712), [#1736](https://github.com/linq2db/linq2db/issues/1736))
- [Firebird] added detection for some missing reserved words. Thanks to [@Aceto1](https://github.com/Aceto1) for contribution ([#1741](https://github.com/linq2db/linq2db/issues/1741))
- [PostgreSQL] reverted force mapping of `DateTime` with `Kind = UTC` to `timestamptz`, introduced in `v2.6.4`. Use `DbType` or `DataType` mapping properties to map `DateTime` to `date` or `timestamptz` types ([#1742](https://github.com/linq2db/linq2db/issues/1742))

***

### Release 2.7.1

Small bugfix release:
- fixed regression in handling of ternary expressions in select projections ([#1717](https://github.com/linq2db/linq2db/issues/1717))
- fixed error when CTE clause could be missing in generated SQL ([#1709](https://github.com/linq2db/linq2db/issues/1709))
- fixed incorrect optimization of left join to subquery with nested joins ([#1718](https://github.com/linq2db/linq2db/issues/1718))
- fixed handling of columns with `DATETIME2`/`DATETIMEOFFSET`/`TIME` types with precision in CreateTable APIs ([#1721](https://github.com/linq2db/linq2db/issues/1721))

***

### Release 2.7.0

#### Breaking changes
- behavior of `DatabaseName`/`SchemaName`/`TableName` `ITable` extensions changed to return new instance instead of mutating current one. For more details check this issue: [#1274](https://github.com/linq2db/linq2db/issues/1274)

#### New package linq2db.Tools
With this release we introduce new package `linq2db.Tools`, which will be used for various useful utility classes. Right now it contains the following components:
- `LinqToDB.Tools.Comparers.ComparerBuilder` class to generate comparison utilities for arbitrary types like equality and hash-code delegates, `IEqualityComparer<T>` instances;
- `LinqToDB.Tools.EntityServices.IdentityMap` class as entity cache over linq2db data context
- `LinqToDB.Tools.Mapper` namespace contains classes to create mapping of objects of one type to another
- `LinqToDB.Tools.MappingSchemaExtensions` class contains set of extension methods to generate `IEqualityComparer<T>` for entities, defined in `MappingSchema`

We plan to add more documentation later on this package, but for now you can check [existing tests](https://github.com/linq2db/linq2db/tree/master/Tests/Linq/Tools) for use-cases.

#### NULL handling improvements
To improve `NULL` values handling in generated SQL, we introduced new property `Sql.IsNullableType Sql.ExpressionAttribute.IsNullable` to `Sql.ExpressionAttribute` (and it's descendands `Sql.PropertyAttribute`, `Sql.FunctionAttribute`) to allow better fine-grained specification if the expression result could be `NULL` (compared to the already existing `CanBeNull` boolean property). Using this new property you can specify conditions when the expression could return `NULL`.

E.g. here we tell `Linq To DB` that `Convert` function could return `NULL` value only if second parameter (original value) could be `NULL`:
```cs
Sql.Function("Convert", 0, 1, 2,
    ServerSideOnly = true,
    IsNullable = IsNullableType.SameAsSecondParameter)]
public static TTo Convert<TTo, TFrom>(TTo to, TFrom from, int format) {...}
```

In other words this improvement will allow `Linq To DB` to generate `IS (NOT) NULL` checks more reasonably.

Available options:
```cs
public static class Sql
{
    public enum IsNullableType
    {
        Undefined = 0,
        Nullable,
        NotNullable,
        IfAnyParameterNullable,
        SameAsFirstParameter,
        SameAsSecondParameter,
        SameAsThirdParameter,
        SameAsLastParameter
    }
}
```

Existing `Sql` class properties/functions were updated to use new property.

#### Caching improvements

linq2db adopted Microsoft's `MemoryCache` in-proc cache implementation in several places.

##### DML queries cache ([#1491](https://github.com/linq2db/linq2db/issues/1491))

Old query caching mechanisms for `Delete`/`Insert`/`InsertOrReplace`/`Update` operations have been replaced with `MemoryCache`. Now cached queries will be evicted from cache if they have not been used for 1 hour.

Cache control options:
- to change query expiration timeout use new `LinqToDB.Common.Configuration.Linq.CacheSlidingExpiration` option;
- to force clear cached queries use already existing `Query.ClearCaches()` method.

##### Mapping schema caching ([#1587](https://github.com/linq2db/linq2db/issues/1587))
Mapping schema entity descriptors now will be cached. Fixes performance issues in some cases ([#1585](https://github.com/linq2db/linq2db/issues/1585)).

Cache control options:
- to change query expiration timeout, use the new `LinqToDB.Common.Configuration.Linq.CacheSlidingExpiration` option (shared with DML queries caching);
- to force clear cached descriptors use new method `MappingSchema.ClearCache()`.

#### Better support for async connection and transaction methods ([#1540](https://github.com/linq2db/linq2db/issues/1540), [#1582](https://github.com/linq2db/linq2db/issues/1582))

This change adds new async methods for connection and transaction management:
```cs
// already added in 2.6.4 and only updated
// to use provider's BeginTransactionAsync method when awailable
Task<DataConnectionTransaction> DataConnection.BeginTransactionAsync(
    CancellationToken);
Task<DataConnectionTransaction> DataConnection.BeginTransactionAsync(
    IsolationLevel,
    CancellationToken);

// new API
Task DataConnection.CommitTransactionAsync(CancellationToken);
Task DataConnection.RollbackTransactionAsync(CancellationToken);
Task DataConnection.CloseAsync(CancellationToken);
Task DataConnection.DisposeAsync(CancellationToken);

Task DataConnectionTransaction.CommitAsync(CancellationToken);
Task DataConnectionTransaction.RollbackAsync(CancellationToken);

Task DataContextTransaction.CommitTransactionAsync(CancellationToken);
Task DataContextTransaction.RollbackTransactionAsync(CancellationToken);
```

##### Providers support
IMPORTANT: new methods will work in synchronous mode for most of providers as they require support from underlying provider.

Currently, only the following providers have some of required methods:
- `npgsql`: CommitAsync, RollbackAsync (BeginTransactionAsync not needed for npgsql, as it is not async operation for it)
- `MySqlConnector`: CommitAsync, RollbackAsync, BeginTransactionAsync

`MySql.Data` provides the following methods: BeginTransactionAsync, CloseAsync (connection), but implemens them synchronously, so you will not benefit from them.

Currently linq2db detects and use following methods (if a provider later adds one of the methods shown below, it will be used by linq2db automatically):
```cs
// Connection methods
Task CloseAsync(CancellationToken);
Task<IDbTransactionBasedType> BeginTransactionAsync(
    CancellationToken);
Task<IDbTransactionBasedType> BeginTransactionAsync(
    IsolationLevel,
    CancellationToken);

// Transaction methods
Task CommitAsync(CancellationToken);
Task RollbackAsync(CancellationToken);
```

If your provider has methods with other signatures that implement this functionality, you can implement your own `IAsyncDbConnection` or `IAsyncDbTransaction` wrapper and register factory for it in `AsyncFactory` class. Still, it is recommended to report it to linq2db, so we can add new signature support in later versions.

#### Mapping
- `DbDataType` class now includes type length/size information, which could be useful e.g. for cases when you need to specify query parameter size explicitly ([#1628](https://github.com/linq2db/linq2db/issues/1628))
- fixed several cases when default value was assigned to selected non-nullable field from nullable association even if user specified cast to nullable type or used `Sql.ToNullable` helper ([#1546](https://github.com/linq2db/linq2db/issues/1546), [#1659](https://github.com/linq2db/linq2db/issues/1659))
- fixed issue with fluent mapper, where some changes to mapping were ignored ([#446](https://github.com/linq2db/linq2db/issues/446))
- fixed reading out list of mapped types from mapping schema for fluent mapping using new `MappingSchema.GetDefinedTypes()` method. Method returns only known types, so it will not work for attribute-based mapping ([#924](https://github.com/linq2db/linq2db/issues/924))
- added additional parameter to `MappingSchema.GetAttributes(exactForConfiguration: false)` to return result immediately when attributes are found for most specific configuration instead of returning attributes for all compatible configurations ([#1562](https://github.com/linq2db/linq2db/issues/1562))

##### Skipping field update/insert based on provided value ([#981](https://github.com/linq2db/linq2db/issues/981), [#1598](https://github.com/linq2db/linq2db/issues/1598))
This feature adds set of mapping attributes to dynamically exclude a column from insert or update operation for DataConnection`s Insert/Update/InsertOrUpdate/InsertWithIdentity* methods (IMPORTANT: feature doesn't affect LINQ-based insert/update operations).

Following attributes available:
```cs
// specify column values to skip on insert operation
[SkipValuesOnInsertAttribute(comma, separated, list, of, values, to, skip)]
// specify column values to skip on update operation
[SkipValuesOnUpdateAttribute(comma, separated, list, of, values, to, skip)]

// Following classes could be used to implement own skip attributes.
//
// They could be useful if you want to implement
// custom skip logic, that doesn't just check column's value against list
// of values to skip
//
// or
// 
// to load list of skipped values from somewhere else or to support
// values of types, not allowed as attribute parameter

// use to implement custom skip logic
abstract class SkipBaseAttribute {}

// use to implement custom skipped values load logic
abstract class SkipValuesByListAttribute {}
```

#### LINQ
- fixed long-standing issue with `DatabaseName`/`SchemaName`/`TableName` `ITable` extensions mutating and returning original object instead of creating new `ITable` instance ([#1274](https://github.com/linq2db/linq2db/issues/1274)). Starting from this release those extensions will always return a new `ITable` instance.
- added support for `ExpressionMethodAttribute` on type conversion methods, e.g. on conversion operators ([#1664](https://github.com/linq2db/linq2db/issues/1664))
- fixed nullability tracking issues in projection expressions, when null record could have been replaced by a record with default values ([#1665](https://github.com/linq2db/linq2db/issues/1665))
- fixed `IndexOutOfRangeException` in some cases for single-select subqueries ([#1675](https://github.com/linq2db/linq2db/issues/1675))

#### SQL
- added new property `Sql.CurrentTimestampUtc`, which will be converted to the corresponding SQL to get UTC time, when used in a query ([#1577](https://github.com/linq2db/linq2db/issues/1577)). Note that for Firebird, SQL CE and Access it will be converted to query parameter/literal, as they do not provide such functionality on the server-side.
- fixed issue when dependent CTE statement wasn't emitted in resulting SQL ([#1653](https://github.com/linq2db/linq2db/issues/1653))
- fixed multiple issues with existing `Sql.DateAdd()`, `Sql.DateDiff()` and `Sq.DatePart()` extensions ([#780](https://github.com/linq2db/linq2db/issues/780), [#1615](https://github.com/linq2db/linq2db/issues/1615))

##### String join extensions ([#1559](https://github.com/linq2db/linq2db/issues/1559), [#1562](https://github.com/linq2db/linq2db/issues/1562))
Added new extensions to join strings into single string:
- `Sql.StringAggregate()` aggregate functions to aggregate column into single string in group with optional sorting
- `Sql.ConcatStrings()` function to concat string parameters into single string

Those functions require underlying database support.

`Sql.ConcatStrings` support:
- MySQL
- PostgreSQL
- SQLite
- SQL Server (any version)

`Sql.StringAggregate` support:
- Firebird (doesn't support ordering of aggregated values)
- DB2
- MySQL
- Oracle Database
- PostgreSQL
- SAP HANA
- SQLite (doesn't support ordering of aggregated values)
- SQL Server 2017+

How to use (for more examples check our [tests](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/Linq/StringFunctionTests.cs))

`Sql.StringAggregate` aggregate function:
```cs
// aggregate should start from StringAggregate() call
// optionally add sorting using
// OrderBy/OrderByDescending/ThenBy/ThenByDescending
// methods
// and end with ToValue() method call (!)
from t in table
    group t.Value1 by new {t.Id, Value = t.Value1}
	into g
	select new
	{
	    Max = g.Max(),
            // concat Value1 column values in each group using " -> " as
            // values separator, ordered alphabetically by Value1 column
	    Values = g.StringAggregate(" -> ").OrderBy(e => e).ToValue(),
	};

from t in table
    group t by new {t.Id, Value = t.Value1}
	into g
	select new
	{
            // concat Value1 column values in each group using " -> " as
            // values separator, ordered alphabetically by Value3 column
            // then in descending order by Value1 column
            Values     = g.StringAggregate(" -> ", e => e.Value1)
                          .OrderBy(e => e.Value3)
                          .ThenByDescending(e => e.Value1)
                          .ToValue(),
	};

// string concatenation of entire query results without explicit grouping
table.Select(t => t.Value1).StringAggregate(" -> ").ToValue();
```

`Sql.ConcatStrings` function:
```c#
// concatenate values of 3 fields into a single string with " -> " as values separator
table.Select(t => Sql.ConcatStrings(" -> ", t.Value3, t.Value1, t.Value2));
```

#### Remote Context
- fixed `DateTime` values serialization to preserve `Kind` for `Kind = Utc` ([#1629](https://github.com/linq2db/linq2db/issues/1629))
- updated `DateTime` and `DateTimeOffset` values serialization to be more compact ([#1638](https://github.com/linq2db/linq2db/issues/1638))

#### Other changes
- added new `DataConnenction` events `OnBeforeConnectionOpen` and `OnBeforeConnectionOpenAsync`, triggered before database connection open for synchronous and asynchronous code. Events accept `IDbConnection` instance as one of the parameters, which allows you to pre-configure it. E.g. it can be used to set `SqlConnection.AccessToken` for SQL Server Azure connection ([#1604](https://github.com/linq2db/linq2db/issues/1604), [#1607](https://github.com/linq2db/linq2db/issues/1607))
- added new `DataConnection(providerName, connectionString)` constructor ([#1513](https://github.com/linq2db/linq2db/issues/1513))
- removed `System.Data.DataRowExtensions` extensions from `netstandard2.0`/`netcoreapp2.0` builds. Replaced by Microsoft's [nuget](https://www.nuget.org/packages/System.Data.DataSetExtensions) ([#1635](https://github.com/linq2db/linq2db/issues/1635))
- fixed incorrect documentation for `DataConnection.CommandTimeout`. Previously it was saying that by default `CommantTimeout` is 0 (infinite). Now it properly states that by default it use provider's default timeout (must be 30 seconds, but could depend on provider's implementation). Also now `CommandTimeout` will return -1 instead of 0, if the default value is used, to indicate that provider's default timeout used. Assigning a negative value to this property will also reset timeout to default value ([#1668](https://github.com/linq2db/linq2db/issues/1668), [#1670](https://github.com/linq2db/linq2db/issues/1670))
- restored Xamarin compatibility, broken since linq2db 2.6.0 release ([#1487](https://github.com/linq2db/linq2db/issues/1487))
- added `LinqToDB.Data.DbCommandProcessor.DbCommandProcessorExtensions.Instance` extension point to override `DbCommand.Execute*` methods implementation ([#1699](https://github.com/linq2db/linq2db/issues/1699))

#### Provider-specific changes

##### Firebird
- added type casts to `DateTime`-based SQL literals to avoid errors when Firebird cannot guess which type to use ([#780](https://github.com/linq2db/linq2db/issues/780))

##### Informix
- changed default value `LinqToDB.DataProvider.Informix.InformixConfiguration.ExplicitFractionalSecondsSeparator` from `false` to `true` to support modern server versions by default. If you use a version older than v11.70.xC8/v12.10.xC2, you should set it back to false.

##### MySQL/MariaDB
- fixed `SqlNullValueException` thrown from `MySql.Data` provider on `GetSchema` calls for databases with full-text index ([#1606](https://github.com/linq2db/linq2db/issues/1606))

###### Full-text search support ([#1649](https://github.com/linq2db/linq2db/issues/1649))
Added following extension methods to use `MATCH AGAINST` full-text search clause:

```cs
// MATCH AGAINST as predicate
bool Sql.Ext.MySql().Match(string search, params object[] columns);
bool Sql.Ext.MySql().Match(
    MatchModifier modifier,
    string search,
    params object[] columns);

// MATCH AGAINST as relevance
double Sql.Ext.MySql().MatchRelevance(string search, params object[] columns);
double Sql.Ext.MySql().MatchRelevance(
    MatchModifier modifier,
    string search,
    params object[] columns);
```

Check [tests](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/Linq/FullTextTests.MySql.cs) for use examples.

###### MySqlConnector provider support ([#1470](https://github.com/linq2db/linq2db/issues/1470), [#1484](https://github.com/linq2db/linq2db/issues/1484))

Thanks to [@Mitch528](https://github.com/Mitch528) now we have support for [MySqlConnector](https://github.com/mysql-net/MySqlConnector) provider for MySQL. To use new provider you just need to add reference to this provider to your project instead of `MySql.Data` provider.

In cases when you need to use both providers in same application, you should use `MySql.Data` and `MySqlConnector` provider names in your connection string settings, to tell `Linq To DB` explicitly which provider to use with a specific connection.

A new `T4` model generator nuget package `linq2db.MySqlConnector` is also released for this provider.

##### PostgreSQL
- added `Schema` property to `SequenceNameAttribute` to specify sequence generator schema name ([#1580](https://github.com/linq2db/linq2db/issues/1580))
- updated `InsertWithIdentity` methods SQL to use `RETURNING id` statement instead of extra query to select sequence value ([#1581](https://github.com/linq2db/linq2db/issues/1581))
- added `Description` field population for tables in columns in `GetSchema` API ([#1658](https://github.com/linq2db/linq2db/issues/1658))
- added type hint to `DateTime`-based SQL literals to avoid errors when PostgreSQL cannot guess which type to use e.g. for functions with overloads ([#780](https://github.com/linq2db/linq2db/issues/780))

##### SAP HANA
- fixed `HanaException : Data is Null. This method or property cannot be called on Null values` exception on some complex queries ([#1684](https://github.com/linq2db/linq2db/issues/1684))

##### SQLite
- added support for `Sql.DateDiff`/`Sql.DateAdd()` extensions ([#1688](https://github.com/linq2db/linq2db/issues/1688))

###### Full-text search support ([#1649](https://github.com/linq2db/linq2db/issues/1649))
Added the following extension methods for `FTS3/4` and `FTS5` full-text extensions:

```cs
// MATCH against table or column
Sql.Ext.SQLite().Match(entityOrColumn, search);
Sql.Ext.SQLite().MatchTable(table, search);

// hidden columns
Sql.Ext.SQLite().RowId(table);
Sql.Ext.SQLite().Rank(table);

// FTS3/4 functions
Sql.Ext.SQLite().FTS3Offsets(table);
Sql.Ext.SQLite().FTS3MatchInfo(table);
Sql.Ext.SQLite().FTS3MatchInfo(table, format);
Sql.Ext.SQLite().FTS3Snippet(table);
Sql.Ext.SQLite().FTS3Snippet(table, startMatch);
Sql.Ext.SQLite().FTS3Snippet(table, startMatch, endMatch);
Sql.Ext.SQLite().FTS3Snippet(table, startMatch, endMatch, ellipses);
Sql.Ext.SQLite().FTS3Snippet(table, startMatch, endMatch, ellipses, columnIndex);
Sql.Ext.SQLite().FTS3Snippet(table, startMatch, endMatch, ellipses, columnIndex
                                                                  , tokensNumber);

// FTS5 functions
Sql.Ext.SQLite().FTS5bm25(table);
Sql.Ext.SQLite().FTS5bm25(table, params[] weights);
Sql.Ext.SQLite().FTS5Highlight(table, columnIndex, startMatch, endMatch);
Sql.Ext.SQLite().FTS5Snippet(table, columnIndex, startMatch, endMatch, ellipses
                                                                     , tokensNumber);

// FTS3/4 commands (extension methods for DataConnection)
dc.FTS3Optimize(table);
dc.FTS3Rebuild(table);
dc.FTS3IntegrityCheck(table);
dc.FTS3Merge(table, blocks, segments);
dc.FTS3AutoMerge(table, segments);

// FTS5 commands (extension methods for DataConnection)
dc.FTS5AutoMerge(table, value);
dc.FTS5CrisisMerge(table, value);
dc.FTS5Delete(table, rowid, record);
dc.FTS5DeleteAll(table);
dc.FTS5IntegrityCheck(table);
dc.FTS5Merge(table, value);
dc.FTS5Optimize(table);
dc.FTS5Pgsz(table, value);
dc.FTS5Rank(table, function);
dc.FTS5Rebuild(table);
dc.FTS5UserMerge(table, value);
```

Check [tests](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/Linq/FullTextTests.SQLite.cs) for use examples.

##### SQL Server
- fix issue with explicitly sizing `varchar` and `nvarchar` parameters as `varchar(8000)`/`nvarchar(4000)`, when non-string type used for value ([#1555](https://github.com/linq2db/linq2db/issues/1555))
- added new provider version to support `SQL Server 2017+`-specific functionality. Right now it used only for `CONCAT_WS`/`STRING_AGG` functions/aggregates, but later we can add more new features. To switch to new version, just change provider version to `2017`, if you specified version explicitly in connection configuration. If you used version autodetect, it will switch to the new provider version automatically if your database runs in v2017+ compatibility mode ([#1683](https://github.com/linq2db/linq2db/issues/1683))

###### `varchar`, `nvarchar`, `varbinary` parameter size improvements ([#1578](https://github.com/linq2db/linq2db/issues/1578), [#1628](https://github.com/linq2db/linq2db/issues/1628))
Previously `linq2db` was using the following logic for `varchar`/`nvarchar` parameters size configuration: if value size is not bigger than max configurable size (8000 for `varchar` and 4000 for `nvarchar`), we were setting parameter size to `8000`/`4000` respectively to reduce number of cached query plans, otherwize `Size=-1` used.

With this release we made following changes to it:
- added `varbinary` type support
- if mapping for corresponding column has length specified, `linq2db` will use it instead of `4000`/`8000` values.

###### Full-text search support ([#1649](https://github.com/linq2db/linq2db/issues/1649))
Added the following extension methods for full-text search support:

```cs
// FREETEXTTABLE(table, *, ...)
Sql.Ext.SqlServer().FreeTextTable(table, search);
Sql.Ext.SqlServer().FreeTextTable(table, search, top);
Sql.Ext.SqlServer().FreeTextTableWithLanguage(table, search, top, language_name);
Sql.Ext.SqlServer().FreeTextTableWithLanguage(table, search, top, language_name, top);
Sql.Ext.SqlServer().FreeTextTableWithLanguage(table, search, top, language_code);
Sql.Ext.SqlServer().FreeTextTableWithLanguage(table, search, top, language_code, top);

// FREETEXTTABLE(table, (columns), ...)
Sql.Ext.SqlServer().FreeTextTable(table, columns_expr, search);
Sql.Ext.SqlServer().FreeTextTable(table, columns_expr, search, top);
Sql.Ext.SqlServer().FreeTextTableWithLanguage(table, columns_expr, search, top
                                                   , language_name);
Sql.Ext.SqlServer().FreeTextTableWithLanguage(table,, columns_expr search, top
                                                   , language_name, top);
Sql.Ext.SqlServer().FreeTextTableWithLanguage(table, columns_expr, search, top
                                                   , language_code);
Sql.Ext.SqlServer().FreeTextTableWithLanguage(table, columns_expr, search, top
                                                   , language_code, top);

// CONTAINSTABLE(table, *, ...)
Sql.Ext.SqlServer().ContainsTable(table, search);
Sql.Ext.SqlServer().ContainsTable(table, search, top);
Sql.Ext.SqlServer().ContainsTableWithLanguage(table, search, top, language_name);
Sql.Ext.SqlServer().ContainsTableWithLanguage(table, search, top, language_name, top);
Sql.Ext.SqlServer().ContainsTableWithLanguage(table, search, top, language_code);
Sql.Ext.SqlServer().ContainsTableWithLanguage(table, search, top, language_code, top);

// CONTAINSTABLE(table, (columns), ...)
Sql.Ext.SqlServer().ContainsTable(table, columns_expr, search);
Sql.Ext.SqlServer().ContainsTable(table, columns_expr, search, top);
Sql.Ext.SqlServer().ContainsTableWithLanguage(table, columns_expr, search, top
                                                   , language_name);
Sql.Ext.SqlServer().ContainsTableWithLanguage(table,, columns_expr search, top
                                                   , language_name, top);
Sql.Ext.SqlServer().ContainsTableWithLanguage(table, columns_expr, search, top
                                                   , language_code);
Sql.Ext.SqlServer().ContainsTableWithLanguage(table, columns_expr, search, top
                                                   , language_code, top);

// FREETEXT(table.*, ..)
Sql.Ext.SqlServer().FreeText(table, search);
Sql.Ext.SqlServer().FreeTextWithLanguage(table, search, language_name);
Sql.Ext.SqlServer().FreeTextWithLanguage(table, search, language_code);

// FREETEXT((columns), ..)
Sql.Ext.SqlServer().FreeText(table, search, param[] columns);
Sql.Ext.SqlServer().FreeTextWithLanguage(table, search, language_name
                                                      , param[] columns);
Sql.Ext.SqlServer().FreeTextWithLanguage(table, search, language_code
                                                      , param[] columns);

// CONTAINS(table.*, ..)
Sql.Ext.SqlServer().Contains(table, search);
Sql.Ext.SqlServer().ContainsWithLanguage(table, search, language_name);
Sql.Ext.SqlServer().ContainsWithLanguage(table, search, language_code);

// CONTAINS((columns), ..)
Sql.Ext.SqlServer().Contains(table, search, param[] columns);
Sql.Ext.SqlServer().ContainsWithLanguage(table, search, language_name
                                                      , param[] columns);
Sql.Ext.SqlServer().ContainsWithLanguage(table, search, language_code
                                                      , param[] columns);

// CONTAINS(PROPERTY())
Sql.Ext.SqlServer().ContainsProperty(column, property, search);
Sql.Ext.SqlServer().ContainsPropertyWithLanguage(column, property, search
                                                       , language_name);
Sql.Ext.SqlServer().ContainsPropertyWithLanguage(column, property, search
                                                       , language_code);
```

Check [tests](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/Linq/FullTextTests.SqlServer.cs) for use examples.

###### Obsolete old full-text search functionality
With adding full support for all full-text search functions and predicates, we have decided to obsolete the following functionality and highly recommend to migrate to new extensions:
- `FreeTextTableExpressionAttribute` now obsoleted. We recommend switching to `Sql.Ext.SqlServer().FreeTextTable*` functions as they do not require the user to define special table access method and support all `FREETEXTTABLE` function options. Also note that this attribute is broken for some queries: [#386](https://github.com/linq2db/linq2db/issues/386);
- `Sql.FreeText()` function is now obsoleted. We recommend switching to the new `Sql.Ext.SqlServer().FreeText/FreeTextWithLanguage` functions with full support for all `FREETEXT` predecate options;
- T4 SqlServer-specific option `GenerateSqlServerFreeText` changed defaults from to 'false`, as with new extensions you don't need to generate special support for full-text search tables. This option is still here for compatibility reasons.

***

### Release 2.6.4

Bug-fix release.
- fixed `NotImplementedException` from `SelectContext.ConvertToIndexInternal` on some complex queries with record comparison ([#1569](https://github.com/linq2db/linq2db/issues/1569))
- fixed incorrect `SQL` generation for conditions like `!(field == null)`
- [DB2, Schema] fixed schema provider issue with foreign keys load
- removed `System.ValueTuple` dependency, added with `npgsql4` support changes in `2.6.0`
- [Oracle] improved generated SQL for `DropTable()` API for Oracle
- fixed `NullReferenceException` when using associations in join conditions ([#1556](https://github.com/linq2db/linq2db/issues/1556))
- fixed support for joins of `UNION` subqueries ([#1397](https://github.com/linq2db/linq2db/issues/1397))
- fixed incorrect optimization of `APPLY` joins ([#1560](https://github.com/linq2db/linq2db/issues/1560))
- fixed issues with custom aggregates (required for [#1559](https://github.com/linq2db/linq2db/issues/1559))
- fixed support for async compiled queries ([#1472](https://github.com/linq2db/linq2db/issues/1472))
- fixed support for expressions as parameters for `FromSql(string format, params object[] parameters)` override (already works properly for interpolated string override) ([#1566](https://github.com/linq2db/linq2db/issues/1566))
- [T4] add `#pragma warning disable 1591` to generated files to avoid `CS1591 Missing XML comment for publicly visible type or member` warnings from generated code ([#1551](https://github.com/linq2db/linq2db/issues/1551))
- [T4] moved `BeforeGenerateModel` method call after header comment generation, to have header comment on top of file when `BeforeGenerateModel` performs code generation ([#1550](https://github.com/linq2db/linq2db/issues/1550))
- added extention point, required for better `EF.Core` integration ([#1573](https://github.com/linq2db/linq2db/issues/1573))
- [PostgreSQL] `DateTime` parameters with `Kind == DateTimeKind.Utc` will be sent to server with `timestamptz` type ([#1573](https://github.com/linq2db/linq2db/issues/1573))
- [PostgreSQL] initial support for `range` type ([#1573](https://github.com/linq2db/linq2db/issues/1573))

***

### Release 2.6.3

Bug-fix release.
- [T4] don't prefix table mappings with schema name for default schema and add option to disable prefixing for non-default schemas ([#1515](https://github.com/linq2db/linq2db/issues/1515))
- [T4] use proper escaping logic for C# string literals generation ([#1516](https://github.com/linq2db/linq2db/issues/1516))
- [SAP HANA] fixed `Parameter/Column (N) not bound` error for queries with same parameter used multiple times ([#1528](https://github.com/linq2db/linq2db/issues/1528))
- [BulkCopy] Don't update `BulkCopyOptions` instance, passed by user to `Linq To DB`, to avoid issues when it reused for multiple calls ([#1541](https://github.com/linq2db/linq2db/issues/1541))
- [T4] Added back old pluralization logic, replaced with fix for [#1439](https://github.com/linq2db/linq2db/issues/1439) in `release 2.6.0` ([#1520](https://github.com/linq2db/linq2db/issues/1520)). To enable old logic, add following code to your `T4` template:
```cs
ToPlural   = Pluralization.ToPluralVersion1;
ToSingular = Pluralization.ToSingularVersion1;
```
- [T4] Added back `Type` property to `T4` model `MemberBase` class, replaced with `TypeBuilder` delegate in `release 2.6.0` to support existing custom user's templates without rewrite to `TypeBuilder` and to support configuring type using string as before, when type is static and cannot change during generation process ([#1534](https://github.com/linq2db/linq2db/issues/1534))
- [Informix] Improve provider support by ignoring absense of `GetBigInt` and `GetIfxTimeSpan` methods in some provider versions ([#1520](https://github.com/linq2db/linq2db/issues/1520))
- [Fluent Mapping] Fixed expression properties, defined for members of base class ([#1543](https://github.com/linq2db/linq2db/issues/1543))
- [Async] Add `BeginTransactionAsync` methods. Currently it will only open connection in async manner if required ([#1497](https://github.com/linq2db/linq2db/issues/1497)). For next release we will bring better async support for transaction API for providers that support such API. More details could be found [here](https://github.com/linq2db/linq2db/issues/1540)
- [T4][Refactoring] `InitMappingSchema` method generated as partial method ([#1537](https://github.com/linq2db/linq2db/issues/1537))
- [LINQ] Fixed issues with association queries loading with `LoadWith` when hints applied to source table ([#1523](https://github.com/linq2db/linq2db/issues/1523))
- [Dynamic] Fix support for dynamic columns in update/insert setters ([#1529](https://github.com/linq2db/linq2db/issues/1529))
- [Mapping] Fix mapping to complex properties for non-linq queries ([#1524](https://github.com/linq2db/linq2db/issues/1524))
- fixed race conditions in connection creation
- improved handling of `throwExceptionIfNotExists=false` parameter in `DropTable` methods to execute SQL with schema check instead of `DROP TABLE` with exception catching for some providers (DB2) and plain `DROP TABLE` when `throwExceptionIfNotExists=true`. Partially fixes [#798](https://github.com/linq2db/linq2db/issues/798)

***

### Release 2.6.2

Bug-fix release.
- [CTE] fixed regression in 2.5.1+ causing `NotImplementedException` exception for recursive `CTE` queries ([#1492](https://github.com/linq2db/linq2db/issues/1492))
- [T4] fixed regression in 2.6.1 causing generation of `Find` methods names with counter at the end ([#1490](https://github.com/linq2db/linq2db/issues/1490))
- [Schema/T4] fixed issue with Access reporting `bit` columns as identity fields, which led to incorrect mappings generated by `T4` for such columns ([#1485](https://github.com/linq2db/linq2db/issues/1485))
- [T4] added new `T4` option `GenerateProcedureResultAsList` (false by default to preserve existing behavior) to generate procedure methods with `List<T>` result instead of IEnumerable<T>. Note that it will lead to load of all data into list on method call and could lead to performance issues on big datasets ([#1489](https://github.com/linq2db/linq2db/issues/1489), [#1505](https://github.com/linq2db/linq2db/issues/1505))
- improved `AllowMultipleQueries` option support for situations, when you need to pass existing `IDbConnection` to `DataConnection` constructor. Now it will also work for `sqlce` and Microsoft's `sqlite` providers ([#1486](https://github.com/linq2db/linq2db/issues/1486))
- added `DataConnection` constructors that accept `IDbConnection` factory method. This will also fix lack of support for `AllowMultipleQueries=true` option with external `IDbConnection` scenarios for `SAP HANA`, `Oracle` and `MySqlConnector` (will be shipped in v2.7.0) providers ([#1486](https://github.com/linq2db/linq2db/issues/1486))
- fixed regression in 2.6.0 causing `TypeLoadException` exception for Xamarin projects ([#1487](https://github.com/linq2db/linq2db/issues/1487))
- fixed predicate/query associations support for `AllowMultipleQueries=true` option ([#1498](https://github.com/linq2db/linq2db/issues/1498))
- added more `Association` methods to fluent mapping builder to support all association types on both entity and property builders ([#1499](https://github.com/linq2db/linq2db/issues/1499))

***

### Release 2.6.1

Bug-fix release.
- fixed issue with async queries to `MySQL` using `netstandard1.x` targets not closing data reader properly due to incorrect disposal implementation by provider for that target (all `MySql.Data` provider versions)
- `IDataContext` non-linq extension methods `Insert`, `Delete`, `InsertOrReplace`, `InsertWithIdentity`, `Update` and their variations will respect `Common.Configuration.Linq.DisableQueryCache` option ([#1474](https://github.com/linq2db/linq2db/issues/1474))
- [Mapping] assign unique configuration name to new `MappingSchema` object, if it is not specififed explicitly, to avoid situation, when new schema not used for query, if similar query exists in query cache ([#1471](https://github.com/linq2db/linq2db/issues/1471))
- fixed query caching issue with queries that aggregate other lambda expressions using `Compile` method ([#1469](https://github.com/linq2db/linq2db/issues/1469))
- [T4] fixed generation of procedures for non-default schemas with `GenerateSchemaAsType = true;` option set ([#1479](https://github.com/linq2db/linq2db/issues/1479))
- [T4] fixed generation of one-to-many extension methods with `GenerateAssociationExtensions = true;` option set ([#1482](https://github.com/linq2db/linq2db/issues/1482))
- [T4] fixed duplicate names of members in `TableExtensions` class ([#1482](https://github.com/linq2db/linq2db/issues/1482))
- [T4] fixed connection leak in T4 templates

***

### Release 2.6.0

#### Breaking changes
This release will introduce minor breaking changes for those, who implement custom database/schema providers or inherit from them to override existing functionality. Main goal of those changes is to improve type system to support more fine-grained mappings and implement new features, that cannot be implemented with old types. More details could be found [here](#type-system-refactoring-1263) and [here](#table-valued-parameters-support-332-1034-1451-1459).

#### Type system refactoring ([#1263](https://github.com/linq2db/linq2db/issues/1263))
This feature made changes to `Linq To DB` type-system by introducing new type descriptor class `DbDataType(Type, DataType, string dbType)` and use it in places, where only one of those type components used before or all components used without aggregation into single object.

List of changes and additions to public interfaces (don't include changes to query AST):
- added new field `string DataParameter.DbType`. Having database type name information for parameters allows us to support features like table-valued parameters for SQL Server that require UDT type name provided or hint parameter type in places, where database have difficulties to infer type
- `IDataProvider.SetParameter` `dataType` parameter changed type from `DataType` to new type `DbDataType`
- `IDataProvider.ConvertParameterType` `dataType` parameter changed type from `DataType` to new type `DbDataType`
- `DataProviderBase.SetParameterType` `dataType` parameter changed type from `DataType` to new type `DbDataType`
- `BasicMergeBuilder.AddSourceValueAsParameter` accepts additional `string dbType` parameter
- new overrides with `DbDataType` support added to `MappingSchema`: `GetConvertExpression`, `SetConvertExpression`, `SetConverter`

#### Packaging
- added missing `MSBUILD` namepace to props files to fix Visual Studio 2015 compatibility ([#1401](https://github.com/linq2db/linq2db/issues/1401))

#### LINQ
- fixed issue with handling of `Count` function in subqueries ([#873](https://github.com/linq2db/linq2db/issues/873))
- fixed issues in `CTE` and `UNION` queries with selects into wrapper objects ([#1435](https://github.com/linq2db/linq2db/issues/1435))
- added initial support for client-side collections in queries ([#957](https://github.com/linq2db/linq2db/issues/957), [#1415](https://github.com/linq2db/linq2db/issues/1415)). For now implementation limited to collection of scalar values. Full support planned for version 3
- added calls of `LinqExtensions.ProcessSourceQueryable` query preprocessing extension point to some missing places ([#1449](https://github.com/linq2db/linq2db/issues/1449))

##### Raw SQL support in Linq queries ([#1388](https://github.com/linq2db/linq2db/issues/1388))

This feature adds support for raw SQL queries use in Linq queries. API is available as a set of `FromSql<TResult>` extension methods that accept raw SQL string with or without parameters. Both `string.Format`-like `{N}` and interpolated string parameter placeholders supported. Note that query should return columns with names, matching mapping information from mapped entity `TResult`.

To support interpolated string templates with .net framework, new `net46` build target was added.

Note: right now, `TResult` cannot be scalar value and for single-column queries, you need to use wrapping class with one column. This limitation will be removed in future.

###### Example

```cs
int startId = 5;
int endId   = 15;

// using string.Format-like templates
var result1 = db.FromSql<SampleClass>(@"
SELECT *
    FROM sample_class
    WHERE id >= {0} and id < {1}",
        new DataParameter("startId", startId, DataType.Int64),
        endId)
    .Where(c => c.Id > 10)
    .Select(c => new { c.Value, c.Id })
    .ToArray();

// using interpolated string template
var result2 = db.FromSql<SampleClass>($@"
SELECT *
    FROM sample_class
    WHERE id >= {new DataParameter("startId", startId, DataType.Int64)}
        and id < {endId}")
    .Where(c => c.Id > 10)
    .Select(c => new { c.Value, c.Id })
    .ToArray();
```

Note: queries, starting from `SELECT` word, will be wrapped into parentheses in resulting SQL. If you need parentheses in other cases, you must add them to your SQL.

#### SQL

##### Human-readable aliases support ([#1405](https://github.com/linq2db/linq2db/issues/1405))

This feature replaces autogenerated aliases like `c1`, `t1`, etc with aliases, based on aliased entity name to improve readability of generated SQL.

Also it adds couple of options to control this functionality:
- `LinqToDB.Common.Configuration.Sql.AssociationAlias` (default value: `"a_{0}"`) - defines template for joined association alias, where {0} is a placeholder for association name. E.g. for association `Customers` alias will look like `a_Customers`;
- `LinqToDB.Common.Configuration.Sql.GenerateFinalAliases` (default value: `false`) - enables or disables column aliases for top-level select statement. Aliases generated using name of member, to which specific column value should be mapped;
- `AssociationAttribute.AliasName` this new property allows you to set association name for sql alias generation. By default member name is used.

Example:

```cs
var query = from child in db.Child
    select new
    {
        Child = child.ChildID,
        Counter = child.Parent.Value1
    };
```

SQL (comments are not a part of generated SQL):
```sql
SELECT
    -- "GenerateFinalAliases = true" enables generation of alias
    -- based on name of field in C# code (Child, Counter)
    [child].[ChildID] as [Child],
    [a_Parent].[Value1] as [Counter]
FROM
    -- in previous version table alias will look like [t1]
    [Child] [child]
        -- here you can see alias for association Parent
        LEFT JOIN [Parent] [a_Parent] ON ([child].[ParentID] = [a_Parent].[ParentID])
```

#### Mappings

- fixed issue when selection of mapped entity without explicit list of selected fields fails if you have column order configured in your mapping for this entity ([#1403](https://github.com/linq2db/linq2db/issues/1403))
- added support for `CreateTable` column type inferrence from `DataParameter` converter ([#1032](https://github.com/linq2db/linq2db/issues/1032))
- added support for procedures, that could return table data with different shape (e.g. based on input parameter value) ([#1423](https://github.com/linq2db/linq2db/issues/1423))
- more fine-grained mapping configuration support. For more details check [this](#type-system-refactoring-1263) ([#1219](https://github.com/linq2db/linq2db/issues/1219))

##### Aliases and calculated fields support in fluent mapper ([#1365](https://github.com/linq2db/linq2db/issues/1365), [#1389](https://github.com/linq2db/linq2db/issues/1389))
With this change you can easily configure aliases, expression properties and calculated properties using fluent mapper (attribute-based mapper already had support for it):
- lias means that some member is just an alias for another existing member in your mapping and when you query database using alias, `Linq To DB` should use member, referenced by alias.
- expression property is a property, defined as some expression. When such property used in query, `Linq To DB` replace it with configured expression
- calculated property is an expression property, that additionally filled in with value on entity materialization.

```cs
interface IEntity
{
    int    Id             { get; set; }
    int    Value          { get; set; }
    string ValueWithId    { get; set; }
    string ValueWithoutId { get; set; }
}

class Entity : IEntity
{
    int    Id              { get; set; }
    int    Value           { get; set; }
    string ValueWithId     => Id + Value; // calculated property in C#
    string ValueWithoutId  { get; set; }

    public int    EntityId { get => Id; set => Id = value; }
}

MappingSchema.Default.GetFluentMappingBuilder().Entity<Entity>()
    .Property(e => e.Id) // column Id
    .Property(e => e.Value) // column Value
     // Alias: EntityId is just an alias for Id property
    .Member(e => e.EntityId).IsAlias(e => e.Id)
     // Calculated property
     // in generated SQL will be replaced with "t.Id + t.Value"
    .Member(e => e.ValueWithId).IsExpression(e => e.Id + e.Value)
     // Materialized calculated property
     // in generated SQL will be replaced with "t.Value - t.Id"
     // during materialization, will select value of expression into property
    .Member(e => e.ValueWithId).IsExpression(e => e.Value - e.Id, true);
```

List of new APIs:
###### `EntityMappingBuilder`
- `Member(Expression propertyOrFieldSelector)` - defines mapping member of entity
###### `PropertyMappingBuilder`
- `Member(Expression propertyOrFieldSelector)` - defines mapping member of entity
- `IsAlias(Expression aliasedMemberSelector)` - marks mapping member as alias using expression
- `IsAlias(string aliasedMember)` - marks mapping member as alias using target member name
- `IsExpression(Expression expression, bool materialized = false)` - marks mapping member as calculated property using expression (with or without materialization)

##### Associations to queries support ([#1457](https://github.com/linq2db/linq2db/issues/1457), [#1465](https://github.com/linq2db/linq2db/issues/1465))

This feature allows to define association as join to a custom query or even table function and available using both atrribute and fluent mappings:
```cs
[Table]
public class SomeEntity
{
   [Column]
   public int Id { get; set; }

   [Column]
   public string OwnerStr { get; set; }

   [Association(QueryExpressionMethod = nameof(OtherImpl), CanBeNull = true)]
   public SomeOtherEntity Other { get; set; }

   // new QueryExpressionMethod property allow
   // us to specify source of linq query to join with
   [Association(QueryExpressionMethod = nameof(OtherImpl), CanBeNull = true)]
   public List<SomeOtherEntity> Others { get; set; } = new List<SomeOtherEntity>();

   // query method should return expression with 2 input parameters:
   // - current entity,
   // - data context;
   // and return value of IQueryable<QUERY_RESULT_MAPPING> type
   private static
   Expression<Func<SomeEntity, IDataContext, IQueryable<SomeOtherEntity>>> OtherImpl()
   {
      return (e, db) => db.GetTable<SomeOtherEntity>().Where(se => se.Id == e.Id)
         .Select(o => new SomeOtherEntity { Id = o.Id, StrValue = o.StrValue + "_A" })
         .Take(1);
   }
}

// or using fluent mapping
db.MappingSchema.GetFluentMappingBuilder()
    .Entity<SomeEntity>()
    .Association(
        e => Other,
        (e, db) => db.GetTable<SomeOtherEntity>().Where(se => se.Id == e.Id)
         .Select(o => new SomeOtherEntity { Id = o.Id, StrValue = o.StrValue + "_A" })
         .Take(1));
```

#### Merge API
- fixed issue where `Linq To DB` could use parameter value from parallel `MERGE` query, if same query (with different parameters) executed in parallel on another thread ([#1398](https://github.com/linq2db/linq2db/issues/1398))

#### T4 Models
- fixed issue when classes with the same name generated for tables with same name but different schema and `GenerateSchemaAsType` option set to `false` ([#1425](https://github.com/linq2db/linq2db/issues/1425))
- fixed issues with last word detection for pluralization logic, when detected "word" could contain non-letter characters ([#1439](https://github.com/linq2db/linq2db/issues/1439))
- fixed casing of pluralized upper-cased words, when word remained pluralized with lower-cased plural suffix (e.g. `TABLE` -> `TABLEs` instead of proper `Tables`) ([#1433](https://github.com/linq2db/linq2db/issues/1433))
- `linq2db.t4models` package now contains `tt` templates for all supported databases and can generate models for them ([#1433](https://github.com/linq2db/linq2db/issues/1433))
- T4 [documentation](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB.Templates/README.md) reviewed to include all available flags and provide more clear descriptions for them ([#1433](https://github.com/linq2db/linq2db/issues/1433))
- fixed procedure method name generation producing non-valid C# method name if procedure name contains unsupported characters ([#1433](https://github.com/linq2db/linq2db/issues/1433))
- fixed invalid C# generation for procedures with output-only parameters and table results ([#1433](https://github.com/linq2db/linq2db/issues/1433))

##### T4 generator refactoring ([#1433](https://github.com/linq2db/linq2db/issues/1433))

T4 model generator implementation refactored to use lazy/delayed generation approach. This allowed to fix multiple issues, when model builder was renaming mapping classes to avoid name clashes, but references to those classes still used original name. That resulted in generation of non-compilable code.

#### Other changes

- `Query.CleanCaches()` will also clean caches for `IDataContext` non-linq extension methods: `Insert`, `Delete`, `InsertOrReplace`, `InsertWithIdentity`, `Update` and their variations ([#1428](https://github.com/linq2db/linq2db/issues/1428))
- `CreateTable` will throw `LinqToDBException("Database type cannot be determined automatically and must be specified explicitly")` when it is unable to generate database type for column instead of generating `UNKNOWN` type and fail on query execution ([#1428](https://github.com/linq2db/linq2db/issues/1428))
- `CreateTable` will generate `db_text_type(max_supported_value)` instead of `db_text_type`/`db_text_type(invalid_value)` if column length is not specified or outside of supported range ([#1428](https://github.com/linq2db/linq2db/issues/1428)). See provider-specific notes to see if it applies to your provider.
- fixed async read operations to use `CommandBehavior`, provided by data provider instead of hardcoded `CommandBehavior.Default` value ([#1448](https://github.com/linq2db/linq2db/issues/1448))
- new build target `net46` added to support interpolated string override for `FromSql` API with .net framework ([#1388](https://github.com/linq2db/linq2db/issues/1388))
- updated mapper to throw `LinqToDBConvertException` with `ColumnName` property set on error to help diagnose mapping errors ([#1417](https://github.com/linq2db/linq2db/issues/1417))

#### Provider-specific changes

##### Access
- added missed `DateTime?` mapping configuration ([#1428](https://github.com/linq2db/linq2db/issues/1428))

##### DB2
- added support for installed provider discovery using `DbProviderFactories` ([#1385](https://github.com/linq2db/linq2db/issues/1385))
- CreateTable will use `nvarchar(8168)` type for columns with unknown or out-of-range length ([#1428](https://github.com/linq2db/linq2db/issues/1428))

##### Informix
- added support for installed provider discovery using `DbProviderFactories` ([#1385](https://github.com/linq2db/linq2db/issues/1385))
- CreateTable will use `nvarchar(255)` type for columns with unknown or out-of-range length ([#1428](https://github.com/linq2db/linq2db/issues/1428))

##### Firebird
- added default support for `System.Boolean` in `CreateTable`. By default it is mapped to `CHAR` type to support Firebird versions without `BOOLEAN` type support ([#1428](https://github.com/linq2db/linq2db/issues/1428))
- CreateTable will use `VARCHAR(10921) CHARACTER SET UNICODE_FSS` type for columns with unknown or out-of-range length ([#1428](https://github.com/linq2db/linq2db/issues/1428))

##### MySQL
- CreateTable will use `char(255)` type for columns with unknown or out-of-range length ([#1428](https://github.com/linq2db/linq2db/issues/1428))

##### Oracle
- added missed `Guid?` mapping configuration ([#1428](https://github.com/linq2db/linq2db/issues/1428))
- CreateTable will use `varchar2(4000)` type for columns with unknown or out-of-range length ([#1428](https://github.com/linq2db/linq2db/issues/1428))

##### PostgreSQL
- fixed issue with `CreateTable` failing to generate proper column type for enum property, mapped to string, without explicit `DbType` or `Length` ([#1394](https://github.com/linq2db/linq2db/issues/1394))
- fixed long-standing issue with unusable `PostgreSQL` provider when `Configuration.AvoidSpecificDataProviderAPI` set to `true` or connection wrappers like [MiniProfiler](https://github.com/MiniProfiler/dotnet) used ([#360](https://github.com/linq2db/linq2db/issues/360), [#942](https://github.com/linq2db/linq2db/issues/942), [#1438](https://github.com/linq2db/linq2db/issues/1438))
- `npgsql4` compatibility fix: read of fixed-length column values wasn't trimming trailing spaces ([#1436](https://github.com/linq2db/linq2db/issues/1436))
- `npgsql4` compatibility fix: fixed `NpgsqlInet` type mapping ([#1436](https://github.com/linq2db/linq2db/issues/1436))
- `npgsql4` compatibility fix: fixed `character(1)` columns generated as `string` instead of `char` by T4 templates ([#1436](https://github.com/linq2db/linq2db/issues/1436))
- fixed generation of columns with `NpgsqlDateTime` instead of `DateTimeOffset` by T4 templates for `timestamp`/`timestamptz` columns ([#1436](https://github.com/linq2db/linq2db/issues/1436))
- fixed exception reading procedures schema for PostgreSQL 11 ([#1427](https://github.com/linq2db/linq2db/issues/1427))
- fixed incorrect type name in types normalization code from `varchar varying` to `character varying` ([#1452](https://github.com/linq2db/linq2db/issues/1452))

##### SAP HANA
- CreateTable will use `nvarchar(5000)`/`varchar(5000)`/`varbinary(5000)` type for columns with unknown or out-of-range length ([#1428](https://github.com/linq2db/linq2db/issues/1428))
- fixed issue when incorrect identifier (e.g. table, view or procedure name) generated in query, if it contained dots ([#1462](https://github.com/linq2db/linq2db/issues/1462))

##### SAP/Sybase ASE
- `linq2db.Sybase.DataAction` nuget package `AdoNetCore.AseClient` dependency version bumped to `0.13.1`. This version contains fixes to remaining issues, discovered during `Linq To DB` testing with this provider.
- added support for installed provider discovery using `DbProviderFactories` ([#1385](https://github.com/linq2db/linq2db/issues/1385))
- CreateTable will use `nvarchar(5461)` type for columns with unknown or out-of-range length ([#1428](https://github.com/linq2db/linq2db/issues/1428))

##### SQL Server
- added missed `System.Data.SqlTypes.Sql*?` structs mapping configuration ([#1428](https://github.com/linq2db/linq2db/issues/1428))
- CreateTable will use `nvarchar(max)`/`varchar(max)`/`varbinary(max)` type for columns with unknown or out-of-range length ([#1428](https://github.com/linq2db/linq2db/issues/1428))
- fix `Find` method generation by T4 for tables with `hierarchyid` primary key fields ([#1115](https://github.com/linq2db/linq2db/issues/1115))
- improved identifiers quotation logic ([#1459](https://github.com/linq2db/linq2db/issues/1459))

###### Table-valued parameters support ([#332](https://github.com/linq2db/linq2db/issues/332), [#1034](https://github.com/linq2db/linq2db/issues/1034), [#1451](https://github.com/linq2db/linq2db/issues/1451), [#1459](https://github.com/linq2db/linq2db/issues/1459))
This feature adds support for table-valued parameters (TVP) in queries, procedure calls and T4 templates using parameters of `DataTable` or `IEnumerable<SqlDataRecord>` type.

Because TVP parameters must provide name of user-defined table type, you cannot use `DataTable` or `IEnumerable<SqlDataRecord>` values directly, and should wrap them into `DataParameter` type.

Example of stored procedure call:
```cs
// for procedure parameters you can skip type name
// because usually it could be taken from procedure definition
db.QueryProc<TVPRecord>(
    "ProcedureWithTVPParameter",
    new DataParameter("@param_name", value));

// or with DataType
db.QueryProc<TVPRecord>(
   "ProcedureWithTVPParameter",
   new DataParameter("@param_name", value, DataType.Structured));

// or also with udt type name
db.QueryProc<TVPRecord>(
   "ProcedureWithTVPParameter",
   new DataParameter("@param_name", value, DataType.Structured, "dbo.CustomType"));
```

Example of Linq query (note that we use another new feature from this release: [FromSql method](#raw-sql-support-in-linq-queries-1388)):
```cs
// TVP mapping class
public class TVPRecord
{
    public int?   Id   { get; set; }
    public string Name { get; set; }
}

// sample query (not very usefull, as we just roundtrip data to server and back)
// DataType could be omitted, but udt type name is required!
from record in
    db.FromSql<TVPRecord>(
        $"{new DataParameter("param_name", value, "dbo.CustomType")}")
    select new { record.Id, record.Name };
```

For full examples check tests [here](https://github.com/linq2db/linq2db/blob/master/Tests/Linq/DataProvider/SqlServerTypesTests.TVP.cs)

Public API changes, introduced by this feature:
- `SchemaProviderBase.GetDbType` method accepts 3 new parameters: `string udtCatalog, string udtSchema, string udtName`


##### SQL Sever CE
- added support for installed provider discovery using `DbProviderFactories` ([#1385](https://github.com/linq2db/linq2db/issues/1385))
- added missed `System.Data.SqlTypes.Sql*?` structs mapping configuration ([#1428](https://github.com/linq2db/linq2db/issues/1428))
- CreateTable will use `nvarchar(4000)` type for columns with unknown or out-of-range length ([#1428](https://github.com/linq2db/linq2db/issues/1428))

***

### Release 2.5.4

- proper fix for issue with `UNION` queries ([#1412](https://github.com/linq2db/linq2db/issues/1412)). Previous one didn't fixed it completely.

***

### Release 2.5.3

- contains another fix to `UNION` queries regression ([#1412](https://github.com/linq2db/linq2db/issues/1412))

***

### Release 2.5.2

- contains important fix to `UNION` queries regression, introduced in 2.4.0 ([#1347](https://github.com/linq2db/linq2db/issues/1347))

***

### Release 2.5.1

Small bugfix release to address:
- Several CTE issues, reported by users ([#1284](https://github.com/linq2db/linq2db/issues/1284), [#1380](https://github.com/linq2db/linq2db/issues/1380)).
- PostgreSQL procedure schema reader to work with npgsql 4.x ([#1382](https://github.com/linq2db/linq2db/issues/1382))
- Fix ODP.NET Oracle provider discovery from GAC ([#1384](https://github.com/linq2db/linq2db/issues/1384))

***

### Release 2.5.0

#### LINQ
- fixed `LinqException("Sequence '{0}' cannot be converted to SQL")` when LINQ expression use client-side methods with lambda parameters ([#1316](https://github.com/linq2db/linq2db/issues/1316))

##### Support for lambda expression parameters in query expressions
You can use lambda expression parameters for query composition in LINQ expressions using `Compile` method call ([#1343](https://github.com/linq2db/linq2db/issues/1343)).
```cs
public IQueryable<T> SampleQuery<T>(Expression<Func<T, bool>> filter)
{
    return from record in _db.GetTable<T>
        // add Compile() call here to make code compilable
        // and Linq To DB will interpret it correctly
        where filter.Compile()(record)
        select record;
}
```

#### SQL Generation
- fixed CTE name generation in expressions with multiple CTEs ([#1340](https://github.com/linq2db/linq2db/issues/1340))
- fixed incorrect CTE `RECURSIVE` keyword placing in expressions with multiple CTEs ([#1340](https://github.com/linq2db/linq2db/issues/1340))
- fixed issues with CTE ordering in expressions with multiple CTEs ([#1340](https://github.com/linq2db/linq2db/issues/1340))
- fixed invalid SQL generation for CTE combined with COUNT ([#1348](https://github.com/linq2db/linq2db/issues/1348))
- fixed incorrect detection of parameters in sub-queries ([#1363](https://github.com/linq2db/linq2db/issues/1363))
- fixed issue with recursive CTE with nesting ([#1361](https://github.com/linq2db/linq2db/issues/1361))

#### T4 and Schema
- fixed issue with incorrect name generation for generic classes and methods with duplicate names ([#1346](https://github.com/linq2db/linq2db/issues/1346))

#### Mappings
- fixed support of members with `new` modifier ([#1313](https://github.com/linq2db/linq2db/issues/1313))
- fixed missing `sbyte` type handling ([#1351](https://github.com/linq2db/linq2db/issues/1351))

#### Packaging
- minimal `System.Data.SqlClient` dependency bumped to `v4.5.1` ([#1342](https://github.com/linq2db/linq2db/issues/1342))

#### Other changes
- added `Completed` trace event to `DataReader`/`DataReaderAsync` ([#1358](https://github.com/linq2db/linq2db/issues/1358))
- added `StartTime` property to all trace events to provide operation start time in UTC ([#1368](https://github.com/linq2db/linq2db/issues/1368))
- added `ExecutionTime` property to all trace events except `BeforeExecute` event to provide elapsed time since operation start ([#1368](https://github.com/linq2db/linq2db/issues/1368))
- fixed trace event's `ExecutionTime` property having invalid value if DST shift occured between operation start and generated trace event ([#1368](https://github.com/linq2db/linq2db/issues/1368))
- updating `DataConnection.CommandTimeout` will update existing `CommandTimeout` for associated `DbCommand` instance ([#1354](https://github.com/linq2db/linq2db/issues/1354))
- added new events `DataConnection.OnConnectionOpened` and `DataConnection.OnConnectionOpenedAsync` ([#1372](https://github.com/linq2db/linq2db/issues/1372)). Those events triggered when database connection opened using synchonous or asynchronous API and could be used to perform some connection initialization logic. E.g. you can use it with SQLite to [configure encryption keys](https://www.bricelam.net/2016/06/13/sqlite-encryption.html)

#### Provider-specific changes

##### SQL CE
- add new option `SqlCeConfiguration.InlineFunctionParameters` to force function parameters inlining to support SQL CE 3.0 ([#1350](https://github.com/linq2db/linq2db/issues/1350))

##### SQL Server
- fixed issue with duplicate column entry in table schema when column participate in index and both index and column have description ([#1144](https://github.com/linq2db/linq2db/issues/1144))
- enabled UDT support for netstandard2.0 build ([#1349](https://github.com/linq2db/linq2db/issues/1349))

***

### Release 2.4.0

#### LINQ
- fixed `InvalidCastException` exception when `LinqExtensions` join methods builder tried to parse `Queryable` join methods ([#1209](https://github.com/linq2db/linq2db/issues/1209), [#1324](https://github.com/linq2db/linq2db/issues/1324))
- fixed exception on use of dynamic columns feature with `UNION` queries ([#1319](https://github.com/linq2db/linq2db/issues/1319))

#### SQL Generation
- add duplicate parameters detection for all providers except providers with positional parameters (Informix) ([#1315](https://github.com/linq2db/linq2db/issues/1315))
- fixed incorrect optimization of selected fields for `UNION` sub-queries ([#1222](https://github.com/linq2db/linq2db/issues/1222))
- fixed comparison of `Nullable<T>` properties with `null`
- fixed `IDataContext.InlineParameters = true` flag to be honored by `byte[]`-typed parameters (`System.Linq.Data.Binary`-typed parameters already worked) ([#1304](https://github.com/linq2db/linq2db/issues/1304))

#### T4
- fixed code generation for procedure/function with name, used by C# keyword ([#1295](https://github.com/linq2db/linq2db/issues/1295))
- fixed namespaces generation to take into account types, used by procedure/function parameters ([#1295](https://github.com/linq2db/linq2db/issues/1295))
- fixed `Sql.FunctionAttribute.Name` generation to include schema/owner if it is not default schema ([#1328](https://github.com/linq2db/linq2db/issues/1328))

#### Mappings

##### Table column ordering for create ([#1305](https://github.com/linq2db/linq2db/issues/1305))
This feature allows you to specify how columns will be ordered in `CREATE TABLE` statement.

To specify column order using mapping attributes you can use ColumnAttribute.Order property:
```cs
[Table]
public class Table
{
    [Column(Order = 1)]
    public int ID { get; set; }
```
For fluent mapping you should use `PropertyMappingBuilder.HasOrder(order)` method:
```cs
db.MappingSchema.GetFluentMappingBuilder()
    .Entity<Table>()
        .Property(t => t.ID)
            .HasOrder(1);
```

Columns ordered using following rules:
- first columns with non-negative order in ascending order
- second columns without order specified in arbitrary order
- last columns with negative order in ascending order

E.g.
```
Column order configuration:
Column1.Order = 1
Column2.Order = 10
Column3.Order = -1
Column4.Order = -10
Column5.Order = null (unspecified)

Order in CREATE TABLE:
Column1(1), Column2(10), Column5(unspecified), Column4(-10), Column5(-1)
```


#### Provider-specific changes

##### Access
- add support for binary literals for `byte[]` and `System.Linq.Data.Binary` types ([#1304](https://github.com/linq2db/linq2db/issues/1304))

##### DB2
- add support for binary literals for `byte[]` and `System.Linq.Data.Binary` types ([#1304](https://github.com/linq2db/linq2db/issues/1304))

##### Firebird
- add support for `byte[]` and `System.Linq.Data.Binary` fields in `CreateTable()` using `BLOB` type ([#1304](https://github.com/linq2db/linq2db/issues/1304))
- add support for binary literals for `byte[]` and `System.Linq.Data.Binary` types ([#1304](https://github.com/linq2db/linq2db/issues/1304))

##### MySQL/MariaDB
- add support for binary literals for `byte[]` and `System.Linq.Data.Binary` types ([#1304](https://github.com/linq2db/linq2db/issues/1304))

##### Oracle
- `linq2db.Oracle.Managed` nuget package dependency for `netstandard2.0` projects updated to release version (2.18.3) of `Oracle.ManagedDataAccess.Core` ([#1329](https://github.com/linq2db/linq2db/issues/1329))
- add support for binary literals for `byte[]` and `System.Linq.Data.Binary` types using `HEXTORAW` function ([#1304](https://github.com/linq2db/linq2db/issues/1304))

##### PostgreSQL
- add support for binary literals for `byte[]` and `System.Linq.Data.Binary` types ([#1304](https://github.com/linq2db/linq2db/issues/1304))
- fixed issue with generation of properties/parameters of `object` type instead of `Npgsql*Type` type by T4 model generator ([#1331](https://github.com/linq2db/linq2db/issues/1331))

##### SAP HANA
- add support for binary literals for `System.Linq.Data.Binary` type ([#1304](https://github.com/linq2db/linq2db/issues/1304))

##### SQL Sever CE
- add support for binary literals for `byte[]` and `System.Linq.Data.Binary` types ([#1304](https://github.com/linq2db/linq2db/issues/1304))

##### SQLite
- add support for binary literals for `byte[]` and `System.Linq.Data.Binary` types ([#1304](https://github.com/linq2db/linq2db/issues/1304))

##### Sybase/SAP ASE
- stopped generating `N` prefix for string literals as it doesn't have special meaning in ASE and conflicts with `u&` prefix ([#1300](https://github.com/linq2db/linq2db/issues/1300))
- `linq2db.Sybase.DataAction` nuget package `AdoNetCore.AseClient` dependency version bumped to `0.11.0`. We recommed to use this version (or higher) as it contains fixes to most of issues, discovered during `Linq To DB` testing with this provider (remaining issues will be fixed with 0.12 release).
- add support for binary literals for `byte[]` and `System.Linq.Data.Binary` types ([#1304](https://github.com/linq2db/linq2db/issues/1304))

***

### Release 2.3.0

#### LINQ
- fixed support for dynamic properties in DML operations ([#1268](https://github.com/linq2db/linq2db/issues/1268))
- enabled use of default value (`default(T)`) for dynamic property, if it doesn't have value in `DynamicColumnsStore` ([#1268](https://github.com/linq2db/linq2db/issues/1268))
- fixed handling of `char? Property == '<SOME_CHAR>'` pattern ([#1287](https://github.com/linq2db/linq2db/issues/1287))

#### SQL Generation
- fixed missing `IS (NOT) NULL` check in complex conditions over nullable fields ([#1261](https://github.com/linq2db/linq2db/issues/1261))
- added `CanBeNull` property to `Sql.ExtensionAttribute` to tell `Linq To DB` if it needs to generate extra check for `NULL` in comparison expressions. Default value: `true` ([#1292](https://github.com/linq2db/linq2db/issues/1292))

#### WCF Service
- fixed serialization for `double`/`decimal` nullable values to allways use invariant culture format ([#1290](https://github.com/linq2db/linq2db/issues/1290))
- apply provider's `ExecutionScope` method to WCF queries ([#1290](https://github.com/linq2db/linq2db/issues/1290))

#### T4
- updated T4 templates instructions to create new template file instead of copying of sample template, as it doesn't work with new project types ([#1194](https://github.com/linq2db/linq2db/issues/1194))
- added new nuget package `linq2db.SQLite.MS`, which references `Microsoft.Data.Sqlite.Core` package instead of `System.Data.SQLite.Core` ([#1175](https://github.com/linq2db/linq2db/issues/1175))

#### Mappings
- removed `sealed` keyword from `PrimaryKeyAttribute` and `NotNullAttribute` mapping attributes ([#1281](https://github.com/linq2db/linq2db/issues/1281))

#### Merge API

##### Merge hints support ([#1273](https://github.com/linq2db/linq2db/issues/1273))
Added `MERGE` statement hints support for providers, that support them: Informix, Oracle, MS SQL. Hints could be specified using new overrides to `Merge` and `MergeInto` methods with additional `string hint` parameter.

Below you can find examples how to use API and generated `SQL`.

###### Sql Server
Hint applied to merge target (OPTIONS clause still not supported). Example:
```cs
db.TargetTable
    .Merge("HOLDLOCK")
    .UsingTarget()
    .OnTargetKey()
    .UpdateWhenMatched()
    .Merge();
```
```sql
MERGE INTO [TargetTable] WITH(HOLDLOCK) [Target] ....
```

###### Oracle
```cs
db.TargetTable
    .Merge("append")
    .UsingTarget()
    .OnTargetKey()
    .UpdateWhenMatched()
    .Merge();
```
```sql
MERGE /*+ append */ INTO TestMerge1 Target ...
```

###### Informix
```cs
db.TargetTable
    .Merge("AVOID_STMT_CACHE")
    .UsingTarget()
    .OnTargetKey()
    .UpdateWhenMatched()
    .Merge();
```
```sql
MERGE {+ AVOID_STMT_CACHE } INTO TestMerge1 Target ...
```

#### Other changes and fixes
- added [SourceLink](https://github.com/dotnet/designs/blob/master/accepted/diagnostics/source-link.md) support ([#1217](https://github.com/linq2db/linq2db/issues/1217))
- `linq2db.t4models` nuget package updated to work with latest `linq2db` version ([#1251](https://github.com/linq2db/linq2db/issues/1251))

#### Provider-specific changes

##### Informix

###### Merge hints support ([#1273](https://github.com/linq2db/linq2db/issues/1273))
See description above in Merge API section.

###### New option to control `TO_DATE` fractional seconds formatting ([#1265](https://github.com/linq2db/linq2db/issues/1265))

Starting from Informix releases v11.70.xC8 and v12.10.xC2 IBM [changed](https://www.ibm.com/support/knowledgecenter/SSGU8G_12.1.0/com.ibm.po.doc/new_features_ce.htm#newxc2__xc2_datetime) [behavior](https://www.ibm.com/support/knowledgecenter/SSGU8G_11.70.0/com.ibm.po.doc/new_features.htm#xc8__xc8_datetime) of `%F` directive. To support both old and new behavior, new option was introduced:
```cs
// set it to true, if you use v11.70.xC8/v12.10.xC2
// or newer release on Informix database
LinqToDB.DataProvider.Informix
    .InformixConfiguration.ExplicitFractionalSecondsSeparator = false;
```
Having this option configured incorrectly will result in `ERROR [HY000] [Informix .NET provider][Informix]Missing decimal point in datetime or interval fraction.` error from Informix for queries with DateTime literals with fractional second values.

##### MS SQL
- merge hints support ([#1273](https://github.com/linq2db/linq2db/issues/1273)). See description above in Merge API 
section.
- improved dialect autodetection ([#1289](https://github.com/linq2db/linq2db/issues/1289), [linq2db.EntityFrameworkCore#7](https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/7))

##### Oracle
- merge hints support ([#1273](https://github.com/linq2db/linq2db/issues/1273)). See description above in Merge API section.

##### SQLite
- added new nuget package `linq2db.SQLite.MS`, which references `Microsoft.Data.Sqlite.Core` package instead of `System.Data.SQLite.Core` ([#1175](https://github.com/linq2db/linq2db/issues/1175))
- added workaround for bug in `Microsoft.Data.Sqlite` provider, when it saves char value as string with character code instead of just a string to database if you pass it as parameter ([#1279](https://github.com/linq2db/linq2db/issues/1279))

***

### Release 2.2.0

#### Documentation
- new article: [install `LinqToDB` using nuget](https://linq2db.github.io/articles/get-started/install/index.html)
- new article: [create .NET Framework application for existing database](https://linq2db.github.io/articles/get-started/full-dotnet/existing-db.html)

#### LINQ
- added support for null checks using ternary operator like `p ==/!= null ? p.SomeProperty : null` ([#1202](https://github.com/linq2db/linq2db/issues/1202))

    Note: you don't need to use this pattern in your code with `LinqToDB`.

   `p.SomeProperty`/`(StructType?)p.SomeStructProperty` will work just fine.
- added proper support of `Nullable<T>.HasValue` property ([#1213](https://github.com/linq2db/linq2db/issues/1213))
- added support for expressions that could return `DataParameter | null` type
- added support for `AsQueryable()` method use in queries, e.g. on `IGrouping<,>` value ([#1225](https://github.com/linq2db/linq2db/issues/1225))

#### SQL Generation
- fixed incorrect query generation for full outer join of sub-queries with filters when sub-query filter were moved to join condition ([#1210](https://github.com/linq2db/linq2db/issues/1210), [#426](https://github.com/linq2db/linq2db/issues/426), [#437](https://github.com/linq2db/linq2db/issues/437), [#1218](https://github.com/linq2db/linq2db/issues/1218))
- fixed incorrect comparison generation for nullable primary key columns in `MERGE` `ON` conditions for `InsertOrReplace`, `InsertOrUpdate` and Merge API's `OnTargetKey` methods ([#1238](https://github.com/linq2db/linq2db/issues/1238))

###### `ORDER BY` with `DISTINCT` support
Fixed bad `SQL` generation for queries with `ORDER BY` and `DISTINCT` when sorted column not selected by query for providers that doesn't support such queries ([#1221](https://github.com/linq2db/linq2db/issues/1221)).

Now it will convert query to use `GROUP BY` instead of `DISTINCT`. You can disable this functionality using following option:
```cs
// query transformation enabled by default
Configuration.Linq.KeepDistinctOrdered = false;
```
When you disable this option, `LinqToDB` will remove columns, not selected by query, from `ORDER BY` clause.

Affected providers:
- Access
- DB2
- Informix
- Oracle
- PostgreSQL
- SAP HANA
- SQL Server CE
- SQL Server
- Sybase/SAP ASE

#### T4 Templates
- fixed generation of classes and functions names for tables, views and functions with duplicate names. This could happen when you don't use `GenerateSchemaAsType = true;` generation option and generate objects for all schemas in single class ([#1191](https://github.com/linq2db/linq2db/issues/1191))
- fixed generation of function parameters and association properties names if they use C# keyword for name ([#1205](https://github.com/linq2db/linq2db/issues/1205))
- added initial support for custom aggregate functions generation ([#1184](https://github.com/linq2db/linq2db/issues/1184)). Right now only schema provider for `PostgreSQL` returns such functions. If you need this feature for some other provider, please create new feature request. In meantime, you can add aggregate function metadata to your T4 template manually
- fixed `GenerateSchemaAsType=true;` option to generate separate type for schema without tables or views (function-only schemas). Before fix, such functions were generated in class for default schema ([#1184](https://github.com/linq2db/linq2db/issues/1184))
- added new `InitMappingSchema` method to generated code. This method will contain required `MappingSchema` registration code. For now this method used only for `PostgreSQL` model generation, but later we can use it for other providers, so if you don't use generated constructors, you should add call to this function to your custom constructors ([#1184](https://github.com/linq2db/linq2db/issues/1184))
- added missing generation of `OUT` stored procedure parameters ([#1236](https://github.com/linq2db/linq2db/issues/1236))
- fixed generation of stored procedures with table result to initialize `OUT`/`INOUT` parameters with results after procedure call ([#1237](https://github.com/linq2db/linq2db/issues/1237)). Note that this will disable delayed execution of stored procedure till result enumeration.
- fixed function name incorrectly prefixed with dot (`.`) when function has default schema ([#1237](https://github.com/linq2db/linq2db/issues/1237))

#### Other changes and fixes
- `ITable<TEntity>` interface implementation improved to initialize it's properties (name, schema, database name) from mapping schema on creation
- fixed exception when async query used with `RetryPolicy` ([#1174](https://github.com/linq2db/linq2db/issues/1174))
- fixed binding of output parameters values for async call of stored procedure ([#1223](https://github.com/linq2db/linq2db/issues/1223))

#### Provider-specific changes

##### DB2
- improved support for client-side source columns that contain only `null` values in `Merge` ([#1239](https://github.com/linq2db/linq2db/issues/1239))
- specified default mappings to database types: `DataType.Boolean` -> `smallint`, `DataType.Guid` -> `char(16) for bit data` ([#1239](https://github.com/linq2db/linq2db/issues/1239))

##### MySQL/MariaDB
- fixed incorrect read of stored procedure metadata by SchemaProvider (with incorrect T4 model generated) if procedure has output parameters and doesn't return table results ([#1237](https://github.com/linq2db/linq2db/issues/1237))

##### Oracle
- `linq2db.Oracle.managed` NuGet package supports `netstandard2.0` using [Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core/) package ([#1156](https://github.com/linq2db/linq2db/issues/1156))

###### Lowercase identifiers quotation changes
Previous versions of `LinqToDB` didn't quoted identifiers if they contain lowercase letters. This is not correct, because unquoted identifiers converted by Oracle to upper-case. It was causing issues for users with quoted identifiers with lower-case letters ([#1243](https://github.com/linq2db/linq2db/issues/1243)).

Starting from this release we will add new option to switch to proper quotation mode:
```cs
OracleTools.DontEscapeLowercaseIdentifiers = true;
```
By default quotation mode set to `true` to support existing applications, but we recommend you to disable it and fix your mappings ASAP as we plan to remove this option in future releases.

Who will be affected by this change:
- if you use model, generated by T4 templates, you don't need to fix your mappings as T4 already generates model with uppercase identifiers
- if you defined your model manually, you need to check if identifiers in your mapping use lowercase letters and change them to uppercase

Example:
```cs
//[Table] // old mapping
[Table("TABLE1")] // fixed mapping
public class Table1
{
//    [Column] // old mapping
    [Column("ID")] // fixed mapping
    public int Id { get; set; }
}

//[Table("SomeTable")] // old mapping
[Table("SOMETABLE")] // fixed mapping
public class Table1
{
//    [Column("SomeColumn")] // old mapping
    [Column("SOMECOLUMN")] // fixed mapping
    public int Id { get; set; }
}

```

##### PostgreSQL

###### Functions support by SchemaProvider

[PR](https://github.com/linq2db/linq2db/pull/1184), issues [#1162](https://github.com/linq2db/linq2db/issues/1162), [#1220](https://github.com/linq2db/linq2db/issues/1220).

This feature adds support for functions and procedures description load to PostgreSQL schema provider and support for PostgreSQL functions generation by T4 model.

What it includes:
- loading metadata for functions and procedures by schema provider
- generation of functions by T4 model including functions with output parameters, void functions and aggregates (see details below)

What is not supported:
- procedures (`PostgreSQL` 11+) generation by T4 templates. It could work, but it wasn't tested as v11 is not released yet. Feel free to fill new issue if it doesn't work
- generation of functions with dynamic results by T4 like `json_to_record` or `json_to_recordset`. Such functions need support from `LinqToDB` and could be added on request

Because PostgreSQL didn't have stored procedures prior to version 11, it had support for some features in functions, that usually implemented by procedures in other databases. For those features we need to generate code by T4 in special way.

**Void functions**. Those are functions that doesn't return anything, but still have function semantic. This means you should be able to call such function in a place where you can call normal function. Because C# doesn't allow you to call void function in context, where it expected to have some result returned, we generate such functions using following signature:
```cs
[Sql.Function("some_void_function", ServerSideOnly = true)]
public static object SomeVoidFunction(/*input parameters*/)
    => throw new InvalidOperationException();
```
Note that generated function has `object` return type. It will allways return default value (`null`), Take into account that for `PostgreSQL` `VOID = NULL` will result in `false`, so don't try to make decisions on return value in your queries for such functions.

**Functions with `OUT`/`INOUT` parameters**. Because output parameters is a feature for stored procedures and functions should return data only using return value, `PostgreSQL` return such parameters as a single return value of `record` type, which contains all output parameter values. `LinqToDB` also don't break function semantics here and generate such functions in following way:
```cs
[Sql.Function("test_parameters", ServerSideOnly = true)]
public static TestParametersResult TestParameters(int? param1, int? param2)
    => throw new InvalidOperationException();

public class TestParametersResult
{
	public int? param2 { get; set; }
	public int? param3 { get; set; }
}
```
As you can see we generate separate `POCO` class to store values for output parameters. Because `npgsql` provider returns `record` values as `object[]`, we need to register mapping from `object[]` to `POCO` class in mapping schema. This is done by `InitMappingSchema` method in generated code. If you don't use constructors, generated by T4, you need to call this method from your custom constructors.

##### SQL Server
- fixed dialect detection logic to use database compatibility level instead of server version when it is not specified explicitly ([#1204](https://github.com/linq2db/linq2db/issues/1204))

***

### Release 2.1.0

#### Schema Provider Changes

- Updated schema provider to skip foreign keys that reference columns, not available in schema. Fixes Access schema provider failure, when JET provider fails to return columns for some system tables, but returns foreign keys with references to those columns ([#1164](https://github.com/linq2db/linq2db/issues/1164))
- Fixed trailing underscore in member name for associations if foreign key column ends with `_id` suffix ([#1173](https://github.com/linq2db/linq2db/issues/1173))

#### T4 Templates

- fixed v2.0 regression in class/property names generation from table and column names, where names like 'SomeName'/`SomeName_OtherName` were converted to `Somename`/`SomenameOthername` instead of `SomeName`/`SomeNameOtherName` ([#1161](https://github.com/linq2db/linq2db/issues/1161))

#### Extensions

- fixed too aggressive caching of parameters, added to query by extension builders ([#1177](https://github.com/linq2db/linq2db/issues/1177))

#### Other changes and fixes
- Added new `GetDataProvider` API to `DataConnection` class to get data provider instance by provider name, configuration name and connection string
- fixed bug with association predicate expression that could lead to various errors ([#975](https://github.com/linq2db/linq2db/issues/975), [#1195](https://github.com/linq2db/linq2db/issues/1195), [#1196](https://github.com/linq2db/linq2db/issues/1196))

##### Prefix query hints support

Added support for query hints, added before query. To add such hint, you need to start it with `**`.

Postfix hints example (`SQL Server`):
```cs
// adds suffix OPTION(RECOMPILE) hint only to next query
db.NextQueryHints.Add(SqlServerTools.Sql.OptionRecompile);

// adds suffix OPTION(RECOMPILE) hint to all queries
db.QueryHints.Add(SqlServerTools.Sql.OptionRecompile);
```

Prefix hints example (`EXPLAIN` modifier for MySQL):
```cs
// adds prefix EXPLAIN only to next query
db.NextQueryHints.Add("**EXPLAIN ");

// adds prefix EXPLAIN to all queries
db.QueryHints.Add("**EXPLAIN ");
```

#### Provider-specific changes

##### DB2

- CTE support enabled

##### Firebird

- added recursive CTE support ([#1168](https://github.com/linq2db/linq2db/issues/1168))

##### MySQL

- added recursive CTE support ([#1168](https://github.com/linq2db/linq2db/issues/1168))

##### Oracle

- Added support for Oracle `NVARCHAR2` type ([#633](https://github.com/linq2db/linq2db/issues/633)). Note that default mapping will map `System.String` to VARCHAR type for backward compatibility. To use `NVARCHAR2` type, you need to specify proper type in your column mapping using `DataType.NVarChar` enumeration value or override default mapping for `System.String` in your MappingSchema:
```cs
// set default string mapping
MappingSchema.Default.SetDataType(
    typeof(string),
    new SqlDataType(DataType.NVarChar, typeof(string), 255));
```

##### PostgreSQL

- added recursive CTE support ([#1168](https://github.com/linq2db/linq2db/issues/1168))

##### SAP HANA

- Fixed provider assembly discovery when referenced from GAC ([#239](https://github.com/linq2db/linq2db/issues/239))

##### SAP/Sybase ASE

###### DataAction AdoNetCore.AseClient provider support

Added support for fully managed provider with .NET Core support from [DataAction](https://github.com/DataAction/AdoNetCore.AseClient). To use new provider you just need to add reference to this provider to your project instead of official provider and `LINQ To DB` will start to use it.

If you want to use both providers at the same time, you will need to explicitly specify provider using provider name `AdoNetCore.AseClient` or `Sybase.Managed`.

New `T4` model generator nuget package `linq2db.Sybase.DataAction` created for this provider.

We would recommend to use post-0.10.1 `DataAction` provider when it will be released as we discovered several issues during integration and fixes for them were not yet released when those notes created. Or you can build provider from master branch to get version with latest fixes.

###### Other fixes
- `InvalidCastException` from ASE native provider when loading schema for some procedure parameters. `LINQ To DB` will not use provider's functionality to load parameters metadata. Note that such parameters will have their direction properties (`IsIn`, `IsOut`, `IsResult`) set to `false` ([#1060](https://github.com/linq2db/linq2db/issues/1060))
- disable quotation for columns with name starting from # ([#1064](https://github.com/linq2db/linq2db/issues/1064))

***

### Release 2.0.0

[[Release Notes 2.0.0]]

***

### Release 1.10.1

- fix async retry policy (#919)
- fix connection management (#927)

- feature: allow to configure null checking in predicates (#932)

- obsoletes: LinqToDB.Configuration.Linq.CheckNullForNotEquals, use CompareNullsAsValues instead

***

### Release 1.10.0

- breaking change: [Oracle] bulk mode property (OracleTools.UseAlternativeBulkCopy) changed type from bool to AlternativeBulkCopy enum. If you assigned it to true, you should replace it with AlternativeBulkCopy.InsertInto value.
- breaking change: [Oracle] Old implementation used TO_DATE literal for DataType.DateTime type and TO_TIMESTAMP literal for other date and time types. New implementation will use TO_TIMESTAMP for DataType.DateTime2 type and TO_DATE for other date and time types (#879)

- documentation: Added XML documentation for mapping functionality (#836)
- documentation: Added documentation on explicit join definition: https://github.com/linq2db/linq2db/wiki/Join-Operators

- feature: LINQ extension methods to define inner and outer (left, right, full) joins (#685)
- feature: [Oracle] New bulk insert mode added (AlternativeBulkCopy.InsertDual) (#878)
- feature: new DataConnection async extensions: ExecuteAsync, ExecuteProcAsync, QueryToListAsync, QueryToArrayAsync (#838)
- feature: [MySQL] BulkCopyOptions.RetrieveSequence support (only for single integer indentity per table) (#866)
- feature: support for query expression preprocessing (#852)

- improvement: added Sql.NoConvert helper function to remove unnecessary conversions, introduced by LINQ (#870). Fixes #722
- improvement: [MS SQL] XML parameters support for SQL Server (#859)
- improvement: joins optimization improvements (#834)
- improvement: expression tests generator improvements (#877)
- improvement: [Informix] return default schema flag and schema name for tables in schema provider (#858)

- fix: [Firebird] regression in string literals support for Firebird < 2.5 (#851). You should set FirebirdConfiguration.IsLiteralEncodingSupported to false for firebird < 2.5
- fix: internal IDataContextEx interface functionality merged into IDataContext interface to allow custom data contexts implementations (#837)
- fix: functions doesn't work in association predicate exptessions (#841)
- fix: query cache ignores changes to query, made by ProcessQuery method (#862)
- fix: group by issues when grouping by date parts (#264, #790)
- fix: some scenarios doesn't work with extension method associations (#833)
- fix: [.net core] SerializableAttribute type redefinition conflict (#839)
- fix: [.net core] Removed strong name from linq2db.core (#867)
- fix: execute query before returning enumerable result to user (#872)
- fix: [PostgreSQL] added result conversion to integer for DatePart function (#882)
- fix: [DB2] fix exception in DB2 schema provider (#880)
- fix: fix potential NRE (#875)
- fix: configuration (#906)
- fix: generating in (#909)

All changes: https://github.com/linq2db/linq2db/milestone/6

***

### Release 1.9.0

- breaking change: [MySql] access to a table using fully-qualified name using schema/owner is not supported anymore. You should update your code to use database name for it (#681)

- feature: async support (#758)
- feature: SQL MERGE support with new Merge API (#686)
- feature: LINQ query cache management (#645)
- feature: associations could be defined using extension methods (#786)
- feature: overrides, typed by resulting type, added for InsertWithIdentity (#774)
- feature: added possibility to provide predicate expression for associations (#753)
- feature: custom aggregate functions support (#73, #353, #679, #699, #775)

- documentation: initial job on API documentation
- documentation: Added XML documentation to major public API and XML documentation file included to nuget package
- documentation: documentation published on github.io: https://linq2db.github.io
- documentation: new Merge API articles added: https://github.com/linq2db/linq2db/wiki/Merge-API

- improvement: performance improvements for multi-threaded environments (#278)
- improvement: [PostgreSQL] DateTimeOffset type mapped to TimeStampTZ by default (#794)
- improvement: [PostgreSQL] Guid type mapped to uuid by default (#804)
- improvement: tables support in extensions (#773, #777)

- fix: regression in queries generation in 1.8.3 (#825)
- fix: Take/Skip/Distinct promoted to main query from joined subquery (#829)
- fix: wrong SQL generated for Contains subqueries with Take/Skip (#329)
- fix: fixed various issues with BinaryAggregateExpression option enabled (#812)
- fix: better support for Nullable<T> parameters in LINQ queries (#820)
- fix: use of function with IQueryable<T> result type in LINQ query could fail (#822)
- fix: use of retry policy fails for SAP HANA and DB for iSeries, if connection string contains credentials (#772)
- fix: call to Count after GroupBy executed on client (#781)
- fix: better support for SQL literals (#200, #668, #686)
- fix: [Informix] support for cultures with non-dot decimal separator (#145)
- fix: DbConnection.ExecuteReader ignores commandBehavior parameter (#801)
- fix: [Firebird, Oracle, MySql, Informix, SAP HANA, DB2] fully-qualified table name generation fixes for various providers (#778)
- fix: [SQLite] schema generation fails when foreign key targets table (#784)
- fix: [SQL Server] Set SkipOnInsert and SkipOnUpdate flags for computed columns in schema provider (#793)

All changes: https://github.com/linq2db/linq2db/milestone/5

***

### Release 1.8.3
---------------------------------
[!] Fixed problems with Configuration.Linq.UseBinaryAggregateExpression (#708, #716)
[!] Experimental support for query retry (#736, https://github.com/linq2db/linq2db/blob/master/Tests/Linq/Data/RetryPolicyTest.cs)

- Better support for NpgSql 3.2.3 (#714, #715)
- Fixed issue with wrong convert optimization (#722)
- Fixed join optimization (#728)
- Fixed nullable enum mapping edge cases (#726)
- Fixed issue with cached query (#737, #738)
- Update with OrderBy support (#205, #729)
- Changed string trimming for fixed size string columns (trim only spaces) (#727)
- Better support for creating tables with Oracle (#731, #750, #723, #724)
- Fixed InsertOrUpdate to work as InsertIfNotExists when update fields not specified (all providers) (#100, #732, #746)

***

### Release 1.8.2

[!] Configuration.Linq.UseBinaryAggregateExpression is set to false by default as supposed to be unstable

***

### Release 1.8.1

- Fixed issue with !IEnumerable.Contains (#228)
- Fixed GROUP BY DateTime.Year (#264, #652)
- Fixed query optimization (#269)
- Fixed BinaryAggregateExpression (#667)
- Fixed nullable enums support (#693)

- Improved Npgsql 3.2 support (#665)
- Improved JOIN build (#676)
- Improved SqlCe support (#695 )

- Minor changes (#664 #696)

***

### Release 1.8.0

- Added support for Window (Analytic) Functions: https://github.com/linq2db/linq2db/pull/613
- Now ObjectDisposedException will be thrown while trying to use disposed IDataContext instance: https://github.com/linq2db/linq2db/issues/445
- Added experimental support for big logical expressions optimization: https://github.com/linq2db/linq2db/issues/447
- Optimized use of different MappingSchemas: https://github.com/linq2db/linq2db/issues/615
- Added CROSS JOIN support
- Added support of TAKE hints: https://github.com/linq2db/linq2db/issues/560
- Added protection from writing GroupBy queries that lead to unexpected behaviour: https://github.com/linq2db/linq2db/issues/365
- MySql: string.Length is now properly returns number of characters instead of size in bytes when used in query: https://github.com/linq2db/linq2db/issues/343
- Fluent mapping enchantments (fixed inheritance & changing attributes several times) 

- Number of bug fixes and optimizations

***

### Release 1.7.6

- Multi-threading issues fixes
- Inner Joins optimizations (Configuration.Linq.OptimizeJoins)
- Fixed issues with paths on Linux
- F# options support

***

### Release 1.0.7.5

- Added JOIN LATERAL support for PostgreSQL.

***

### Release 1.0.7.4

- SqlServer Guid Identity support.

- New Update method overload:

```cs
(
    from p1 in db.Parent
    join p2 in db.Parent on p1.ParentID equals p2.ParentID
    where p1.ParentID < 3
    select new { p1, p2 }
)
.Update(q => q.p1, q => new Parent { ParentID = q.p2.ParentID });
```

- New configuration option - LinqToDB.DataProvider.SqlServer.SqlServerConfiguration.GenerateScopeIdentity.
- New DataConnection event OnTraceConnection.
- PostgreSQL v3+ support.

***

### Release 1.0.7.3

- New DropTable method overload:

```cs
using (var db = new DataConnection())
{
    var table = db.CreateTable<MyTable>("#TempTable");
    table.DropTable();
}
```

- New BulkCopy method overload:

```cs
using (var db = new DataConnection())
{
    var table = db.CreateTable<MyTable>("#TempTable");
    table.BulkCopy(...);
}
```

- New Merge method overload:

```cs
using (var db = new DataConnection())
{
    var table = db.CreateTable<MyTable>("#TempTable");
    table.Merge(...);
}
```

- New LinqToDBConvertException class is thrown for invalid convertion.

***