using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LinqToDB.SqlQuery.Visitors
{
	using Linq.Builder;
	using SqlProvider;

	public class SqlQueryValidatorVisitor : QueryElementVisitor
	{
		SelectQuery?     _parentQuery;
		SqlJoinedTable?  _fakeJoin;
		//SelectQuery?     _joinQuery;
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
			//_joinQuery           = null;
			_providerFlags       = default!;
			_isValid             = true;
			_columnSubqueryLevel = default;
			_errorMessage        = default!;
		}

		public void SetInvalid(string errorMessage)
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

		public bool IsValidQuery(IQueryElement element,
			SelectQuery?                       parentQuery,
			SqlJoinedTable?                    fakeJoin,
			bool                               forColumn,
			SqlProviderFlags                   providerFlags,
			out string?                        errorMessage)
		{
			_isValid             = true;
			_errorMessage        = default!;
			_parentQuery         = parentQuery;
			_fakeJoin            = fakeJoin;
			_providerFlags       = providerFlags;
			_columnSubqueryLevel = forColumn ? 0 : null;

			Visit(element);

			errorMessage = _errorMessage;

			return IsValid;
		}

		public bool IsValidSubQuery(SelectQuery selectQuery, [NotNullWhen(false)] out string? errorMessage)
		{
			bool? isDependedOnOuterSources = null;

			bool IsDependsOnOuterSources()
			{
				isDependedOnOuterSources ??= QueryHelper.IsDependsOnOuterSources(selectQuery);

				return isDependedOnOuterSources.Value;
			}

			if (!_providerFlags.IsCorrelatedSubQueryTakeSupported && selectQuery.Select.TakeValue != null)
			{
				if (_columnSubqueryLevel != null && IsDependsOnOuterSources())
				{
					errorMessage = ErrorHelper.Error_Take_in_Correlated_Subquery;
					return false;
				}
			}

			if (_columnSubqueryLevel != null)
			{
				if (_providerFlags.DoesNotSupportCorrelatedSubquery)
				{
					if (IsDependsOnOuterSources())
					{
						var isValied = false;
						if (_providerFlags.IsSupportedSimpleCorrelatedSubqueries && IsSimpleCorrelatedSubquery(selectQuery))
						{
							isValied = true;
						}

						if (!isValied)
						{
							errorMessage = ErrorHelper.Error_Correlated_Subqueries;
							return false;
						}
					}
				}

				if (!_providerFlags.IsSubQueryTakeSupported && selectQuery.Select.TakeValue != null && IsDependsOnOuterSources())
				{
					if (_parentQuery?.From.Tables.Count > 0 || IsDependsOnOuterSources())
					{
						errorMessage = ErrorHelper.Error_Take_in_Subquery;
						return false;
					}
				}

				if (!_providerFlags.IsSubQuerySkipSupported && selectQuery.Select.SkipValue != null && IsDependsOnOuterSources())
				{
					if (_parentQuery?.From.Tables.Count > 0 || IsDependsOnOuterSources())
					{
						errorMessage = ErrorHelper.Error_Skip_in_Subquery;
						return false;
					}
				}

				if (!_providerFlags.IsSubQueryOrderBySupported && !selectQuery.OrderBy.IsEmpty && IsDependsOnOuterSources())
				{
					if (_parentQuery?.From.Tables.Count > 0 || IsDependsOnOuterSources())
					{
						errorMessage = ErrorHelper.Error_OrderBy_in_Subquery;
						return false;
					}
				}

				if (!_providerFlags.IsSubqueryWithParentReferenceInJoinConditionSupported)
				{
					var current = QueryHelper.EnumerateAccessibleSources(selectQuery).ToList();

					foreach (var innerJoin in QueryHelper.EnumerateJoins(selectQuery))
					{
						if (QueryHelper.IsDependsOnOuterSources(innerJoin.Condition, currentSources: current))
						{
							errorMessage = ErrorHelper.Error_Join_ParentReference_Condition;
							return false;
						}
					}
				}

				if (_providerFlags.IsColumnSubqueryShouldNotContainParentIsNotNull)
				{
					if (HasIsNotNullParentReference(selectQuery))
					{
						errorMessage = ErrorHelper.Error_ColumnSubqueryShouldNotContainParentIsNotNull;
						return false;
					}
				}

				var shouldCheckNesting = _columnSubqueryLevel            > 0     && !_providerFlags.IsColumnSubqueryWithParentReferenceSupported
				                         || selectQuery.Select.TakeValue != null && !_providerFlags.IsColumnSubqueryWithParentReferenceAndTakeSupported;

				if (shouldCheckNesting)
				{
					if (IsDependsOnOuterSources())
					{
						errorMessage = ErrorHelper.Error_Correlated_Subqueries;
						return false;
					}
				}

			}
			else
			{
				if (!_providerFlags.IsDerivedTableOrderBySupported && !selectQuery.OrderBy.IsEmpty)
				{
					errorMessage = ErrorHelper.Error_OrderBy_in_Derived;
					return false;
				}
			}

			errorMessage = null;
			return true;
		}

		static bool IsSimpleCorrelatedSubquery(SelectQuery selectQuery)
		{
			if (selectQuery.Where.SearchCondition.IsOr)
				return false;

			if (selectQuery.Where.SearchCondition.Predicates.Any(p => p is SqlSearchCondition))
				return false;

			if (QueryHelper.IsDependsOnOuterSources(selectQuery, elementsToIgnore : new[] { selectQuery.Where }))
				return false;

			return true;
		}

		static bool HasIsNotNullParentReference(SelectQuery selectQuery)
		{
			var visitor = new ValidateThatQueryHasNoIsNotNullParentReferenceVisitor();

			visitor.Visit(selectQuery);

			return visitor.ContainsNotNullExpr;
		}

		class ValidateThatQueryHasNoIsNotNullParentReferenceVisitor : SqlQueryVisitor
		{
			public Stack<ISqlTableSource> _currentSources = new Stack<ISqlTableSource>();

			public ValidateThatQueryHasNoIsNotNullParentReferenceVisitor() : base(VisitMode.ReadOnly, null)
			{
			}

			public bool ContainsNotNullExpr {get; private set; }

			protected override IQueryElement VisitSqlTableSource(SqlTableSource element)
			{
				_currentSources.Push(element.Source);

				base.VisitSqlTableSource(element);

				_currentSources.Pop();

				return element;
			}

			public override IQueryElement? Visit(IQueryElement? element)
			{
				if (ContainsNotNullExpr)
					return element;

				return base.Visit(element);
			}

			protected override IQueryElement VisitIsNullPredicate(SqlPredicate.IsNull predicate)
			{
				if (predicate.IsNot)
				{
					if (QueryHelper.IsDependsOnOuterSources(predicate, currentSources : _currentSources))
					{
						ContainsNotNullExpr = true;
					}
				}

				return base.VisitIsNullPredicate(predicate);
			}
		}

		protected override IQueryElement VisitSqlSearchCondition(SqlSearchCondition element)
		{
			var saveColumnSubqueryLevel = _columnSubqueryLevel;
			_columnSubqueryLevel = null;

			var result = base.VisitSqlSearchCondition(element);

			_columnSubqueryLevel = saveColumnSubqueryLevel;
			return result;
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
						SetInvalid(ErrorHelper.Error_Correlated_Subqueries);
					}
					else
					{
						SetInvalid(ErrorHelper.Error_OUTER_Joins);
					}
					return element;
				}
			}

			if (element != _fakeJoin)
			{
				if (!_providerFlags.IsSupportsJoinWithoutCondition && element.JoinType is JoinType.Left or JoinType.Inner)
				{
					if (element.Condition.IsTrue() || element.Condition.IsFalse())
					{
						SetInvalid(ErrorHelper.Error_Join_Without_Condition);
						return element;
					}
				}
			}

			if (_providerFlags.IsJoinDerivedTableWithTakeInvalid && element.Table.Source is SelectQuery { Select.TakeValue: not null })
			{
				SetInvalid(ErrorHelper.Error_JoinToDerivedTableWithTakeInvalid);
				return element;
			}

			//_joinQuery = element.Table.Source as SelectQuery;

			var result = base.VisitSqlJoinedTable(element);

			//_joinQuery = null;

			return result;
		}

		protected override IQueryElement VisitSqlTableSource(SqlTableSource element)
		{
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
					SetInvalid(errorMessage);
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
				_columnSubqueryLevel -= 1;

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
