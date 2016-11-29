using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Data
{
	using Model;

	[TestFixture]
	public class TransactionTests : TestBase
	{
		[Test, DataContextSource(false)]
		public void AutoRollbackTransaction(string context)
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

		[Test, DataContextSource(false)]
		public void CommitTransaction(string context)
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

		[Test, DataContextSource(false)]
		public void RollbackTransaction(string context)
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
