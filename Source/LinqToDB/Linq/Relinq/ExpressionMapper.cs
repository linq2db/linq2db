using System.Collections.Generic;
using System.Linq.Expressions;
using LinqToDB.Expressions;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;

namespace LinqToDB.Linq.Relinq
{
	public class ExpressionMapper
	{
		readonly Dictionary<Expression, Expression> _mapping = new Dictionary<Expression, Expression>(new ExpressionEqualityComparer());

		public void RegisterMapping(Expression fromExpression, Expression toExpression)
		{
			_mapping.Add(fromExpression, toExpression);
		}

		public Expression ResolveExpression(Expression fromExpression)
		{
			var result = fromExpression;
			if (_mapping.Count > 0)
			{
				result = fromExpression.Transform(e =>
				{
					if (_mapping.TryGetValue(e, out var value))
						e = ResolveExpression(value);

					return e;
				});
			}
			return result;
		}

		public void Resolve(IClause clause)
		{
			if (_mapping.Count > 0)
				clause.TransformExpressions(ResolveExpression);
		}

		public void Resolve(ResultOperatorBase resultOperator)
		{
			if (_mapping.Count > 0)
				resultOperator.TransformExpressions(ResolveExpression);
		}

		public void Resolve(QueryModel queryModel)
		{
			if (_mapping.Count > 0)
				queryModel.TransformExpressions(ResolveExpression);
		}

		public bool HasRegistration(Expression expression, out Expression transformedTo)
		{
			return _mapping.TryGetValue(expression, out transformedTo);
		}

		public int Count => _mapping.Count;
	}
}
