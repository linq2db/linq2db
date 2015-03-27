using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	internal class SelectBuilder1 : ExpressionBuilderBase
	{
		public SelectBuilder1(Expression expression)
			: base(expression)
		{
		}

		public override SqlBuilderBase GetSqlBuilder()
		{
			var sql = Prev.GetSqlBuilder();
			return sql;
		}

		protected override void Init()
		{
		}
	}
}