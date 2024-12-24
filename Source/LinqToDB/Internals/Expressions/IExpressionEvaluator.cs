using System.Linq.Expressions;

namespace LinqToDB.Internals.Expressions
{
	public interface IExpressionEvaluator
	{
		bool CanBeEvaluated(Expression expression);
		object? Evaluate(Expression expression);
	}
}
