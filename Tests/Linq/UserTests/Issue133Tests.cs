using System;
using System.Linq;
using LinqToDB;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue133Tests : TestBase
	{
		[AttributeUsage(AttributeTargets.Parameter)]
		public class SupportsAnalyticFunctionsContextAttribute: IncludeDataSourcesAttribute
		{
			public SupportsAnalyticFunctionsContextAttribute(bool includeLinqService = true)
				: base(includeLinqService, TestProvName.AllSqlServer2005Plus, TestProvName.AllOracle)
			{
			}
		}

		[Sql.Expression("COUNT(*) * 100E0 / SUM(COUNT(*)) OVER()", ServerSideOnly = true, IsAggregate = true)]
		private static double CountPercents()
		{
			throw new InvalidOperationException("This function should be used only in database code");
		}

		[Test, ActiveIssue("Wrong Having detection")]
		public void NegativeWhereTest([SupportsAnalyticFunctionsContext] string context)
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

		[Test] // LinqService fails with decimals
		public void PositiveHavingTest([SupportsAnalyticFunctionsContext(false)] string context)
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

		[Test, ActiveIssue("Wrong Having detection")]
		public void PositiveWindowFunctionsWhereTest([SupportsAnalyticFunctionsContext] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.Child
					.GroupBy(_ => _.ParentID)
					.Select(_ => new
					{
						CountPercents = _.Count() * 100d / Sql.Ext.Sum(_.Count()).Over().ToValue(),
						Sum = _.Sum(r => r.ParentID)
					})
					.Where(_ => _.Sum != 36)
					.ToList();

				Assert.AreEqual(5, result.Count);
				Assert.AreEqual(100d, result.Sum(_ => _.CountPercents), 0.001);
			}
		}

		[Test]
		public void PositiveWindowFunctionsHavingTest([SupportsAnalyticFunctionsContext] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.Child
					.GroupBy(_ => _.ParentID)
					.Select(_ => new
					{
						CountPercents = _.Count() * 100d / Sql.Ext.Sum(_.Count()).Over().ToValue(),
						Sum = _.Sum(r => r.ParentID)
					})
					.Having(_ => _.Sum != 36)
					.ToList();

				Assert.AreEqual(5, result.Count);
				Assert.AreEqual(100d, result.Sum(_ => _.CountPercents), 0.001);
			}
		}
	}
}
