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

		internal override SqlBuilderBase GetSqlBuilder()
		{
			throw new NotImplementedException();
		}

		public override Expression BuildQuery()
		{
			throw new NotImplementedException();
		}
	}
}