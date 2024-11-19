using System.Linq;

using LinqToDB.Common;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery.Visitors;

namespace LinqToDB.SqlQuery
{
	public class SqlQueryOrderByOptimizer : SqlQueryVisitor
	{
		SqlProviderFlags _providerFlags = default!;
		bool             _disableOrderBy;
		bool             _insideSetOperator;
		bool             _optimized;

		public bool IsOptimized => _optimized;

		public SqlQueryOrderByOptimizer() : base(VisitMode.Modify, null)
		{
		}

		public override void Cleanup()
		{
			base.Cleanup();

			_disableOrderBy    = false;
			_insideSetOperator = false;
			_optimized         = false;
			_providerFlags       = default!;
		}

		public void OptimizeOrderBy(IQueryElement element, SqlProviderFlags providerFlags)
		{
			_disableOrderBy    = false;
			_optimized         = false;
			_insideSetOperator = false;
			_providerFlags     = providerFlags;

			ProcessElement(element);
		}

		void CorrectOrderBy(SelectQuery selectQuery, bool disable)
		{
			if (!selectQuery.OrderBy.IsEmpty)
			{
				if (!selectQuery.IsLimited)
				{
					if (disable || 
					    selectQuery.Select.Columns.Count > 0 && selectQuery.Select.Columns.All(c => QueryHelper.IsAggregationOrWindowFunction(c.Expression))
					   )
					{
						selectQuery.OrderBy.Items.Clear();
						_optimized = true;
					}
				}

				if (!selectQuery.OrderBy.IsEmpty)
				{
					Utils.RemoveDuplicates(selectQuery.OrderBy.Items, item => item.Expression);
				}
			}
		
		}

		protected override IQueryElement VisitSqlSetOperator(SqlSetOperator element)
		{
			var saveDisableOrderBy    = _disableOrderBy;
			var saveInsideSetOperator = _insideSetOperator;
			_insideSetOperator = true;
			
			_disableOrderBy = _disableOrderBy                                ||
			                  element.Operation == SetOperation.Except       ||
			                  element.Operation == SetOperation.ExceptAll    ||
			                  element.Operation == SetOperation.Intersect    ||
			                  element.Operation == SetOperation.IntersectAll ||
			                  element.Operation == SetOperation.Union;

			var newElement = base.VisitSqlSetOperator(element);

			_disableOrderBy     = saveDisableOrderBy;
			_insideSetOperator  = saveInsideSetOperator;

			return newElement;
		}

		protected override IQueryElement VisitFuncLikePredicate(SqlPredicate.FuncLike element)
		{
			var saveDisableOrderBy = _disableOrderBy;

			_disableOrderBy = true;

			var newElement = base.VisitFuncLikePredicate(element);

			_disableOrderBy = saveDisableOrderBy;

			return newElement;
		}

		protected override IQueryElement VisitSqlJoinedTable(SqlJoinedTable element)
		{
			var saveDisableOrderBy = _disableOrderBy;

			_disableOrderBy = true;

			var newElement = base.VisitSqlJoinedTable(element);

			_disableOrderBy = saveDisableOrderBy;

			return newElement;
		}

		protected override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
		{
			var saveDisableOrderBy = _disableOrderBy;
			
			if (selectQuery.HasSetOperators)
			{
				var setOperator = selectQuery.SetOperators[0];
				if (setOperator.Operation == SetOperation.Union     || 
				    setOperator.Operation == SetOperation.Except    || 
				    setOperator.Operation == SetOperation.Intersect || 
				    setOperator.Operation == SetOperation.IntersectAll)
				{
					_disableOrderBy = true;
				}

				var saveInsideSetOperator = _insideSetOperator;
				_insideSetOperator = true;

				Visit(selectQuery.From);

				_insideSetOperator = saveInsideSetOperator;
			}
			else
			{
				Visit(selectQuery.From);
			}

			CorrectOrderBy(selectQuery, _disableOrderBy);

			Visit(selectQuery.Select );
			Visit(selectQuery.Where  );
			Visit(selectQuery.GroupBy);
			Visit(selectQuery.Having );
			Visit(selectQuery.OrderBy);

			if (selectQuery.HasSetOperators)
				VisitElements(selectQuery.SetOperators, VisitMode.Modify);

			if (selectQuery.HasUniqueKeys)
				VisitListOfArrays(selectQuery.UniqueKeys, VisitMode.Modify);

			VisitElements(selectQuery.SqlQueryExtensions, VisitMode.Modify);

			_disableOrderBy = saveDisableOrderBy;

			return selectQuery;
		}

		protected override IQueryElement VisitSqlTableSource(SqlTableSource element)
		{
			var saveDisableOrderBy = _disableOrderBy;

			if (!_insideSetOperator)
			{
				_disableOrderBy = true;
			}

			var newElement = base.VisitSqlTableSource(element);

			_disableOrderBy = saveDisableOrderBy;

			return newElement;
		}

		protected override IQueryElement VisitSqlWhereClause(SqlWhereClause element)
		{
			var saveDisableOrderBy = _disableOrderBy;
			_disableOrderBy = false;

			var newElement = base.VisitSqlWhereClause(element);

			_disableOrderBy = saveDisableOrderBy;

			return newElement;
		}

		protected override IQueryElement VisitSqlGroupByClause(SqlGroupByClause element)
		{
			var saveDisableOrderBy = _disableOrderBy;
			_disableOrderBy = false;

			var newElement = base.VisitSqlGroupByClause(element);

			_disableOrderBy = saveDisableOrderBy;

			return newElement;
		}

		protected override ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
		{
			var saveDisableOrderBy = _disableOrderBy;
			_disableOrderBy = false;

			expression = base.VisitSqlColumnExpression(column, expression);

			_disableOrderBy = saveDisableOrderBy;

			return expression;
		}

		protected override IQueryElement VisitCteClause(CteClause element)
		{
			var saveDisableOrderBy = _disableOrderBy;

			_disableOrderBy = !_providerFlags.IsCTESupportsOrdering;

			var newElement = base.VisitCteClause(element);

			_disableOrderBy = saveDisableOrderBy;

			return newElement;
		}

		protected override IQueryElement VisitInSubQueryPredicate(SqlPredicate.InSubQuery predicate)
		{
			var saveDisableOrderBy = _disableOrderBy;

			_disableOrderBy = true;

			var newElement = base.VisitInSubQueryPredicate(predicate);

			_disableOrderBy = saveDisableOrderBy;

			return newElement;
		}
	}
}
