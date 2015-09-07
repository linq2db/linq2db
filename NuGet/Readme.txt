LINQ to DB 1.0.7.3  Release Notes
---------------------------------

Welcome to the release notes for LINQ to DB 1.0.7.3


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
