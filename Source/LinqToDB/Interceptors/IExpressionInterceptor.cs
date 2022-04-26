using System.Linq.Expressions;
using LinqToDB.Mapping;

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
		/// <param name="mappingSchema">Current MappingSchema.</param>
		/// <param name="expression">Expression for transforming.</param>
		/// <returns>
		/// Transformed Expression
		/// </returns>
		public Expression ProcessExpression(MappingSchema mappingSchema, Expression expression);
	}
}
