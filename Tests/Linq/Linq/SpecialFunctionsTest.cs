using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;

using NUnit.Framework;

namespace Tests.Linq
{
	public class SpecialFunctionsTest : TestBase
	{
		[Test]
		public void Ordinal([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			var query = from q in db.Parent.AsQueryable()
						orderby Sql.Ordinal(q.ParentID), q.Value1
						select q;

			var result = query.ToList();

			var orderBy = query.GetSelectQuery().OrderBy;

			Assert.That(orderBy.Items, Has.Count.EqualTo(2));
			Assert.That(orderBy.Items[0].IsPositioned, Is.True);
		}

		[Test]
		public void Parameter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			var query = from q in db.Parent.AsQueryable()
						where q.ParentID == Sql.Parameter(1)
						select q;

			var result = query.ToList();

			var cacheMissCount = query.GetCacheMissCount();

			var query2 = from q in db.Parent.AsQueryable()
						 where q.ParentID == Sql.Parameter(2)
						 select q;

			var result2 = query2.ToList();

			Assert.That(query2.GetCacheMissCount(), Is.EqualTo(cacheMissCount));

			var parameters = new List<SqlParameter>();
			query.GetSelectQuery().CollectParameters(parameters);

			Assert.That(parameters, Has.Count.EqualTo(1));
			Assert.That(parameters[0].IsQueryParameter, Is.True);
		}

		[Test]
		public void Constant([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values(1, 2)] int iteration)
		{
			using var db = GetDataContext(context);
			var parentId = iteration;

			var cacheMissCount = db.Parent.GetCacheMissCount();

			var query = from q in db.Parent.AsQueryable()
						where q.ParentID == Sql.Constant(parentId)
						select q;
			var result = query.ToList();

			if (iteration > 1)
			{
				Assert.That(query.GetCacheMissCount(), Is.EqualTo(cacheMissCount));
			}

			var parameters = new List<SqlParameter>();
			query.GetSelectQuery().CollectParameters(parameters);

			Assert.That(parameters, Has.Count.EqualTo(1));
			Assert.That(parameters[0].IsQueryParameter, Is.False);
		}
	}
	
}
