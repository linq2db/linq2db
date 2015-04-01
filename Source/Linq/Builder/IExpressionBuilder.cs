using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	interface IExpressionBuilder
	{
		IExpressionBuilder Prev { get; set; }
		IExpressionBuilder Next { get; set; }

		SqlBuilderBase GetSqlBuilder();
		Expression     BuildQuery<T>();
		void           BuildQuery<T>(Query<T> query);
	}
}
