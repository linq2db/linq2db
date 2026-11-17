#if NET10_0_OR_GREATER
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class RightJoinMethodTests : TestBase
	{
		[Test]
		public void RightJoinSimple([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				db.Parent.RightJoin(db.Child, p => p.ParentID, c => c.ParentID, (p, c) => new { ParentID = (int?)p!.ParentID, ChildID = (int?)c!.ChildID });

			AssertQuery(query);
		}

		[Test]
		public void RightJoinWithFilter([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				db.Parent
					.Where(p => p.ParentID >= 4)
					.RightJoin(db.Child, p => p.ParentID, c => c.ParentID, (p, c) => new { p, c });

			AssertQuery(query);
		}

		[Test]
		public void RightJoinWithNullableKey([DataSources] string context)
		{
			using var db = GetDataContext(context, o => o.OmitUnsupportedCompareNulls(context));

			var query =
				db.Parent.RightJoin(
					db.Parent,
					p1 => new { p1.ParentID, p1.Value1 },
					p2 => new { p2.ParentID, p2.Value1 },
					(p1, p2) => p2);

			AssertQuery(query);
		}

		[Test]
		public void RightJoinWithSubquery([DataSources(TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				db.Parent
					.Where(p => p.ParentID > 0)
					.Take(10)
					.RightJoin(
						db.Child,
						p => p.ParentID,
						c => c.ParentID,
						(p, c) => new { ParentID = (int?)p!.ParentID, ChildID = (int?)c!.ChildID });

			AssertQuery(query);
		}

		[Test]
		public void RightJoinWithCompositeKey([DataSources] string context)
		{
			using var db = GetDataContext(context, o => o.OmitUnsupportedCompareNulls(context));

			var query =
				db.Parent.RightJoin(
					db.Child,
					p => new { p.ParentID, ID2 = p.Value1 ?? 0 },
					c => new { c.ParentID, ID2 = c.ParentID },
					(p, c) => new { p, c });

			AssertQuery(query);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6310")]
		public void RightJoinWithNullComparer([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				db.Parent.RightJoin(db.Child, p => p.ParentID, c => c.ParentID, (p, c) => new { ParentID = (int?)p!.ParentID, ChildID = (int?)c!.ChildID }, null);

			AssertQuery(query);
		}

#if NET11_0_OR_GREATER
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6311")]
		public void RightJoinTupleOverload([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Parent.RightJoin(db.Child, p => p.ParentID, c => c.ParentID);

			AssertQuery(query);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6311")]
		public void RightJoinTupleOverloadComposed([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Parent
				.Where(p => p.ParentID >= 4)
				.RightJoin(db.Child, p => p.ParentID, c => c.ParentID)
				.Select(t => new { ParentID = (int?)t.Outer!.ParentID, t.Inner.ChildID });

			AssertQuery(query);
		}
#endif
	}
}
#endif
