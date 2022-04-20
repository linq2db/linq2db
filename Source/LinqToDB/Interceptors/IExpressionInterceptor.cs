using System.Linq.Expressions;

namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Allows to modify Expression Tree before it is being translated.
	/// </summary>
	public interface IExpressionInterceptor : IInterceptor
	{
		/// <summary>
		/// Executed when Expression is ready to translate. 
		/// </summary>
		/// <param name="expression">Expression for transforming.</param>
		/// <returns>
		/// Transformed Expression
		/// </returns>
		/// <remarks>
		/// ExpressionMethodAttribute injection already done at this point.
		/// </remarks>
		public Expression ProcessExpression(Expression expression);
	}
}
