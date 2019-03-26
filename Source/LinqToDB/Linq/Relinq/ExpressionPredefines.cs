using System.Data;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Relinq
{
	public static class ExpressionPredefines
	{
		public static readonly ParameterExpression DataContextParam = Expression.Parameter(typeof(IDataContext), "ctx");
		public static readonly ParameterExpression DataReaderParam  = Expression.Parameter(typeof(IDataReader),  "rd");
		public static readonly ParameterExpression ExpressionParam  = Expression.Parameter(typeof(Expression),   "expr");
		public static readonly ParameterExpression ParametersParam  = Expression.Parameter(typeof(object[]),     "ps");
	}
}
