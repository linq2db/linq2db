using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue356Tests : TestBase
	{
		// test depends on data order from db
		// fails at least for SAP HANA for this reason
		[Test]
		public void Test1Unsorted([DataSources(TestProvName.AllPostgreSQL, TestProvName.AllSapHana)] string context)
		{
			using var db = GetDataContext(context);
			var resultUnion = db.Child.Union(db.Child).Distinct();
			var result = db.Parent
					.SelectMany(x => resultUnion.Where(c => c.ParentID == x.ParentID).Select(z => new { x.ParentID, z.ChildID }))
					.Take(10);

			var expectedUnion = Child.Union(Child).Distinct();
			var expected = Parent
					.SelectMany(x => expectedUnion.Where(c => c.ParentID == x.ParentID).Select(z => new { x.ParentID, z.ChildID }))
					.Take(10);

			AreEqual(expected, result, src => src.OrderBy(_ => _.ParentID).ThenBy(_ => _.ChildID));
		}

		// Test without sorting order dependency
		// Generated SQL not supported by Access
		[Test]
		public void Test1([DataSources(TestProvName.AllPostgreSQL, TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);
			var resultUnion = db.Child.Union(db.Child).Distinct();
			var result = db.Parent
					.SelectMany(x => resultUnion.Where(c => c.ParentID == x.ParentID).Select(z => new {x.ParentID, z.ChildID}))
					.OrderBy(_ => _.ParentID).ThenBy(_ => _.ChildID)
					.Take(10);

			var expectedUnion = Child.Union(Child).Distinct();
			var expected = Parent
					.SelectMany(x => expectedUnion.Where(c => c.ParentID == x.ParentID).Select(z => new {x.ParentID, z.ChildID}))
					.OrderBy(_ => _.ParentID).ThenBy(_ => _.ChildID)
					.Take(10);

			AreEqual(expected, result);
		}

		[Test]
		public void Test2([DataSources(TestProvName.AllSybase, TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);
			var resultUnion = db.Child.Union(db.Child).OrderBy(_ => _.ParentID).Take(10);
			var result = db.Parent
					.SelectMany(x => resultUnion.Where(c => c.ParentID == x.ParentID).Select(z => new {x.ParentID, z.ChildID}))
					.OrderBy(_ => _.ParentID)
					.Take(10);

			var expectedUnion = Child.Union(Child).OrderBy(_ => _.ParentID).Take(10);
			var expected = Parent
					.SelectMany(x => expectedUnion.Where(c => c.ParentID == x.ParentID).Select(z => new {x.ParentID, z.ChildID}))
					.OrderBy(_ => _.ParentID)
					.Take(10);

			AreEqual(expected, result);
		}

		[Test]
		public void Test3([DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			var resultUnion = db.Child.Union(db.Child).OrderBy(_ => _.ParentID).Skip(10).Take(10);
			var result = db.Parent
					.SelectMany(x => resultUnion.Where(c => c.ParentID == x.ParentID).Select(z => new {x.ParentID, z.ChildID}))
					.OrderBy(_ => _.ParentID)
					.Take(10);

			var expectedUnion = Child.Union(Child).OrderBy(_ => _.ParentID).Skip(10).Take(10);
			var expected = Parent
					.SelectMany(x => expectedUnion.Where(c => c.ParentID == x.ParentID).Select(z => new {x.ParentID, z.ChildID}))
					.OrderBy(_ => _.ParentID)
					.Take(10);

			AreEqual(expected, result);
		}
	}
}
