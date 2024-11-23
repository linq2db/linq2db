using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Common.Internal;
	using LinqToDB.Expressions;

	public static class EvaluationHelper
	{
		public static object? EvaluateExpression(Expression? expression, IDataContext dataContext, object?[]? parameterValues)
		{
			if (expression == null)
				return null;

			// Shortcut for constants
			if (expression.NodeType == ExpressionType.Constant)
				return ((ConstantExpression)expression).Value;

			var expr = expression.Transform(e =>
			{
				if (e is SqlQueryRootExpression root)
				{
					if (((IConfigurationID)root.MappingSchema).ConfigurationID ==
					    ((IConfigurationID)dataContext.MappingSchema).ConfigurationID)
					{
						return Expression.Constant(dataContext, e.Type);
					}
				}
				else if (e.NodeType == ExpressionType.ArrayIndex)
				{
					if (parameterValues != null)
					{
						var arrayIndexExpr = (BinaryExpression)e;

						var index = EvaluateExpression(arrayIndexExpr.Right, dataContext, parameterValues) as int?;
						if (index != null)
						{
							return Expression.Constant(parameterValues[index.Value]);
						}
					}
				}
				else if (e.NodeType == ExpressionType.Parameter)
				{
					if (e == ExpressionConstants.DataContextParam)
					{
						return Expression.Constant(dataContext, e.Type);
					}
				}

				return e;
			});

			return expr.EvaluateExpression();
		}
	}
}
