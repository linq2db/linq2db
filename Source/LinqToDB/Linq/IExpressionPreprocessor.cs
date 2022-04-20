using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	/// <summary>
	/// Allows to modify Expression Tree before it is being translated.
	/// </summary>
	/// <remarks>
	/// Use <see cref="Interceptors.IExpressionInterceptor"/> for new codebase.
	/// </remarks>
	public interface IExpressionPreprocessor
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
		Expression ProcessExpression(Expression expression);
	}
}
