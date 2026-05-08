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
		Expression?                   FallbackExpression,
		bool                          IsSkipped = false
	)
	{
		public static BuildAggregationFunctionResult Error(SqlErrorExpression errorExpression) =>
			new BuildAggregationFunctionResult(null, null, errorExpression, null);

		public static BuildAggregationFunctionResult FromSqlExpression(ISqlExpression sqlExpression, Func<Expression, Expression>? validator = null) =>
			new BuildAggregationFunctionResult(sqlExpression, validator, null, null);

		public static BuildAggregationFunctionResult FromFallback(Expression? fallbackExpression) =>
			new BuildAggregationFunctionResult(null, null, null, fallbackExpression);

		/// <summary>
		/// Sentinel: the builder declines to produce SQL because an arg/item/value couldn't be
		/// translated AND the surrounding visitor is in Expression mode AND the aggregate config
		/// has <see cref="AggregateFunctionBuilder.ModeConfig.IsServerSideOnly"/> set to <see langword="false"/>.
		/// <see cref="AggregateFunctionBuilder.Build"/> returns <see langword="null"/> so the
		/// dispatch chain cascades to the surrounding partial-translation fallback.
		/// </summary>
		public static BuildAggregationFunctionResult Skipped() =>
			new BuildAggregationFunctionResult(null, null, null, null, true);
	};
}
