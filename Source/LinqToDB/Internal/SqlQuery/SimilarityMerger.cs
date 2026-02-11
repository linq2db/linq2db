using System.Collections.Generic;

// #pragma warning disable CS0162 // Unreachable code detected

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SimilarityMerger : ISimilarityMerger
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
			else if (predicate is SqlPredicate.Not notPredicate)
			{
				yield return notPredicate.Predicate.GetElementHashCode();
			}
		}

		public bool TryMerge(NullabilityContext nullabilityContext, bool isNestedPredicate, ISqlPredicate predicate1, ISqlPredicate predicate2, bool isLogicalOr, out ISqlPredicate? mergedPredicate)
		{
			if (predicate1.Equals(predicate2, SqlExtensions.DefaultComparer))
			{
				mergedPredicate = predicate1;
				return true;
			}

			if (predicate1 is SqlPredicate.IsNull isNull1)
			{
				if (predicate2 is SqlPredicate.IsNull isNull2)
				{
					if (isNull2.Expr1.Equals(((SqlPredicate.IsNull)predicate1).Expr1, SqlExtensions.DefaultComparer))
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
				else if (predicate2 is SqlPredicate.ExprExpr { Operator: SqlPredicate.Operator.Equal } exprExpr2
					&& (exprExpr2.UnknownAsValue == true || !isNestedPredicate))
				{
					if (!isLogicalOr && isNull1.IsNot && !nullabilityContext.IsEmpty)
					{
						if (exprExpr2.Expr1.Equals(isNull1.Expr1, SqlExtensions.DefaultComparer))
						{
							if (exprExpr2.NotNullableExpr1)
							{
								mergedPredicate = exprExpr2;
								return true;
							}

							mergedPredicate = new SqlPredicate.ExprExpr(exprExpr2.Expr1, exprExpr2.Operator, exprExpr2.Expr2, exprExpr2.UnknownAsValue, true, exprExpr2.NotNullableExpr2);
							return true;
						}

						if (exprExpr2.Expr2.Equals(isNull1.Expr1, SqlExtensions.DefaultComparer))
						{
							if (exprExpr2.NotNullableExpr2)
							{
								mergedPredicate = exprExpr2;
								return true;
							}

							mergedPredicate = new SqlPredicate.ExprExpr(exprExpr2.Expr1, exprExpr2.Operator, exprExpr2.Expr2, exprExpr2.UnknownAsValue, exprExpr2.NotNullableExpr1, true);
							return true;
						}
					}
				}
			}
			else if (predicate1 is SqlPredicate.ExprExpr exprExpr1 && predicate2 is SqlPredicate.ExprExpr exprExpr2)
			{
				if ((exprExpr1.Operator == exprExpr2.Operator
				     && exprExpr1.Expr1.Equals(exprExpr2.Expr1, SqlExtensions.DefaultComparer)
				     && exprExpr1.Expr2.Equals(exprExpr2.Expr2, SqlExtensions.DefaultComparer)
				     && exprExpr1.NotNullableExpr1 == exprExpr2.NotNullableExpr1 && exprExpr1.NotNullableExpr2 == exprExpr2.NotNullableExpr2)
				    ||
				    (SqlPredicate.ExprExpr.SwapOperator(exprExpr1.Operator) == exprExpr2.Operator
				     && exprExpr1.Expr1.Equals(exprExpr2.Expr2, SqlExtensions.DefaultComparer)
				     && exprExpr1.Expr2.Equals(exprExpr2.Expr1, SqlExtensions.DefaultComparer)
				     && exprExpr1.NotNullableExpr1 == exprExpr2.NotNullableExpr2
				     && exprExpr1.NotNullableExpr2 == exprExpr2.NotNullableExpr1))
				{
					mergedPredicate = predicate1;
					return true;
				}
			}
			else if (predicate1 is SqlPredicate.Not notPredicate1)
			{
				// NOT P OR P => true
				// NOT P AND P => false
				if (notPredicate1.Predicate.Equals(predicate2, SqlExtensions.DefaultComparer))
				{
					mergedPredicate = isLogicalOr ? SqlPredicate.True : SqlPredicate.False;
					return true;
				}
			}

			// A x !A
			if (   (predicate1.CanInvert(nullabilityContext) && predicate1.Invert(nullabilityContext).Equals(predicate2, SqlExtensions.DefaultComparer))
				|| (predicate2.CanInvert(nullabilityContext) && predicate1.Equals(predicate2.Invert(nullabilityContext), SqlExtensions.DefaultComparer)))
			{
				mergedPredicate = isLogicalOr ? SqlPredicate.True : SqlPredicate.False;
				return true;
			}

			mergedPredicate = null;
			return false;
		}

		public bool TryMerge(NullabilityContext nullabilityContext, bool isNestedPredicate, ISqlPredicate single, ISqlPredicate predicateFromList, bool isLogicalOr, out ISqlPredicate? mergedSinglePredicate,
			out ISqlPredicate?                  mergedListPredicate)
		{
			if (single.Equals(predicateFromList, SqlExtensions.DefaultComparer))
			{
				mergedSinglePredicate = single;
				mergedListPredicate   = isLogicalOr ? SqlPredicate.False : SqlPredicate.True;
				return true;
			}

			// A x (!A)
			if (   (single           .CanInvert(nullabilityContext) && single.Invert(nullabilityContext).Equals(predicateFromList                           , SqlExtensions.DefaultComparer))
				|| (predicateFromList.CanInvert(nullabilityContext) && single                           .Equals(predicateFromList.Invert(nullabilityContext), SqlExtensions.DefaultComparer)))
			{
				mergedSinglePredicate = single;
				mergedListPredicate   = isLogicalOr ? SqlPredicate.True : SqlPredicate.False;
				return true;
			}

			mergedSinglePredicate = null;
			mergedListPredicate = null;
			return false;
		}
	}
}
