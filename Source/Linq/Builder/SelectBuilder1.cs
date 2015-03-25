using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	internal class SelectBuilder1 : ClauseBuilderBase
	{
		public SelectBuilder1(Expression expression)
			: base(expression)
		{
		}
	}
}