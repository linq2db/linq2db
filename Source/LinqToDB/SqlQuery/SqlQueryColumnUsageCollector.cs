using System;
using System.Collections.Generic;

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

			if (QueryHelper.UnwrapNullablity(predicate.Expr1).ElementType == QueryElementType.SqlRow && QueryHelper.UnwrapNullablity(predicate.Expr2) is SelectQuery selectQuery2)
			{
				foreach (var column in selectQuery2.Select.Columns)
				{
					RegisterColumn(column);
				}
			}

			if (QueryHelper.UnwrapNullablity(predicate.Expr2).ElementType == QueryElementType.SqlRow && QueryHelper.UnwrapNullablity(predicate.Expr1) is SelectQuery selectQuery1)
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
			if (_parentSelectQuery == null || selectQuery.HasSetOperators || selectQuery.Select.IsDistinct || selectQuery.From.Tables.Count == 0)
			{
				foreach (var c in selectQuery.Select.Columns)
				{
					RegisterColumn(c);
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

			var saveParentQuery = _parentSelectQuery;
			_parentSelectQuery  = selectQuery;

			base.VisitSqlQuery(selectQuery);

			_parentSelectQuery  = saveParentQuery;

			return selectQuery;
		}

	}
}
