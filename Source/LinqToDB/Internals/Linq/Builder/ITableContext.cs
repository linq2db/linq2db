using System;

using LinqToDB.Internals.SqlQuery;

namespace LinqToDB.Internals.Linq.Builder
{
	interface ITableContext : ILoadWithContext
	{
		public Type ObjectType { get; }
		public SqlTable SqlTable { get; }
	}
}
