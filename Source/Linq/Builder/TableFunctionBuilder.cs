using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	internal class TableFunctionBuilder : TableBuilder1
	{
		public TableFunctionBuilder(Expression expression)
			: base(expression)
		{
		}
	}
}