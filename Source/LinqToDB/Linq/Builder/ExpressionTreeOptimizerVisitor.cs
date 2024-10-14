using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.Expressions;

namespace LinqToDB.Linq.Builder
{
	public class ExpressionTreeOptimizerVisitor : ExpressionVisitorBase
	{
		protected override Expression VisitConditional(ConditionalExpression node)
		{
			var test      = Visit(node.Test);
			var trueExpr  = Visit(node.IfTrue);
			var falseExpr = Visit(node.IfFalse);

			if (test is ConstantExpression constantExpr)
			{
				if (constantExpr.Value is true)
					return Visit(node.IfTrue);

				if (constantExpr.Value is false)
					return Visit(node.IfFalse);
			}
			else if (node.Test.NodeType is ExpressionType.Equal or ExpressionType.NotEqual)
			{
				var binary  = (BinaryExpression)node.Test;
				var isEqual = node.Test.NodeType is ExpressionType.Equal;

				if (binary.Left.Type == typeof(bool) && binary.Right.Type == typeof(bool) &&
					(HandleBoolean(binary.Left, binary.Right, isEqual, out var result) || HandleBoolean(binary.Right, binary.Left, isEqual, out result)))
				{
					test = result;
				}
			}

			return node.Update(test, trueExpr, falseExpr);
		}

		protected override Expression VisitUnary(UnaryExpression node)
		{
			if (node is { NodeType: ExpressionType.Not, Operand.NodeType: ExpressionType.Not })
			{
				return Visit(node.Operand);
			}

			return base.VisitUnary(node);
		}

		bool HandleBoolean(Expression left, Expression right, bool isEqual, [NotNullWhen(true)] out Expression? result)
		{
			if (left is ConstantExpression { Value: bool boolValue })
			{
				result = isEqual == boolValue ? right : Expression.Not(right);
				return true;
			}

			result = null;
			return false;
		}
	}
}
