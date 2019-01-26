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
		[Test, Explicit("Executed synchronously due to connection pooling, when executed with other tests")]
		public async Task DataContextBeginTransactionAsync([DataSources(false)] string context)
		{
			var tid = Thread.CurrentThread.ManagedThreadId;

			using (var db = new DataContext(context))
			using (await db.BeginTransactionAsync())
			{
				Assert.AreNotEqual(tid, Thread.CurrentThread.ManagedThreadId);

				db.Insert(new Parent { ParentID = 1010, Value1 = 1010 });
			}
		}

		[Test, Explicit("Executed synchronously due to connection pooling, when executed with other tests")]
		public async Task DataConnectionBeginTransactionAsync([DataSources(false)] string context)
		{
			var tid = Thread.CurrentThread.ManagedThreadId;

			using (var db = new DataConnection(context))
			using (await db.BeginTransactionAsync())
			{
				Assert.AreNotEqual(tid, Thread.CurrentThread.ManagedThreadId);

				db.Insert(new Parent { ParentID = 1010, Value1 = 1010 });
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
