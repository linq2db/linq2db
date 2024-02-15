using System.Diagnostics.CodeAnalysis;

using LinqToDB.Linq.Builder;
using LinqToDB.SqlProvider;

namespace LinqToDB.SqlQuery.Visitors
{
	public class SqlQueryValidatorVisitor : QueryElementVisitor
	{
		SelectQuery?     _parentQuery;
		SqlProviderFlags _providerFlags = default!;
		int?             _columnSubqueryLevel;

		bool    _isValid;
		string? _errorMessage;

		public bool IsValid
		{
			get => _isValid;
		}

		public string? ErrorMessage
		{
			get => _errorMessage;
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
			_errorMessage        = default!;
		}

		public void SetInvlaid(string errorMessage)
		{
			_isValid      = false;
			_errorMessage = errorMessage;
		}

		bool IsSubquery(SelectQuery selectQuery)
		{
			if (_parentQuery == null)
				return false;
			if (selectQuery == _parentQuery) 
				return false;

			return true;
		}

		public bool IsValidQuery(
			IQueryElement    element,       
			SelectQuery?     parentQuery, 
			bool             forColumn,
			SqlProviderFlags providerFlags, 
			out string?      errorMessage)
		{
			_isValid             = true;
			_errorMessage        = default!;
			_parentQuery         = parentQuery;
			_providerFlags       = providerFlags;
			_columnSubqueryLevel = forColumn ? 0 : null;

			Visit(element);

			errorMessage = _errorMessage;

			return IsValid;
		}

		public bool IsValidSubQuery(SelectQuery selectQuery, [NotNullWhen(false)] out string? errorMessage)
		{
			if (_columnSubqueryLevel != null)
			{
				if (!_providerFlags.IsSubQueryTakeSupported && selectQuery.Select.TakeValue != null)
				{
					errorMessage = ErrorHelper.Error_Take_in_Subquery;
					return false;
				}

				if (!_providerFlags.IsSubQuerySkipSupported && selectQuery.Select.SkipValue != null)
				{
					errorMessage = ErrorHelper.Error_Skip_in_Subquery;
					return false;
				}

				if (_providerFlags.DoesNotSupportCorrelatedSubquery)
				{
					if (QueryHelper.IsDependsOnOuterSources(selectQuery))
					{
						errorMessage = ErrorHelper.Error_Correlated_Subqueries;
						return false;
					}
				}
			}

			errorMessage = null;
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
					if (_providerFlags.DoesNotSupportCorrelatedSubquery)
					{
						SetInvlaid(ErrorHelper.Error_Correlated_Subqueries);
					}
					else
					{
						SetInvlaid(ErrorHelper.Error_OUTER_Joins);
					}
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
						SetInvlaid(ErrorHelper.Error_Correlated_Subqueries);
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
				string? errorMessage;
				if (!IsValidSubQuery(selectQuery, out errorMessage))
				{
					SetInvlaid(errorMessage);
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
