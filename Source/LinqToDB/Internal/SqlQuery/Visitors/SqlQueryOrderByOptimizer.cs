using System.Diagnostics.CodeAnalysis;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.SqlProvider;

namespace LinqToDB.Internal.SqlQuery.Visitors
{
	public sealed class SqlQueryOrderByOptimizer : SqlQueryVisitor
	{
		SqlProviderFlags _providerFlags = default!;
		bool             _disableOrderBy;
		bool             _insideSetOperator;
		bool             _optimized;
		bool             _needsNestingUpdate;

		public bool IsOptimized => _optimized;
		public bool NeedsNestingUpdate => _needsNestingUpdate;

		public SqlQueryOrderByOptimizer() : base(VisitMode.Modify, null)
		{
		}

		public override void Cleanup()
		{
			base.Cleanup();

			_disableOrderBy     = false;
			_insideSetOperator  = false;
			_optimized          = false;
			_needsNestingUpdate = false;
			_providerFlags      = default!;
		}

		public void OptimizeOrderBy(IQueryElement element, SqlProviderFlags providerFlags)
		{
			_disableOrderBy     = false;
			_optimized          = false;
			_insideSetOperator  = false;
			_needsNestingUpdate = false;
			_providerFlags      = providerFlags;

			ProcessElement(element);
		}

		void CorrectOrderBy(SelectQuery selectQuery, bool disable)
		{
			if (!selectQuery.OrderBy.IsEmpty)
			{
				// This is the case when we have 
				// SELECT [TOP x]
				//     COUNT(*),
				//     AVG(Value)
				// FROM Table
				// ORDER BY ...
				if (QueryHelper.IsAggregationQuery(selectQuery))
				{
					selectQuery.OrderBy.Items.Clear();
					_optimized = true;
					return;
				}

				if (!selectQuery.IsLimited)
				{
					if (disable)
					{
						selectQuery.OrderBy.Items.Clear();
						_optimized = true;
						return;
					}
				}

				selectQuery.OrderBy.Items.RemoveDuplicates(item => item.Expression);
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

		protected override IQueryElement VisitExistsPredicate(SqlPredicate.Exists predicate)
		{
			var saveDisableOrderBy = _disableOrderBy;

			_disableOrderBy = true;

			var newElement = base.VisitExistsPredicate(predicate);

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

		static bool ExtractOrderBy(SelectQuery selectQuery, [NotNullWhen(true)] out SqlOrderByClause? orderBy)
		{
			orderBy = null;

			if (!selectQuery.Select.HasModifier && !selectQuery.HasSetOperators)
			{
				if (!selectQuery.OrderBy.IsEmpty)
				{
					orderBy = selectQuery.OrderBy;
					return true;
				}

				if (selectQuery.From.GroupBy.IsEmpty && selectQuery.From.Tables is [{ Joins.Count: 0, Source: SelectQuery subQuery }])
				{
					return ExtractOrderBy(subQuery, out orderBy);
				}
			}

			return false;
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
				if (!_disableOrderBy)
				{
					if (selectQuery.OrderBy.IsEmpty && ExtractOrderBy(selectQuery, out var orderBy))
					{
						// we can preserve order in simple cases
						selectQuery.OrderBy.Items.AddRange(orderBy.Items);
						orderBy.Items.Clear();

						_optimized          = true;
						_needsNestingUpdate = true;
					}
				}

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
