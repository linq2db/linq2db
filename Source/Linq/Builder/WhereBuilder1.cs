using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	class WhereBuilder1 : ExpressionBuilderBase
	{
		public static QueryExpression Translate(QueryExpression qe, MethodCallExpression expression)
		{
			return qe.AddBuilder(new WhereBuilder1(expression));
		}

		WhereBuilder1(Expression expression) : base(expression)
		{
		}

		public override Expression BuildQueryExpression<T>(QueryBuilder<T> builder)
		{
			return Prev.BuildQueryExpression(builder);
		}

		public override void BuildQuery<T>(QueryBuilder<T> builder)
		{
			Prev.BuildQuery(builder);
		}

		public override SqlQuery BuildSql<T>(QueryBuilder<T> builder, SqlQuery sqlQuery)
		{
			throw new NotImplementedException();
		}
	}
}
