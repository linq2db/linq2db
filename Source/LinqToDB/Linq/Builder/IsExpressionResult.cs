using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	/// <summary>
	/// Contains result of <see cref="IBuildContext.IsExpression"/> function call.
	/// </summary>
	struct IsExpressionResult
	{
		/// <summary>
		/// Indicates when test or request was successfull.
		/// </summary>
		public readonly bool          Result;

		/// <summary>
		/// Stores found Context during <see cref="RequestFor.Table"/> request.
		/// </summary>
		public readonly IBuildContext Context;

		/// <summary>
		/// Stores found expression request.
		/// </summary>
		public readonly Expression    Expression;

		public IsExpressionResult(bool result, Expression expression = null)
		{
			Result     = result;
			Context    = null;
			Expression = expression;
		}

		public IsExpressionResult(bool result, IBuildContext context, Expression expression = null)
		{
			Result     = result;
			Context    = context;
			Expression = expression;
		}

		/// <summary>
		/// Static value for indicating successful test.
		/// </summary>
		public static IsExpressionResult True  = new IsExpressionResult(true);

		/// <summary>
		/// Static value for indicating unsuccessful test.
		/// </summary>
		public static IsExpressionResult False = new IsExpressionResult(false);
	}
}
