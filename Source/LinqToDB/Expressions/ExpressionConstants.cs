using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	public class ExpressionConstants
	{
		public static readonly ParameterExpression DataContextParam = Expression.Parameter(typeof(IDataContext), "dctx");
	}
}
