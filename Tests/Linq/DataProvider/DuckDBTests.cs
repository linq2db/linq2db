using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.DataProvider
{
	[TestFixture]
	public class DuckDBTests : TestBase
	{
		[Table]
		sealed class BetweenUsesRow
		{
			[PrimaryKey]                       public int      Id    { get; set; }
			[Column(DataType = DataType.Date)] public DateTime Day   { get; set; }
			[Column]                           public DateTime Stamp { get; set; }

			public static readonly BetweenUsesRow[] Data =
			[
				new() { Id = 1, Day = new DateTime(2020, 1, 1), Stamp = new DateTime(2020, 1, 1, 0, 15, 0) },
				new() { Id = 2, Day = new DateTime(2020, 1, 2), Stamp = new DateTime(2020, 1, 2, 12, 0, 0) },
			];
		}

		// DuckDB types a parameter by what it is compared with and binds one its uses type differently as text. A timestamp value
		// of a BETWEEN beside a date bound is cast to its type in every use of the statement, so each use keeps the value's time.
		[Test]
		public void BetweenValueKeepsItsTypeInEveryUse([IncludeDataSources(true, ProviderName.DuckDB)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(BetweenUsesRow.Data);

			var value = new DateTime(2020, 1, 2, 12, 0, 0);
			var high  = new DateTime(2020, 1, 2, 6, 0, 0);

			// Beside a date the query computes: with its time the value is above the second row's date too.
			table
				.Where(r => Sql.Between(value, r.Day, high) || value <= r.Stamp.Date)
				.Select(r => r.Id)
				.ToArray()
				.ShouldBeEmpty();

			// A use in a correlated subquery.
			table
				.Where(r => Sql.Between(value, r.Day, high) || table.Any(x => x.Id == r.Id && value >= x.Stamp.Date))
				.Select(r => r.Id)
				.OrderBy(id => id)
				.ToArray()
				.ShouldBe([1, 2]);

			// A use in date arithmetic over a column.
			table
				.Where(r => Sql.Between(value, r.Day, high) || value.AddDays(r.Id - 2) <= r.Stamp)
				.Select(r => r.Id)
				.ToArray()
				.ShouldBe([2]);

			DateTime? none = null;

			table
				.Where(r => Sql.Between(none, r.Day, high) || r.Stamp == none)
				.Select(r => r.Id)
				.ToArray()
				.ShouldBeEmpty();
		}
	}
}
