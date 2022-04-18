using System.Linq.Expressions;

namespace LinqToDB.Interceptors
{
	public interface IExpressionInterceptor : IInterceptor
	{
		public Expression ProcessExpression(Expression expression);
	}
}
