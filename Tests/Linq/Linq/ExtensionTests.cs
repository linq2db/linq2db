using System;
using System.Linq;

using LinqToDB;

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
			using (var db = new TestDbManager())
				db.GetTable<ParenTable>().TableName("Parent").ToList();
		}

		[Test]
		public void DatabaseName()
		{
			using (var db = new TestDbManager())
				db.GetTable<Parent>().DatabaseName("TestData").ToList();
		}

		[Test]
		public void OwnerName()
		{
			using (var db = new TestDbManager())
				db.GetTable<Parent>().OwnerName("dbo").ToList();
		}

		[Test]
		public void AllNames()
		{
			using (var db = new TestDbManager())
				db.GetTable<ParenTable>()
					.DatabaseName("TestData")
					.OwnerName("dbo")
					.TableName("Parent")
					.ToList();
		}
	}
}
