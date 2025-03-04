using System.Linq.Expressions;

namespace LinqToDB.Internal.Expressions
{
	public static class ExpressionConstants
	{
		public static readonly ParameterExpression DataContextParam = Expression.Parameter(typeof(IDataContext), "dctx");
	}
}
