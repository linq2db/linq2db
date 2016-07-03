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

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008)]
		public void TableName(string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<ParenTable>().TableName("Parent").ToList();
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008)]
		public void DatabaseName(string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<Parent>().DatabaseName("TestData").ToList();
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008)]
		public void OwnerName(string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<Parent>().SchemaName("dbo").ToList();
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008)]
		public void AllNames(string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<ParenTable>()
					.DatabaseName("TestData")
					.SchemaName("dbo")
					.TableName("Parent")
					.ToList();
		}
	}
}
