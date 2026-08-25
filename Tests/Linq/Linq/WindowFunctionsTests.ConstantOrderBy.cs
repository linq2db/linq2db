using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

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
					// Deliberately not 1 and not ascending: where the clause empties and the provider still wants
					// an ordering, the caller's own key comes back wrapped, so the 5 and the DESC have to survive.
					ConstantOnly  = Sql.Window.RowNumber(w => w.OrderByDesc(5)),
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

			var sql = query.ToSqlQuery().Sql;

			// No dialect may see a bare constant sort key, in either spelling.
			sql.ShouldNotContain("ORDER BY 1");
			sql.ShouldNotContain("ORDER BY 5");

			// The other half of the fix, on the two providers whose IsRowNumberWithoutOrderBySupported is false:
			// the clause is not dropped outright but re-emitted as a scalar subquery, carrying the caller's own
			// value and direction rather than an invented 1 ASC.
			if (context.IsAnyOf(TestProvName.AllSqlServer, TestProvName.AllOracle))
			{
				sql.ShouldContain("ORDER BY (");
				sql.ShouldContain("5");
				sql.ShouldContain("DESC");
			}
		}

		// #5806 companion: Sql.Window.DefineWindow can hand a ranking function a window with no ORDER BY at all -
		// the fluent chain demands one on the direct call, UseWindow does not. SQL Server ("The function
		// 'ROW_NUMBER' must have an OVER clause with ORDER BY") and Oracle (ORA-30485) refuse that, so those two
		// get a stand-in ordering instead. SAP HANA is not one of them - it runs an unordered ROW_NUMBER happily,
		// which is why SapHanaSqlExpressionConvertVisitor.IsWindowOrderByRequired deliberately excludes it.
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

		// #5806: a RANGE/GROUPS frame with a value offset is defined relative to the sort key, so the standard
		// ties it to the ORDER BY and every dialect enforces that - dropping a constant key must not take the
		// clause with it. Before the drop this ran everywhere (the constant kept the clause non-empty); without
		// the frame arm in the base requirement it emits OVER ( RANGE ...) and the server refuses it, e.g.
		// SQLite "RANGE with offset PRECEDING/FOLLOWING requires one ORDER BY expression".
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, TestProvName.AllSapHana, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRange)]
		public void ConstantWindowOrderByWithOffsetFrame([SupportsAnalyticFunctionsContext(
			// SQL Server does not support RANGE with value offsets
			TestProvName.AllSqlServer,
			// PostgreSQL < 11 supports RANGE frames only with UNBOUNDED (value offsets need PG 11+)
			TestProvName.AllPostgreSQL10Minus)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				select new
				{
					t.Id,
					t.CategoryId,
					Framed = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(1).RangeBetweenValues(1, 2)),
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("RANGE");
			sql.ShouldNotContain("ORDER BY 1");

			// The frame still needs a sort key, so the clause has to survive the constant being dropped.
			sql.ShouldContain("ORDER BY");

			// Every row ties on the constant key, so each row's frame covers its whole partition and every
			// member of a partition sees the same total.
			foreach (var partition in query.ToList().GroupBy(r => r.CategoryId))
			{
				var expected = data.Where(d => d.CategoryId == partition.Key).Sum(d => d.IntValue);

				foreach (var row in partition)
					row.Framed.ShouldBe(expected);
			}
		}

		// #5806: the ranking and offset functions carry the ORDER BY requirement on their own, which is the
		// other way into the stand-in ordering - RANK reaches it on SQL Server, Oracle, SAP HANA, DB2, Informix
		// and MariaDB, none of which the ROW_NUMBER cases above touch.
		[Test]
		public void ConstantWindowOrderByOnRank([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				select new
				{
					t.Id,
					Rank = Sql.Window.Rank(w => w.OrderBy(1)),
				};

			var rows = query.ToList();
			var sql  = query.ToSqlQuery().Sql;

			sql.ShouldNotContain("ORDER BY 1");

			// The whole reason MariaDB has its own convert visitor: it rejects a window with no ordering for the
			// ranking functions, MySQL proper takes a bare OVER () for every one of them. Same query, and the two
			// have to come out different - otherwise the split is silently doing nothing.
			if (context.IsAnyOf(TestProvName.AllMariaDB))
				sql.ShouldContain("ORDER BY (");
			else if (context.IsAnyOf(TestProvName.AllMySql80))
				sql.ShouldNotContain("ORDER BY (");

			// Every row ties on the constant - whether the clause was dropped outright or replaced by a
			// stand-in, nothing outranks anything.
			rows.Count.ShouldBe(data.Length);

			foreach (var row in rows)
				row.Rank.ShouldBe(1);
		}

		// #5806: NTILE is the only function ClickHouse demands an ordering for, and it reaches the stand-in on
		// SQL Server, Oracle, SAP HANA, DB2 and Informix as well. Nothing else in the suite hands it a constant.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, ErrorMessage = ErrorHelper.Error_WindowFunction_NTile)]
		public void ConstantWindowOrderByOnNTile([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				select new
				{
					t.Id,
					Bucket = Sql.Window.NTile(4, w => w.OrderBy(1)),
				};

			var rows = query.ToList();

			query.ToSqlQuery().Sql.ShouldNotContain("ORDER BY 1");

			// Unordered, so which row lands in which bucket is the server's business - but NTILE still has to
			// fill four buckets as evenly as the row count allows.
			rows.Count.ShouldBe(data.Length);
			rows.ShouldAllBe(r => r.Bucket >= 1 && r.Bucket <= 4);

			var buckets = rows.GroupBy(r => r.Bucket).Select(g => g.Count()).ToList();

			buckets.Count.ShouldBe(4);
			(buckets.Max() - buckets.Min()).ShouldBeLessThanOrEqualTo(1);
		}

		// #5806: NTH_VALUE is the fifth member of the family that reads a neighbouring row, and SAP HANA demands
		// an ordering for it exactly as it does for FIRST_VALUE/LAST_VALUE - measured, NTH_VALUE(x, 2) OVER ()
		// there returns "feature not supported: Function must have ORDER BY clause". Oracle and DB2 execute the
		// same statement without complaint, which is why neither of them lists it.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, TestProvName.AllSqlServer2012Plus, TestProvName.AllInformix, ErrorMessage = ErrorHelper.Error_WindowFunction_NthValue)]
		public void ConstantWindowOrderByOnNthValue([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				select new
				{
					t.Id,
					Nth = Sql.Window.NthValue(t.IntValue, 2L, w => w.OrderBy(1)),
				};

			var rows = query.ToList();

			query.ToSqlQuery().Sql.ShouldNotContain("ORDER BY 1");

			// One window over the whole table with every row tied, so each row reads the same second value.
			rows.Count.ShouldBe(data.Length);
			rows.Select(r => r.Nth).Distinct().Count().ShouldBe(1);
		}

		// #5806 goes wider than the literal it reports: a captured local holds one value for the whole execution,
		// so it ties every row exactly as a literal does and is dropped the same way - whether it reaches the AST
		// as a parameter or as an inlined constant, neither spelling may show up as a sort key.
		[Test]
		public void CapturedLocalWindowOrderBy([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();
			var key  = 7;

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				select new
				{
					t.Id,
					Number = Sql.Window.RowNumber(w => w.OrderBy(key)),
				};

			var rows = query.ToList();
			var sql  = query.ToSqlQuery().Sql;

			sql.ShouldNotContain("ORDER BY 7");
			sql.ShouldNotContain("ORDER BY @");

			// Every row ties on it, so the numbering is arbitrary but still a complete 1..N.
			rows.Select(r => r.Number)
				.OrderBy(n => n)
				.ShouldBe(Enumerable.Range(1, data.Length).Select(n => (long)n));
		}
	}
}
