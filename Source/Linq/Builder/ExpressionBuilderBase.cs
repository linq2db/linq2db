using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	abstract class ExpressionBuilderBase : IExpressionBuilder
	{
		protected ExpressionBuilderBase(Expression expression)
		{
			Expression = expression;
		}

		public Expression Expression { get; private set; }

		public IExpressionBuilder Prev { get; set; }
		public IExpressionBuilder Next { get; set; }
		public Type               Type { get { return Expression.Type; } }

		public abstract SqlBuilderBase GetSqlBuilder();
		public abstract Expression     BuildQuery<T>();
		public abstract void           BuildQuery<T>(Query<T> query);
	}
}
