using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

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
		public void TestWrongValue([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<LinqDataTypes2>()
					.Where(_ => GetSource(db, ID1.Value).Select(r => r.ID).Contains(_.ID));

				ID1 = 3;
				var result = query.ToList();
				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(3, result[0].ID);

				query = db.GetTable<LinqDataTypes2>()
					.Where(_ => GetSource(db, ID2.Value).Select(r => r.ID).Contains(_.ID));

				ID1 = 2;
				ID2 = 4;
				result = query.ToList();
				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(4, result[0].ID);
			}
		}

		[Test]
		public void TestNullValue([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<LinqDataTypes2>()
					.Where(_ => GetSource(db, ID1.Value).Select(r => r.ID).Contains(_.ID));

				ID1 = 3;
				var result = query.ToList();
				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(3, result[0].ID);

				query = db.GetTable<LinqDataTypes2>()
					.Where(_ => GetSource(db, ID2.Value).Select(r => r.ID).Contains(_.ID));

				ID1 = null;
				ID2 = 4;
				result = query.ToList();
				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(4, result[0].ID);
			}
		}

		IQueryable<LinqDataTypes2> GetSource(ITestDataContext db, int id)
		{
			return db.GetTable<LinqDataTypes2>().Where(_ => _.ID == id);
		}
	}
}
