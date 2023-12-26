using LinqToDB.Linq.Builder;
using LinqToDB.SqlProvider;

namespace LinqToDB.SqlQuery.Visitors
{
	public class SqlQueryValidatorVisitor : QueryElementVisitor
	{
		SelectQuery?     _parentQuery;
		SqlProviderFlags _providerFlags = default!;
		int?              _columnSubqueryLevel;

		bool         _isValid;

		public bool IsValid
		{
			get => _isValid;
			private set => _isValid = value;
		}

		public SqlQueryValidatorVisitor() : base(VisitMode.ReadOnly)
		{
		}

		public void Cleanup()
		{
			_parentQuery         = null;
			_providerFlags       = default!;
			_isValid             = true;
			_columnSubqueryLevel = default;
		}

		bool IsSubquery(SelectQuery selectQuery)
		{
			if (_parentQuery == null)
				return false;
			if (selectQuery == _parentQuery) 
				return false;

			return true;
		}

		public bool IsValidQuery(IQueryElement element, SelectQuery? parentQuery, bool forColumn,
			SqlProviderFlags                   providerFlags)
		{
			_isValid             = true;
			_parentQuery         = parentQuery;
			_providerFlags       = providerFlags;
			_columnSubqueryLevel = forColumn ? 0 : null;

			Visit(element);

			return IsValid;
		}

		public bool IsValidSubQuery(SelectQuery selectQuery)
		{
			if (!_providerFlags.IsSubQueryTakeSupported && selectQuery.Select.TakeValue != null)
			{
				if (!_providerFlags.IsWindowFunctionsSupported)
					return false;
			}

			if (!_providerFlags.IsSubQuerySkipSupported && selectQuery.Select.SkipValue != null)
			{
				if (!_providerFlags.IsWindowFunctionsSupported)
					return false;
			}

			return true;
		}

		public override IQueryElement? Visit(IQueryElement? element)
		{
			if (!IsValid)
				return element;

			return base.Visit(element);
		}

		protected override IQueryElement VisitSqlJoinedTable(SqlJoinedTable element)
		{
			if (!_providerFlags.IsApplyJoinSupported)
			{
				// No apply joins are allowed
				if (element.JoinType == JoinType.CrossApply ||
				    element.JoinType == JoinType.OuterApply ||
				    element.JoinType == JoinType.FullApply  ||
				    element.JoinType == JoinType.RightApply)
				{
					IsValid = false;
					return element;
				}
			}

			return base.VisitSqlJoinedTable(element);
		}

		protected override IQueryElement VisitSqlTableSource(SqlTableSource element)
		{
			if (_columnSubqueryLevel > 0 && !_providerFlags.IsColumnSubqueryWithParentReferenceSupported)
			{
				if (element.Source is SelectQuery sq)
				{
					if (SequenceHelper.HasDependencyWithOuter(sq))
					{
						IsValid = false;
						return element;
					}
				}
			}

			base.VisitSqlTableSource(element);

			return element;
		}

		protected override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
		{
			if (IsSubquery(selectQuery))
			{
				if (!IsValidSubQuery(selectQuery))
				{
					IsValid = false;
					return selectQuery;
				}
			}

			var saveParent = _parentQuery;
			_parentQuery = selectQuery;

			base.VisitSqlQuery(selectQuery);

			_parentQuery = saveParent;

			return selectQuery;
		}

		protected override IQueryElement VisitSqlFromClause(SqlFromClause element)
		{
			if (_columnSubqueryLevel != null)
				_columnSubqueryLevel += 1;

			base.VisitSqlFromClause(element);

			if (_columnSubqueryLevel != null)
				_columnSubqueryLevel += 1;

			return element;
		}

		protected override ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
		{
			var saveLevel = _columnSubqueryLevel;

			_columnSubqueryLevel = 0;

			base.VisitSqlColumnExpression(column, expression);

			_columnSubqueryLevel = saveLevel;

			return expression;
		}
	}
}
