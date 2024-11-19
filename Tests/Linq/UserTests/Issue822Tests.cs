using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue822Tests : TestBase
	{
		int? ID1;

		int? ID2;

		[Test]
		public void TestWrongValue([DataSources(TestProvName.AllClickHouse)] string context, [Values(1, 2)] int _)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<LinqDataTypes2>()
					.Where(_ => GetSource(db, ID1!.Value).Select(r => r.ID).Contains(_.ID));

				ID1 = 3;
				var result = query.ToList();
				Assert.That(result, Has.Count.EqualTo(1));
				Assert.That(result[0].ID, Is.EqualTo(3));

				query = db.GetTable<LinqDataTypes2>()
					.Where(_ => GetSource(db, ID2!.Value).Select(r => r.ID).Contains(_.ID));

				ID1 = 2;
				ID2 = 4;
				result = query.ToList();
				Assert.That(result, Has.Count.EqualTo(1));
				Assert.That(result[0].ID, Is.EqualTo(4));
			}
		}

		[Test]
		public void TestNullValue([DataSources(TestProvName.AllClickHouse)] string context, [Values(1, 2)] int _)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<LinqDataTypes2>()
					.Where(_ => GetSource(db, ID1!.Value).Select(r => r.ID).Contains(_.ID));

				ID1 = 3;
				var result = query.ToList();
				Assert.That(result, Has.Count.EqualTo(1));
				Assert.That(result[0].ID, Is.EqualTo(3));

				query = db.GetTable<LinqDataTypes2>()
					.Where(_ => GetSource(db, ID2!.Value).Select(r => r.ID).Contains(_.ID));

				ID1 = null;
				ID2 = 4;
				result = query.ToList();
				Assert.That(result, Has.Count.EqualTo(1));
				Assert.That(result[0].ID, Is.EqualTo(4));
			}
		}

		IQueryable<LinqDataTypes2> GetSource(ITestDataContext db, int id)
		{
			return db.GetTable<LinqDataTypes2>().Where(_ => _.ID == id);
		}
	}
}
