using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.SqlProvider;

namespace LinqToDB.Internal.SqlQuery.Visitors
{
	public sealed class SqlQueryOrderByOptimizer : QueryElementVisitor
	{
		SqlProviderFlags               _providerFlags          = default!;
		SqlQueryColumnNestingCorrector _columnNestingCorrector = default!;

		public bool IsOptimized { get; private set; }

		public SqlQueryOrderByOptimizer() : base(VisitMode.Modify)
		{
		}

		public override void Cleanup()
		{
			base.Cleanup();
			IsOptimized             = false;
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
				IsOptimized = true;
				if (needsNestingUpdate)
				{
					_columnNestingCorrector.CorrectColumnNesting(selectQuery);
				}
			}

			Visit(selectQuery.From);

			return selectQuery;
		}

		bool CorrectOrderByForSelectQuery(SelectQuery selectQuery, SelectQuery? parentSelectQuery, SqlSetOperator? setOperator, Stack<SelectQuery> doNotAcceptOrder, ref bool needsNestingUpdate)
		{
			var optimized = false;

			if (selectQuery.HasSetOperators && (setOperator == null || setOperator.SelectQuery == selectQuery))
			{
				var firstSetOperation = selectQuery.SetOperators[0];

				if (firstSetOperation.Operation != SetOperation.UnionAll || !_providerFlags.IsUnionAllOrderBySupported)
				{
					RemoveOrderBy(selectQuery, true);
				}

				foreach (var so in selectQuery.SetOperators)
				{
					if (so.Operation != SetOperation.UnionAll || !_providerFlags.IsUnionAllOrderBySupported)
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
					if (CorrectOrderByForSelectQuery(so.SelectQuery, null, so, doNotAcceptOrder, ref needsNestingUpdate))
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

							bool canPopulateUpperLevel = !selectQuery.IsLimitedToOneRecord();

							if (canPopulateUpperLevel && parentSelectQuery.Select.IsDistinct)
							{
								// The order can move above a DISTINCT parent only when its expression is built entirely
								// from columns/fields the DISTINCT projects (#5626). Then it is functionally determined by
								// the DISTINCT output, so ordering by it above the DISTINCT is well-defined and exposing it
								// as an extra output column cannot change which rows are distinct. Ordering by anything
								// outside the projection stays unsupported and the order is dropped.
								canPopulateUpperLevel = AllOrderColumnsProducedByDistinct(parentSelectQuery, orderByItem.Expression);
							}

							if (canPopulateUpperLevel && !parentSelectQuery.GroupBy.IsEmpty)
							{
								// The order can move above a GROUP BY parent only when its expression is built entirely
								// from the grouping keys (#5626): `GROUP BY a, b ... ORDER BY a * 100 + b` is valid, but
								// ordering by a non-grouped column is not. The exact-column form is kept for back-compat.
								canPopulateUpperLevel =
									selectQuery.Select.Columns.Exists(c => QueryHelper.SameWithoutNullablity(c.Expression, orderByItem.Expression))
									|| AllOrderColumnsAreGroupingKeys(parentSelectQuery, orderByItem.Expression);
							}

							if (canPopulateUpperLevel)
							{
								// Raw-template AST nodes (Sql.Expr / Sql.Fragment) may carry trailing direction
								// modifiers in their template text (e.g. "{0} NULLS FIRST"). Wrapping them as a synthetic
								// subquery column captured the modifier inside the column expression and emitted invalid
								// SQL like `... END NULLS FIRST as c1`. Push such expressions directly to the parent ORDER
								// BY so the modifier stays in the outer clause.
								// When a DISTINCT or GROUP BY is involved we also push the expression as-is instead of
								// materializing it as a column (#5626): the order has been validated to use only columns the
								// DISTINCT projects / grouping keys, so adding a synthetic column would needlessly widen the
								// DISTINCT key or the projection. The column-nesting corrector re-levels the references onto
								// the existing output columns.
								if (orderByItem.Expression is SqlExpression or SqlFragment
									|| parentSelectQuery.Select.IsDistinct
									|| selectQuery.Select.IsDistinct
									|| !parentSelectQuery.GroupBy.IsEmpty)
								{
									parentSelectQuery.OrderBy.Items.Add(new SqlOrderByItem(orderByItem.Expression, orderByItem.IsDescending, orderByItem.IsPositioned, orderByItem.NullsPosition));
								}
								else
								{
									// Every other AST node (structured expressions, columns, fields, functions, case, etc.)
									// keeps the AddColumn dedup path - their SQL output is always a valid scalar.
									var column = selectQuery.Select.AddColumn(orderByItem.Expression);
									parentSelectQuery.OrderBy.Items.Add(new SqlOrderByItem(column, orderByItem.IsDescending, orderByItem.IsPositioned, orderByItem.NullsPosition));
								}

								needsNestingUpdate = true;
							}

							selectQuery.OrderBy.Items.RemoveAt(i);
							i--;

							optimized = true;
						}
					}
				}
			}

			if (parentSelectQuery is { HasSetOperators: false } && !doNotAcceptOrder.Contains(parentSelectQuery))
			{
				if (!_providerFlags.IsSubQueryOrderBySupported)
					RemoveOrderBy(selectQuery, true);
			}

			if (!selectQuery.HasOrderBy)
			{
				if (selectQuery.From.Tables.Count == 1 && selectQuery.From.Tables[0].Source is SelectQuery sunQuery)
				{
					if (sunQuery.HasSetOperators)
					{
						RemoveOrderBy(sunQuery, false);
					}
				}
			}

			if (selectQuery.HasOrderBy)
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

		/// <summary>
		/// Returns <see langword="true"/> when every column/field referenced by <paramref name="orderExpression"/>
		/// is produced as an output column of the DISTINCT <paramref name="distinctQuery"/>. In that case the
		/// expression is functionally determined by the DISTINCT projection, so it can be ordered above the
		/// DISTINCT and exposed as an additional output column without changing which rows are kept.
		/// </summary>
		static bool AllOrderColumnsProducedByDistinct(SelectQuery distinctQuery, ISqlExpression orderExpression)
		{
			var referenced = new List<ISqlExpression>();

			orderExpression.VisitParentFirst(referenced, static (list, e) =>
			{
				if (e.ElementType is QueryElementType.Column or QueryElementType.SqlField)
				{
					list.Add((ISqlExpression)e);
					return false; // leaf reference - don't descend into the column/field definition
				}

				return true;
			});

			if (referenced.Count == 0)
				return false;

			foreach (var reference in referenced)
			{
				var produced = distinctQuery.Select.Columns.Exists(c =>
					c.Expression is SqlColumn column
						? QueryHelper.SameWithoutNullablity(column.Expression, reference)
						: QueryHelper.SameWithoutNullablity(c.Expression, reference));

				if (!produced)
					return false;
			}

			return true;
		}

		/// <summary>
		/// Returns <see langword="true"/> when every column/field referenced by <paramref name="orderExpression"/>
		/// is one of the grouping keys of <paramref name="groupedQuery"/>. In that case the expression is a function
		/// of the grouping keys, so it can be ordered above the GROUP BY. Restricted to a plain GROUP BY -
		/// ROLLUP/CUBE/GROUPING SETS emit super-aggregate rows where ordering by a key is not well-defined.
		/// </summary>
		static bool AllOrderColumnsAreGroupingKeys(SelectQuery groupedQuery, ISqlExpression orderExpression)
		{
			if (groupedQuery.GroupBy.GroupingType != GroupingType.Default)
				return false;

			var referenced = new List<ISqlExpression>();

			orderExpression.VisitParentFirst(referenced, static (list, e) =>
			{
				if (e.ElementType is QueryElementType.Column or QueryElementType.SqlField)
				{
					list.Add((ISqlExpression)e);
					return false; // leaf reference - don't descend into the column/field definition
				}

				return true;
			});

			if (referenced.Count == 0)
				return false;

			foreach (var reference in referenced)
			{
				var isGroupingKey = groupedQuery.GroupBy.Items.Exists(k =>
					k is SqlColumn column
						? QueryHelper.SameWithoutNullablity(column.Expression, reference)
						: QueryHelper.SameWithoutNullablity(k, reference));

				if (!isGroupingKey)
					return false;
			}

			return true;
		}

		void RemoveOrderBy(SelectQuery selectQuery, bool exceptSetOperators)
		{
			if (CanRemoveOrderBy(selectQuery))
			{
				selectQuery.OrderBy.Items.Clear();
				IsOptimized = true;
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

		protected internal override IQueryElement VisitExistsPredicate(SqlPredicate.Exists predicate)
		{
			RemoveOrderBy(predicate.SubQuery, false);
			return base.VisitExistsPredicate(predicate);
		}
	}
}
