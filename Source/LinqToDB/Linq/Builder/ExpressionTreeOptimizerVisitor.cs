using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;

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
					return trueExpr;

				if (constantExpr.Value is false)
					return falseExpr;
			}
			else if (node.Test.NodeType is ExpressionType.Equal or ExpressionType.NotEqual)
			{
				var binary  = (BinaryExpression)node.Test;
				var isEqual = node.Test.NodeType is ExpressionType.Equal;

				if (HandleConditionalOptimization(binary.Left, binary.Right, isEqual, out var result) ||
				    HandleConditionalOptimization(binary.Right, binary.Left, isEqual, out result))
				{
					test = result;
				}
			}

			if (ExpressionEqualityComparer.Instance.Equals(node.IfTrue, node.IfFalse))
			{
				return node.IfTrue;
			}

			if (trueExpr is ConstantExpression { Value: bool ifTrueBoolValue } && falseExpr is ConstantExpression { Value: bool ifFalseBoolValue })
			{
				if (ifTrueBoolValue)
					return node.Test;

				return Visit(Expression.Not(node.Test));
			}

			return node.Update(test, trueExpr, falseExpr);
		}

		protected override Expression VisitUnary(UnaryExpression node)
		{
			if (node is { NodeType: ExpressionType.Not, Operand.NodeType: ExpressionType.Not })
			{
				return Visit(((UnaryExpression)node.Operand).Operand);
			}

			return base.VisitUnary(node);
		}

		bool HandleConditionalOptimization(Expression left, Expression right, bool isEqual, [NotNullWhen(true)] out Expression? result)
		{
			if (left is ConstantExpression { Value: bool boolValue })
			{
				result = isEqual == boolValue ? right : Expression.Not(right);
				return true;
			} 
			
			if (left is ConditionalExpression conditional)
			{
				if (right.IsNullValue() || right is SqlPlaceholderExpression p && p.Sql.IsNullValue())
				{
					if (conditional.IfTrue is SqlGenericConstructorExpression)
					{
						result = !isEqual ? conditional.Test : Expression.Not(conditional.Test);
						return true;
					}

					if (conditional.IfFalse is SqlGenericConstructorExpression)
					{
						result = isEqual ? conditional.Test : Expression.Not(conditional.Test);
						return true;
					}
				}
			}

			result = null;
			return false;
		}

	}
}
