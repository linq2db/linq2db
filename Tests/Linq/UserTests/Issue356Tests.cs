using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue356Tests : TestBase
	{
		// Take(10) without ORDER BY: the server picks which rows come back,
		// so the check accepts any 10 distinct rows of the full expected set
		[Test]
		public void Test1Unsorted([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var resultUnion = db.Child.Union(db.Child).Distinct();
			var result = db.Parent
					.SelectMany(x => resultUnion.Where(c => c.ParentID == x.ParentID).Select(z => new { x.ParentID, z.ChildID }))
					.Take(10)
					.ToList();

			var expectedUnion = Child.Union(Child).Distinct();
			var expected = Parent
					.SelectMany(x => expectedUnion.Where(c => c.ParentID == x.ParentID).Select(z => new { x.ParentID, z.ChildID }))
					.ToList();

			result.Count.ShouldBe(Math.Min(10, expected.Count));
			result.ShouldBeUnique();
			result.ShouldBeSubsetOf(expected);
		}

		// Test without sorting order dependency
		// Generated SQL not supported by Access
		[Test]
		public void Test1([DataSources(TestProvName.AllPostgreSQL, TestProvName.AllNativeAccess)] string context)
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
		public void Test2([DataSources(TestProvName.AllSybase, TestProvName.AllNativeAccess)] string context)
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
		public void Test3([DataSources(TestProvName.AllNativeAccess, TestProvName.AllSybase)] string context)
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
