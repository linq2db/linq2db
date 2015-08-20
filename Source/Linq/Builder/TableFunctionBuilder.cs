using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	internal class TableFunctionBuilder : TableBuilderNew
	{
		public TableFunctionBuilder(QueryBuilder builder, Expression expression)
			: base(expression)
		{
		}
	}
}