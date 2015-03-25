using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	class WhereBuilder1 : ClauseBuilderBase
	{
		public WhereBuilder1(Expression expression)
			: base(expression)
		{
		}
	}
}