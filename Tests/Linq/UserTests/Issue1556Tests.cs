using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1556Tests : TestBase
	{
		[Test]
		public void Issue1556Test([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in db.Parent
				from c in db.Child
				where p.ParentID == c.ParentID || c.GrandChildren.Any(y => y.ParentID == p.ParentID)
				select new { p, c }
				,
				db.Parent
					.InnerJoin(db.Child,
						(p, c) => p.ParentID == c.ParentID || c.GrandChildren.Any(y => y.ParentID == p.ParentID),
						(p, c) => new { p, c }));
		}
	}
}
