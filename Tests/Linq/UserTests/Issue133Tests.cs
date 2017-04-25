using System;
using System.Linq;
using LinqToDB;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue133Tests : TestBase
	{
		public class SupportsAnalyticFunctionsContextAttribute: IncludeDataContextSourceAttribute
		{
			public SupportsAnalyticFunctionsContextAttribute(bool includeLinqService = true) : base(includeLinqService, 
				ProviderName.SqlServer2000, ProviderName.SqlServer2005, ProviderName.SqlServer2008,
				ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative,
				ProviderName.SqlServer2012, ProviderName.SqlServer2014, TestProvName.SqlAzure)
			{
			}
		}

		[Sql.Expression("COUNT(*) * 100E0 / SUM(COUNT(*)) OVER()", ServerSideOnly = true)]
		private static double CountPercents()
		{
			throw new InvalidOperationException("This function should be used only in database code");
		}

		[Test, SupportsAnalyticFunctionsContext, Ignore("Wrong Having detection")]
		public void NegativeWhereTest(string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.Child
					.GroupBy(_ => _.ParentID)
					.Select(_ => new { CountPercents = CountPercents(), Sum = _.Sum(r => r.ParentID) })
					.Where(_ => _.Sum != 36)
					.ToList();

				Assert.AreEqual(5, result.Count);

				Assert.AreEqual(100d, result.Sum(_ => _.CountPercents), 0.001);
			}
		}

		[Test, SupportsAnalyticFunctionsContext]
		public void PositiveHavingTest(string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.Child
					.GroupBy(_ => _.ParentID)
					.Select(_ => new { CountPercents = CountPercents(), Sum = _.Sum(r => r.ParentID) })
					.Having(_ => _.Sum != 36)
					.ToList();

				Assert.AreEqual(5, result.Count);
				Assert.AreEqual(100d, result.Sum(_ => _.CountPercents), 0.001);
			}
		}

//TODO: Uncomment after merging of Window Functions

//		[Test, SupportsAnalyticFunctionsContext]
//		public void PositiveWindowFunctionsWhereTest(string context)
//		{
//			using (var db = GetDataContext(context))
//			{
//				var result = db.Child
//					.GroupBy(_ => _.ParentID)
//					.Select(_ => new
//					{
//						CountPercents = _.Count() * 100d / Sql.Ext.Sum(_.Count()).Over().ToValue(),
//						Sum = _.Sum(r => r.ParentID)
//					})
//					.Where(_ => _.Sum != 36)
//					.ToList();
//
//				Assert.AreEqual(5, result.Count);
//				Assert.AreEqual(100d, result.Sum(_ => _.CountPercents), 0.001);
//			}
//		}
//
//		[Test, SupportsAnalyticFunctionsContext]
//		public void PositiveWindowFunctionsHavingTest(string context)
//		{
//			using (var db = GetDataContext(context))
//			{
//				var result = db.Child
//					.GroupBy(_ => _.ParentID)
//					.Select(_ => new
//					{
//						CountPercents = _.Count() * 100d / Sql.Ext.Sum(_.Count()).Over().ToValue(),
//						Sum = _.Sum(r => r.ParentID)
//					})
//					.Having(_ => _.Sum != 36)
//					.ToList();
//
//				Assert.AreEqual(5, result.Count);
//				Assert.AreEqual(100d, result.Sum(_ => _.CountPercents), 0.001);
//			}
//		}
	}
}