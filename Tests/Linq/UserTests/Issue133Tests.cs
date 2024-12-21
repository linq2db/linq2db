using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue133Tests : TestBase
	{
		static readonly string[] SupportedProviders = new[]
		{
			TestProvName.AllSqlServer,
			TestProvName.AllOracle,
			TestProvName.AllClickHouse
		}.SelectMany(_ => _.Split(',')).ToArray();

		[AttributeUsage(AttributeTargets.Parameter)]
		public class SupportsAnalyticFunctionsContextAttribute : IncludeDataSourcesAttribute
		{
			public SupportsAnalyticFunctionsContextAttribute(bool includeLinqService = true, params string[] excludedProviders)
				: base(includeLinqService, SupportedProviders.Except(excludedProviders.SelectMany(_ => _.Split(','))).ToArray())
			{
			}
		}

		[Sql.Expression("COUNT(*) * 100E0 / SUM(COUNT(*)) OVER()", ServerSideOnly = true, IsAggregate = true)]
		private static double CountPercents()
		{
			throw new InvalidOperationException("This function should be used only in database code");
		}

		[Test]
		public void NegativeWhereTest([SupportsAnalyticFunctionsContext] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.Child
					.GroupBy(_ => _.ParentID)
					.Select(_ => new { CountPercents = CountPercents(), Sum = _.Sum(r => r.ParentID) })
					.Where(_ => _.Sum != 36)
					.ToList();

				Assert.That(result, Has.Count.EqualTo(5));

				Assert.That(result.Sum(_ => _.CountPercents), Is.EqualTo(100d).Within(0.001));
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

				Assert.That(result, Has.Count.EqualTo(5));
				Assert.That(result.Sum(_ => _.CountPercents), Is.EqualTo(100d).Within(0.001));
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

				Assert.That(result, Has.Count.EqualTo(5));
				Assert.That(result.Sum(_ => _.CountPercents), Is.EqualTo(100d).Within(0.001));
			}
		}

		[Test]
		public void PositiveWindowFunctionsHavingTest([SupportsAnalyticFunctionsContext] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.Child
					.GroupBy(c => c.ParentID)
					.Select(g => new
					{
						CountPercents = g.Count() * 100d / Sql.Ext.Sum(g.Count()).Over().ToValue(),
						Sum = g.Sum(r => r.ParentID)
					})
					.Having(x => x.Sum != 36)
					.ToList();

				Assert.That(result, Has.Count.EqualTo(5));
				Assert.That(result.Sum(_ => _.CountPercents), Is.EqualTo(100d).Within(0.001));
			}
		}
	}
}
