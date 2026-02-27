using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5381Tests : TestBase
	{
		[Table]
		public class DateRanges
		{
			[Column] public int Year { get; set; }
		}

		[Table]
		public class SmallerDateRanges
		{
			[Column] public int Year { get; set; }
		}

		[Test]
		public void InvalidSQLGeneratedWhenJoiningNonGroupedAggregationQuery([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var dateRangesData = new[]
			{
				new DateRanges { Year = 2020 },
				new DateRanges { Year = 2021 },
				new DateRanges { Year = 2022 }
			};

			var smallerDateRangesData = new[]
			{
				new SmallerDateRanges { Year = 2020 },
				new SmallerDateRanges { Year = 2021 }
			};

			using var db = GetDataContext(context);
			using var dateRanges = db.CreateLocalTable(dateRangesData);
			using var smallerDateRanges = db.CreateLocalTable(smallerDateRangesData);

			var aggregation =
				from dr in dateRanges
				group dr by Sql.GroupBy.None
				into grouping
				select new
				{
					MinYear = grouping.Min(d => d.Year), 
					MaxYear = grouping.Max(d => d.Year)
				};

			var query =
				from a in aggregation
				from t in smallerDateRanges.Where(t => a.MinYear <= t.Year && a.MaxYear >= t.Year)
				select t.Year;

			AssertQuery(query);
		}

		[Test]
		public void InvalidSQLGeneratedWithComplexAggregations([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var dateRangesData = new[]
			{
				new DateRanges { Year = 2020 },
				new DateRanges { Year = 2021 },
				new DateRanges { Year = 2022 }
			};

			var smallerDateRangesData = new[]
			{
				new SmallerDateRanges { Year = 2020 },
				new SmallerDateRanges { Year = 2021 }
			};

			using var db = GetDataContext(context);
			using var dateRanges = db.CreateLocalTable(dateRangesData);
			using var smallerDateRanges = db.CreateLocalTable(smallerDateRangesData);

			var aggregation =
				from dr in dateRanges
				group dr by Sql.GroupBy.None
				into grouping
				select new
				{
					MinYear = grouping.Min(d => d.Year), 
					MaxYear = grouping.Max(d => d.Year), 
					Count = grouping.Count()
				};

			var query =
				from a in aggregation
				from t in smallerDateRanges.Where(t => a.MinYear <= t.Year && a.MaxYear >= t.Year)
				select new { t.Year, a.Count };

			AssertQuery(query);
		}
	}
}
