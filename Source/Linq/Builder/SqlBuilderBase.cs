using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	abstract class SqlBuilderBase
	{
		public abstract SelectQuery GetSelectQuery ();
		public abstract Expression  BuildExpression();
	}
}
