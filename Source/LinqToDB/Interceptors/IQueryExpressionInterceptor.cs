using System.Linq.Expressions;
namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Allows to modify Expression Tree before it is being translated.
	/// </summary>
	/// <remarks>
	/// This is a pre-translation extensibility point.
	/// Use it when a query shape must be rewritten at the LINQ expression-tree level before SQL translation begins.
	/// For lower-level SQL-member translation, see <see cref="Linq.Translation.IMemberTranslator"/>.
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