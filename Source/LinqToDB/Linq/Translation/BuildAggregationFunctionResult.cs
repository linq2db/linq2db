using System;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Linq.Translation
{
	public record BuildAggregationFunctionResult(
		ISqlExpression?                                                  SqlExpression,
		Func<Expression, Expression>?                                    MaterializationCheck,
		Func<SqlPlaceholderExpression, SqlPlaceholderExpression>?        SqlRewriter,
		SqlErrorExpression?                                              ErrorExpression,
		Expression?                                                      FallbackExpression
	)
	{
		public static BuildAggregationFunctionResult Error(SqlErrorExpression errorExpression) =>
			new BuildAggregationFunctionResult(null, null, null, errorExpression, null);

		public static BuildAggregationFunctionResult FromSqlExpression(
			ISqlExpression                                                  sqlExpression,
			Func<Expression, Expression>?                                   materializationCheck = null,
			Func<SqlPlaceholderExpression, SqlPlaceholderExpression>?       sqlRewriter          = null) =>
			new BuildAggregationFunctionResult(sqlExpression, materializationCheck, sqlRewriter, null, null);

		public static BuildAggregationFunctionResult FromFallback(Expression? fallbackExpression) =>
			new BuildAggregationFunctionResult(null, null, null, null, fallbackExpression);
	};
}
