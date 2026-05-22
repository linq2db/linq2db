using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2785Tests : TestBase
	{
		[Test]
		public void Issue2785TestTopLevel([IncludeDataSources(TestProvName.AllOracle, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			var query = from a in db.Person
						join b in  db.Person on a.ID equals b.ID
						select new { Id = a.ID, Id2 = b.ID };

			var res = query.Take(10).ToList();
		}

		[Test]
		public void Issue2785TestSubquery([IncludeDataSources(TestProvName.AllOracle, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			var query = from a in db.Person
						join b in  db.Person on a.ID equals b.ID
						select new { Id = a.ID, Id2 = b.ID };

			query.Take(10).OrderBy(_ => _.Id2).ToList();
		}
	}
}
