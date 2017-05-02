LINQ to DB 1.8.0  Release Notes
---------------------------------

Added support for Window (Analytic) Functions: https://github.com/linq2db/linq2db/pull/613
Now ObjectDisposedException would be thrown while trying to used disposed IDataContext instance: https://github.com/linq2db/linq2db/issues/445
Added experimental support to optimize big logical expressions: https://github.com/linq2db/linq2db/issues/447
Optimized using different MappingSchemas: https://github.com/linq2db/linq2db/issues/615
Added CROSS JOIN support
Added support TAKE hints support: https://github.com/linq2db/linq2db/issues/560
Added protection from writing GroubBy queries which leads to unexpected behaviour: https://github.com/linq2db/linq2db/issues/365
String.Length() now converted to SQL functions, returning number of symbols, not bytes: https://github.com/linq2db/linq2db/issues/343
Fluent mapping enchantments (fixed inheritance & changing attributes several times) 

Number of bug fixies and optimizations

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
