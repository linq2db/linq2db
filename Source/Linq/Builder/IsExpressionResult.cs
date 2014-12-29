using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	struct IsExpressionResult
	{
		public readonly bool          Result;
		public readonly IBuildContext Context;
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

		public static IsExpressionResult True  = new IsExpressionResult(true);
		public static IsExpressionResult False = new IsExpressionResult(false);
	}
}
