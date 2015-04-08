using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	interface IExpressionBuilder
	{
		IExpressionBuilder Prev { get; set; }
		IExpressionBuilder Next { get; set; }
		Type               Type { get;      }

		SqlBuilderBase GetSqlBuilder          ();
		Expression     BuildQueryExpression<T>();
		void           BuildQuery<T>          (QueryBuilder<T> builder);
	}
}
