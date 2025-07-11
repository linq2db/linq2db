using System.Linq.Expressions;

namespace LinqToDB.Internal.Linq
{
	public interface IQueryExpressions
	{
		Expression MainExpression { get; }
		Expression GetQueryExpression(int expressionId);
	}
}
