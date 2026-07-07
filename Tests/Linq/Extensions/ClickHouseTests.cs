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

		static (AsofTrade[] trades, AsofQuote[] quotes) GetAsofData() =>
		(
			[
				new AsofTrade { ID = 1, Symbol = "A", Time = new DateTime(2020, 1, 1, 9, 0, 15) },
			],
			[
				new AsofQuote { ID = 1, Symbol = "A", Time = new DateTime(2020, 1, 1, 9, 0,  0) },
				new AsofQuote { ID = 2, Symbol = "A", Time = new DateTime(2020, 1, 1, 9, 0, 10) },
			]
		);

		[Test]
		public void AsOfJoinHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var (tradeData, quoteData) = GetAsofData();
			using var trades = db.CreateLocalTable(tradeData);
			using var quotes = db.CreateLocalTable(quoteData);

			var res = trades
				.Join(
					quotes.AsClickHouse().JoinAsOfHint(),
					SqlJoinType.Left,
					(trade, quote) => trade.Symbol == quote.Symbol && trade.Time >= quote.Time,
					(trade, quote) => new { trade.ID, QuoteID = quote.ID })
				.ToList();

			Assert.That(LastQuery, Contains.Substring("LEFT ASOF JOIN"));
			// ASOF picks the latest quote at or before the trade time (9:00:15) -> quote ID 2 (9:00:10).
			res.ShouldHaveSingleItem().QuoteID.ShouldBe(2);
		}

		[Test]
		public void ObsoleteAllAsOfJoinHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var (tradeData, quoteData) = GetAsofData();
			using var trades = db.CreateLocalTable(tradeData);
			using var quotes = db.CreateLocalTable(quoteData);

#pragma warning disable CS0618 // Type or member is obsolete
			var res = trades
				.Join(
					quotes.AsClickHouse().JoinAllAsOfHint(),
					SqlJoinType.Left,
					(trade, quote) => trade.Symbol == quote.Symbol && trade.Time >= quote.Time,
					(trade, quote) => new { trade.ID, QuoteID = quote.ID })
				.ToList();
#pragma warning restore CS0618 // Type or member is obsolete

			// Deprecated AllAsOf alias re-points to the standalone ASOF hint.
			Assert.That(LastQuery, Contains.Substring("LEFT ASOF JOIN"));
			res.ShouldHaveSingleItem().QuoteID.ShouldBe(2);
		}

		[Test]
		public void GlobalAsOfJoinHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var (tradeData, quoteData) = GetAsofData();
			using var trades = db.CreateLocalTable(tradeData);
			using var quotes = db.CreateLocalTable(quoteData);

			var res = trades
				.Join(
					quotes.AsClickHouse().JoinGlobalAsOfHint(),
					SqlJoinType.Left,
					(trade, quote) => trade.Symbol == quote.Symbol && trade.Time >= quote.Time,
					(trade, quote) => new { trade.ID, QuoteID = quote.ID })
				.ToList();

			Assert.That(LastQuery, Contains.Substring("GLOBAL LEFT ASOF JOIN"));
			res.ShouldHaveSingleItem().QuoteID.ShouldBe(2);
		}
	}
}
