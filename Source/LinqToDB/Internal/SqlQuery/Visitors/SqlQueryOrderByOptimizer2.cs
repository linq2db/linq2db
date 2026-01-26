using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.SqlProvider;

namespace LinqToDB.Internal.SqlQuery.Visitors
{
	public sealed class SqlQueryOrderByOptimizer2 : QueryElementVisitor
	{
		SqlProviderFlags               _providerFlags          = default!;
		SqlQueryColumnNestingCorrector _columnNestingCorrector = default!;

		bool _optimized;

		public bool IsOptimized        => _optimized;

		public SqlQueryOrderByOptimizer2() : base(VisitMode.Modify)
		{
		}

		public override void Cleanup()
		{
			base.Cleanup();
			_optimized              = false;
			_providerFlags          = default!;
			_columnNestingCorrector = default!;
		}

		public void OptimizeOrderBy(IQueryElement element, SqlProviderFlags providerFlags, SqlQueryColumnNestingCorrector columnNestingCorrector)
		{
			Cleanup();

			_providerFlags          = providerFlags;
			_columnNestingCorrector = columnNestingCorrector;

			Visit(element);
		}

		protected internal override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
		{
			Visit(selectQuery.Select);
			Visit(selectQuery.GroupBy);
			Visit(selectQuery.OrderBy);
			Visit(selectQuery.Where);
			Visit(selectQuery.Having);

			var needsNestingUpdate = false;

			if (CorrectOrderByForSelectQuery(selectQuery, null, null, [], ref needsNestingUpdate))
			{
				_optimized = true;
				if (needsNestingUpdate)
				{
					_columnNestingCorrector.CorrectColumnNesting(selectQuery);
				}
			}

			return selectQuery;
		}

