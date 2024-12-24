using System.Linq.Expressions;

namespace LinqToDB.Internals.Linq
{
	public interface IExpressionPreprocessor
	{
		Expression ProcessExpression(Expression expression);
	}
}
