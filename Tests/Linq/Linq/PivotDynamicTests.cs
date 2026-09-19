using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class PivotDynamicTests : TestBase
	{
		[Table]
		sealed class Sales
		{
			[Column] public string   Category { get; set; } = null!;
			[Column] public int      Year     { get; set; }
			[Column] public decimal? Amount   { get; set; }
			[Column] public string?  Note     { get; set; }

			public static readonly Sales[] Data =
			{
				new() { Category = "A", Year = 2000, Amount = 10m, Note = "a0" },
				new() { Category = "A", Year = 2010, Amount = 20m, Note = "a1" },
				new() { Category = "B", Year = 2000, Amount = 5m,  Note = "b0" },
				new() { Category = "B", Year = 2010, Amount = 15m, Note = "b1" },
			};
		}

		sealed class SalesDto
		{
			public string Category { get; set; } = null!;
			public decimal? Total  { get; set; }

			[DynamicColumnsStore]
			public IDictionary<string, object> Cells { get; set; } = null!;
		}

		static string Year(int y) => "Y" + y.ToString(CultureInfo.InvariantCulture);

		[Test]
		public void PivotsRuntimeValuesIntoPivotRow([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			// Runtime, and built by a query so it cannot fold to a constant.
			var years = t.Select(x => x.Year).Distinct().ToList();

			var result = t
				.Pivot(x => x.Category, x => x.Year, years,
					PivotCell<Sales, int>.Sum(x => x.Amount, Year))
				.ToList()
				.OrderBy(r => r.Key)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Key.ShouldBe("A");
			result[0]["Y2000"].ShouldBe(10m);
			result[0]["Y2010"].ShouldBe(20m);

			result[1].Key.ShouldBe("B");
			result[1]["Y2000"].ShouldBe(5m);
			result[1]["Y2010"].ShouldBe(15m);
		}

		/// <summary>
		/// Several cell templates crossed with the runtime values, alongside static aggregated members - the
		/// shape a real report needs, and the one the static projection API cannot express.
		/// </summary>
		[Test]
		public void PivotsMultipleCellsIntoUserType([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			var years = new[] { 2000, 2010 };

			var result = t
				.Pivot(x => x.Category, x => x.Year, years,
					g => new SalesDto { Category = g.Key, Total = g.Sum(x => x.Amount) },
					PivotCell<Sales, int>.Sum(x => x.Amount, y => "AMT_" + Year(y)),
					PivotCell<Sales, int>.Max(x => x.Note,   y => "NOTE_" + Year(y)))
				.ToList()
				.OrderBy(r => r.Category)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Category.ShouldBe("A");
			result[0].Total.ShouldBe(30m);
			result[0].Cells["AMT_Y2000"].ShouldBe(10m);
			result[0].Cells["NOTE_Y2010"].ShouldBe("a1");

			result[1].Category.ShouldBe("B");
			result[1].Total.ShouldBe(20m);
			result[1].Cells["AMT_Y2010"].ShouldBe(15m);
			result[1].Cells["NOTE_Y2000"].ShouldBe("b0");
		}

		[Test]
		public void MultipleCellsWithoutNameFactoryThrows([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			// Two templates would otherwise generate two columns called "2000".
			System.Action act = () => t
				.Pivot(x => x.Category, x => x.Year, new[] { 2000 },
					PivotCell<Sales, int>.Sum(x => x.Amount),
					PivotCell<Sales, int>.Max(x => x.Note))
				.ToList();

			act.ShouldThrow<System.ArgumentException>();
		}
	}
}
