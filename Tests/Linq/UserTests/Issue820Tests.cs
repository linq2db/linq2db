using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue820Tests : TestBase
	{
		[Test, DataContextSource]
		public void TestAndWithValue(string context)
		{
			short? param = 1;
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<LinqDataTypes2>()
					.Where(_ => param != null && param.Value == _.SmallIntValue);

				var result1 = query.ToList();
				param = null;
				var result2 = query.ToList();

				Assert.AreEqual(1, result1.Count);
				Assert.AreEqual(0, result2.Count);
			}
		}

		[Test, DataContextSource]
		public void TestWithoutValue(string context)
		{
			short? param = 1;
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<LinqDataTypes2>()
					.Where(_ => param == _.SmallIntValue);

				var result1 = query.ToList();
				param = null;
				var result2 = query.ToList();

				Assert.AreEqual(1, result1.Count);
				Assert.AreEqual(0, result2.Count);
			}
		}

		[Test, DataContextSource]
		public void TestOrWithValue(string context)
		{
			short? param = 1;
			using (var db = GetDataContext(context))
			{
				var cnt = db.GetTable<LinqDataTypes2>().Count();

				var query = db.GetTable<LinqDataTypes2>()
					.Where(_ => param == null || param.Value == _.SmallIntValue);

				var result1 = query.ToList();
				param = null;
				var result2 = query.ToList();

				Assert.AreEqual(1, result1.Count);
				Assert.AreEqual(cnt, result2.Count);
			}
		}
	}
}
