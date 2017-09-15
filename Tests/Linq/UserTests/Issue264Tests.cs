using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue264Tests : TestBase
	{
		[Test, IncludeDataContextSource(false, ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void Test(string context)
		{
			using (var db = new DataConnection(context))
			{
				var actualCount = db.GetTable<LinqDataTypes>()
					.GroupBy(_ => new { month = ByMonth(_.DateTimeValue), year = ByYear(_.DateTimeValue) })
					.Count();

				var expectedCount = Types
					.GroupBy(_ => new { month = ByMonth(_.DateTimeValue), year = ByYear(_.DateTimeValue) })
					.Count();

				Assert.AreEqual(expectedCount, actualCount);

				var actual = db.GetTable<LinqDataTypes>()
					.GroupBy(_ => new { month = ByMonth(_.DateTimeValue), year = ByYear(_.DateTimeValue) })
					.Select(_ => _.Key).ToList();

				var expected = Types
					.GroupBy(_ => new { month = ByMonth(_.DateTimeValue), year = ByYear(_.DateTimeValue) })
					.Select(_ => _.Key).ToList();

				AreEqual(expected, actual);
			}
		}

		[Test, IncludeDataContextSource(false, ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void TestWorkaround(string context)
		{
			using (var db = new DataConnection(context))
			{
				var actualCount = db.GetTable<LinqDataTypes>()
					.GroupBy(_ => new { month = ByMonth(_.DateTimeValue), year = ByYear(_.DateTimeValue) })
					.Count();

				var expectedCount = Types
					.GroupBy(_ => new { month = ByMonth(_.DateTimeValue), year = ByYear(_.DateTimeValue) })
					.Count();

				Assert.AreEqual(expectedCount, actualCount);

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
		public static int ByMonth(DateTime date)
		{
			return date.Month;
		}

		[Sql.Expression("YEAR({0})", ServerSideOnly = true)]
		public static int ByYear(DateTime date)
		{
			return date.Year;
		}
	}
}
