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
		[IncludeDataContextSource(false, ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void Test(string context)
		{
			using (var db = new DataConnection(context))
			{
				var cnt = db.GetTable<LinqDataTypes>()
					.GroupBy(_ => new { month = ByMonth(_.DateTimeValue), year = ByYear(_.DateTimeValue) })
					// select required due to #781, remove when fixed
					.Select(_ => _.Key)
					.Count();

				Assert.AreEqual(4, cnt);

				var values = db.GetTable<LinqDataTypes>()
					.GroupBy(_ => new { month = ByMonth(_.DateTimeValue), year = ByYear(_.DateTimeValue) })
					.Select(_ => _.Key).ToList();

				Assert.AreEqual(4, values.Count);
			}
		}

		[IncludeDataContextSource(false, ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void TestWorkaround(string context)
		{
			using (var db = new DataConnection(context))
			{
				var cnt = db.GetTable<LinqDataTypes>()
					.GroupBy(_ => new { month = ByMonth(_.DateTimeValue), year = ByYear(_.DateTimeValue) })
					// select required due to #781, remove when fixed
					.Select(_ => _.Key)
					.Count();

				Assert.AreEqual(4, cnt);

				var values = db.GetTable<LinqDataTypes>()
					.GroupBy(_ => new { month = ByMonth(_.DateTimeValue), year = ByYear(_.DateTimeValue) })
					.Select(_ => new { _.Key.month, _.Key.year }).ToList();

				Assert.AreEqual(4, values.Count);
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
