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

Welcome to the release notes for LINQ to DB 1.7.6

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
