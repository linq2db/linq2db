using System;
using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.Ydb;

using NUnit.Framework;

namespace Tests.Extensions
{
	[TestFixture]
	public class YdbTests : TestBase
	{
		[Test]
		public void UniqueHintTest([IncludeDataSources(true, TestProvName.AllYdb)] string context)
		{
			using var db = GetDataContext(context);

			var q = db.Parent.UniqueHint("ParentID");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"--+ {YdbHints.Unique}(ParentID)"));
		}

		[Test]
		public void DistinctHintTest([IncludeDataSources(true, TestProvName.AllYdb)] string context)
		{
			using var db = GetDataContext(context);

			var q = db.Parent.DistinctHint("ParentID");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"--+ {YdbHints.Distinct}(ParentID)"));
		}

		[Test]
		public void UniqueHintAsYdbTest([IncludeDataSources(true, TestProvName.AllYdb)] string context)
		{
			using var db = GetDataContext(context);

			// Exercise the IYdbSpecificQueryable<T> overload via AsYdb() on a query.
			var q =
				(
					from p in db.Parent
					select p
				)
				.AsYdb()
				.UniqueHint("ParentID", "Value1");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"--+ {YdbHints.Unique}(ParentID Value1)"));
		}

		[Test]
		public void QueryHintTest([IncludeDataSources(true, TestProvName.AllYdb)] string context)
		{
			using var db = GetDataContext(context);

			var q = db.Parent.QueryHint(YdbHints.Distinct, "ParentID", "Value1");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"--+ {YdbHints.Distinct}(ParentID Value1)"));
		}
	}
}
