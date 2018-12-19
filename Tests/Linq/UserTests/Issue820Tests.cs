using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue820Tests : TestBase
	{
		[Sql.Expression("{0}", ServerSideOnly = true)]
		public static short Nope2(short? value)
		{
			return value.Value;
		}

		[Sql.Expression("{0}", ServerSideOnly = true)]
		public static short Nope(short value)
		{
			return value;
		}

		[Test]
		public void TestAndWithFunction([DataSources] string context)
		{
			short? param = 1;
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<LinqDataTypes2>()
					.Where(_ => param != null && Nope2(param) == _.SmallIntValue);

				var result1 = query.ToList();
				param = null;
				var result2 = query.ToList();

				Assert.AreEqual(1, result1.Count);
				Assert.AreEqual(0, result2.Count);
			}
		}

		[Test]
		public void TestAndWithCastAndFunction([DataSources] string context)
		{
			short? param = 1;
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<LinqDataTypes2>()
					.Where(_ => param != null && Nope((short)param) == _.SmallIntValue);

				var result1 = query.ToList();
				param = null;
				var result2 = query.ToList();

				Assert.AreEqual(1, result1.Count);
				Assert.AreEqual(0, result2.Count);
			}
		}

		[Test]
		public void TestAndWithCast([DataSources] string context)
		{
			short? param = 1;
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<LinqDataTypes2>()
					.Where(_ => param != null && (short)param == _.SmallIntValue);

				var result1 = query.ToList();
				param = null;
				var result2 = query.ToList();

				Assert.AreEqual(1, result1.Count);
				Assert.AreEqual(0, result2.Count);
			}
		}

		[Test]
		public void TestAndWithValue([DataSources] string context)
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

		[Test]
		public void TestWithoutValue([DataSources] string context)
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

		[Test]
		public void TestOrWithValue([DataSources] string context)
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
