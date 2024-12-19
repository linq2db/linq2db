using System;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.SqlQuery;

	interface ITableContext : ILoadWithContext
	{
		public Type     ObjectType { get; }
		public SqlTable SqlTable { get; }
	}
}
