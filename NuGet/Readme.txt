LINQ to DB 2.0.0  Release Notes
---------------------------------
*IMPORTANT: LINQ to DB 2.0 is not released yet*
- breaking change: dropped support for .NET 4.0, Silverlight 4-5 and Windows 8 Store frameworks. New target frameworks list is netcoreapp2.0, netstandard1.6, netstandard2.0 and net45
- breaking change: behavior of enum mapping to a text type for enum without configured mappings changed to use ToString() instead of (enum underlying type).ToString(). To return old behavior, you should set Configuration.UseEnumValueNameForStringColumns to false (#1006, #1071)
- breaking change: [PostgreSQL] If you used BulkCopy with ProviderSpecific method specified, check https://github.com/linq2db/linq2db/wiki/Bulk-Copy for important notes regarding provider-specific support notes
- breaking change: [Firebird] Changed default identifier quotation mode to FirebirdIdentifierQuoteMode.Auto from None (#1120)

- feature: predicate expression support added for associations configuration using fluent mapping (#961)
- feature: support creation of query parameters in extension builders (#964)
- feature: new configuration flag LinqToDB.Common.Configuration.PrefereApply
- feature: new In/NotIn extension methods added to LinqToDB.Tools.Extensions
[Not fully functional yet]- feature: [Firebird, Informix, MySql, Oracle, PostgreSQL, SQLite, MS SQL] CTE (common table expressions) support implemented including WCF support (see DataExtensions.GetCte(),  LinqExtensions.AsCte() methods)
- feature: IBM.Data.DB2.Core provider support
- feature: Oracle Data Provider for .NET Core provider support
- feature: parameters to override table, schema/owner and database names added to InsertOrReplace*, InsertWith*Identity*, Update* and Delete* IDataContext extension methods
- feature: [MySQL] Procedures and function support added to schema provider (#991)
- feature: [BulkCopy][SAP HANA, SQL CE] BulkCopyOptions.KeepIdentity support added (#1037)
- feature: Calculated columns support through ExpressionMethodAttribute.IsColumn property (#1004)
- feature: Dynamic columns support (#964, #507, #744, #1083)
- feature: [PostgreSQL] Added support for native INSERT ON CONFLICT UPDATE statement for InsertOrUpdate operation for PostgreSQL 9.5+ (#948)
- feature: [PostgreSQL][BulkCopy] Provider-specific copy method implemented (#935)
- feature: Added IQueryable interceptor callback. This feature could be used to attach linq2db to other IQueryable providers (#1116)
- feature: Extra overrides to Join* extensions that accept two queryable sources, join predicate and result selector expression (#1076, #1088)

- improvement: [MS SQL] query parameters for varchar/nvarchar types will use fixed size 8000/4000 to improve query plans caching by server (#989)
- improvement: [Oracle] corrected date literal generation (#969)
- improvement: [Merge] Support partial projection in source query
- improvement: [Merge] Merge operation will throw LinqToDBException: "Column <column_name> doesn't exist in source" if projection in source query doesn't select needed field
- improvement: [Merge][MS SQL] Prefer parameters over literals for binary types in client-side source to avoid generation of huge SQL queries
- improvement: [BulkCopy] BulkCopy operation will throw LinqToDBException: "BulkCopyOptions.KeepIdentity = true is not supported by BulkCopyType.RowByRow mode" if KeepIdentity set to true for unsupported copy mode (#1037)
- improvement: [BulkCopy][SAP HANA] BulkCopy operation will throw LinqToDBException if BulkCopyOptions.KeepIdentity set to true for unsupported provider version to avoid unexpected results (#1037)
- improvement: [BulkCopy][Firebird] BulkCopy operation will throw LinqToDBException if BulkCopyOptions.KeepIdentity set to true to avoid unexpected results (#1037)
- improvement: Reading of schema for procedures will be wrapped into transaction with rollback if called without ambient transaction
- improvement: Allow basic mappings modifications using MappingSchema.EntityDescriptorCreatedCallback callback (#1074)
- improvement: Exception during column mapping will be wrappped into LinqToDBException with information which column failed with original error as InnerException (#1065)
- improvement: [PostgreSQL] Improved suppport for some types (#1091)
- improvement: [Firebird] Check table existence in DropTable (#1120)

- fix: fixed another case of defect #170, where default(T) value could be selected for non-nullable field instead of NULL from left join, if SelectMany() call used in source (#1012)
- fix: [MS SQL, Sybase] updated Merge insert operation to respect SkipOnInsert mapping flag for identity fields when no custom insert expression specified. With this fix merge operation will allow database to generate identity value instead of use of value from source (#914)
- fix: [MS SQL] fixed old(!) Merge API to not try to update identity fields during update operation (#1007)
- fix: [SQLite] fixed incorrect SQL generation for DateTime.AddDays() method (#998)
- fix: fixed issue where incorrect query filter generated for left join association to an entity with inheritance mapping (#956)
- fix: fixed exception during sql generation for some queries when joins optimization enabled and table hints used (#956)
- fix: fixed regression in sql generation for complex subqueries containing joins, goup by and contains expressions (#928)
- fix: rare conversion error for binary data selection over WCF (#925)
- fix: [DB2, MS SQL, SQL CE]fixed invalid sql generation for queries with empty select list and Take()/Skip() calls (#817)
- fix: name spelling fix: ForeingKeyInfo -> ForeignKeyInfo (#941)
- fix: Sql.Lower/Sql.Upper functions should be evaluated on server when possible (#819)
- fix: [Firebird] fixed Sql.DatePart function support for seconds and milliseconds (#967)
- fix: async query could be blocked by Connection.Open used internally instead of Connection.OpenAsync (#1023)
- fix: Fixed "Table not found for 't18.[3]t19.Field2'" error for merge with source query using cross joins or SelectMany (#896)
- fix: [MS SQL] Drop table in another database doesn't work (#1030)
- fix: [Inheritance mapping] Fixed exception when you try to select inherited record as a field/property of specific type instead of base inheritance type (#1046)
- fix: [DB2, Oracle] Schema provider will not find any procedures when called with GetTables = false (#1068)
- fix: [MySQL, Sybase] Reading of procedure schema from ambient transaction will throw LinqToDbException to avoid unintentional database corruption (#792)
- fix: [Merge] Fixed API to not propose Merge call right after On* call when no operations added yet
- fix: MappingSchema converters not used for enum types (#1006)
- fix: [Inheritance mapping] Passing derived entity using parameter of base type use parameter type instead of entity type in Update, Delete and Insert methods for query generation (#1017)
- fix: [Flient Mapping] Complex types mapping doesn't work with fluent mapping (#1005)
- fix: Adding multiple metadata readers to default mapping schema could result in use of only last added reader (#1066)
- fix: LoadWith do not support type casts in load expression (#1069)
- fix: [Inheritance mapping] Support for loading of derived entities in LoadWith (#994)
- fix: [Inheritance mapping] Fixed different cases of type conversions using type cast or `is` operator between base and derived entities (#1065, #1057)
- fix: Fix issue when insert query from subquery data source with nullable parameter called first with null value fails for subsequential calls with non-null parameters (#1098)
- fix: [PostgreSQL] Proper type names generated for: System.Int16 identity fields, DataType.VarBinary/System.Linq.Binary, DataType.NChar/System.Char. Those type names used by CreateTable() method (#1091)
- fix: Default System.Char mapping changed to use length = 1 instead of unspecified (#1091)
- fix: [PostgreSQL] Interval type supports both NpgsqlTimeSpan and NpgsqlInterval (#1091)
- fix: [Oracle, Firebird] Support escaping of identifiers that use reserved words (#1110, #1095)
- fix: Improved support for mappings to interfaces (#1099)
- fix: [MSSQL] DateTime string literal generation fix (#1107)
- fix: Fixed T4 templates to work with .NET Core projects out-of-box using nuget package (#1067)
- fix: [Access] Fixed schema read failure when ACE OleDb provider used (https://github.com/linq2db/linq2db.LINQPad/issues/10)
- fix: [Access] Schema provider will now return system tables too (TableInfo.IsProviderSpecific == true) (#1119)
- fix: [Firebird] Use proper identifiers quotation for create/drop/truncate table for tables with generators and fix detection of cases when quotation is required (#1120)

- other changes: t4models repository moved to main repository
- other changes: [Firebird] Made changes to Firebird provider/sql optimizer to allow subclassing (#1000)

- for developers: SelectQuery class refactoring (#936, #938)
- for developers: *DataProviders.txt tests configuration file replaced with JSON-based *DataProviders.json files
- for developers: migrated to latest C# version
- for developers: moved methods SchemaProviderBase.ToTypeName(), SchemaProviderBase.ToValidName() to public API (#944, #963)
- for developers: dual owner/schema naming replaced with schema name in code
- for developers: ActiveIssue attribute added for tests


LINQ to DB 1.10.1  Release Notes
---------------------------------
- fix async retry policy (#919)
- fix connection management (#927)

- feature: allow to configure null checking in predicates (#932)

- obsoletes: LinqToDB.Configuration.Linq.CheckNullForNotEquals, use CompareNullsAsValues instead


LINQ to DB 1.10.0  Release Notes
---------------------------------
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


LINQ to DB 1.9.0  Release Notes
---------------------------------
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


LINQ to DB 1.8.3  Release Notes
---------------------------------
[!] Fixed problems with Configuration.Linq.UseBinaryAggregateExpression (#708, #716)
[!] Experimental support for query retry (#736, https://github.com/linq2db/linq2db/blob/master/Tests/Linq/Data/RetryPolicyTest.cs)

Better support for NpgSql 3.2.3 (#714, #715)
Fixed issue with wrong convert optimization (#722)
Fixed join optimization (#728)
Fixed nullable enum mapping edge cases (#726)
Fixed issue with cached query (#737, #738)
Update with OrderBy support (#205, #729)
Changed string trimming for fixed size string columns (trim only spaces) (#727)
Better support for creating tables with Oracle (#731, #750, #723, #724)
Fixed InsertOrUpdate to work as InsertIfNotExists when update fields not specified (all providers) (#100, #732, #746)


LINQ to DB 1.8.2  Release Notes
---------------------------------
[!] Configuration.Linq.UseBinaryAggregateExpression is set to false by default as supposed to be unstable


LINQ to DB 1.8.1  Release Notes
---------------------------------
Fixed issue with !IEnumerable.Contains (#228)
Fixed GROUP BY DateTime.Year (#264, #652)
Fixed query optimization (#269)
Fixed BinaryAggregateExpression (#667)
Fixed nullable enums support (#693)

Improved Npgsql 3.2 support (#665)
Improved JOIN build (#676)
Improved SqlCe support (#695 )

Minor changes (#664 #696)


LINQ to DB 1.8.0  Release Notes
---------------------------------

Added support for Window (Analytic) Functions: https://github.com/linq2db/linq2db/pull/613
Now ObjectDisposedException will be thrown while trying to use disposed IDataContext instance: https://github.com/linq2db/linq2db/issues/445
Added experimental support for big logical expressions optimization: https://github.com/linq2db/linq2db/issues/447
Optimized use of different MappingSchemas: https://github.com/linq2db/linq2db/issues/615
Added CROSS JOIN support
Added support of TAKE hints: https://github.com/linq2db/linq2db/issues/560
Added protection from writing GroupBy queries that lead to unexpected behaviour: https://github.com/linq2db/linq2db/issues/365
MySql: string.Length is now properly returns number of characters instead of size in bytes when used in query: https://github.com/linq2db/linq2db/issues/343
Fluent mapping enchantments (fixed inheritance & changing attributes several times) 

Number of bug fixes and optimizations


LINQ to DB 1.7.6  Release Notes
---------------------------------


What's new in 1.7.6
---------------------

Multi-threading issues fixes
Inner Joins optimizations (Configuration.Linq.OptimizeJoins)
Fixed issues with paths on Linux
F# options support


What's new in 1.0.7.5
---------------------

Added JOIN LATERAL support for PostgreSQL.



What's new in 1.0.7.4
---------------------

SqlServer Guid Identity support.


New Update method overload:

	(
		from p1 in db.Parent
		join p2 in db.Parent on p1.ParentID equals p2.ParentID
		where p1.ParentID < 3
		select new { p1, p2 }
	)
	.Update(q => q.p1, q => new Parent { ParentID = q.p2.ParentID });


New configuration option - LinqToDB.DataProvider.SqlServer.SqlServerConfiguration.GenerateScopeIdentity.


New DataConnection event OnTraceConnection.


PostgreSQL v3+ support.



What's new in 1.0.7.3
---------------------

New DropTable method overload:

	using (var db = new DataConnection())
	{
		var table = db.CreateTable<MyTable>("#TempTable");
		table.DropTable();
	}


New BulkCopy method overload:

	using (var db = new DataConnection())
	{
		var table = db.CreateTable<MyTable>("#TempTable");
		table.BulkCopy(...);
	}


New Merge method overload:

	using (var db = new DataConnection())
	{
		var table = db.CreateTable<MyTable>("#TempTable");
		table.Merge(...);
	}


New LinqToDBConvertException class is thrown for invalid convertion.
