using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	internal sealed class ExpressionConvertHelper 
	{
		public static Expression? ConvertUnary(MappingSchema mappingSchema, UnaryExpression node)
		{
			var l = LinqToDB.Linq.Expressions.ConvertUnary(mappingSchema, node);
			if (l != null)
			{
				var body = l.Body.Unwrap();
				var expr = body.Transform((l, node), static (context, wpi) =>
				{
					if (wpi.NodeType == ExpressionType.Parameter)
					{
						if (context.l.Parameters[0] == wpi)
							return context.node.Operand;
					}

					return wpi;
				});

				if (expr.Type != node.Type)
					expr = new ChangeTypeExpression(expr, node.Type);

				return expr;
			}

			return null;
		}

		public static Expression? ConvertBinary(MappingSchema mappingSchema, BinaryExpression node)
		{
			var l = LinqToDB.Linq.Expressions.ConvertBinary(mappingSchema, node);
			if (l != null)
			{
				var body = l.Body.Unwrap();
				var expr = body.Transform((l, node), static (context, wpi) =>
				{
					if (wpi.NodeType == ExpressionType.Parameter)
					{
						if (context.l.Parameters[0] == wpi)
							return context.node.Left;
						if (context.l.Parameters[1] == wpi)
							return context.node.Right;
					}

					return wpi;
				});

				if (expr.Type != node.Type)
					expr = new ChangeTypeExpression(expr, node.Type);

				return expr;
			}

			return null;
		}
	}
}
