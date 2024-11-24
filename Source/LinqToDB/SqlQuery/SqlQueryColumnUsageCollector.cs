using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	using Common;
	using Visitors;

	public class SqlQueryColumnUsageCollector : SqlQueryVisitor
	{
		SelectQuery?                _parentSelectQuery;
		readonly HashSet<SqlColumn> _usedColumns = new(Utils.ObjectReferenceEqualityComparer<SqlColumn>.Default);

		public SqlQueryColumnUsageCollector() : base(VisitMode.ReadOnly, null)
		{
		}

		public override void Cleanup()
		{
			base.Cleanup();

			_parentSelectQuery = null;
			_usedColumns.Clear();
		}

		public IReadOnlyCollection<SqlColumn> UsedColumns => _usedColumns;

		public IQueryElement CollectUsedColumns(IQueryElement element)
		{
			Cleanup();
			var result = Visit(element);
			return result;
		}

		void RegisterColumn(SqlColumn column)
		{
			if (!_usedColumns.Add(column))
				return;

			if (column.Parent?.HasSetOperators == true)
			{
				var idx = column.Parent.Select.Columns.IndexOf(column);
				if (idx >= 0)
				{
					foreach (var set in column.Parent.SetOperators)
					{
						RegisterColumn(set.SelectQuery.Select.Columns[idx]);
					}
				}
			}

			column.Expression.VisitParentFirst(this, (v, e) =>
			{
				if (e is SqlSelectClause selectClause)
				{
					foreach(var ec in selectClause.Columns)
					{
						_usedColumns.Add(ec);
					}

					return false;
				}

				if (e is SqlColumn c)
				{
					v.RegisterColumn(c);
				}

				return true;
			});
		}

		protected override IQueryElement VisitSqlColumnReference(SqlColumn element)
		{
			RegisterColumn(element);

			return base.VisitSqlColumnReference(element);
		}

		protected override IQueryElement VisitSqlGroupByClause(SqlGroupByClause element)
		{
			var saveParentQuery = _parentSelectQuery;
			_parentSelectQuery = null;

			var result = base.VisitSqlGroupByClause(element);

			_parentSelectQuery = saveParentQuery;
			return result;
		}

		protected override IQueryElement VisitSqlOrderByClause(SqlOrderByClause element)
		{
			var saveParentQuery = _parentSelectQuery;
			_parentSelectQuery = null;

			var result = base.VisitSqlOrderByClause(element);

			_parentSelectQuery = saveParentQuery;
			return result;
		}

		protected override IQueryElement VisitSqlSearchCondition(SqlSearchCondition element)
		{
			var saveParentQuery = _parentSelectQuery;
			_parentSelectQuery = null;

			var result = base.VisitSqlSearchCondition(element);

			_parentSelectQuery = saveParentQuery;
			return result;
		}

		protected override ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
		{
			if (!_usedColumns.Contains(column))
			{
				return expression;
			}

			var saveParentQuery = _parentSelectQuery;

			_parentSelectQuery = null;

			var newExpression =  base.VisitSqlColumnExpression(column, expression);

			_parentSelectQuery = saveParentQuery;

			return newExpression;
		}

		protected override IQueryElement VisitExprExprPredicate(SqlPredicate.ExprExpr predicate)
		{
			base.VisitExprExprPredicate(predicate);

			var unwrapped1 = QueryHelper.UnwrapNullablity(predicate.Expr1);
			var unwrapped2 = QueryHelper.UnwrapNullablity(predicate.Expr2);

			if (unwrapped1.ElementType == QueryElementType.SqlQuery)
			{
				foreach (var column in ((SelectQuery)unwrapped1).Select.Columns)
				{
					RegisterColumn(column);
				}
			}
			else if (unwrapped1.ElementType == QueryElementType.SqlRow && unwrapped2 is SelectQuery selectQuery2)
			{
				foreach (var column in selectQuery2.Select.Columns)
				{
					RegisterColumn(column);
				}
			}

			if (unwrapped2.ElementType == QueryElementType.SqlQuery)
			{
				foreach (var column in ((SelectQuery)unwrapped2).Select.Columns)
				{
					RegisterColumn(column);
				}
			}
			else if (unwrapped2.ElementType == QueryElementType.SqlRow && unwrapped1 is SelectQuery selectQuery1)
			{
				foreach (var column in selectQuery1.Select.Columns)
				{
					RegisterColumn(column);
				}
			}

			return predicate;
		}

		protected override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
		{
			if (_parentSelectQuery == null || selectQuery.Select.IsDistinct || selectQuery.From.Tables.Count == 0
				// we cannot remove unused columns for non-UNION ALL operators as it could affect result
				|| (selectQuery.HasSetOperators && selectQuery.SetOperators.Any(o => o.Operation != SetOperation.UnionAll)))
			{
				foreach (var c in selectQuery.Select.Columns)
				{
					RegisterColumn(c);
				}

				if (selectQuery.HasSetOperators)
				{
					foreach (var so in selectQuery.SetOperators)
					{
						foreach (var c in so.SelectQuery.Select.Columns)
						{
							RegisterColumn(c);
						}
					}
				}
			}
			else
			{
				if (!selectQuery.GroupBy.IsEmpty)
				{
					if (selectQuery.Select.Columns.Count == 1)
						RegisterColumn(selectQuery.Select.Columns[0]);
				}
				else
				{
					foreach (var column in selectQuery.Select.Columns)
					{
						if (QueryHelper.ContainsAggregationOrWindowFunction(column.Expression))
						{
							RegisterColumn(column);
							break;
						}
					}
				}
			}

			var saveParentQuery = _parentSelectQuery;
			_parentSelectQuery  = selectQuery;

			base.VisitSqlQuery(selectQuery);

			_parentSelectQuery  = saveParentQuery;

			return selectQuery;
		}

	}
}
