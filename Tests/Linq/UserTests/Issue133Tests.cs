using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue133Tests : TestBase
	{
		[Sql.Expression("COUNT(*) * 100E0 / SUM(COUNT(*)) OVER()", ServerSideOnly = true, IsAggregate = true)]
		private static double CountPercents()
		{
			throw new ServerSideOnlyException(nameof(CountPercents));
		}

		[Test]
		public void NegativeWhereTest([SupportsAnalyticFunctionsContext] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Child
					.GroupBy(_ => _.ParentID)
					.Select(_ => new { CountPercents = CountPercents(), Sum = _.Sum(r => r.ParentID) })
					.Where(_ => _.Sum != 36)
					.ToList();

			Assert.That(result, Has.Count.EqualTo(5));

			Assert.That(result.Sum(_ => _.CountPercents), Is.EqualTo(100d).Within(0.001));
		}

		[Test] // LinqService fails with decimals
		public void PositiveHavingTest([SupportsAnalyticFunctionsContext(false)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Child
					.GroupBy(_ => _.ParentID)
					.Select(_ => new { CountPercents = CountPercents(), Sum = _.Sum(r => r.ParentID) })
					.Having(_ => _.Sum != 36)
					.ToList();

			Assert.That(result, Has.Count.EqualTo(5));
			Assert.That(result.Sum(_ => _.CountPercents), Is.EqualTo(100d).Within(0.001));
		}

		[Test, ActiveIssueNew(133, ErrorMessage = "Expected: 100.0d +/- 0.001d",
			Details = "The percentages sum to 65 rather than 100 because the window runs over the filtered rows. Declared on the constraint's own text rather than the caller expression NUnit prints above it, which is not stable across environments. Same number on SQLite, SQL Server and PostgreSQL, direct and remote.")]
		public void PositiveWindowFunctionsWhereTest([SupportsAnalyticFunctionsContext] string context)
		{
			using var db = GetDataContext(context);
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

		[Test]
		public void PositiveWindowFunctionsHavingTest([SupportsAnalyticFunctionsContext] string context)
		{
			using var db = GetDataContext(context);
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
