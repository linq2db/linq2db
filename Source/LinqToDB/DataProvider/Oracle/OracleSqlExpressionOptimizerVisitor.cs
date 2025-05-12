using System;

using LinqToDB.Common;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Oracle
{
	sealed class OracleSqlExpressionOptimizerVisitor(bool allowModify) : SqlExpressionOptimizerVisitor(allowModify)
	{
		protected override IQueryElement VisitExprExprPredicate(SqlPredicate.ExprExpr predicate)
		{
			var (a, op, b, withNull) = predicate;

			// We want to modify comparisons involving "" as Oracle treats "" as null

			// Comparisons to a literal constant "" are always converted to IS [NOT] NULL (same as == null or == default)
			if (op is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual)
			{
				if (QueryHelper.UnwrapNullablity(a) is SqlValue { Value: string { Length: 0 } })
					return Visit(new SqlPredicate.IsNull(SqlNullabilityExpression.ApplyNullability(b, true), isNot: op == SqlPredicate.Operator.NotEqual));
				if (QueryHelper.UnwrapNullablity(b) is SqlValue { Value: string { Length: 0 } })
					return Visit(new SqlPredicate.IsNull(SqlNullabilityExpression.ApplyNullability(a, true), isNot: op == SqlPredicate.Operator.NotEqual));
			}

			// CompareNulls.LikeSql compiles as-is, no change
			// CompareNulls.LikeSqlExceptParameters sniffs parameters to == and != and replaces by IS [NOT] NULL
			// CompareNulls.LikeClr (withNull) always handles nulls.
			// Note: LikeClr sometimes generates `withNull: null` expressions, in which case it works the
			//       same way as LikeSqlExceptParameters (for backward compatibility).

			if (withNull != null
				|| (DataOptions.LinqOptions.CompareNulls != CompareNulls.LikeSql
					&& op is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual))
			{
				if (Oracle11SqlOptimizer.IsTextType(b, MappingSchema) &&
					b.TryEvaluateExpressionForServer(EvaluationContext, out var bValue) &&
					bValue is string { Length: 0 })
				{
					return Visit(CompareToEmptyString(a, op));
				}

				if (Oracle11SqlOptimizer.IsTextType(a, MappingSchema) &&
					a.TryEvaluateExpressionForServer(EvaluationContext, out var aValue) &&
					aValue is string { Length: 0 })
				{
					return Visit(CompareToEmptyString(b, InvertDirection(op)));
				}
			}

			return base.VisitExprExprPredicate(predicate);

			static ISqlPredicate CompareToEmptyString(ISqlExpression x, SqlPredicate.Operator op)
			{
				return op switch
				{
					SqlPredicate.Operator.NotGreater or
					SqlPredicate.Operator.LessOrEqual or
					SqlPredicate.Operator.Equal          => new SqlPredicate.IsNull(SqlNullabilityExpression.ApplyNullability(x, true), isNot: false),
					SqlPredicate.Operator.NotLess or
					SqlPredicate.Operator.Greater or
					SqlPredicate.Operator.NotEqual       => new SqlPredicate.IsNull(SqlNullabilityExpression.ApplyNullability(x, true), isNot: true),
					SqlPredicate.Operator.GreaterOrEqual => new SqlPredicate.ExprExpr(
						// Always true
						new SqlValue(1), SqlPredicate.Operator.Equal, new SqlValue(1), unknownAsValue: null),
					SqlPredicate.Operator.Less           => new SqlPredicate.ExprExpr(
						// Always false
						new SqlValue(1), SqlPredicate.Operator.Equal, new SqlValue(0), unknownAsValue: null),
					// Overlaps doesn't operate on strings
					_ => throw new InvalidOperationException(),
				};
			}

			static SqlPredicate.Operator InvertDirection(SqlPredicate.Operator op)
			{
				return op switch
				{
					SqlPredicate.Operator.NotEqual or
					SqlPredicate.Operator.Equal          => op,
					SqlPredicate.Operator.Greater        => SqlPredicate.Operator.Less,
					SqlPredicate.Operator.GreaterOrEqual => SqlPredicate.Operator.LessOrEqual,
					SqlPredicate.Operator.Less           => SqlPredicate.Operator.Greater,
					SqlPredicate.Operator.LessOrEqual    => SqlPredicate.Operator.GreaterOrEqual,
					SqlPredicate.Operator.NotGreater     => SqlPredicate.Operator.NotLess,
					SqlPredicate.Operator.NotLess        => SqlPredicate.Operator.NotGreater,
					// Overlaps doesn't operate on strings
					_ => throw new InvalidOperationException(),
				};
			}
		}
	}
}
