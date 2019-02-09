using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Data
{
	using LinqToDB.Data;
	using Model;
	using System.Threading;
	using System.Threading.Tasks;

	[TestFixture]
	public class TransactionTests : TestBase
	{
		[Test]
		public async Task DataContextBeginTransactionAsync([DataSources(false)] string context)
		{
			var tid = Thread.CurrentThread.ManagedThreadId;

			using (var db = new DataContext(context))
			using (await db.BeginTransactionAsync())
			{
				db.Insert(new Parent { ParentID = 1010, Value1 = 1010 });

				if (tid != Thread.CurrentThread.ManagedThreadId)
					Assert.Inconclusive("Executed synchronously due to lack of async support or there were no underlying async operations");
			}
		}

		[Test]
		public async Task DataConnectionBeginTransactionAsync([DataSources(false)] string context)
		{
			var tid = Thread.CurrentThread.ManagedThreadId;

			using (var db = new DataConnection(context))
			using (await db.BeginTransactionAsync())
			{
				db.Insert(new Parent { ParentID = 1010, Value1 = 1010 });

				if (tid != Thread.CurrentThread.ManagedThreadId)
					Assert.Inconclusive("Executed synchronously due to lack of async support or there were no underlying async operations");
			}
		}

		[Test]
		public void AutoRollbackTransaction([DataSources(false)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Insert(new Parent { ParentID = 1010, Value1 = 1010 });

				try
				{
					using (db.BeginTransaction())
					{
						db.Parent.Update(t => t.ParentID == 1010, t => new Parent { Value1 = 1012 });
					}

					var p = db.Parent.First(t => t.ParentID == 1010);

					Assert.That(p.Value1, Is.Not.EqualTo(1012));
				}
				finally
				{
					db.Parent.Delete(t => t.ParentID >= 1000);
				}
			}
		}

		[Test]
		public void CommitTransaction([DataSources(false)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Insert(new Parent { ParentID = 1010, Value1 = 1010 });

				try
				{
					using (var tr = db.BeginTransaction())
					{
						db.Parent.Update(t => t.ParentID == 1010, t => new Parent { Value1 = 1011 });
						tr.Commit();
					}

					var p = db.Parent.First(t => t.ParentID == 1010);

					Assert.That(p.Value1, Is.EqualTo(1011));
				}
				finally
				{
					db.Parent.Delete(t => t.ParentID >= 1000);
				}
			}
		}

		[Test]
		public void RollbackTransaction([DataSources(false)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Insert(new Parent { ParentID = 1010, Value1 = 1010 });

				try
				{
					using (var tr = db.BeginTransaction())
					{
						db.Parent.Update(t => t.ParentID == 1010, t => new Parent {Value1 = 1012});
						tr.Rollback();
					}

					var p = db.Parent.First(t => t.ParentID == 1010);

					Assert.That(p.Value1, Is.Not.EqualTo(1012));
				}
				finally
				{
					db.Parent.Delete(t => t.ParentID >= 1000);
				}
			}
		}
	}
}
