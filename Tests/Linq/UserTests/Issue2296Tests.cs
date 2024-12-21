using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2296Tests : TestBase
	{
		[Test]
		public void Issue2296Test(
			[IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context,
			[Values] bool reverseWhereQuery)
		{
			using (var db = GetDataContext(context))
			{
				var varInt = 3;

				var localQuery =
					from p in Parent
					join c in GrandChild on p.ParentID equals c.ParentID
					select p;

				localQuery = localQuery.Where(p => varInt < p.ParentID);
				var expected = localQuery.ToArray();

				var dbQuery =
					from p in db.Parent
					join c in db.GrandChild on p.ParentID equals c.ParentID
					select p;

				if (reverseWhereQuery)
					dbQuery = dbQuery.Where(p => varInt < p.ParentID);
				else
					dbQuery = dbQuery.Where(p => p.ParentID > varInt);

				var actual = dbQuery.ToArray();

				Assert.That(actual, Is.EqualTo(expected));
			}
		}
	}
}
