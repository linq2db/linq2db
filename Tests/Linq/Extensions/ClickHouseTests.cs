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
	}
}
