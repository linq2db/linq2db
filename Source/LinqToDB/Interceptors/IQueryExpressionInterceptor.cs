using System.Linq.Expressions;
namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Intercepts LINQ expressions before they are translated.
	/// </summary>
	/// <remarks>
	/// This is a pre-translation extensibility point.
	/// Use it when a query shape must be rewritten at the LINQ expression-tree level before SQL translation begins.
	/// The method can be called for exposed query expressions, full query expressions, filters, or associations;
	/// use <see cref="QueryExpressionArgs.ExpressionKind"/> to identify the call site.
	/// For lower-level SQL-member translation, see <see cref="Linq.Translation.IMemberTranslator"/>.
	/// </remarks>
	public interface IQueryExpressionInterceptor : IInterceptor
	{
		/// <summary>
		/// Called when an expression is ready for translation.
		/// </summary>
		/// <param name="expression">Expression to inspect or replace.</param>
		/// <param name="args">Additional translation context.</param>
		/// <returns>Expression to continue translation with.</returns>
		public Expression ProcessExpression(Expression expression, QueryExpressionArgs args);
	}
}
