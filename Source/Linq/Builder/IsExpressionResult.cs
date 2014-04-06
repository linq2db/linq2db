using System;

namespace LinqToDB.Linq.Builder
{
	struct IsExpressionResult
	{
		public readonly bool          Result;
		public readonly IBuildContext Context;

		public IsExpressionResult(bool result)
		{
			Result  = result;
			Context = null;
		}

		public IsExpressionResult(bool result, IBuildContext context)
		{
			Result  = result;
			Context = context;
		}

		public static IsExpressionResult True  = new IsExpressionResult(true);
		public static IsExpressionResult False = new IsExpressionResult(false);
	}
}
