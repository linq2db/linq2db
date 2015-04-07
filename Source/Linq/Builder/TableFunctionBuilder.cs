using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	internal class TableFunctionBuilder : TableBuilder
	{
		public TableFunctionBuilder(QueryBuilder builder, Expression expression)
			: base(builder, expression)
		{
		}
	}
}