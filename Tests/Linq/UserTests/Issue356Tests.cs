using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue356Tests : TestBase
	{
		[Test]
		public void Test1([DataSources(ProviderName.Sybase, ProviderName.PostgreSQL)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var resultUnion = db.Child.Union(db.Child).Distinct();
				var result = db.Parent
					.SelectMany(x => resultUnion.Where(c => c.ParentID == x.ParentID).Select(z => new {x.ParentID, z.ChildID}))
					.Take(10);

				var expectedUnion = Child.Union(Child).Distinct();
				var expected = Parent
					.SelectMany(x => expectedUnion.Where(c => c.ParentID == x.ParentID).Select(z => new {x.ParentID, z.ChildID}))
					.Take(10);

				AreEqual(expected, result);
			}
		}

		[Test]
		public void Test2([DataSources(
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test]
		public void Test3([DataSources(true,
				ProviderName.Access, ProviderName.SqlServer2000, ProviderName.Sybase, ProviderName.SybaseManaged)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
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
}
