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
	}
}
