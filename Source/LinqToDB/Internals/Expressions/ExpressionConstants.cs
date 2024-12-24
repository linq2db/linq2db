using System.Linq.Expressions;

namespace LinqToDB.Internals.Expressions
{
	public static class ExpressionConstants
	{
		public static readonly ParameterExpression DataContextParam = Expression.Parameter(typeof(IDataContext), "dctx");
	}
}
