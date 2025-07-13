using System.Linq.Expressions;

namespace LinqToDB.Internal.Linq
{
	public interface IExpressionPreprocessor
	{
		Expression ProcessExpression(Expression expression);
	}
}
