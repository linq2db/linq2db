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

		// NonParallelizable: mutates shared per-fixture instance fields ID1/ID2 mid-test that the query closures capture; concurrent cases corrupt them.
		[Test, NonParallelizable]
		public void TestWrongValue([DataSources(TestProvName.AllClickHouse)] string context, [Values(1, 2)] int _)
		{
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

		// NonParallelizable: mutates shared per-fixture instance fields ID1/ID2 mid-test that the query closures capture; concurrent cases corrupt them.
		[Test, NonParallelizable]
		public void TestNullValue([DataSources(TestProvName.AllClickHouse)] string context, [Values(1, 2)] int _)
		{
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
