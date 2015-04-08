using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	interface IExpressionBuilder
	{
		IExpressionBuilder Prev { get; set; }
		IExpressionBuilder Next { get; set; }
		Type               Type { get;      }

		Expression BuildQueryExpression<T>(QueryBuilder<T> builder);
		void       BuildQuery<T>          (QueryBuilder<T> builder);
		SqlQuery   BuildSql<T>            (QueryBuilder<T> builder, SqlQuery sqlQuery);
	}
}
