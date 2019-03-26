using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using LinqToDB.Expressions;
using LinqToDB.SqlQuery;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;

namespace LinqToDB.Linq.Relinq
{
	public class SqlMapper
	{
		private readonly Dictionary<Expression, ISqlExpression> _mapping = new Dictionary<Expression, ISqlExpression>(new ExpressionEqualityComparer());
		private readonly Dictionary<IQuerySource, ISqlTableSource> _sourceMapping = new Dictionary<IQuerySource, ISqlTableSource>();
		private readonly Dictionary<Expression, ISqlTableSource> _expressionMapping = new Dictionary<Expression, ISqlTableSource>(new ExpressionEqualityComparer());

		public ISqlExpression ResolveExpression<T>(T expression, Func<T, ISqlExpression> generatorFunc)
			where T: Expression
		{
			if (!_mapping.TryGetValue(expression, out var value))
			{
				value = generatorFunc(expression);
				_mapping.Add(expression, value);
			}

			return value;
		}

		public void RegisterSource(IQuerySource querySource, Expression queryExpression, ISqlTableSource tableSource)
		{
			_sourceMapping.Add(querySource, tableSource);
			//if (queryExpression != null)
			//	_expressionMapping.Add(queryExpression, tableSource);
		}

		public ISqlTableSource GetTableSource(Expression querySource)
		{
			if (_expressionMapping.TryGetValue(querySource, out var value))
				return value;
			if (querySource is QuerySourceReferenceExpression referenceExpression
			    && _sourceMapping.TryGetValue(referenceExpression.ReferencedQuerySource, out value))
				return value;

			throw new LinqToDBException($"Source for reference '{querySource}' not registered");
		}

	}
}
