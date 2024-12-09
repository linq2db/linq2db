using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	public interface IQueryExpressions
	{
		Expression MainExpression { get; }
		Expression GetQueryExpression(int expressionId);
	}
}
