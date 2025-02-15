using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

using Tests.Model;

namespace Tests.xUpdate
{
	[TestFixture]
	[Order(10000)]
	public class BatchTests : TestBase
	{
		[Test]
		public void Transaction([DataSources(false)] string context)
		{
			using (var db = GetDataConnection(context))
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
				db.BulkCopy(GetDefaultBulkCopyOptions(context), list);
				db.CommitTransaction();

				foreach (var parent in list)
					db.Parent.Delete(p => p.ParentID == parent.ParentID);
			}
		}

		[Test]
		public void NoTransaction([DataSources(false)] string context)
		{
			using (var db = GetDataConnection(context))
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

				db.BulkCopy(GetDefaultBulkCopyOptions(context), list);

				foreach (var parent in list)
					db.Parent.Delete(p => p.ParentID == parent.ParentID);
			}
		}
	}
}
