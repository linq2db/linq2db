using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	class WhereBuilder1 : ExpressionBuilderBase
	{
		public WhereBuilder1(Expression expression)
			: base(expression)
		{
		}

		public override SqlBuilderBase GetSqlBuilder()
		{
			var sql = Prev.GetSqlBuilder();
			return sql;
		}

		public override Expression BuildQuery()
		{
			return Prev.BuildQuery();
		}
	}
}
