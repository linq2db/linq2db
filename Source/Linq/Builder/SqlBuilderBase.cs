using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	abstract class SqlBuilderBase
	{
		public SelectQuery SelectQuery;

		public abstract Expression BuildExpression();
	}
}
