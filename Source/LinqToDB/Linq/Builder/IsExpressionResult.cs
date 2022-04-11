using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	/// <summary>
	/// Contains result of <see cref="IBuildContext.IsExpression"/> function call.
	/// </summary>
	public struct IsExpressionResult
	{
		/// <summary>
		/// Indicates when test or request was successful.
		/// </summary>
		public   readonly bool           Result;

		/// <summary>
		/// Stores found Context during <see cref="RequestFor.Table"/> request.
		/// </summary>
		internal readonly IBuildContext? Context;

		/// <summary>
		/// Stores found expression request.
		/// </summary>
		internal readonly Expression?    Expression;

		internal IsExpressionResult(bool result, Expression? expression = null)
		{
			Result     = result;
			Context    = null;
			Expression = expression;
		}

		internal IsExpressionResult(bool result, IBuildContext? context, Expression? expression = null)
		{
			Result     = result;
			Context    = context;
			Expression = expression;
		}

		/// <summary>
		/// Static value for indicating successful test.
		/// </summary>
		internal static readonly IsExpressionResult True  = new (true);

		/// <summary>
		/// Static value for indicating unsuccessful test.
		/// </summary>
		internal static readonly IsExpressionResult False = new (false);

		/// <summary>
		/// Returns cached instance of <see cref="IsExpressionResult"/> without expression and context.
		/// </summary>
		internal static IsExpressionResult GetResult(bool result) => result ? True : False;
	}
}
