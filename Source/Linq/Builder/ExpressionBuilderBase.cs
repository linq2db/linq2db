using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

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

		public abstract Expression     BuildQueryExpression<T>(QueryBuilder<T> builder);
		public abstract void           BuildQuery<T>          (QueryBuilder<T> builder);
		public abstract SqlQuery       BuildSql<T>            (QueryBuilder<T> builder, SqlQuery sqlQuery);
	}
}
