using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	internal class TableFunctionBuilder : TableBuilder1
	{
		public TableFunctionBuilder(Query query, Expression expression)
			: base(query, expression)
		{
		}
	}
}