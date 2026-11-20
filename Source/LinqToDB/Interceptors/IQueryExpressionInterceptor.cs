using System.Linq.Expressions;

namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Allows to modify Expression Tree before it is being translated.
	/// </summary>
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
		/// <remarks>
		/// Called on every execution of a LINQ query in a context that registers the interceptor, not only when a query is
		/// translated: the query cache key does not include interceptor instances, so the result is what decides whether a
		/// cached query can be reused. Compiled queries (<see cref="CompiledQuery"/>) do not call the
		/// <see cref="QueryExpressionArgs.ExpressionKind.Query"/> and <see cref="QueryExpressionArgs.ExpressionKind.ExposedQuery"/>
		/// stages; the <see cref="QueryExpressionArgs.ExpressionKind.QueryFilter"/> and
		/// <see cref="QueryExpressionArgs.ExpressionKind.AssociationExpression"/> stages are called whenever a cached query
		/// that contains them is reused. Keep it cheap, and return <paramref name="expression"/> itself when there is nothing
		/// to change: a new tree equal to the input gives the same result, but costs another cache lookup on every
		/// execution, and an <see cref="System.Linq.IQueryable"/> executed again does not keep its translated query.
		/// </remarks>
		public Expression ProcessExpression(Expression expression, QueryExpressionArgs args);
	}
}
