using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.SqlQuery;

namespace LinqToDB.SqlProvider
{
	// TODO: refactoring/removal pending
	sealed partial class JoinsOptimizer
	{
		void OptimizeFilters()
		{
			if (_additionalFilter == null)
				return;

			foreach (var pair in _additionalFilter)
			{
				OptimizeSearchCondition(pair.Value);

				if (!ReferenceEquals(pair.Key, pair.Value) && pair.Value.Conditions.Count == 1)
				{
					// conditions can be optimized so we have to remove empty SearchCondition
					if (pair.Value.Conditions[0].Predicate is SqlSearchCondition searchCondition &&
						searchCondition.Conditions.Count == 0)
						pair.Key.Conditions.Remove(pair.Value.Conditions[0]);
				}
			}
		}

		bool? EvaluateLogical(SqlCondition condition)
		{
			switch (condition.ElementType)
			{
				case QueryElementType.Condition:
				{
					if (condition.Predicate is SqlPredicate.ExprExpr expr && expr.Operator == SqlPredicate.Operator.Equal)
						return CompareExpressions(expr.Expr1, expr.Expr2);
					break;
				}
			}

			return null;
		}

		bool CompareExpressions(SqlPredicate.ExprExpr expr1, SqlPredicate.ExprExpr expr2)
		{
			if (expr1.Operator != expr2.Operator)
				return false;

			if (expr1.ElementType != expr2.ElementType)
				return false;

			switch (expr1.ElementType)
			{
				case QueryElementType.ExprExprPredicate:
				{
					return CompareExpressions(expr1.Expr1, expr2.Expr1) == true
						&& CompareExpressions(expr1.Expr2, expr2.Expr2) == true
						|| CompareExpressions(expr1.Expr1, expr2.Expr2) == true
						&& CompareExpressions(expr1.Expr2, expr2.Expr1) == true;
				}
			}

			return false;
		}

		bool? CompareExpressions(ISqlExpression expr1, ISqlExpression expr2)
		{
			if (expr1.ElementType != expr2.ElementType)
				return null;

			switch (expr1.ElementType)
			{
				case QueryElementType.SqlNullabilityExpression:
				{
					return CompareExpressions(QueryHelper.UnwrapNullablity(expr1), QueryHelper.UnwrapNullablity(expr2));
				}

				case QueryElementType.Column:
				{
					return CompareExpressions(((SqlColumn)expr1).Expression, ((SqlColumn)expr2).Expression);
				}

				case QueryElementType.SqlField:
				{
					var field1 = GetNewField((SqlField) expr1);
					var field2 = GetNewField((SqlField) expr2);

					if (field1.Equals(field2))
						return true;

					break;
				}
			}

			return null;
		}

		void OptimizeSearchCondition(SqlSearchCondition searchCondition)
		{
			var items = searchCondition.Conditions;

			if (items.Any(c => c.IsOr))
				return;

			for (var i1 = 0; i1 < items.Count; i1++)
			{
				var c1 = items[i1];
				var cmp = EvaluateLogical(c1);

				if (cmp != null)
					if (cmp.Value)
					{
						items.RemoveAt(i1);
						--i1;
						continue;
					}

				switch (c1.ElementType)
				{
					case QueryElementType.Condition:
					case QueryElementType.SearchCondition:
					{
						if (c1.Predicate is SqlSearchCondition search)
						{
							OptimizeSearchCondition(search);
							if (search.Conditions.Count == 0)
							{
								items.RemoveAt(i1);
								--i1;
								continue;
							}
						}
						break;
					}
				}

				for (var i2 = i1 + 1; i2 < items.Count; i2++)
				{
					var c2 = items[i2];
					if (CompareConditions(c2, c1))
					{
						searchCondition.Conditions.RemoveAt(i2);
						--i2;
					}
				}
			}
		}

		bool CompareConditions(SqlCondition cond1, SqlCondition cond2)
		{
			if (cond1.ElementType != cond2.ElementType)
				return false;

			if (cond1.Predicate.ElementType != cond2.Predicate.ElementType)
				return false;

			switch (cond1.Predicate.ElementType)
			{
				case QueryElementType.IsNullPredicate:
				{
					var isNull1 = (SqlPredicate.IsNull) cond1.Predicate;
					var isNull2 = (SqlPredicate.IsNull) cond2.Predicate;

					return isNull1.IsNot == isNull2.IsNot && CompareExpressions(isNull1.Expr1, isNull2.Expr1) == true;
				}
				case QueryElementType.ExprExprPredicate:
				{
					var expr1 = (SqlPredicate.ExprExpr) cond1.Predicate;
					var expr2 = (SqlPredicate.ExprExpr) cond2.Predicate;

					return CompareExpressions(expr1, expr2);
				}
			}
			return false;
		}
	}
}
