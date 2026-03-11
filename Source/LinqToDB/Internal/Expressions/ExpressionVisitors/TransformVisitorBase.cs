using System.Linq.Expressions;

namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	abstract class TransformVisitorBase : TransformVisitorsBase
	{
		protected override Expression VisitUnary(UnaryExpression node)
		{
			var o = Visit(node.Operand);
			return o != node.Operand ? Expression.MakeUnary(node.NodeType, o, node.Type, node.Method) : node;
		}
	}
}
