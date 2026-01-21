using System.Linq;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	public class SelectQueryOptimizationTests : TestBase
	{
		[Test]
		public void CountFromGroupByShouldIgnoreAllColumnsAndReplaceWithValue()
		{
			using var db = GetDataConnection();

			var query = db.Child
				.GroupBy(x => x.ChildID)
				.Select(_ => new
				{
					x = _.Key,
					y = _.Average(r => r.ParentID)
				});

			var select = query.GetSelectQuery(q => q.Count());

			var groupingQuery = (SelectQuery)select.Select.From.Tables[0].Source;
			groupingQuery.Select.Columns[0].Expression.ShouldBeOfType<SqlValue>();
		}

		[Test]
		public void CountFromUnionAllShouldKeepOnlyOneColumn()
		{
			using var db = GetDataConnection();

			var query = db.Child.Concat(db.Child);

			var unionSelect = query.GetSelectQuery();
			unionSelect.Select.Columns.Count.ShouldBe(2);

			var countSelect = query.GetSelectQuery(q => q.Count());

			var countQuery = (SelectQuery)countSelect.Select.From.Tables[0].Source;
			countQuery.Select.Columns.Count.ShouldBe(1);
		}

		[Test]
		public void CountFromUnionShouldKeepAllColumns()
		{
			using var db = GetDataConnection();

			var query = db.Child.Union(db.Child);

			var unionSelect = query.GetSelectQuery();
			unionSelect.Select.Columns.Count.ShouldBe(2);

			var countSelect = query.GetSelectQuery(q => q.Count());

			var countQuery = (SelectQuery)countSelect.Select.From.Tables[0].Source;
			countQuery.Select.Columns.Count.ShouldBe(2);
		}

		[Test]
		public void GroupByWithSelectingGroupingKeysShouldBecomeDistinct()
		{
			using var db = GetDataConnection();

			var query = db.Child
				.GroupBy(x => new { x.ParentID, x.ChildID })
				.Select(x => x.Key);

			var groupingSelect = query.GetSelectQuery();

			groupingSelect.Select.IsDistinct.ShouldBeTrue();
			groupingSelect.GroupBy.IsEmpty.ShouldBeTrue();
		}

		[Test]
		public void GroupByWithSelectingGroupingKeysWithHavingShouldNotOptimize()
		{
			using var db = GetDataConnection();

			var query = db.Child
				.GroupBy(x => new { x.ParentID, x.ChildID })
				.Where(g => g.Count() > 1)
				.Select(x => x.Key);

			var groupingSelect = query.GetSelectQuery();

			groupingSelect.Select.IsDistinct.ShouldBeFalse();
			groupingSelect.GroupBy.IsEmpty.ShouldBeFalse();
			groupingSelect.Having.IsEmpty.ShouldBeFalse();
		}

		[Test]
		public void LeaveOnlyUsedFieldsInCte()
		{
			using var db = GetDataConnection();

			var cteQuery =
				from c in
				(
					from c in db.Child
					select new { c.ChildID, c.ParentID, Computed = c.ChildID + 1 }
				)
				where c.ChildID > 10
				select c;

			cteQuery = cteQuery.AsCte();

			var fullCteStatement = (SqlSelectStatement)cteQuery.GetStatement();
			fullCteStatement.With?.Clauses.Count.ShouldBe(1);

			var fullCteClause = fullCteStatement.With!.Clauses[0];
			fullCteClause.Fields.Count.ShouldBe(3);
			fullCteClause.Body?.Select.Columns.Count.ShouldBe(3);

			var query = 
				from c in cteQuery
				select new { c.ChildID };

			var selectStatement = (SqlSelectStatement)query.GetStatement();
			selectStatement.With?.Clauses.Count.ShouldBe(1);

			var cteClause = selectStatement.With!.Clauses[0];
			cteClause.Fields.Count.ShouldBe(1);
			cteClause.Body?.Select.Columns.Count.ShouldBe(1);
		}
	}
}
