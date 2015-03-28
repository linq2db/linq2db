using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	abstract class ExpressionBuilderBase
	{
		protected ExpressionBuilderBase(Expression expression)
		{
			Expression = expression;
		}

		public Type            Type { get { return Expression.Type; } }
		public Expression      Expression;

		public ExpressionBuilderBase Prev;
		public ExpressionBuilderBase Next;
		public QueryExpression       Query;

		internal  abstract SqlBuilderBase GetSqlBuilder();
		public    abstract Expression     BuildQuery   ();
	}
}
