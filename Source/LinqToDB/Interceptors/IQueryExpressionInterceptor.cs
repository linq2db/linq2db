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
		public Expression ProcessExpression(Expression expression, QueryExpressionArgs args);
	}
}
