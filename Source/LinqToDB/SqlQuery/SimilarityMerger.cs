using System;
using System.Collections.Generic;
#pragma warning disable CS0162 // Unreachable code detected

namespace LinqToDB.SqlQuery
{
	public class SimilarityMerger : ISimilarityMerger
	{
		public static readonly SimilarityMerger Instance = new SimilarityMerger();

		public IEnumerable<int> GetSimilarityCodes(ISqlPredicate predicate)
		{
			yield return predicate.GetElementHashCode();

			if (predicate is SqlPredicate.ExprExpr exprExpr)
			{
				yield return exprExpr.Expr1.GetElementHashCode();
				yield return exprExpr.Expr2.GetElementHashCode();
			}
			else if (predicate is SqlPredicate.IsNull isNull)
			{
				yield return isNull.Expr1.GetElementHashCode();
			}
		}

		public bool TryMerge(NullabilityContext nullabilityContext, ISqlPredicate predicate1, ISqlPredicate predicate2, bool isLogicalOr, out ISqlPredicate? mergedPredicate)
		{
			if (predicate1.Equals(predicate2, SqlExpression.DefaultComparer))
			{
				mergedPredicate = predicate1;
				return true;
			}

			if (predicate1 is SqlPredicate.IsNull isNull1)
			{
				if (predicate2 is SqlPredicate.IsNull isNull2)
				{
					if (isNull2.Expr1.Equals(((SqlPredicate.IsNull)predicate1).Expr1, SqlExpression.DefaultComparer))
					{
						if (isNull1.IsNot != isNull2.IsNot)
						{
							mergedPredicate = isLogicalOr ? SqlPredicate.True : SqlPredicate.False;
							return true;
						}

						// Theoretically should never happen.
						mergedPredicate = predicate1;
						return true;
					}
				}
				else if (predicate2 is SqlPredicate.ExprExpr exprExpr2)
				{
					if (!isLogicalOr && isNull1.IsNot && !nullabilityContext.IsEmpty && exprExpr2 is { Operator: SqlPredicate.Operator.Equal, UnknownAsValue: false} &&
					    (
						    (exprExpr2.Expr1.Equals(isNull1.Expr1, SqlExpression.DefaultComparer) && !exprExpr2.Expr2.CanBeNullable(nullabilityContext)) ||
						    (exprExpr2.Expr2.Equals(isNull1.Expr1, SqlExpression.DefaultComparer) && !exprExpr2.Expr1.CanBeNullable(nullabilityContext))
					    ))
					{
						mergedPredicate = exprExpr2;
						return true;
					}
				}
			}
			else if (predicate1 is SqlPredicate.ExprExpr exprExpr1 && predicate2 is SqlPredicate.ExprExpr exprExpr2)
			{
				if ((exprExpr1.Operator                                     == exprExpr2.Operator && exprExpr1.Expr1.Equals(exprExpr2.Expr1, SqlExpression.DefaultComparer) && exprExpr1.Expr2.Equals(exprExpr2.Expr2, SqlExpression.DefaultComparer)) ||
				    (SqlPredicate.ExprExpr.SwapOperator(exprExpr1.Operator) == exprExpr2.Operator && exprExpr1.Expr1.Equals(exprExpr2.Expr2, SqlExpression.DefaultComparer) && exprExpr1.Expr2.Equals(exprExpr2.Expr1, SqlExpression.DefaultComparer)))
				{
					mergedPredicate = predicate1;
					return true;
				}
			}

			mergedPredicate = null;
			return false;
		}
	}
}


