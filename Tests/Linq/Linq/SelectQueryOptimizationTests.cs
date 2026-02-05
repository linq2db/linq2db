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
		public void CountFromGroupByShouldIgnoreAllColumnsAndReplaceWithGroupingKey()
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
			groupingQuery.Select.Columns.Count.ShouldBe(1);
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
				.GroupBy(x => new { x.ParentID, ChildID = x.ChildID + 1 })
				.Select(x => x.Key);

			var groupingSelect = query.GetSelectQuery();

			groupingSelect.Select.IsDistinct.ShouldBeTrue();
			groupingSelect.GroupBy.IsEmpty.ShouldBeTrue();
		}

		[Test]
		public void GroupByShouldBeRemovedWhenGroupingKeyIsUnique()
		{
			using var db = GetDataConnection();

			var query = db.Child
				.GroupBy(x => new { x.ParentID, ChildID = x.ChildID })
				.Select(x => x.Key);

			var groupingSelect = query.GetSelectQuery();

			groupingSelect.Select.IsDistinct.ShouldBeFalse();
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
		public void GroupByWithSelectingGroupingKeysWithOrderByAggregateShouldNotOptimize()
		{
			using var db = GetDataConnection();

			var query = db.Child
				.GroupBy(x => new { x.ParentID, x.ChildID })
				.OrderBy(g => g.Sum(x => x.ChildID))
				.Select(x => x.Key);

			var groupingSelect = query.GetSelectQuery();

			groupingSelect.Select.IsDistinct.ShouldBeFalse();
			groupingSelect.GroupBy.IsEmpty.ShouldBeFalse();
			groupingSelect.OrderBy.IsEmpty.ShouldBeFalse();
		}

		[Test]
		public void PopulatingOrderingFromManyDerivedTables()
		{
			using var db = GetDataConnection();

			var query =
				from p in db.Parent
				orderby p.ParentID
				from c in db.Child
					.Where(c => c.ParentID == p.ParentID)
					.OrderBy(c => c.ChildID)
					.ThenBy(c => p.Children.OrderBy(x => x.ChildID).FirstOrDefault()!.ParentID)
				select new { p.ParentID, c.ChildID };

			var selectQuery = query.GetSelectQuery();

			selectQuery.OrderBy.Items.Count.ShouldBe(3);
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

		[Test]
		public void OrderByPromotedFromSubqueryToParentQuery()
		{
			using var db = GetDataConnection();

			var query =
				from p in
					(from p in db.Parent
					orderby p.Value1
					select p)
				from c in db.Child.Where(c => c.ParentID == p.ParentID)
				select new { p.ParentID, c.ChildID };

			var selectQuery = query.GetSelectQuery();

			selectQuery.OrderBy.Items.Count.ShouldBe(1);
		}

		[Test]
		public void OrderByPreservedWhenCountAggregateUsesIt()
		{
			using var db = GetDataConnection();

			var query = db.Parent
				.OrderBy(p => p.ParentID)
				.Select(p => new { p.ParentID, Count = db.Child.Where(c => c.ParentID == p.ParentID).Count() });

			var selectQuery = query.GetSelectQuery();
			
			selectQuery.OrderBy.Items.Count.ShouldBe(1);
		}

		[Test]
		public void OrderByPreservedForAllSelectedColumnsWithDistinct()
		{
			using var db = GetDataConnection();

			var query = db.Child
				.OrderBy(c => c.ParentID)
				.ThenBy(c => c.ChildID)
				.Select(c => new { c.ParentID, c.ChildID })
				.Distinct();

			var selectQuery = query.GetSelectQuery();

			selectQuery.Select.IsDistinct.ShouldBeTrue();
			selectQuery.OrderBy.Items.Count.ShouldBe(2);
		}

		[Test]
		public void OrderByOptimizedToOnlyUsedColumnWithDistinct()
		{
			using var db = GetDataConnection();

			var query = db.Child
				.OrderBy(c => c.ParentID)
				.ThenBy(c => c.ChildID)
				.Select(c => c.ParentID)
				.Distinct();

			var selectQuery = query.GetSelectQuery();

			selectQuery.OrderBy.Items.Count.ShouldBe(1);
			var orderField = QueryHelper.GetUnderlyingField(selectQuery.OrderBy.Items[0].Expression);
			orderField.ShouldNotBeNull();
			orderField!.Name.ShouldBe("ParentID");
		}

		[Test]
		public void OrderByPreservedAfterGroupByWithOrdering()
		{
			using var db = GetDataConnection();

			var query = db.Child
				.GroupBy(c => c.ParentID)
				.OrderBy(g => g.Key)
				.Select(g => new { ParentID = g.Key, Count = g.Count() });

			var selectQuery = query.GetSelectQuery();

			selectQuery.OrderBy.Items.Count.ShouldBe(1);
		}

		[Test]
		public void OrderByRemovedFromBothQueriesInUnion()
		{
			using var db = GetDataConnection();

			var query1 = db.Child.Where(c => c.ParentID < 3).OrderBy(c => c.ChildID);
			var query2 = db.Child.Where(c => c.ParentID >= 3).OrderBy(c => c.ParentID);

			var union = query1.Union(query2);

			var selectQuery = union.GetSelectQuery();

			var setQuery1 = selectQuery;
			var setQuery2 = selectQuery.SetOperators[0].SelectQuery;

			setQuery1.OrderBy.IsEmpty.ShouldBeTrue();
			setQuery2.OrderBy.IsEmpty.ShouldBeTrue();
		}

		[Test]
		public void OrderByKeptInBothQueriesForConcatUnionAll()
		{
			using var db = GetDataConnection();

			var query1 = db.Child.Where(c => c.ParentID < 3).OrderBy(c => c.ChildID);
			var query2 = db.Child.Where(c => c.ParentID >= 3).OrderBy(c => c.ParentID);

			var concat = query1.Concat(query2);

			var selectQuery = concat.GetSelectQuery();

			var setQuery1 = (SelectQuery)selectQuery.From.Tables[0].Source;
			var setQuery2 = (SelectQuery)selectQuery.SetOperators[0].SelectQuery.From.Tables[0].Source;

			setQuery1.OrderBy.Items.Count.ShouldBe(1);
			setQuery2.OrderBy.Items.Count.ShouldBe(1);
		}

		[Test]
		public void OrderByWithBothDirectionsPreservedWithTake()
		{
			using var db = GetDataConnection();

			var query = db.Parent
				.OrderBy(p => p.Value1)
				.ThenByDescending(p => p.ParentID)
				.Take(5);

			var selectQuery = query.GetSelectQuery();

			selectQuery.OrderBy.Items.Count.ShouldBe(2);
			selectQuery.OrderBy.Items[0].IsDescending.ShouldBeFalse();
			selectQuery.OrderBy.Items[1].IsDescending.ShouldBeTrue();
		}

		[Test]
		public void OrderByRequiredForSkipTakePagination()
		{
			using var db = GetDataConnection();

			var query = db.Parent
				.OrderBy(p => p.ParentID)
				.Skip(2)
				.Take(3);

			var selectQuery = query.GetSelectQuery();

			selectQuery.OrderBy.Items.Count.ShouldBe(1);
		}

		[Test]
		public void OrderByPromotedFromMultipleJoinedSources()
		{
			using var db = GetDataConnection();

			var query =
				from p in db.Parent.OrderBy(p => p.ParentID)
				join c in db.Child.OrderBy(c => c.ChildID) on p.ParentID equals c.ParentID
				join g in db.GrandChild.OrderBy(g => g.GrandChildID ?? 0) on c.ChildID equals g.ChildID!.Value
				select new { p.ParentID, c.ChildID, g.GrandChildID };

			var selectQuery = query.GetSelectQuery();

			selectQuery.OrderBy.Items.Count.ShouldBe(3);
		}

		[Test]
		public void OrderByRemovedFromExistsPredicateSubquery()
		{
			using var db = GetDataConnection();

			var query = db.Parent.Where(p => 
				db.Child.OrderBy(c => c.ChildID).Any(c => c.ParentID == p.ParentID));

			query.ToArray();

			var selectQuery = query.GetSelectQuery();
			selectQuery.Where.SearchCondition.Predicates.ShouldNotBeEmpty();
		}

		[Test]
		public void OrderByPreservedInFirstOrDefaultCorrelatedSubquery()
		{
			using var db = GetDataConnection();

			var query = db.Parent
				.Select(p => new
				{
					p.ParentID,
					FirstChild = db.Child
						.Where(c => c.ParentID == p.ParentID)
						.OrderBy(c => c.ChildID)
						.Select(c => c.ChildID)
						.FirstOrDefault()
				});

			var selectQuery = query.GetSelectQuery();
			selectQuery.Select.Columns.Count.ShouldBe(2);
		}

		[Test]
		public void OrderByReplacedWhenQueryingFromCteWithNewOrdering()
		{
			using var db = GetDataConnection();

			var cte = db.Child
				.OrderBy(c => c.ParentID)
				.ThenBy(c => c.ChildID)
				.AsCte();

			var query = cte
				.Where(c => c.ParentID > 1)
				.OrderByDescending(c => c.ChildID);

			var selectQuery = query.GetSelectQuery();

			selectQuery.OrderBy.Items.Count.ShouldBeGreaterThanOrEqualTo(1);
		}

		[Test]
		public void OrderByWithCoalesceAndCalculatedExpressions()
		{
			using var db = GetDataConnection();

			var query = db.Parent
				.OrderBy(p => p.Value1 ?? p.ParentID)
				.ThenByDescending(p => p.Value1.HasValue ? p.Value1.Value * 2 : p.ParentID)
				.Select(p => new { p.ParentID, p.Value1 });

			var selectQuery = query.GetSelectQuery();

			selectQuery.OrderBy.Items.Count.ShouldBe(2);
		}

		[Test]
		public void OrderByDeduplicatesIdenticalOrderingExpressions()
		{
			using var db = GetDataConnection();

			var query = db.Parent
				.OrderBy(p => p.ParentID)
				.ThenBy(p => p.Value1)
				.ThenBy(p => p.ParentID)
				.ThenBy(p => p.Value1);

			var selectQuery = query.GetSelectQuery();

			selectQuery.OrderBy.Items.Count.ShouldBe(2);
		}

		[Test]
		public void OrderByFromSubqueryReplacedByOuterOrdering()
		{
			using var db = GetDataConnection();

			var subQuery = db.Child
				.OrderBy(c => c.ChildID)
				.Select(c => new { c.ParentID, c.ChildID });

			var query = subQuery
				.OrderByDescending(c => c.ParentID)
				.ThenBy(c => c.ChildID);

			var selectQuery = query.GetSelectQuery();

			selectQuery.OrderBy.Items.Count.ShouldBeGreaterThanOrEqualTo(2);
		}

		[Test]
		public void OrderByWithConditionalExpressionInLeftJoin()
		{
			using var db = GetDataConnection();

			var query =
				from p in db.Parent.OrderBy(p => p.ParentID)
				from c in db.Child.Where(c => c.ParentID == p.ParentID).DefaultIfEmpty()
				orderby c == null ? 0 : c.ChildID
				select new { p.ParentID, ChildID = (int?)c.ChildID };

			var selectQuery = query.GetSelectQuery();

			selectQuery.OrderBy.Items.Count.ShouldBeGreaterThanOrEqualTo(1);
		}

		[Test]
		public void OrderByRemovedFromBothQueriesInExceptOperation()
		{
			using var db = GetDataConnection();

			var query1 = db.Child.OrderBy(c => c.ParentID);
			var query2 = db.Child.OrderByDescending(c => c.ChildID);

			var except = query1.Except(query2);

			var selectQuery = except.GetSelectQuery();

			var setQuery1 = selectQuery;
			var setQuery2 = selectQuery.SetOperators[0].SelectQuery;

			setQuery1.OrderBy.IsEmpty.ShouldBeTrue();
			setQuery2.OrderBy.IsEmpty.ShouldBeTrue();
		}

		[Test]
		public void OrderByPreservedInBothQueriesWhenIntersectHasTake()
		{
			using var db = GetDataConnection();

			var query1 = db.Child.OrderBy(c => c.ParentID).Take(10);
			var query2 = db.Child.OrderByDescending(c => c.ChildID).Take(5);

			var intersect = query1.Intersect(query2);

			var selectQuery = intersect.GetSelectQuery();

			var setQuery1 = (SelectQuery)selectQuery.From.Tables[0].Source;
			var setQuery2 = (SelectQuery)selectQuery.SetOperators[0].SelectQuery.From.Tables[0].Source;

			setQuery1.OrderBy.Items.Count.ShouldBe(1);
			setQuery2.OrderBy.Items.Count.ShouldBe(1);
		}

		[Test]
		public void OrderByRequiredForEnumerableSelectWithIndex()
		{
			using var db = GetDataConnection();

			var query = db.Parent
				.OrderBy(p => p.ParentID)
				.Select((p, index) => new { p.ParentID, RowNum = index + 1 });

			var selectQuery = query.GetSelectQuery();

			selectQuery.OrderBy.Items.Count.ShouldBe(1);
		}

		[Test]
		public void OrderByPromotedFromSubqueryUsedInJoinCondition()
		{
			using var db = GetDataConnection();

			var orderedChildren = db.Child
				.OrderBy(c => c.ChildID)
				.Select(c => new { c.ParentID, c.ChildID });

			var query =
				from p in db.Parent
				join c in orderedChildren on p.ParentID equals c.ParentID
				select new { p.ParentID, c.ChildID };

			var selectQuery = query.GetSelectQuery();

			selectQuery.OrderBy.Items.Count.ShouldBe(1);
		}

		[Test]
		public void OrderByAndGroupByConstantAndLimitOrderShouldBeRemoved()
		{
			using var db = GetDataConnection();
			
			var qry =
				from ch in db.Child
				orderby ch.ChildID
				select ch;

			var query =
				from ch in qry
				group ch by 1 into g
				select new
				{
					Count = g.Count(),
					Expr  = 1 + g.Min(c => c.ChildID),
					Max   = g.Max(c => c.ChildID),
				};

			query = query.Take(1);

			var selectQuery = query.GetSelectQuery();
			selectQuery.OrderBy.IsEmpty.ShouldBeTrue();
			selectQuery.IsLimited.ShouldBeTrue();
		}

		[Test]
		public void GroupByShouldBeRemovedOnUniqueKeyGrouping()
		{
			using var db = GetDataConnection();

			var query =
				from ch in db.Child
				group ch by new { ch.ChildID,  ch.ParentID } into g
				select new
				{
					Value = 1,
					Key   = g.Key,
				};

			var selectQuery = query.GetSelectQuery();

			selectQuery.GroupBy.IsEmpty.ShouldBeTrue();
		}

		[Test]
		public void GroupByWithAggregateShouldRemainOnUniqueKeyGrouping()
		{
			using var db = GetDataConnection();

			var query =
				from ch in db.Child
				group ch by new { ch.ChildID,  ch.ParentID } into g
				select new
				{
					Value = g.Count(),
					Key   = g.Key,
				};

			var selectQuery = query.GetSelectQuery();

			selectQuery.GroupBy.IsEmpty.ShouldBeFalse();
		}

		[Test]
		public void GroupByWithReducedSelectShouldRemain()
		{
			using var db = GetDataConnection();

			var query =
				from ch in db.Child
				group ch by new { Key1 = ch.ChildID, Key2 = ch.ChildID + 1 } into g
				select new { Key = g.Key, }
				into s
				select s.Key.Key1;

			var selectQuery = query.GetSelectQuery();

			selectQuery.GroupBy.IsEmpty.ShouldBeFalse();
			selectQuery.Select.IsDistinct.ShouldBeFalse();
		}

		[Test]
		public void GroupByWithGroupingSetsShouldRemain()
		{
			using var db = GetDataConnection();

			var query =
				from ch in db.Child
				group ch by Sql.GroupBy.GroupingSets(new { Set1 = new { ch.ChildID, ch.ParentID }, Set2 = new { ch.ParentID }, Set3 = new {}}) into g
				select new { Key = g.Key.Set1, }
				into s
				select s.Key.ChildID;

			var selectQuery = query.GetSelectQuery();

			selectQuery.GroupBy.IsEmpty.ShouldBeFalse();
			selectQuery.Select.IsDistinct.ShouldBeFalse();
		}

		[Test]
		public void CountOrderByShouldBeRemoved()
		{
			using var db = GetDataConnection();

			var query = db.Parent
				.OrderBy(p => p.ParentID);

			var selectQuery = query.GetSelectQuery(q => q.Count());

			selectQuery.OrderBy.IsEmpty.ShouldBeTrue();
		}

	}
}
