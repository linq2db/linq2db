using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	abstract class ClauseBuilderBase
	{
		protected ClauseBuilderBase(Expression expression)
		{
			Expression = expression;
		}

		public Type            Type       { get { return Expression.Type; } }
		public Expression      Expression;
		public QueryExpression Query;
	}
}