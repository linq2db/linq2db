using System.Linq.Expressions;

namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Allows to modify Expression Tree before it is being translated.
	/// </summary>
	/// <remarks>
	/// An interceptor that also implements <see cref="Internal.Interceptors.IInterceptorCacheKey"/> has its translated
	/// queries reused without its rewrites being checked again.
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
