using System;
using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Extensions
{
	[TestFixture]
	public partial class ClickHouseTests : TestBase
	{
		sealed class ReplacingMergeTreeTable
		{
			public uint     ID;
			public DateTime TS;
		}

		[Table("AsofTrade")]
		sealed class AsofTrade
		{
			[Column] public int      ID     { get; set; }
			[Column] public string   Symbol { get; set; } = null!;
			[Column] public DateTime Time   { get; set; }
		}

		[Table("AsofQuote")]
		sealed class AsofQuote
		{
			[Column] public int      ID     { get; set; }
			[Column] public string   Symbol { get; set; } = null!;
			[Column] public DateTime Time   { get; set; }
		}

		[Test]
		public void FinalHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.GetTable<ReplacingMergeTreeTable>()
					.AsClickHouse()
					.FinalHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring(ClickHouseHints.Table.Final));
		}

		[Test]
		public void FinalHintTest2([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.GetTable<ReplacingMergeTreeTable>()
				from c in db.GetTable<ReplacingMergeTreeTable>()
					.AsClickHouse()
					.FinalHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring(ClickHouseHints.Table.Final));
		}

		[Test]
		public void FinalSubQueryHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.GetTable<ReplacingMergeTreeTable>()
				from c in db.GetTable<ReplacingMergeTreeTable>()
					.AsClickHouse()
					.FinalHint()
					.AsSubQuery()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring(ClickHouseHints.Table.Final));
		}

		[Test]
		public void FinalInScopeHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.GetTable<ReplacingMergeTreeTable>()
					.AsSubQuery()
					.AsClickHouse()
					.FinalInScopeHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring(ClickHouseHints.Table.Final));
		}

		[Test]
		public void SettingsHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsClickHouse()
			.SettingsHint("convert_query_to_cnf=false");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SETTINGS convert_query_to_cnf=false"));
		}

		[Test]
		public void SettingsHintWithParamsTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("ch")
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsClickHouse()
			.SettingsHint("additional_table_filters = {{'{0}': 'ParentID != 2'}}", Sql.TableName("ch"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SETTINGS additional_table_filters = {'Child': 'ParentID != 2'}"));
		}

		[Test]
		public void ClickHouseUnionTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.GetTable<ReplacingMergeTreeTable>()
					.AsClickHouse()
					.FinalHint()
				select p
			)
			.Union
			(
				from p in db.GetTable<ReplacingMergeTreeTable>()
				from c in db.GetTable<ReplacingMergeTreeTable>()
					.AsClickHouse()
					.FinalHint()
					.AsSubQuery()
				select p
			)
			.AsClickHouse()
			.SettingsHint("convert_query_to_cnf=false")
			;

			_ = q.ToList();

			Assert.That(LastQuery, Should.Contain(
				"FINAL",
				"UNION",
				"FINAL",
				"SETTINGS convert_query_to_cnf=false"));
		}

		[Test]
		public void StringFinalHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.GetTable<ReplacingMergeTreeTable>()
					.TableHint(ClickHouseHints.Table.Final)
				select p;

			_ = q.ToList();

			LastQuery
				.ShouldNotBeNull()
				.ShouldContain(" " + ClickHouseHints.Table.Final);
		}

		[Test]
		public void AsOfJoinHintSqlGeneration([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q = db.GetTable<AsofTrade>()
				.Join(
					db.GetTable<AsofQuote>().AsClickHouse().JoinAsOfHint(),
					SqlJoinType.Left,
					(trade, quote) => trade.Symbol == quote.Symbol && trade.Time >= quote.Time,
					(trade, quote) => new { trade.ID, QuoteID = quote.ID });

			var sql = q.ToSqlQuery().Sql;

			sql.ShouldContain("LEFT ASOF JOIN");
			sql.ShouldContain(" >= ");
		}

		[Test]
		public void ObsoleteAllAsOfJoinHintSqlGeneration([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

#pragma warning disable CS0618 // Type or member is obsolete
			var q = db.GetTable<AsofTrade>()
				.Join(
					db.GetTable<AsofQuote>().AsClickHouse().JoinAllAsOfHint(),
					SqlJoinType.Left,
					(trade, quote) => trade.Symbol == quote.Symbol && trade.Time >= quote.Time,
					(trade, quote) => new { trade.ID, QuoteID = quote.ID });
#pragma warning restore CS0618 // Type or member is obsolete

			var sql = q.ToSqlQuery().Sql;

			sql.ShouldContain("LEFT ASOF JOIN");
			sql.ShouldContain(" >= ");
		}

		[Test]
		public void GlobalAsOfJoinHintSqlGeneration([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q = db.GetTable<AsofTrade>()
				.Join(
					db.GetTable<AsofQuote>().AsClickHouse().JoinGlobalAsOfHint(),
					SqlJoinType.Left,
					(trade, quote) => trade.Symbol == quote.Symbol && trade.Time >= quote.Time,
					(trade, quote) => new { trade.ID, QuoteID = quote.ID });

			var sql = q.ToSqlQuery().Sql;

			sql.ShouldContain("GLOBAL LEFT ASOF JOIN");
			sql.ShouldContain(" >= ");
		}
	}
}
