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
		Expression?                                                      FallbackExpression,
		bool                                                             IsSkipped = false
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

		/// <summary>
		/// Sentinel: the builder declines to produce SQL because an arg/item/value couldn't be
		/// translated AND the surrounding visitor is in Expression mode AND the aggregate config
		/// has <see cref="AggregateFunctionBuilder.ModeConfig.IsServerSideOnly"/> set to <see langword="false"/>.
		/// <see cref="AggregateFunctionBuilder.Build"/> returns <see langword="null"/> so the
		/// dispatch chain cascades to the surrounding partial-translation fallback.
		/// </summary>
		public static BuildAggregationFunctionResult Skipped() =>
			new BuildAggregationFunctionResult(null, null, null, null, null, true);
	};
}
