using System.Linq.Expressions;

using LinqToDB.Expressions;

namespace LinqToDB.Linq.Builder
{
	public class ExpressionTreeOptimizerVisitor : ExpressionVisitorBase
	{
		protected override Expression VisitConditional(ConditionalExpression node)
		{
			if (node.Test is ConstantExpression constantExpr)
			{
				if (constantExpr.Value is true)
					return Visit(node.IfTrue);

				if (constantExpr.Value is false)
					return Visit(node.IfFalse);
			}

			return base.VisitConditional(node);
		}
	}
}
