using System;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.xUpdate
{
	using Model;

	[TestFixture]
	public class BatchTest : TestBase
	{
		[Test, DataContextSource(false)]
		public void Transaction(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var list = new[]
				{
					new Parent { ParentID = 1111, Value1 = 1111 },
					new Parent { ParentID = 2111, Value1 = 2111 },
					new Parent { ParentID = 3111, Value1 = 3111 },
					new Parent { ParentID = 4111, Value1 = 4111 },
				};

				foreach (var parent in list)
					db.Parent.Delete(p => p.ParentID == parent.ParentID);

				db.BeginTransaction();
				db.BulkCopy(list);
				db.CommitTransaction();

				foreach (var parent in list)
					db.Parent.Delete(p => p.ParentID == parent.ParentID);
			}
		}

		[Test, DataContextSource(false)]
		public void NoTransaction(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var list = new[]
				{
					new Parent { ParentID = 1111, Value1 = 1111 },
					new Parent { ParentID = 2111, Value1 = 2111 },
					new Parent { ParentID = 3111, Value1 = 3111 },
					new Parent { ParentID = 4111, Value1 = 4111 },
				};

				foreach (var parent in list)
					db.Parent.Delete(p => p.ParentID == parent.ParentID);

				db.BulkCopy(list);

				foreach (var parent in list)
					db.Parent.Delete(p => p.ParentID == parent.ParentID);
			}
		}
	}
}
