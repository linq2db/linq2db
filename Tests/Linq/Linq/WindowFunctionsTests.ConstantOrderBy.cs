using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		// #5806: a window ORDER BY key that holds the same value for every row sorts nothing, yet it used to reach
		// the server verbatim. SQL Server rejects it ("do not support integer indices" for an integer, "do not
		// support constants" for anything else), SAP HANA rejects it ("Constants are not allowed on ORDER BY clause
		// of window functions"), and MySQL 8 reads an integer one as a legacy column position. It is dropped now -
		// the same treatment a statement's ORDER BY already got - so no dialect sees one.
		[Test]
		public void ConstantWindowOrderBy([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				select new
				{
					t.Id,
					ConstantOnly  = Sql.Window.RowNumber(w => w.OrderBy(1)),
					TrailingConst = Sql.Window.RowNumber(w => w.OrderBy(t.Id).ThenBy(1)),
					LeadingConst  = Sql.Window.RowNumber(w => w.OrderBy(1).ThenBy(t.Id)),
				};

			var rows = query.ToList();

			// A constant never breaks a tie, so wherever it sits the real key alone decides the numbering.
			var expected = data
				.OrderBy(d => d.Id)
				.Select((d, i) => (d.Id, Number: (long)(i + 1)))
				.ToDictionary(x => x.Id, x => x.Number);

			foreach (var row in rows)
			{
				row.TrailingConst.ShouldBe(expected[row.Id]);
				row.LeadingConst.ShouldBe(expected[row.Id]);
			}

			// Ordering by nothing but a constant leaves every row tied, so which row gets which number is the
			// server's business - but the numbering must still be a complete 1..N with no repeats.
			rows.Select(r => r.ConstantOnly)
				.OrderBy(n => n)
				.ShouldBe(Enumerable.Range(1, data.Length).Select(n => (long)n));

			query.ToSqlQuery().Sql.ShouldNotContain("ORDER BY 1");
		}

		// #5806 companion: Sql.Window.DefineWindow can hand a ranking function a window with no ORDER BY at all -
		// the fluent chain demands one on the direct call, UseWindow does not. SQL Server ("The function
		// 'ROW_NUMBER' must have an OVER clause with ORDER BY"), Oracle (ORA-30485) and SAP HANA ("Function must
		// have ORDER BY clause") all refuse that, so those providers get a stand-in ordering instead.
		[Test]
		public void RankingFunctionWithoutWindowOrderBy([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				let wnd = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId))
				select new
				{
					t.Id,
					t.CategoryId,
					Number = Sql.Window.RowNumber(w => w.UseWindow(wnd)),
				};

			var rows = query.ToList();

			rows.Count.ShouldBe(data.Length);

			// Unordered, so the numbering within a partition is arbitrary - but it must still cover 1..partition
			// size exactly once.
			foreach (var partition in rows.GroupBy(r => r.CategoryId))
			{
				partition.Select(r => r.Number)
					.OrderBy(n => n)
					.ShouldBe(Enumerable.Range(1, partition.Count()).Select(n => (long)n));
			}
		}
	}
}
