using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;

namespace LinqToDB.Linq.Builder
{
	sealed class BinaryExpressionAggregatorVisitor : ExpressionVisitorBase
	{
		public static readonly ExpressionVisitor Instance = new BinaryExpressionAggregatorVisitor();

		protected override Expression VisitBinary(BinaryExpression node)
		{
			if (node.NodeType is ExpressionType.Or or ExpressionType.And or ExpressionType.OrElse or ExpressionType.AndAlso
				// has nesting
				&& (node.Right.NodeType == node.NodeType || node.Left.NodeType == node.NodeType))
			{
				var stack  = new Stack<Expression>();
				var leafs  = new List<Expression>();

				stack.Push(node.Right);
				stack.Push(node.Left);
				while (stack.Count > 0)
				{
					var item = stack.Pop();
					if (item.NodeType == node.NodeType)
					{
						var be = (BinaryExpression)item;
						stack.Push(be.Right);
						stack.Push(be.Left);
					}
					else
						leafs.Add(item);
				}

				if (leafs.Count > 3)
				{
					// having N items will lead to NxM recursive calls in expression visitors and
					// will result in stack overflow on relatively small numbers (~1000 items).
					// To fix it we will rebalance condition tree here which will result in
					// LOG2(N)*M recursive calls, or 10*M calls for 1000 items.
					//
					// E.g. we have condition A OR B OR C OR D OR E
					// as an expression tree it represented as tree with depth 5
					//   OR
					// A    OR
					//    B    OR
					//       C    OR
					//          D    E
					// for rebalanced tree it will have depth 4
					//                  OR
					//        OR
					//   OR        OR        OR
					// A    B    C    D    E    F
					// Not much on small numbers, but huge improvement on bigger numbers
					while (leafs.Count != 1)
					{
						leafs = CompactTree(leafs, node.NodeType);
					}

					return leafs[0];
				}
			}

			return base.VisitBinary(node);
		}

		List<Expression> CompactTree(List<Expression> leafs, ExpressionType nodeType)
		{
			var result = new List<Expression>();

			// traverse list from left to right to preserve calculation order
			for (var i = 0; i < leafs.Count; i += 2)
			{
				if (i + 1 == leafs.Count)
				{
					// last non-paired item
					var leaf = leafs[i];
					if (leaf is not BinaryExpression be || be.NodeType != nodeType)
						leaf = Visit(leaf);

					result.Add(leaf);
				}
				else
				{
					var left = leafs[i];
					var right = leafs[i + 1];

					if (left is not BinaryExpression beLeft || beLeft.NodeType != nodeType)
						left = Visit(left);
					if (right is not BinaryExpression beRight || beRight.NodeType != nodeType)
						right = Visit(right);

					result.Add(Expression.MakeBinary(nodeType, left, right));
				}
			}

			return result;
		}
	}
}
