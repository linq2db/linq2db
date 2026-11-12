#if NET11_0_OR_GREATER
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class FullJoinMethodTests : TestBase
	{
		[Test]
		public void FullJoinSimple([DataSources(
			TestProvName.AllSQLite,
			TestProvName.AllAccess,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				db.Parent.FullJoin(db.Child, p => p.ParentID, c => c.ParentID, (p, c) => new { ParentID = (int?)p!.ParentID, ChildID = (int?)c!.ChildID });

			AssertQuery(query);
		}

		[Test]
		public void FullJoinUnmatchedOnBothSides([DataSources(
			TestProvName.AllSQLite,
			TestProvName.AllAccess,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				db.Parent
					.Where(p => p.ParentID < 3)
					.FullJoin(
						db.Child.Where(c => c.ParentID > 1),
						p => p.ParentID,
						c => c.ParentID,
						(p, c) => new { ParentID = (int?)p!.ParentID, ChildID = (int?)c!.ChildID });

			AssertQuery(query);
		}

		[Test]
		public void FullJoinEntities([DataSources(
			TestProvName.AllSQLite,
			TestProvName.AllAccess,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				db.Parent
					.Where(p => p.ParentID < 3)
					.FullJoin(
						db.Child.Where(c => c.ParentID > 1),
						p => p.ParentID,
						c => c.ParentID,
						(p, c) => new { p, c });

			AssertQuery(query);
		}

		[Test]
		public void FullJoinWithCompositeKey([DataSources(
			TestProvName.AllSQLite,
			TestProvName.AllAccess,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context, o => o.OmitUnsupportedCompareNulls(context));

			var query =
				db.Parent.FullJoin(
					db.Child,
					p => new { p.ParentID, ID2 = p.Value1 ?? 0 },
					c => new { c.ParentID, ID2 = c.ParentID },
					(p, c) => new { p, c });

			AssertQuery(query);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6311")]
		public void FullJoinTupleOverload([DataSources(
			TestProvName.AllSQLite,
			TestProvName.AllAccess,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Parent
				.Where(p => p.ParentID < 3)
				.FullJoin(db.Child.Where(c => c.ParentID > 1), p => p.ParentID, c => c.ParentID);

			AssertQuery(query);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6311")]
		public void FullJoinTupleOverloadComposed([DataSources(
			TestProvName.AllSQLite,
			TestProvName.AllAccess,
			ProviderName.SqlCe,
			TestProvName.AllMySql,
			TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Parent
				.Where(p => p.ParentID < 3)
				.FullJoin(db.Child.Where(c => c.ParentID > 1), p => p.ParentID, c => c.ParentID)
				.Select(t => new { ParentID = (int?)t.Outer!.ParentID, ChildID = (int?)t.Inner!.ChildID });

			AssertQuery(query);
		}
	}
}
#endif
