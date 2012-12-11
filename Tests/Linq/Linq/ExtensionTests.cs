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
		public void TableName([IncludeDataContexts(ProviderName.SqlServer2008)] string context)
		{
			using (var db = new TestDbManager(context))
				db.GetTable<ParenTable>().TableName("Parent").ToList();
		}

		[Test]
		public void DatabaseName([IncludeDataContexts(ProviderName.SqlServer2008)] string context)
		{
			using (var db = new TestDbManager(context))
				db.GetTable<Parent>().DatabaseName("TestData").ToList();
		}

		[Test]
		public void OwnerName([IncludeDataContexts(ProviderName.SqlServer2008)] string context)
		{
			using (var db = new TestDbManager(context))
				db.GetTable<Parent>().OwnerName("dbo").ToList();
		}

		[Test]
		public void AllNames([IncludeDataContexts(ProviderName.SqlServer2008)] string context)
		{
			using (var db = new TestDbManager(context))
				db.GetTable<ParenTable>()
					.DatabaseName("TestData")
					.OwnerName("dbo")
					.TableName("Parent")
					.ToList();
		}
	}
}
