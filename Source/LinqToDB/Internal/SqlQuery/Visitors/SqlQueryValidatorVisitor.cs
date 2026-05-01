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
		SelectQuery?                  _parentQuery;
		SqlJoinedTable?               _fakeJoin;
		SqlProviderFlags              _providerFlags = default!;
		int?                          _columnSubqueryLevel;
		int                           _columnExpressionDepth;
		Stack<ISqlExpression>?        _ignoredExpressions;
		Stack<ISqlTableSource>?       _currentSources;

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

		public override void Cleanup()
		{
			_parentQuery            = null;
			_fakeJoin               = null;
			_providerFlags          = default!;
			_isValid                = true;
			_columnSubqueryLevel    = default;
			_columnExpressionDepth  = 0;
			_errorMessage           = default!;
			_ignoredExpressions     = null;
			_currentSources         = null;

			base.Cleanup();
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

				if (!_providerFlags.IsSubqueryJoinOnOuterReferenceSupported)
				{
					if (QueryHelper.IsJoinsDependsOnOuterSources(selectQuery))
					{
						errorMessage = ErrorHelper.Error_JoinOnOuterReferenceNotSupported;
						return false;
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

				if (!_providerFlags.IsSubQueryOrderBySupported && !selectQuery.OrderBy.IsEmpty && !selectQuery.IsLimited && IsDependsOnOuterSources())
				{
					var checkOrderBy = true;

					if (_parentQuery != null && QueryHelper.IsAggregationQuery(_parentQuery, out var needsOrderBy))
					{
						checkOrderBy = !needsOrderBy;
					}

					if (checkOrderBy && (_parentQuery?.From.Tables.Count > 0 || IsDependsOnOuterSources()))
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

				// IsColumnSubqueryShouldNotContainParentIsNotNull is enforced inline in
				// VisitIsNullPredicate, where _columnExpressionDepth tells us whether the
				// IS NOT NULL is actually in column position. Doing it here would walk every
				// nested subquery's tree once per ancestor (O(N^2) on depth).

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
				var isDerived = _parentQuery != null && _parentQuery.From.Tables.Exists(t => t.Source == selectQuery);

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

			if (selectQuery.Select.Columns.Exists(c => QueryHelper.IsAggregationFunction(c.Expression)))
				return false;

			if (selectQuery.Where.SearchCondition.Predicates.Exists(p => p is SqlSearchCondition))
				return false;

			if (QueryHelper.IsDependsOnOuterSources(selectQuery, elementsToIgnore : new[] { selectQuery.Where }))
				return false;

			return true;
		}

		protected internal override IQueryElement VisitSqlSearchCondition(SqlSearchCondition element)
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

		protected internal override IQueryElement VisitSqlJoinedTable(SqlJoinedTable element)
		{
			if (!_providerFlags.IsApplyJoinSupported)
			{
				// No apply joins are allowed
				if (element.JoinType
						is JoinType.CrossApply 
						or JoinType.OuterApply
						or JoinType.FullApply
						or JoinType.RightApply)
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
					if (element.Condition.IsTrue || element.Condition.IsFalse)
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

		protected internal override IQueryElement VisitSqlTableSource(SqlTableSource element)
		{
			(_currentSources ??= new Stack<ISqlTableSource>()).Push(element.Source);

			base.VisitSqlTableSource(element);

			_currentSources.Pop();

			return element;
		}

		protected internal override IQueryElement VisitIsNullPredicate(SqlPredicate.IsNull predicate)
		{
			// The Oracle restriction modeled by IsColumnSubqueryWithParentReferenceInIsNotNullSupported
			// is about a column-list scalar subquery containing IS NOT NULL with a parent reference.
			// IS NOT NULL in WHERE / ON / HAVING positions is unaffected — only flag predicates
			// reached through a column-expression path.
			if (predicate.IsNot
				&& _columnExpressionDepth > 0
				&& !_providerFlags.IsColumnSubqueryWithParentReferenceInIsNotNullSupported
				&& QueryHelper.IsDependsOnOuterSources(predicate, currentSources: _currentSources))
			{
				SetInvalid(ErrorHelper.Oracle.Error_ColumnSubqueryShouldNotContainParentIsNotNull);
			}

			return base.VisitIsNullPredicate(predicate);
		}

		protected internal override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
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

		protected internal override IQueryElement VisitSqlFromClause(SqlFromClause element)
		{
			var appendLevel = _providerFlags.CalculateSupportedCorrelatedLevelWithAggregateQueries || !QueryHelper.IsAggregationQuery(element.SelectQuery);

			// A `SELECT * FROM (<inner>) AS alias` wrapper carries no operation of its own.
			// The optimizer inlines such wrappers into <inner>, so the emitted SQL has the
			// same correlation depth as <inner>, not <inner>+1. Treat the wrapper as transparent
			// for column-subquery-level depth so the validator matches the post-optimization shape.
			if (appendLevel && element.SelectQuery?.IsTrivialFromWrapper == true)
				appendLevel = false;

			if (_columnSubqueryLevel != null && appendLevel)
				_columnSubqueryLevel += 1;

			base.VisitSqlFromClause(element);

			if (_columnSubqueryLevel != null && appendLevel)
				_columnSubqueryLevel -= 1;

			return element;
		}

		protected override ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
		{
			if (_ignoredExpressions != null && _ignoredExpressions.Contains(expression))
				return expression;

			var saveLevel = _columnSubqueryLevel;

			_columnSubqueryLevel = 0;
			_columnExpressionDepth++;

			base.VisitSqlColumnExpression(column, expression);

			_columnExpressionDepth--;
			_columnSubqueryLevel = saveLevel;

			return expression;
		}

		protected internal override IQueryElement VisitSqlUpdateStatement(SqlUpdateStatement element)
		{
			_ignoredExpressions ??= new Stack<ISqlExpression>();

			foreach (var item in element.Update.Items)
			{
				if (item.Expression != null)
					_ignoredExpressions.Push(item.Expression);
			}

			var result = base.VisitSqlUpdateStatement(element);

			foreach (var item in element.Update.Items)
			{
				if (item.Expression != null)
					_ignoredExpressions.Pop();
			}

			return result;
		}

		protected internal override IQueryElement VisitSqlInsertOrUpdateStatement(SqlInsertOrUpdateStatement element)
		{
			_ignoredExpressions ??= new Stack<ISqlExpression>();

			foreach (var item in element.Update.Items)
			{
				if (item.Expression != null)
					_ignoredExpressions.Push(item.Expression);
			}

			var result = base.VisitSqlInsertOrUpdateStatement(element);

			foreach (var item in element.Update.Items)
			{
				if (item.Expression != null)
					_ignoredExpressions.Pop();
			}

			return result;
		}
	}
}
