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
		private static short Short2(short? value)
		{
			return value!.Value;
		}

		[Sql.Expression("{0}", ServerSideOnly = true)]
		private static short Short(short value)
		{
			return value;
		}

		[Sql.Expression("{0}", ServerSideOnly = true)]
		private static long Long2(long? value)
		{
			return value!.Value;
		}

		[Sql.Expression("{0}", ServerSideOnly = true)]
		private static long Long(long value)
		{
			return value;
		}

		[Test]
		public void TestAndWithFunction_Short([DataSources] string context)
		{
			short? param = 1;
			using var db = GetDataContext(context);
			var query = db.GetTable<LinqDataTypes2>().Where(_ => param != null && Short2(param) == _.SmallIntValue);

			var result1 = query.ToList();
			param = null;
			var result2 = query.ToList();

			Assert.Multiple(() =>
			{
				Assert.That(result1, Has.Count.EqualTo(1));
				Assert.That(result2, Is.Empty);
				Assert.That(GetCurrentBaselines(), Does.Not.Contain("NULL"));
			});
		}

		[Test]
		public void TestAndWithFunction_Long([DataSources] string context)
		{
			long? param = 1;
			using var db = GetDataContext(context);
			var query = db.GetTable<LinqDataTypes2>().Where(_ => param != null && Long2(param) == _.BigIntValue);

			var result1 = query.ToList();
			param = null;
			var result2 = query.ToList();

			Assert.Multiple(() =>
			{
				Assert.That(result1, Has.Count.EqualTo(1));
				Assert.That(result2, Is.Empty);
				Assert.That(GetCurrentBaselines(), Does.Not.Contain("NULL"));
			});
		}

		[Test]
		public void TestAndWithCastAndFunction_Short([DataSources] string context)
		{
			short? param = 1;
			using var db = GetDataContext(context);
			var query = db.GetTable<LinqDataTypes2>().Where(_ => param != null && Short((short)param) == _.SmallIntValue);

			var result1 = query.ToList();
			param = null;
			var result2 = query.ToList();

			Assert.Multiple(() =>
			{
				Assert.That(result1, Has.Count.EqualTo(1));
				Assert.That(result2, Is.Empty);
				Assert.That(GetCurrentBaselines(), Does.Not.Contain("NULL"));
			});
		}

		[Test]
		public void TestAndWithCastAndFunction_Long([DataSources] string context)
		{
			long? param = 1;
			using var db = GetDataContext(context);
			var query = db.GetTable<LinqDataTypes2>().Where(_ => param != null && Long((short)param) == _.BigIntValue);

			var result1 = query.ToList();
			param = null;
			var result2 = query.ToList();

			Assert.Multiple(() =>
			{
				Assert.That(result1, Has.Count.EqualTo(1));
				Assert.That(result2, Is.Empty);
				Assert.That(GetCurrentBaselines(), Does.Not.Contain("NULL"));
			});
		}

		[Test]
		public void TestAndWithCast_Short([DataSources] string context)
		{
			short? param = 1;
			using var db = GetDataContext(context);
			var query = db.GetTable<LinqDataTypes2>().Where(_ => param != null && (short)param == _.SmallIntValue);

			var result1 = query.ToList();
			param = null;
			var result2 = query.ToList();

			Assert.Multiple(() =>
			{
				Assert.That(result1, Has.Count.EqualTo(1));
				Assert.That(result2, Is.Empty);
				Assert.That(GetCurrentBaselines(), Does.Not.Contain("NULL"));
			});
		}

		[Test]
		public void TestAndWithCast_Long([DataSources] string context)
		{
			long? param = 1;
			using var db = GetDataContext(context);
			var query = db.GetTable<LinqDataTypes2>().Where(_ => param != null && (long)param == _.BigIntValue);

			var result1 = query.ToList();
			param = null;
			var result2 = query.ToList();

			Assert.Multiple(() =>
			{
				Assert.That(result1, Has.Count.EqualTo(1));
				Assert.That(result2, Is.Empty);
				Assert.That(GetCurrentBaselines(), Does.Not.Contain("NULL"));
			});
		}

		[Test]
		public void TestAndWithValue_Short([DataSources] string context)
		{
			short? param = 1;
			using var db = GetDataContext(context);
			var query = db.GetTable<LinqDataTypes2>().Where(_ => param != null && param.Value == _.SmallIntValue);

			var result1 = query.ToList();
			param = null;
			var result2 = query.ToList();

			Assert.Multiple(() =>
			{
				Assert.That(result1, Has.Count.EqualTo(1));
				Assert.That(result2, Is.Empty);
				Assert.That(GetCurrentBaselines(), Does.Not.Contain("NULL"));
			});
		}

		[Test]
		public void TestAndWithValue_Long([DataSources] string context)
		{
			long? param = 1;
			using var db = GetDataContext(context);
			var query = db.GetTable<LinqDataTypes2>().Where(_ => param != null && param.Value == _.BigIntValue);

			var result1 = query.ToList();
			param = null;
			var result2 = query.ToList();

			Assert.Multiple(() =>
			{
				Assert.That(result1, Has.Count.EqualTo(1));
				Assert.That(result2, Is.Empty);
				Assert.That(GetCurrentBaselines(), Does.Not.Contain("NULL"));
			});
		}

		[Test]
		public void TestWithoutValue_Short([DataSources] string context)
		{
			short? param = 1;
			using var db = GetDataContext(context);
			var query = db.GetTable<LinqDataTypes2>().Where(_ => param == _.SmallIntValue);

			var result1 = query.ToList();

			param = null;
			var result2 = query.ToList();

			Assert.Multiple(() =>
			{
				Assert.That(result1, Has.Count.EqualTo(1));
				Assert.That(result2, Is.Empty);
				Assert.That(GetCurrentBaselines(), Does.Contain("IS NULL"));
			});
		}

		[Test]
		public void TestWithoutValue_Long([DataSources] string context)
		{
			long? param = 1;
			using var db = GetDataContext(context);
			var query = db.GetTable<LinqDataTypes2>().Where(_ => param == _.BigIntValue);

			var result1 = query.ToList();

			param = null;
			var result2 = query.ToList();

			Assert.Multiple(() =>
			{
				Assert.That(result1, Has.Count.EqualTo(1));
				Assert.That(result2, Has.Count.EqualTo(10));
				Assert.That(GetCurrentBaselines(), Does.Contain("IS NULL"));
			});
		}

		[Test]
		public void TestOrWithValue_Short([DataSources] string context)
		{
			short? param = 1;
			using var db = GetDataContext(context);
			var cnt = db.GetTable<LinqDataTypes2>().Count();

			var query = db.GetTable<LinqDataTypes2>().Where(_ => param == null || param.Value == _.SmallIntValue);

			var result1 = query.ToList();
			param = null;
			var result2 = query.ToList();

			Assert.Multiple(() =>
			{
				Assert.That(result1, Has.Count.EqualTo(1));
				Assert.That(result2, Has.Count.EqualTo(cnt));
				Assert.That(GetCurrentBaselines(), Does.Not.Contain("NULL"));
			});
		}

		[Test]
		public void TestOrWithValue_Long([DataSources] string context)
		{
			long? param = 1;
			using var db = GetDataContext(context);
			var cnt = db.GetTable<LinqDataTypes2>().Count();

			var query = db.GetTable<LinqDataTypes2>().Where(_ => param == null || param.Value == _.BigIntValue);

			var result1 = query.ToList();
			param = null;
			var result2 = query.ToList();

			Assert.Multiple(() =>
			{
				Assert.That(result1, Has.Count.EqualTo(1));
				Assert.That(result2, Has.Count.EqualTo(cnt));
				Assert.That(GetCurrentBaselines(), Does.Not.Contain("NULL"));
			});
		}
	}
}
