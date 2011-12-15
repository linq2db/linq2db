using System;
using System.Linq;
using LinqToDB.Data.DataProvider;
using LinqToDB.Data.Linq;

using Data.Linq;
using Data.Linq.Model;

using NUnit.Framework;

namespace Update
{
	[TestFixture]
	public class BatchTest : TestBase
	{
		[Test]
		public void Transaction()
		{
			foreach (var provider in Providers)
			{
				using (var db = new TestDbManager(provider.Name))
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
					db.InsertBatch(list);
					db.CommitTransaction();

					foreach (var parent in list)
						db.Parent.Delete(p => p.ParentID == parent.ParentID);
				}
			}
		}

		[Test]
		public void NoTransaction()
		{
			foreach (var provider in Providers)
			{
				using (var db = new TestDbManager(provider.Name))
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

					db.InsertBatch(list);

					foreach (var parent in list)
						db.Parent.Delete(p => p.ParentID == parent.ParentID);
				}
			}
		}
	}
}
