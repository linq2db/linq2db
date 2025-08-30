using System.Linq;

using LinqToDB.DataProvider.SqlServer;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	sealed class SqlServerSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, ISqlServerSpecificQueryable<TSource>
	{
		public SqlServerSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}
}
