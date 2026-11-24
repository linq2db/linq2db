using System.Linq.Expressions;

namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Allows to modify Expression Tree before it is being translated.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="ProcessExpression"/> is called when a query is executed, not only when it is translated: the
	/// <see cref="QueryExpressionArgs.ExpressionKind.Query"/> stage on every query cache lookup of a LINQ query. Compiled
	/// queries (<see cref="CompiledQuery"/>) do not call the <see cref="QueryExpressionArgs.ExpressionKind.Query"/> and
	/// <see cref="QueryExpressionArgs.ExpressionKind.ExposedQuery"/> stages.
	/// </para>
	/// <para>
	/// An interceptor that does not implement <see cref="Internal.Interceptors.IInterceptorCacheKey"/> is called again when
	/// a cached query is reused, and the query is checked against its result: the
	/// <see cref="QueryExpressionArgs.ExpressionKind.ExposedQuery"/> stage on a cache hit, and the
	/// <see cref="QueryExpressionArgs.ExpressionKind.QueryFilter"/> stage whenever a query that contains a query filter is
	/// reused, compiled queries included. An interceptor that implements it may have its translated queries reused without
	/// its rewrites being checked again.
	/// </para>
	/// </remarks>
	public interface IQueryExpressionInterceptor : IInterceptor
	{
		/// <summary>
		/// Executed when Expression is ready to translate. 
		/// </summary>
		/// <param name="expression">Expression for transforming.</param>
		/// <param name="args">Additional arguments.</param> 
		/// <returns>
		/// Transformed Expression
		/// </returns>
		public Expression ProcessExpression(Expression expression, QueryExpressionArgs args);
	}
}
