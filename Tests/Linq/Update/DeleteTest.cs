using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

#region ReSharper disable
// ReSharper disable ConvertToConstant.Local
#endregion

namespace Tests.Update
{
	using Model;

	[TestFixture]
	public class DeleteTest : TestBase
	{
		[Test]
		public void Delete1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

				var parent = new Parent1 { ParentID = 1001, Value1 = 1001 };

				db.Delete(parent);
				db.Insert(parent);

				Assert.AreEqual(1, db.Parent.Count (p => p.ParentID == parent.ParentID));
				Assert.AreEqual(1, db.Parent.Delete(p => p.ParentID == parent.ParentID));
				Assert.AreEqual(0, db.Parent.Count (p => p.ParentID == parent.ParentID));
			}
		}

		[Test]
		public void Delete2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

				var parent = new Parent1 { ParentID = 1001, Value1 = 1001 };

				db.Delete(parent);
				db.Insert(parent);

				Assert.AreEqual(1, db.Parent.Count(p => p.ParentID == parent.ParentID));
				Assert.AreEqual(1, db.Parent.Where(p => p.ParentID == parent.ParentID).Delete());
				Assert.AreEqual(0, db.Parent.Count(p => p.ParentID == parent.ParentID));
			}
		}

		[Test]
		public void Delete3([DataContexts(ProviderName.Informix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

				db.Child.Delete(c => new[] { 1001, 1002 }.Contains(c.ChildID));

				db.Child.Insert(() => new Child { ParentID = 1, ChildID = 1001 });
				db.Child.Insert(() => new Child { ParentID = 1, ChildID = 1002 });

				Assert.AreEqual(3, db.Child.Count(c => c.ParentID == 1));
				Assert.AreEqual(2, db.Child.Where(c => c.Parent.ParentID == 1 && new[] { 1001, 1002 }.Contains(c.ChildID)).Delete());
				Assert.AreEqual(1, db.Child.Count(c => c.ParentID == 1));
			};
		}

		[Test]
		public void Delete4([DataContexts(ProviderName.Informix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

				db.GrandChild1.Delete(gc => new[] { 1001, 1002 }.Contains(gc.GrandChildID.Value));

				db.GrandChild.Insert(() => new GrandChild { ParentID = 1, ChildID = 1, GrandChildID = 1001 });
				db.GrandChild.Insert(() => new GrandChild { ParentID = 1, ChildID = 2, GrandChildID = 1002 });

				Assert.AreEqual(3, db.GrandChild1.Count(gc => gc.ParentID == 1));
				Assert.AreEqual(2, db.GrandChild1.Where(gc => gc.Parent.ParentID == 1 && new[] { 1001, 1002 }.Contains(gc.GrandChildID.Value)).Delete());
				Assert.AreEqual(1, db.GrandChild1.Count(gc => gc.ParentID == 1));
			}
		}

		[Test]
		public void Delete5([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

				var values = new[] { 1001, 1002 };

				db.Parent.Delete(_ => _.ParentID > 1000);

				db.Parent.Insert(() => new Parent { ParentID = values[0], Value1 = 1 });
				db.Parent.Insert(() => new Parent { ParentID = values[1], Value1 = 1 });

				Assert.AreEqual(2, db.Parent.Count(_ => _.ParentID > 1000));
				Assert.AreEqual(2, db.Parent.Delete(_ => values.Contains(_.ParentID)));
				Assert.AreEqual(0, db.Parent.Count(_ => _.ParentID > 1000));
			}
		}

		[Test]
		public void AlterDelete([DataContexts(ProviderName.Informix, ExcludeLinqService=true)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent
						join ch in db.Child on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					where ch != null && ch.ParentID == -1 || ch == null && p.ParentID == -1
					select p;

				q.Delete();

				var sql = ((DataConnection)db).LastQuery;

				if (sql.Contains("EXISTS"))
					Assert.That(sql.IndexOf("(("), Is.GreaterThan(0));
			}
		}
	}
}
