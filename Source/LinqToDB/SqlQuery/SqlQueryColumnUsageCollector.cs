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
				if (e is SelectQuery)
					return false;

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
				foreach (var c in selectQuery.Select.Columns)
				{
					if (!_usedColumns.Contains(c))
					{
						if (!selectQuery.GroupBy.IsEmpty && selectQuery.Select.Columns.Count == 1 || QueryHelper.ContainsAggregationOrWindowFunction(c.Expression))
							RegisterColumn(c);
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

			var newQuery = base.VisitSqlQuery(selectQuery);

			_parentSelectQuery  = saveParentQuery;

			return newQuery;
		}

	}
}
