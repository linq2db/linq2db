using System;
using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.ClickHouse;

using NUnit.Framework;

namespace Tests.Extensions
{
	[TestFixture]
	public class ClickHouseTests : TestBase
	{
		sealed class ReplacingMergeTreeTable
		{
			public uint     ID;
			public DateTime TS;
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
		public void FinalSubQueryHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.GetTable<ReplacingMergeTreeTable>()
				from c in db.GetTable<ReplacingMergeTreeTable>()
					.AsSubQuery()
					.AsClickHouse()
					.FinalHint()
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
		public void JoinOuterHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinOuterHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("LEFT OUTER JOIN"));
		}

		[Test]
		public void JoinSemiHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinSemiHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("LEFT SEMI JOIN"));
		}

		[Test]
		public void JoinAntiHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinAntiHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("LEFT ANTI JOIN"));
		}

		[Test]
		public void JoinAnyHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinAnyHint() on c.ParentID equals p.ParentID
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("INNER ANY JOIN"));
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
		public void JoinGlobalHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinGlobalHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("GLOBAL LEFT JOIN"));
		}

		[Test]
		public void JoinGlobalSemiHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinGlobalSemiHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("GLOBAL LEFT SEMI JOIN"));
		}

		[Test]
		public void JoinAllHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinAllHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("ALL LEFT JOIN"));
		}

		[Test]
		public void JoinAllSemiHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinAllSemiHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("ALL LEFT SEMI JOIN"));
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
					.AsSubQuery()
					.AsClickHouse()
					.FinalHint()
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
	}
}
