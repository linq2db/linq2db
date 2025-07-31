using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.SqlProvider;

namespace LinqToDB.Internal.SqlQuery.Visitors
{
	public class SqlQueryValidatorVisitor : QueryElementVisitor
	{
		SelectQuery?     _parentQuery;
		SqlJoinedTable?  _fakeJoin;
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
			int?                               columnSubqueryLevel,
			SqlProviderFlags                   providerFlags,
			out string?                        errorMessage)
		{
			_isValid             = true;
			_errorMessage        = default!;
			_parentQuery         = parentQuery;
			_fakeJoin            = fakeJoin;
			_providerFlags       = providerFlags;
			_columnSubqueryLevel = columnSubqueryLevel;

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

			if (_columnSubqueryLevel != null)
			{
				if (!_providerFlags.IsSubQueryColumnSupported)
				{
					errorMessage = ErrorHelper.Error_Subquery_in_Column;
					return false;
				}

				if (!_providerFlags.IsSubQueryTakeSupported && selectQuery.Select.TakeValue != null)
				{
					errorMessage = ErrorHelper.Error_Take_in_Subquery;
					return false;
				}

				if (!_providerFlags.IsCorrelatedSubQueryTakeSupported && selectQuery.Select.TakeValue != null)
				{
					if (IsDependsOnOuterSources())
					{
						errorMessage = ErrorHelper.Error_Take_in_Correlated_Subquery;
						return false;
					}
				}

				if (_providerFlags.SupportedCorrelatedSubqueriesLevel != null)
				{
					if (_columnSubqueryLevel >= _providerFlags.SupportedCorrelatedSubqueriesLevel)
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
								if (_providerFlags.SupportedCorrelatedSubqueriesLevel == 0)
									errorMessage = ErrorHelper.Error_Correlated_Subqueries;
								else
								{
									errorMessage = string.Format(CultureInfo.InvariantCulture, ErrorHelper.Error_Correlated_Subqueries_Level, _providerFlags.SupportedCorrelatedSubqueriesLevel.Value);
								}

								return false;
							}
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
						errorMessage = ErrorHelper.Oracle.Error_ColumnSubqueryShouldNotContainParentIsNotNull;
						return false;
					}
				}

				var shouldCheckNesting = selectQuery.Select.TakeValue != null && !_providerFlags.IsColumnSubqueryWithParentReferenceAndTakeSupported;

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
				var isDerived = _parentQuery != null && _parentQuery.From.Tables.Any(t => t.Source == selectQuery);

				if (isDerived)
				{
					if (!_providerFlags.IsDerivedTableOrderBySupported && !selectQuery.OrderBy.IsEmpty)
					{
						errorMessage = ErrorHelper.Error_OrderBy_in_Derived;
						return false;
					}

					if (!_providerFlags.IsDerivedTableTakeSupported && selectQuery.Select.TakeValue != null)
					{
						errorMessage = ErrorHelper.Error_Take_in_Derived;
						return false;
					}
				}
			}

			errorMessage = null;
			return true;
		}

		static bool IsSimpleCorrelatedSubquery(SelectQuery selectQuery)
		{
			if (selectQuery.Where.SearchCondition.IsOr)
				return false;

			if (selectQuery.Select.HasModifier)
				return false;

			if (selectQuery.Select.Columns.Any(c => QueryHelper.IsAggregationFunction(c.Expression)))
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

		sealed class ValidateThatQueryHasNoIsNotNullParentReferenceVisitor : SqlQueryVisitor
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
					if (_providerFlags.SupportedCorrelatedSubqueriesLevel == 0)
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
				SetInvalid(ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid);
				return element;
			}

			var result = base.VisitSqlJoinedTable(element);

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

		protected override IQueryElement VisitSqlFunction(SqlFunction element)
		{
			var saveLevel = _columnSubqueryLevel;

			_columnSubqueryLevel = null;

			base.VisitSqlFunction(element);

			_columnSubqueryLevel = saveLevel;

			return element;
		}

		protected override IQueryElement VisitSqlConditionExpression(SqlConditionExpression element)
		{
			var saveLevel = _columnSubqueryLevel;

			_columnSubqueryLevel = null;

			base.VisitSqlConditionExpression(element);

			_columnSubqueryLevel = saveLevel;

			return element;
		}

		protected override IQueryElement VisitSqlCaseExpression(SqlCaseExpression element)
		{
			var saveLevel = _columnSubqueryLevel;

			_columnSubqueryLevel = null;

			base.VisitSqlCaseExpression(element);

			_columnSubqueryLevel = saveLevel;

			return element;
		}
	}
}
