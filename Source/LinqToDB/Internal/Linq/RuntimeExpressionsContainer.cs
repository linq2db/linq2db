using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Internal.Linq
{
	public sealed class RuntimeExpressionsContainer(Expression mainExpression) : IQueryExpressions
	{
		public Expression MainExpression { get; } = mainExpression;

		public Expression GetQueryExpression(int expressionId)
		{
			if (expressionId == 0)
				return MainExpression;

			if (_dynamicExpressions == null)
				throw new InvalidOperationException("_dynamicExpressions is null");

			var registered = _dynamicExpressions.FirstOrDefault(a => a.expressionId == expressionId);

			if (registered.expression == null)
				throw new InvalidOperationException($"Dynamic accessor with id {expressionId.ToString(CultureInfo.InvariantCulture)} not found");

			return registered.expression;
		}

		List<(int expressionId, Expression expression)>? _dynamicExpressions;

		public void AddExpression(int expressionId, Expression expression)
		{
			_dynamicExpressions ??= new List<(int expressionId, Expression)>();
			_dynamicExpressions.Add((expressionId, expression));
		}
	}
}
