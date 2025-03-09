using System;

using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	interface ITableContext : ILoadWithContext
	{
		public Type     ObjectType { get; }
		public SqlTable SqlTable { get; }
	}
}
