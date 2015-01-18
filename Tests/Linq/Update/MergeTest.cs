using System;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.Update
{
	using Model;

	[TestFixture]
	public class MergeTest : TestBase
	{
		[Test, IncludeDataContextSource(false, ProviderName.SqlServer2008)]
		public void Merge1(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Merge(db.Types2);
			}
		}

		[Test, IncludeDataContextSource(false, ProviderName.SqlServer2008)]
		public void Merge2(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Merge(false, db.Types2);
			}
		}
	}
}
