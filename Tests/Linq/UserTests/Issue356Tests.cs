using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue356Tests : TestBase
	{
		[Test, DataContextSource(ProviderName.Sybase,
			ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95)]
		public void Test1(string context)
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

		[Test, DataContextSource(ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Access)]
		public void Test2(string context)
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

		[Test, DataContextSource(true, ProviderName.Access, ProviderName.SqlServer2000, ProviderName.Sybase, ProviderName.SybaseManaged)]
		public void Test3(string context)
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
