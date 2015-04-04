using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	interface IExpressionBuilder
	{
		IExpressionBuilder Prev { get; set; }
		IExpressionBuilder Next { get; set; }
		Type               Type { get;      }

		SqlBuilderBase GetSqlBuilder();
		Expression     BuildQuery<T>();
		void           BuildQuery<T>(Query<T> query);
	}
}
