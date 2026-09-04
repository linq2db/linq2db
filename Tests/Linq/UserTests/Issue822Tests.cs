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
		[Test]
		public void TestWrongValue([DataSources(TestProvName.AllClickHouse)] string context, [Values(1, 2)] int _)
		{
			// ID1/ID2 are locals (not fixture fields) so parallel cases don't share state; the query closures
			// still capture them and linq2db re-reads the value at execution, so mutating before ToList() is #822's point.
			int? ID1 = null;
			int? ID2 = null;

			using var db = GetDataContext(context);
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

		[Test]
		public void TestNullValue([DataSources(TestProvName.AllClickHouse)] string context, [Values(1, 2)] int _)
		{
			int? ID1 = null;
			int? ID2 = null;

			using var db = GetDataContext(context);
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

		IQueryable<LinqDataTypes2> GetSource(ITestDataContext db, int id)
		{
			return db.GetTable<LinqDataTypes2>().Where(_ => _.ID == id);
		}
	}
}
