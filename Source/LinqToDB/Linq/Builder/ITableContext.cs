using System;

using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	interface ITableContext : ILoadWithContext
	{
		public Type     ObjectType { get; }
		public SqlTable SqlTable { get; }
	}
}
