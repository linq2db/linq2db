using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class LeftJoinTests : TestBase
	{
		public class A
		{
			public int Id { get; set; }
		}

		public class B
		{
			public int Id { get; set; }
			public int AId { get; set; }
		}

		public class C
		{
			public int Id { get; set; }
			public int BId { get; set; }
		}

		[Test]
		public void left_join_on_sub_query_with_two_inner_joins_results_in_incorrect_SQL([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<A>())
			using (db.CreateLocalTable<B>())
			using (db.CreateLocalTable<C>())
			{
				db.Insert(new A { Id = 1 });

				var query =
					from a in db.GetTable<A>()

					join bc in
						from b in db.GetTable<B>()
						join c in db.GetTable<C>() on b.Id equals c.BId
						select new { aid = b.AId, bid = b.Id, cid = c.Id }
					on a.Id equals bc.aid into gbc

					from bc in gbc.DefaultIfEmpty()

					select new { aid = a.Id, bc };

				var result = query.ToArray();
				Assert.That(result, Has.Length.EqualTo(1));
			}
		}

		[Test]
		public void LeftJoinGroupTest([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
				join c in db.Child on p.Value1 equals c.ParentID into g
				from c in g.DefaultIfEmpty()
				where g == null
				select p.ParentID;

			_ = q.ToSqlQuery().Sql;
		}
	}
}
