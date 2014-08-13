using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Data
{
	using Model;

	[TestFixture]
	public class TransactionTest : TestBase
	{
		[Test]
		public void AutoCommitTrueTransaction()
		{
			using (var db = new TestDataConnection())
			{
				db.Insert(new Parent { ParentID = 1010, Value1 = 1010 });

				try
				{
					using (db.BeginTransaction())
					{
						db.Parent.Update(t => t.ParentID == 1010, t => new Parent { Value1 = 1020 });
					}

					var p = db.Parent.First(t => t.ParentID == 1010);

					Assert.That(p.Value1, Is.EqualTo(1020));
				}
				finally
				{
					db.Parent.Delete(t => t.ParentID >= 1000);
				}
			}
		}
		[Test]
		public void AutoCommitFalseTransaction()
		{
			using (var db = new TestDataConnection())
			{
				db.Insert(new Parent { ParentID = 1010, Value1 = 1010 });

				try
				{
					using (db.BeginTransaction(false))
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
		public void AutoTransactionCommit()
		{
			using (var db = new TestDataConnection())
			{
				db.Insert(new Parent { ParentID = 1010, Value1 = 1010 });

				try
				{
					using (var tr = db.BeginTransaction(false))
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
		public void AutoTransactionRillback()
		{
			using (var db = new TestDataConnection())
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
		[Test]
		public void AutoTransaction()
		{
			using (var db = new TestDataConnection())
			{
				db.Insert(new Parent { ParentID = 1010, Value1 = 1010 });

				try
				{
					var p = db.Parent.First(t => t.ParentID == 1010);
					Assert.That(p.ParentID, Is.EqualTo(1010));

					using (db.BeginTransaction(false))
						db.Parent.Update(t => t.ParentID == 1010, t => new Parent {Value1 = 1012});

					p = db.Parent.First(t => t.ParentID == 1010);
					Assert.That(p.ParentID, Is.Not.EqualTo(1012));

					using (var tr = db.BeginTransaction(true))
					{
						db.Parent.Update(t => t.ParentID == 1010, t => new Parent {Value1 = 1012});
						tr.Rollback();
					}

					p = db.Parent.First(t => t.ParentID == 1010);
					Assert.That(p.ParentID, Is.Not.EqualTo(1012));

					using (var tr = db.BeginTransaction(false))
					{
						db.Parent.Update(t => t.ParentID == 1010, t => new Parent {Value1 = 1011});
						tr.Commit();
					}

					p = db.Parent.First(t => t.ParentID == 1010);
					Assert.That(p.ParentID, Is.EqualTo(1011));

					using (db.BeginTransaction())
						db.Parent.Update(t => t.ParentID == 1010, t => new Parent {Value1 = 1020});

					p = db.Parent.First(t => t.ParentID == 1010);
					Assert.That(p.ParentID, Is.EqualTo(1020));

				}
				finally
				{
					db.Parent.Delete(t => t.ParentID >= 1000);
				}
			}
		}
	}
}
