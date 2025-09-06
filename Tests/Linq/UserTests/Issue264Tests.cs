using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue264Tests : TestBase
	{
		[Test]
		public void Test1([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var actualCount = db.GetTable<LinqDataTypes>()
					.GroupBy(_ => new { month = ByMonth(_.DateTimeValue), year = ByYear(_.DateTimeValue) })
					.Count();

				var expectedCount = Types
					.GroupBy(_ => new { month = ByMonth(_.DateTimeValue), year = ByYear(_.DateTimeValue) })
					.Count();

				Assert.That(actualCount, Is.EqualTo(expectedCount));
			}
		}

		[Test]
		public void Test2([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var actual = db.GetTable<LinqDataTypes>()
					.GroupBy(_ => new { month = ByMonth(_.DateTimeValue), year = ByYear(_.DateTimeValue) })
					.Select(_ => _.Key).ToList();

				var expected = Types
					.GroupBy(_ => new { month = ByMonth(_.DateTimeValue), year = ByYear(_.DateTimeValue) })
					.Select(_ => _.Key).ToList();

				AreEqual(expected, actual);
			}
		}

		[Test]
		public void Test3([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var actual = db.GetTable<LinqDataTypes>()
					.GroupBy(_ => ByMonth(_.DateTimeValue))
					.Select(_ => _.Key).ToList();

				var expected = Types
					.GroupBy(_ => ByMonth(_.DateTimeValue))
					.Select(_ => _.Key).ToList();

				AreEqual(expected, actual);
			}
		}

		[Test]
		public void TestWorkaround([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var actualCount = db.GetTable<LinqDataTypes>()
					.GroupBy(_ => new { month = ByMonth(_.DateTimeValue), year = ByYear(_.DateTimeValue) })
					.Count();

				var expectedCount = Types
					.GroupBy(_ => new { month = ByMonth(_.DateTimeValue), year = ByYear(_.DateTimeValue) })
					.Count();

				Assert.That(actualCount, Is.EqualTo(expectedCount));

				var actual = db.GetTable<LinqDataTypes>()
					.GroupBy(_ => new { month = ByMonth(_.DateTimeValue), year = ByYear(_.DateTimeValue) })
					.Select(_ => new { _.Key.month, _.Key.year }).ToList();

				var expected = Types
					.GroupBy(_ => new { month = ByMonth(_.DateTimeValue), year = ByYear(_.DateTimeValue) })
					.Select(_ => new { _.Key.month, _.Key.year }).ToList();

				AreEqual(expected, actual);
			}
		}

		[Sql.Expression("MONTH({0})", ServerSideOnly = true)]
		private static int ByMonth(DateTime date)
		{
			return date.Month;
		}

		[Sql.Expression("YEAR({0})", ServerSideOnly = true)]
		private static int ByYear(DateTime date)
		{
			return date.Year;
		}
	}
}
