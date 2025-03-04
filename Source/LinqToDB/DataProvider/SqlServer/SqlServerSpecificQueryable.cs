using System.Linq;

using LinqToDB.Internal.DataProvider;

namespace LinqToDB.DataProvider.SqlServer
{
	sealed class SqlServerSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, ISqlServerSpecificQueryable<TSource>
	{
		public SqlServerSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}
}