		bool CorrectOrderByForSelectQuery(SelectQuery selectQuery, SelectQuery? parentSelectQuery, SqlSetOperator? setOperator, Stack<SelectQuery> doNotAcceptOrder, ref bool needsNestingUpdate)
		{
			var optimized = false;

			if (selectQuery.HasSetOperators && (setOperator == null || setOperator.SelectQuery == selectQuery))
			{
				var firstSetOperation = selectQuery.SetOperators[0];

				if (firstSetOperation.Operation != SetOperation.UnionAll)
				{
					RemoveOrderBy(selectQuery, true);
				}

				foreach (var so in selectQuery.SetOperators)
				{
					if (so.Operation != SetOperation.UnionAll)
					{
						RemoveOrderBy(so.SelectQuery, false);
					}
				}

				doNotAcceptOrder.Push(selectQuery);
				if (CorrectOrderByForSelectQuery(selectQuery, null, firstSetOperation, doNotAcceptOrder, ref needsNestingUpdate))
					optimized = true;
				doNotAcceptOrder.Pop();

				foreach (var so in selectQuery.SetOperators)
				{
					doNotAcceptOrder.Push(so.SelectQuery);
					if (CorrectOrderByForSelectQuery(so.SelectQuery, null, setOperator, doNotAcceptOrder, ref needsNestingUpdate))
						optimized = true;
					doNotAcceptOrder.Pop();
				}
			}

			foreach (var sqlTableSource in selectQuery.From.Tables)
			{
				CorrectOrderByInSource(sqlTableSource, ref needsNestingUpdate);
			}

			if (CanRemoveOrderBy(selectQuery))
			{
				if (QueryHelper.IsAggregationQuery(selectQuery, out var needsOrderBy))
				{
					if (!needsOrderBy)
						selectQuery.OrderBy.Items.Clear();
				}
				else if (parentSelectQuery != null)
				{
					if (!parentSelectQuery.HasSetOperators 
					    && !doNotAcceptOrder.Contains(parentSelectQuery)
						&& !(parentSelectQuery.GroupBy.IsEmpty && QueryHelper.IsAggregationQuery(parentSelectQuery, out var parentNeedsOrderBy) && parentNeedsOrderBy)
					    )
					{
						for (var i = 0; i < selectQuery.OrderBy.Items.Count; i++)
						{
							var orderByItem = selectQuery.OrderBy.Items[i];

							var canPopulateUpperLevel = true;

							if (parentSelectQuery.Select.IsDistinct)
							{
								canPopulateUpperLevel = parentSelectQuery.Select.Columns.Any(c =>
								{
									if (c.Expression is SqlColumn column)
										return QueryHelper.SameWithoutNullablity(column.Expression, orderByItem.Expression);
									return false;
								});
							}

							if (canPopulateUpperLevel && !parentSelectQuery.GroupBy.IsEmpty)
							{
								canPopulateUpperLevel = selectQuery.Select.Columns.Any(c => QueryHelper.SameWithoutNullablity(c.Expression, orderByItem.Expression));
							}

							if (canPopulateUpperLevel)
							{
								var column = selectQuery.Select.AddColumn(orderByItem.Expression);
								parentSelectQuery.OrderBy.Items.Add(new SqlOrderByItem(column, orderByItem.IsDescending, orderByItem.IsPositioned));

								needsNestingUpdate = true;
							}

							selectQuery.OrderBy.Items.RemoveAt(i);

							i--;
							optimized = true;
						}
					}
				}
			}

			if (selectQuery.OrderBy.Items.Count > 1)
			{
				var previousCount = selectQuery.OrderBy.Items.Count;
				selectQuery.OrderBy.Items.RemoveDuplicates(item => item.Expression);

				if (previousCount != selectQuery.OrderBy.Items.Count)
					optimized = true;
			}

			return optimized;

			// Local functions

			void CorrectOrderByInSource(SqlTableSource sqlTableSource, ref bool needsNestingUpdate)
			{
				if (sqlTableSource.Source is SelectQuery subQuery)
				{
					if (CorrectOrderByForSelectQuery(subQuery, selectQuery, null, doNotAcceptOrder, ref needsNestingUpdate))
						optimized = true;
				}

				foreach (var join in sqlTableSource.Joins)
				{
					CorrectOrderByInSource(join.Table, ref needsNestingUpdate);
				}
			}
		}

		static bool CanRemoveOrderBy(SelectQuery selectQuery)
		{
			if (selectQuery.OrderBy.IsEmpty)
				return false;

			if (QueryHelper.IsAggregationQuery(selectQuery, out var needsOrderBy))
			{
				if (needsOrderBy)
					return false;
				return true;
			}

			if (selectQuery.IsLimited)
				return false;
			return true;
		}

		void RemoveOrderBy(SelectQuery selectQuery, bool exceptSetOperators)
		{
			if (CanRemoveOrderBy(selectQuery))
			{
				selectQuery.OrderBy.Items.Clear();
				_optimized = true;
			}

			if (!exceptSetOperators && selectQuery.HasSetOperators)
			{
				foreach (var so in selectQuery.SetOperators)
				{
					RemoveOrderBy(so.SelectQuery, false);
				}
			}

			foreach (var sqlTableSource in selectQuery.From.Tables)
			{
				RemoveOrderByFromSource(sqlTableSource);
			}

			void RemoveOrderByFromSource(SqlTableSource sqlTableSource)
			{
				if (sqlTableSource.Source is SelectQuery subQuery)
				{
					RemoveOrderBy(subQuery, false);
				}

				foreach (var join in sqlTableSource.Joins)
				{
					RemoveOrderByFromSource(join.Table);
				}
			}
		}

		protected internal override IQueryElement VisitCteClause(CteClause element)
		{
			base.VisitCteClause(element);

			if (element.Body is { HasSetOperators: false, OrderBy.IsEmpty: false } cteQuery)
			{
				if (!_providerFlags.IsCTESupportsOrdering)
				{
					RemoveOrderBy(cteQuery, false);
				}
			}

			return element;
		}
	}
}
