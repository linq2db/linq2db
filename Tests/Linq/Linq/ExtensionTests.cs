using System;
using System.Linq;

using LinqToDB.Data.Linq;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class ExtensionTests : TestBase
	{
		public class ParenTable
		{
			public int  ParentID;
			public int? Value1;
		}

		[Test]
		public void TableName()
		{
			using (var db = new TestDbManager("Sql2008"))
				db.GetTable<ParenTable>().TableName("Parent").ToList();
		}

		[Test]
		public void DatabaseName()
		{
			using (var db = new TestDbManager("Sql2008"))
				db.GetTable<Parent>().DatabaseName("BLToolkitData").ToList();
		}

		[Test]
		public void OwnerName()
		{
			using (var db = new TestDbManager("Sql2008"))
				db.GetTable<Parent>().OwnerName("dbo").ToList();
		}

		[Test]
		public void AllNames()
		{
			using (var db = new TestDbManager("Sql2008"))
				db.GetTable<ParenTable>()
					.DatabaseName("BLToolkitData")
					.OwnerName("dbo")
					.TableName("Parent")
					.ToList();
		}
	}
}
