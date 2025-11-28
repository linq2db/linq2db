using System;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Linq.Translation
{
	public record BuildAggregationFunctionResult(
		ISqlExpression?               SqlExpression,
		Func<Expression, Expression>? Validator,
		SqlErrorExpression?           ErrorExpression,
		Expression?                   FallbackExpression
	)
	{
		public static BuildAggregationFunctionResult Error(SqlErrorExpression errorExpression) =>
			new BuildAggregationFunctionResult(null, null, errorExpression, null);

		public static BuildAggregationFunctionResult FromSqlExpression(ISqlExpression sqlExpression, Func<Expression, Expression>? validator = null) =>
			new BuildAggregationFunctionResult(sqlExpression, validator, null, null);

		public static BuildAggregationFunctionResult FromFallback(Expression? fallbackExpression) =>
			new BuildAggregationFunctionResult(null, null, null, fallbackExpression);
	};
}
