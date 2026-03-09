using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1397Tests : TestBase
	{
		[Test]
		public void ConcatJoinTest([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var query =
					from m in db.Parent
					from id in
						(
							from t in db.Parent
							where t.ParentID == 1
							select t.ParentID
						)
						.Concat
						(
							from t in db.Parent
							where t.ParentID == 2
							select t.ParentID
						)
						.InnerJoin(id => id == m.ParentID)
					select m;

			var expected =
					from m in db.Parent
					from id in
						(
							from t in db.Parent
							where t.ParentID == 1
							select t.ParentID
						)
						.Concat
						(
							from t in db.Parent
							where t.ParentID == 2
							select t.ParentID
						)
					where id == m.ParentID
					select m;

			AreEqual(expected, query);
		}

		[Test]
		public void ConcatJoinTestChain([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var query =
					db.Parent.InnerJoin(
						db.Parent.Where(t => t.ParentID == 1).Select(t => t.ParentID).Concat(
						db.Parent.Where(t => t.ParentID == 2).Select(t => t.ParentID)),
					(m, id) => m.ParentID == id, (m, id) => m);

			var expected =
					from m in db.Parent
					from id in
						(
							from t in db.Parent
							where t.ParentID == 1
							select t.ParentID
						)
						.Concat
						(
							from t in db.Parent
							where t.ParentID == 2
							select t.ParentID
						)
					where id == m.ParentID
					select m;

			AreEqual(expected, query);
		}
	}
}
