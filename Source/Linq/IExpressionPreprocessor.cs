using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	public interface IExpressionPreprocessor
	{
		Expression ProcessExpression(Expression expression);
	}
}